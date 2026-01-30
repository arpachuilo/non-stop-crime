#nullable enable
using Godot;
using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steam;

/// <summary>
/// Implements Godot's MultiplayerPeerExtension using Steam's P2P networking via relay servers.
/// Provides host/client architecture with automatic peer ID management and message routing.
/// </summary>
/// <remarks>
/// ARCHITECTURE:
/// - Server (Mode.Server): Uses SteamSocketManager to accept connections, SteamConnectionManager to connect to self
/// - Client (Mode.Client): Uses only SteamConnectionManager to connect to host
///
/// PEER ID SYSTEM:
/// - 0 = broadcast to all peers
/// - 1 = always the host/server
/// - >1 = client peer IDs, generated via GenerateUniqueId()
/// - -1 = unassigned/disconnected
///
/// CONNECTION HANDSHAKE:
/// 1. Low-level Steam connection established (peer has -1 ID)
/// 2. Client sends SetupPeerPayload with their unique ID
/// 3. Server receives payload, assigns peer ID, emits PeerConnected
/// 4. Server sends back their own SetupPeerPayload (always ID 1)
/// 5. Handshake complete, messages can flow
///
/// IMPORTANT: Connections exist in _connectionsBySteamId before they have valid peer IDs.
/// They're only added to _peerIdToConnection after the handshake completes.
///
/// RELAY SERVERS: All connections go through Steam's relay servers (ConnectRelay/CreateRelaySocket),
/// not direct P2P. This provides better NAT traversal but adds latency.
/// </remarks>
public partial class SteamMultiplayerPeer : MultiplayerPeerExtension {
  /// <summary>
  /// Operating mode of this peer instance.
  /// </summary>
  public enum Mode {
    None,      // Not initialized
    Server,    // Hosting a game (peer ID 1)
    Client     // Connected to a host (peer ID > 1)
  }

  /// <summary>
  /// Maximum packet size enforced by Steam. Arbitrary value from Steam's documentation.
  /// </summary>
  private const int _maxPacketSize = 524288; // abritrary value from steam. Fun.

  /// <summary>
  /// Manages outgoing connections (used by clients).
  /// </summary>
  private SteamConnectionManager? _steamConnectionManager;

  /// <summary>
  /// Manages incoming connections (used by server/host).
  /// </summary>
  private SteamSocketManager? _steamSocketManager;

  /// <summary>
  /// Maps Godot peer IDs to Steam connections. Only contains fully handshaked connections.
  /// </summary>
  private readonly Dictionary<int, SteamConnection> _peerIdToConnection = new Dictionary<int, SteamConnection>();

  /// <summary>
  /// Maps Steam IDs to connections. Contains ALL connections, even those with unassigned peer IDs (-1).
  /// </summary>
  private readonly Dictionary<ulong, SteamConnection> _connectionsBySteamId = new Dictionary<ulong, SteamConnection>();

  /// <summary>
  /// Public accessor for connections by peer ID.
  /// </summary>
  public Dictionary<int, SteamConnection> ConnectionsByPeerId => _peerIdToConnection;

  /// <summary>
  /// Target peer for the next sent packet. 0 = broadcast to all, >0 = specific peer.
  /// Set via _SetTargetPeer, used by _PutPacketScript.
  /// </summary>
  private int _targetPeer = -1;

  /// <summary>
  /// The local peer's unique ID. 0 = not connected, 1 = host/server, >1 = client.
  /// </summary>
  private int _uniqueId = 0;

  /// <summary>
  /// Current operating mode of this peer.
  /// </summary>
  private Mode _mode;

  /// <summary>
  /// Whether this peer is active (not in None mode).
  /// </summary>
  private bool _isActive => _mode != Mode.None;

  /// <summary>
  /// Current connection status for Godot's multiplayer system.
  /// </summary>
  private ConnectionStatus _connectionStatus = ConnectionStatus.Disconnected;

  /// <summary>
  /// Transfer mode for the next sent packet (Reliable, Unreliable, etc.)
  /// Set via _SetTransferMode, used by _PutPacketScript.
  /// </summary>
  /// <remarks>
  /// NUANCE: Currently ignored - all packets are sent as Reliable regardless of this setting.
  /// See SteamConnection.RawSend.
  /// </remarks>
  private TransferModeEnum _transferMode = TransferModeEnum.Reliable;

  /// <summary>
  /// Queue of received packets waiting to be consumed by Godot.
  /// Populated during _Poll, consumed via _GetPacketScript.
  /// </summary>
  private readonly Queue<SteamPacketPeer> _incomingPackets = new();

  /// <summary>
  /// The local player's Steam ID.
  /// </summary>
  private SteamId _steamId;

  /// <summary>
  /// Transfer channel for the next sent packet. Currently always 0 (only one channel implemented).
  /// </summary>
  private int _transferChannel = 0;

  /// <summary>
  /// Whether to reject new incoming connections. Set via _SetRefuseNewConnections.
  /// </summary>
  private bool _refuseNewConnections = false;

  /// <summary>
  /// Initializes this peer as a host/server.
  /// </summary>
  /// <param name="playerId">The host's Steam ID</param>
  /// <returns>Error.Ok on success, Error.AlreadyInUse if already active</returns>
  /// <remarks>
  /// SETUP:
  /// - Creates a relay socket to accept incoming connections (SteamSocketManager)
  /// - Creates a connection to self via relay (SteamConnectionManager)
  /// 
  /// SELF-CONNECTION: The host connects to itself through Steam's relay. This connection
  /// is filtered out in the event handlers (checking if SteamId != _steamId). This seems
  /// like an architectural quirk - the self-connection is created but intentionally ignored.
  /// 
  /// PEER DISCONNECTION: When a peer disconnects, emits PeerDisconnected signal and removes
  /// them from both dictionaries. The removal order matters: peer ID first, then Steam ID,
  /// since we need the connection's PeerId before removing it from _connectionsBySteamId.
  /// </remarks>
  public Error CreateHost(SteamId playerId) {
    _steamId = playerId;
    if (_isActive) {
      return Error.AlreadyInUse;
    }
    _steamSocketManager = SteamNetworkingSockets.CreateRelaySocket<SteamSocketManager>();
    _steamConnectionManager = SteamNetworkingSockets.ConnectRelay<SteamConnectionManager>(playerId);

    _steamSocketManager.OnConnectionEstablished += (c) => {
      if (c.Item2.Identity.SteamId != _steamId) {
        AddConnection(c.Item2.Identity.SteamId, c.Item1);
      }
    };

    _steamSocketManager.OnConnectionLost += (c) => {
      if (c.Item2.Identity.SteamId != _steamId) {
        EmitSignal(MultiplayerPeer.SignalName.PeerDisconnected, _connectionsBySteamId[c.Item2.Identity.SteamId].PeerId);
        _peerIdToConnection.Remove(_connectionsBySteamId[c.Item2.Identity.SteamId].PeerId);
        _connectionsBySteamId.Remove(c.Item2.Identity.SteamId);
      }
    };

    _uniqueId = 1;
    _mode = Mode.Server;
    _connectionStatus = ConnectionStatus.Connected;
    return Error.Ok;
  }

  /// <summary>
  /// Initializes this peer as a client connecting to a host.
  /// </summary>
  /// <param name="playerId">The client's own Steam ID</param>
  /// <param name="hostId">The host's Steam ID to connect to</param>
  /// <returns>Error.Ok on success, Error.AlreadyInUse if already active</returns>
  /// <remarks>
  /// UNIQUE ID: Client generates their peer ID immediately using GenerateUniqueId().
  /// This ID is then sent to the host during handshake via SendPeer.
  /// 
  /// CONNECTION FLOW:
  /// 1. ConnectRelay initiates connection
  /// 2. OnConnectionEstablished fires when Steam connection succeeds
  /// 3. Status changes to Connected
  /// 4. Client sends SetupPeerPayload with their ID to host
  /// 5. Host processes and responds
  /// 6. Client receives host's ID, connection fully established
  /// 
  /// SELF-FILTERING: Like CreateHost, filters out self-connections (though clients
  /// shouldn't connect to themselves). This is defensive programming.
  /// </remarks>
  public Error CreateClient(SteamId playerId, SteamId hostId) {
    _steamId = playerId;
    if (_isActive) {
      return Error.AlreadyInUse;
    }

    _uniqueId = (int)GenerateUniqueId();

    _steamConnectionManager = SteamNetworkingSockets.ConnectRelay<SteamConnectionManager>(hostId);

    _steamConnectionManager.OnConnectionEstablished += (connection) => {
      if (connection.Identity.SteamId != _steamId) {
        AddConnection(connection.Identity.SteamId, _steamConnectionManager.Connection);
        _connectionStatus = ConnectionStatus.Connected;
        _connectionsBySteamId[connection.Identity.SteamId].SendPeer(_uniqueId);
      }
    };
    _steamConnectionManager.OnConnectionLost += (connection) => {
      if (connection.Identity.SteamId != _steamId) {
        EmitSignal(MultiplayerPeer.SignalName.PeerDisconnected, _connectionsBySteamId[connection.Identity.SteamId].PeerId);
        _peerIdToConnection.Remove(_connectionsBySteamId[connection.Identity.SteamId].PeerId);
        _connectionsBySteamId.Remove(connection.Identity.SteamId);
      }
    };

    _mode = Mode.Client;
    _connectionStatus = ConnectionStatus.Connecting;
    return Error.Ok;
  }

  /// <summary>
  /// Godot override: Closes all connections and resets peer state.
  /// </summary>
  /// <remarks>
  /// CLEANUP ORDER:
  /// 1. Closes all socket manager connections (incoming)
  /// 2. Closes connection manager if server (the self-connection)
  /// 3. Closes socket manager itself
  /// 4. Clears both dictionaries
  /// 5. Resets mode, ID, and status
  /// 
  /// NULL-SAFE: Uses null-conditional operators and null-coalescing to handle cases where
  /// managers might not be initialized (e.g., if Close is called without proper initialization).
  /// </remarks>
  public override void _Close() {
    if (!_isActive || _connectionStatus != ConnectionStatus.Connected) { return; }

    foreach (var connection in _steamSocketManager?.Connected ?? Enumerable.Empty<Connection>()) {
      connection.Close();
    }

    if (_IsServer()) {
      _steamConnectionManager?.Close();
    }

    _steamSocketManager?.Close();
    _peerIdToConnection.Clear();
    _connectionsBySteamId.Clear();
    _mode = Mode.None;
    _uniqueId = 0;
    _connectionStatus = ConnectionStatus.Disconnected;
  }

  /// <summary>
  /// Godot override: Disconnects a specific peer.
  /// </summary>
  /// <param name="pPeer">The peer ID to disconnect</param>
  /// <param name="pForce">If true and we're a client disconnecting from host, closes entire connection</param>
  /// <remarks>
  /// CLEANUP FLOW:
  /// 1. Find connection by peer ID
  /// 2. Close the Steam connection
  /// 3. Flush any pending data
  /// 4. Remove from both dictionaries
  /// 5. If force=true and we're a client, also close our entire connection
  /// 
  /// NUANCE: Flushes peer 0 connection after disconnect if in Client/Server mode.
  /// GetConnectionFromPeer(0) should return null (0 isn't a valid peer ID), so this
  /// code does nothing. Possible bug or leftover from different peer ID scheme?
  /// 
  /// FORCE DISCONNECT: If pForce=true and client mode, clears ALL connections and closes.
  /// This is used when the client forcibly disconnects from host (likely a kick).
  /// </remarks>
  public override void _DisconnectPeer(int pPeer, bool pForce) {
    SteamConnection? connection = GetConnectionFromPeer(pPeer);

    if (connection == null) { return; }

    bool res = connection.Connection.Close();

    if (!res) {
      return;
    }

    connection.Connection.Flush();
    _connectionsBySteamId.Remove(connection.SteamId);
    _peerIdToConnection.Remove(pPeer);
    if (_mode == Mode.Client || _mode == Mode.Server) {
      GetConnectionFromPeer(0)?.Connection.Flush();
    }
    if (pForce && _mode == Mode.Client) {
      _connectionsBySteamId.Clear();
      Close();
    }
  }

  /// <summary>
  /// Godot override: Returns the number of packets waiting to be read.
  /// </summary>
  public override int _GetAvailablePacketCount() {
    return _incomingPackets.Count;
  }

  /// <summary>
  /// Godot override: Returns the current connection status.
  /// </summary>
  public override ConnectionStatus _GetConnectionStatus() {
    return _connectionStatus;
  }

  /// <summary>
  /// Godot override: Returns the maximum packet size supported by Steam.
  /// </summary>
  public override int _GetMaxPacketSize() {
    return _maxPacketSize;
  }

  /// <summary>
  /// Godot override: Returns the channel the current packet was received on.
  /// </summary>
  /// <remarks>
  /// TODO: Multiple channels not implemented yet. Always returns 0.
  /// </remarks>
  public override int _GetPacketChannel() {
    return 0; // todo - implement more channels
  }

  /// <summary>
  /// Godot override: Returns the transfer mode of the next packet to read.
  /// </summary>
  /// <remarks>
  /// DEFAULTS: Returns Reliable if no packets are queued. This matches the default
  /// since all messages are currently sent as Reliable anyway.
  /// </remarks>
  public override TransferModeEnum _GetPacketMode() {
    return _incomingPackets.FirstOrDefault()?.TransferMode ?? TransferModeEnum.Reliable;
  }

  /// <summary>
  /// Godot override: Returns the peer ID of the sender of the next packet to read.
  /// </summary>
  /// <remarks>
  /// POTENTIAL CRASH: If the packet's sender Steam ID is not in _connectionsBySteamId,
  /// this will throw a KeyNotFoundException. This could happen if a packet is queued
  /// but the sender disconnects before it's processed.
  /// 
  /// DEFAULTS: Returns 0 if no packets queued (due to FirstOrDefault returning null,
  /// which becomes SteamId 0, which then causes the dictionary lookup to fail or return
  /// whatever connection has SteamId 0).
  /// </remarks>
  public override int _GetPacketPeer() {
    return _connectionsBySteamId[_incomingPackets.FirstOrDefault()?.SenderSteamId ?? 0].PeerId;
  }

  /// <summary>
  /// Godot override: Dequeues and returns the next packet's data.
  /// </summary>
  /// <returns>Packet byte array, or empty array if no packets available</returns>
  public override byte[] _GetPacketScript() {
    if (_incomingPackets.TryDequeue(out SteamPacketPeer? packet)) {
      return packet.Data;
    }

    return [];
  }

  /// <summary>
  /// Godot override: Returns the current transfer channel.
  /// </summary>
  public override int _GetTransferChannel() {
    return _transferChannel;
  }

  /// <summary>
  /// Godot override: Returns the current transfer mode for sending packets.
  /// </summary>
  public override TransferModeEnum _GetTransferMode() {
    return _transferMode;
  }

  /// <summary>
  /// Godot override: Returns this peer's unique ID.
  /// </summary>
  public override int _GetUniqueId() {
    return _uniqueId;
  }

  /// <summary>
  /// Godot override: Returns whether new connections are being refused.
  /// </summary>
  public override bool _IsRefusingNewConnections() {
    return _refuseNewConnections;
  }

  /// <summary>
  /// Godot override: Returns whether this peer is the server/host.
  /// </summary>
  /// <remarks>
  /// Server is always peer ID 1 by convention.
  /// </remarks>
  public override bool _IsServer() {
    return _uniqueId == 1;
  }

  /// <summary>
  /// Godot override: Returns whether server relay is supported.
  /// </summary>
  /// <remarks>
  /// Returns true for both Server and Client modes. This indicates that the
  /// architecture supports a host-client model (not full mesh P2P).
  /// </remarks>
  public override bool _IsServerRelaySupported() {
    return _mode == Mode.Server || _mode == Mode.Client;
  }

  /// <summary>
  /// Godot override: Polls for new messages and processes them. Called every frame by Godot.
  /// </summary>
  /// <remarks>
  /// PROCESSING ORDER:
  /// 1. Receive from socket manager (incoming connections for server)
  /// 2. Receive from connection manager (outgoing connection for client, or self-connection for server)
  /// 3. Get pending messages from connection manager queue
  /// 4. For each connection, union messages from connection manager and socket manager
  /// 5. Process each message (either as handshake or regular packet)
  ///
  /// HANDSHAKE DETECTION: Messages are distinguished by checking if GetPeerIdFromSteamId returns -1.
  /// If -1, it's a handshake message (SetupPeerPayload), otherwise it's a regular data packet.
  /// </remarks>
  public override void _Poll() {
    if (_steamSocketManager != null) {
      _steamSocketManager.Receive();
    }

    if (_steamConnectionManager != null && _steamConnectionManager.Connected) {
      _steamConnectionManager.Receive();
    }

    IEnumerable<SteamNetworkingMessage> steamNetworkingMessages = _steamConnectionManager?.GetPendingMessages() ?? [];

    foreach (SteamConnection connection in _connectionsBySteamId.Values) {
      IEnumerable<SteamNetworkingMessage> messagesByConnection = steamNetworkingMessages.Union(_steamSocketManager?.ReceiveMessagesOnConnection(connection.Connection) ?? []);
      foreach (SteamNetworkingMessage message in messagesByConnection) {
        if (GetPeerIdFromSteamId(message.Sender) != -1) {
          ProcessMesssage(message);
        } else {
          SteamConnection.SetupPeerPayload? receive = message.Data.ToStruct<SteamConnection.SetupPeerPayload>();

          ProcessPing(receive.Value, message.Sender);
        }
      }
    }
  }

  /// <summary>
  /// Processes a peer handshake message (SetupPeerPayload).
  /// </summary>
  /// <param name="receive">The handshake payload containing peer ID</param>
  /// <param name="sender">The Steam ID of the sender</param>
  /// <remarks>
  /// SERVER RESPONSE: Only servers send back their peer ID. Clients don't respond to
  /// the server's handshake message.
  /// </remarks>
  private void ProcessPing(SteamConnection.SetupPeerPayload receive, ulong sender) {
    SteamConnection connection = _connectionsBySteamId[sender];

    if (receive.PeerId != -1) {
      if (connection.PeerId == -1) {
        SetSteamIdPeer(sender, receive.PeerId);
      }
      if (_IsServer()) {
        connection.SendPeer(_uniqueId);
      }

      EmitSignal(SignalName.PeerConnected, receive.PeerId);
    }
  }

  /// <summary>
  /// Assigns a peer ID to a connection and adds it to the peer ID lookup dictionary.
  /// </summary>
  /// <param name="steamId">The Steam ID of the connection</param>
  /// <param name="peerId">The peer ID to assign</param>
  /// <remarks>
  /// SAFETY: Checks that PeerId is still -1 before assigning to prevent overwriting.
  /// This protects against assigning different peer IDs to the same connection.
  /// </remarks>
  private void SetSteamIdPeer(ulong steamId, int peerId) {
    SteamConnection steamConnection = _connectionsBySteamId[steamId];
    if (steamConnection.PeerId == -1) {
      steamConnection.PeerId = peerId;
      _peerIdToConnection.Add(peerId, steamConnection);
    }
  }

  /// <summary>
  /// Processes a regular data message
  /// </summary>
  /// <param name="message">The received Steam networking message</param>
  /// <remarks>
  /// Creates a SteamPacketPeer wrapper and queues it for consumption by Godot via _GetPacketScript.
  ///
  /// TRANSFER MODE: Hardcoded to Reliable even though the message might have been sent unreliably.
  /// This means packet.TransferMode doesn't reflect the actual transfer mode used.
  /// </remarks>
  private void ProcessMesssage(SteamNetworkingMessage message) {
    SteamPacketPeer packet = new SteamPacketPeer(message.Data, TransferModeEnum.Reliable);
    packet.SenderSteamId = message.Sender;

    _incomingPackets.Enqueue(packet);
  }

  /// <summary>
  /// Godot override: Sends a packet to the target peer(s).
  /// </summary>
  /// <param name="pBuffer">The packet data to send</param>
  /// <returns>Error code indicating success or failure</returns>
  /// <remarks>
  /// BROADCAST: If _targetPeer == 0, sends to all connections in _connectionsBySteamId.
  /// This sends to ALL established Steam connections, even those without peer IDs yet.
  ///
  /// TARGETED: If _targetPeer != 0, sends only to that specific peer ID.
  /// Uses Mathf.Abs(_targetPeer) to handle negative peer IDs (used by Godot for special targeting).
  ///
  /// ERROR HANDLING: When broadcasting, stops on first error and returns it immediately.
  /// Some peers may receive the packet while others don't if an error occurs mid-broadcast.
  /// </remarks>
  public override Error _PutPacketScript(byte[] pBuffer) {
    if (!_isActive || _connectionStatus != ConnectionStatus.Connected) { return Error.Unconfigured; }

    if (_targetPeer != 0 && !_peerIdToConnection.ContainsKey(Mathf.Abs(_targetPeer))) {
      return Error.InvalidParameter;
    }

    if (_mode == Mode.Client && !_peerIdToConnection.ContainsKey(1)) {
      return Error.Bug;
    }

    SteamPacketPeer packet = new SteamPacketPeer(pBuffer, _transferMode);


    if (_targetPeer == 0) {
      Error error = Error.Ok;
      foreach (SteamConnection connection in _connectionsBySteamId.Values) {
        Error packetSendingError = connection.Send(packet);

        if (packetSendingError != Error.Ok) {
          return packetSendingError;
        }
      }

      return error;
    } else {
      return GetConnectionFromPeer(_targetPeer)?.Send(packet) ?? Error.Unavailable;
    }
  }

  /// <summary>
  /// Godot override: Sets whether to refuse new connections.
  /// </summary>
  /// <remarks>
  public override void _SetRefuseNewConnections(bool pEnable) {
    _refuseNewConnections = pEnable;
  }

  /// <summary>
  /// Godot override: Sets the target peer for the next sent packet.
  /// </summary>
  /// <param name="pPeer">0 = broadcast, positive = specific peer, negative = all except abs(pPeer)</param>
  /// <remarks>
  /// TODO: Investigate this claim
  /// NEGATIVE PEER IDS: Godot uses negative peer IDs to mean "send to all except this peer".
  /// This implementation doesn't properly handle negative IDs - it just uses Mathf.Abs
  /// in _PutPacketScript, which means -5 is treated the same as 5 (send only to peer 5).
  ///
  /// The broadcast exclusion feature is not implemented.
  /// </remarks>
  public override void _SetTargetPeer(int pPeer) {
    _targetPeer = pPeer;
  }

  /// <summary>
  /// Godot override: Sets the transfer channel for the next sent packet.
  /// </summary>
  /// <remarks>
  /// TODO: Multiple channels not implemented. This value is stored but never used.
  /// </remarks>
  public override void _SetTransferChannel(int pChannel) {
    _transferChannel = pChannel;
  }

  /// <summary>
  /// Godot override: Sets the transfer mode for the next sent packet.
  /// </summary>
  /// <remarks>
  /// NUANCE: This value is stored but currently ignored. All packets are sent as Reliable
  /// regardless of this setting. See SteamConnection.RawSend.
  /// </remarks>
  public override void _SetTransferMode(TransferModeEnum pMode) {
    _transferMode = pMode;
  }

  /// <summary>
  /// Gets a connection by peer ID.
  /// </summary>
  /// <param name="peerId">The peer ID to look up</param>
  /// <returns>The connection, or null if not found</returns>
  private SteamConnection? GetConnectionFromPeer(int peerId) {
    if (_peerIdToConnection.TryGetValue(peerId, out SteamConnection? value)) {
      return value;
    }

    return null;
  }

  /// <summary>
  /// Gets a peer ID from a Steam ID.
  /// </summary>
  /// <param name="steamId">The Steam ID to look up</param>
  /// <returns>The peer ID, -1 if not found or if it's the local peer's Steam ID</returns>
  /// <remarks>
  /// SELF-LOOKUP: Returns _uniqueId if the Steam ID matches the local peer's ID.
  /// This is used to identify messages from self (which should be filtered out).
  /// </remarks>
  private int GetPeerIdFromSteamId(ulong steamId) {
    if (steamId == _steamId.Value) {
      return _uniqueId;
    }

    if (_connectionsBySteamId.TryGetValue(steamId, out SteamConnection? value)) {
      return value.PeerId;
    }

    return -1;
  }

  /// <summary>
  /// Adds a new connection to the tracking dictionaries.
  /// </summary>
  /// <param name="steamId">The Steam ID of the remote peer</param>
  /// <param name="connection">The Steam connection handle</param>
  /// <exception cref="InvalidOperationException">Thrown if trying to add self as a peer</exception>
  /// <remarks>
  /// INITIAL STATE: Connection is added with PeerId = -1 (unassigned).
  /// Only added to _connectionsBySteamId here. It will be added to _peerIdToConnection
  /// later during the handshake when the peer ID is assigned.
  /// </remarks>
  private void AddConnection(SteamId steamId, Connection connection) {
    if (steamId == _steamId.Value) {
      throw new InvalidOperationException("Cannot add Self as Peer");
    }

    SteamConnection connectionData = new() {
      Connection = connection,
      SteamIdRaw = steamId
    };

    _connectionsBySteamId.Add(steamId, connectionData);
  }
}
