using System.Collections.Generic;
using System.Linq;
using Godot;
using Steam;
using Steamworks;
using SteamLobby = Steamworks.Data.Lobby;

public partial class Lobby : Node {
  /// <summary>
  /// Maps peer IDs to temporary canned names for non-Steam players.
  /// Used to maintain consistent names throughout a session for ENet connections.
  /// </summary>
  private static readonly Dictionary<int, string> _peerIdToCannedName = [];

  /// <summary>
  /// Host a multiplayer game using ENet (IP-based networking).
  ///
  /// Creates an ENet multiplayer peer
  /// Sets IsSteam to false.
  /// </summary>
  public static void ENetHost() {
    var peer = new ENetMultiplayerPeer();
    var error = peer.CreateServer(Settings.Port.Value, Settings.MaxPlayers.Value);
    if (error != Error.Ok) {
      GD.PrintErr("Failed to create ENet host: " + error.ToString());
      return;
    }

    if (peer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Disconnected) {
      GD.PrintErr("Failed to host game.");
      return;
    }

    IsSteam = false;
    EstablishHostPeer(peer);
  }

  /// <summary>
  /// Host a multiplayer game using Steam networking.
  ///
  /// Creates a Steam multiplayer peer
  /// Sets IsSteam to true.
  /// </summary>
  private static void SteamHostLobby() {
    var peer = new SteamMultiplayerPeer();
    var error = peer.CreateHost(SteamManager.PlayerSteamID);
    if (error != Error.Ok) {
      GD.PrintErr("Failed to create Steam host: " + error.ToString());
      return;
    }

    if (peer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Disconnected) {
      GD.PrintErr("Failed to host game.");
      return;
    }

    IsSteam = true;
    EstablishHostPeer(peer);
  }

  /// <summary>
  /// Sets multiplayer peer
  /// Sets up peer connection/disconnection handlers.
  /// Invokes OnHost action
  /// </summary>
  private static void EstablishHostPeer(MultiplayerPeer peer) {
    IsHost = true;

    _peerIdToCannedName.Clear();
    _this.Multiplayer.MultiplayerPeer = peer;
    peer.PeerConnected += _this.OnPlayerConnectedToHost;
    peer.PeerDisconnected += _this.OnPlayerDisconnectedFromHost;

    OnHost?.Invoke();
    _this.UpdateAndSyncMembers();

    GD.Print("Hosting game");
  }

  /// <summary>
  /// Event handler for successful Steam lobby creation.
  /// </summary>
  /// <param name="lobby">The created Steam lobby</param>
  public static void HandleLobbyHost(SteamLobby lobby) {
    GD.Print("Lobby created with ID: ", lobby.Id);
    SteamHostLobby();

    SteamManager.OnLobbySuccessfullyCreated -= HandleLobbyHost;
  }

  /// <summary>
  /// Create and host a Steam lobby.
  /// </summary>
  public async static void SteamHost() {
    SteamManager.OnLobbySuccessfullyCreated += HandleLobbyHost;
    await SteamManager.CreateLobby();
  }

  /// <summary>
  /// Convert a multiplayer peer ID to a Steam Friend.
  ///
  /// HOST ONLY: Only works on the host machine as they have connections to all other peers.
  /// </summary>
  /// <param name="id">The Godot multiplayer peer ID</param>
  /// <returns>The Steam Friend if found in the hosted lobby, null if not Steam or not found</returns>
  private static Friend? PeerIdToSteamFriend(int id) {
    if (_this.Multiplayer.MultiplayerPeer is not SteamMultiplayerPeer steamPeer) return null;

    var connection = steamPeer.ConnectionsByPeerId[id];
    foreach (var member in SteamManager.HostedLobby.Members) {
      if (member.Id.Value == connection.SteamId) {
        return member;
      }
    }

    return null;
  }

  /// <summary>
  /// Retrieves or generates a temporary canned name for a peer ID.
  /// Caches names per peer ID to maintain consistency throughout the session.
  /// </summary>
  /// <param name="peerID">The Godot multiplayer peer ID</param>
  /// <returns>A consistent canned name for this peer ID</returns>
  private static string GetCachedTempName(int peerID) {
    if (_peerIdToCannedName.ContainsKey(peerID)) {
      return _peerIdToCannedName[peerID];
    }

    var name = RandomUtil.FromList(Canned.PlayerNames.Except(_peerIdToCannedName.Values));
    _peerIdToCannedName[peerID] = name;
    return name;
  }

  /// <summary>
  /// Get list of all current members in the lobby, including the host.
  ///
  /// HOST ONLY: Should only be called on the host machine as clients don't have peer information.
  /// </summary>
  /// <returns>Array of all members including the host (index 0)</returns>
  public static Member[] GetMembers() {
    var peers = _this.Multiplayer.GetPeers();
    var members = new Member[peers.Length + 1];

    var id = _this.Multiplayer.MultiplayerPeer.GetUniqueId();
    var host = new Member {
      PeerID = id,
      SteamID = IsSteam ? SteamManager.PlayerSteamID.Value : 0u,
      Name = IsSteam ? SteamClient.Name : GetCachedTempName(id),
      IsHost = IsHost
    };

    int i = 0;
    members[i++] = host;
    foreach (var peer in peers) {
      var friend = PeerIdToSteamFriend(peer);
      var member = new Member {
        PeerID = peer,
        SteamID = friend.HasValue ? friend.Value.Id.Value : 0u,
        Name = friend.HasValue ? friend.Value.Name : GetCachedTempName(peer),
        IsHost = false
      };

      members[i++] = member;
    }

    return members;
  }

  /// <summary>
  /// Updates the member list and synchronizes it to all clients via RPC.
  /// Called internally when players connect or disconnect.
  /// </summary>
  private void UpdateAndSyncMembers() {
    Rpc(nameof(SyncMembers), GetMembers().ToMembersList());
  }

  /// <summary>
  /// Handler for when a player connects to the host.
  /// Triggers member list update and synchronization to all clients.
  /// </summary>
  /// <param name="id">The peer ID of the connected player</param>
  private void OnPlayerConnectedToHost(long id) {
    UpdateAndSyncMembers();
  }

  /// <summary>
  /// Handler for when a player disconnects from the host.
  ///
  /// Removes the peer's canned name from the cache to free it for reuse.
  /// </summary>
  /// <param name="id">The peer ID of the disconnected player</param>
  public void OnPlayerDisconnectedFromHost(long id) {
    UpdateAndSyncMembers();

    _peerIdToCannedName.Remove((int) id);
  }
}
