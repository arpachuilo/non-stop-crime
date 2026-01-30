#nullable enable
using Godot;
using Steamworks.Data;
using Steamworks;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace Steam;

/// <summary>
/// Manages incoming Steam relay socket connections for servers/hosts.
/// Extends Steamworks.SocketManager to handle multiple client connections and message buffering.
/// </summary>
/// <remarks>
/// This class is used by the server/host when accepting connections via CreateRelaySocket.
/// It maintains separate message queues for each connected client.
///
/// ARCHITECTURE: The server uses this class to handle all incoming connections. Each connection
/// gets its own message queue in _connectionMessages, allowing messages to be processed per-connection.
///
/// IMPORTANT: Messages are buffered per-connection and must be retrieved via ReceiveMessagesOnConnection()
/// during the poll cycle, otherwise they accumulate in memory indefinitely.
/// </remarks>
public class SteamSocketManager : SocketManager {
  /// <summary>
  /// Maps Steam connections to their queued messages. Each connection has its own queue.
  /// </summary>
  private Dictionary<Connection, Queue<SteamNetworkingMessage>> _connectionMessages = [];

  /// <summary>
  /// Fired when a new client connection is successfully established.
  /// </summary>
  /// <remarks>
  /// Provides both the Connection handle and ConnectionInfo in a tuple.
  /// </remarks>
  public event Action<(Connection, ConnectionInfo)>? OnConnectionEstablished;

  /// <summary>
  /// Fired when a client connection is lost or disconnected.
  /// </summary>
  public event Action<(Connection, ConnectionInfo)>? OnConnectionLost;

  /// <summary>
  /// Fired when a connection's status changes.
  /// </summary>
  public event Action<(Connection, ConnectionInfo)>? OnConnectionChange;

  /// <summary>
  /// Called when connection status changes. Raises the OnConnectionChange event.
  /// </summary>
  public override void OnConnectionChanged(Connection connection, ConnectionInfo info) {
    base.OnConnectionChanged(connection, info);
    OnConnectionChange?.Invoke((connection, info));
  }

  /// <summary>
  /// Called when a new client successfully connects to the server's relay socket.
  /// Initializes the message queue for this connection and raises the OnConnectionEstablished event.
  /// </summary>
  /// <remarks>
  /// QUEUE INITIALIZATION: Creates a new message queue for this connection.
  /// This queue must be drained via ReceiveMessagesOnConnection() or it will leak memory.
  /// </remarks>
  public override void OnConnected(Connection connection, ConnectionInfo info) {
    base.OnConnected(connection, info);
    _connectionMessages.Add(connection, new Queue<SteamNetworkingMessage>());
    OnConnectionEstablished?.Invoke((connection, info));
  }

  /// <summary>
  /// Called during the connection process before it's fully established.
  /// Currently just passes through to base implementation.
  /// </summary>
  public override void OnConnecting(Connection connection, ConnectionInfo info) {
    base.OnConnecting(connection, info);
  }

  /// <summary>
  /// Called when a client disconnects or loses connection.
  /// Removes the message queue and raises the OnConnectionLost event.
  /// </summary>
  /// <remarks>
  /// CLEANUP: Removes the connection's message queue. Any unprocessed messages in that queue
  /// are discarded. If messages need to be processed before cleanup, they should be retrieved
  /// before disconnection.
  /// </remarks>
  public override void OnDisconnected(Connection connection, ConnectionInfo info) {
    base.OnDisconnected(connection, info);
    _connectionMessages.Remove(connection);
    OnConnectionLost?.Invoke((connection, info));
  }

  /// <summary>
  /// Called when a message is received from a connected client.
  /// Copies the unmanaged data to managed memory and queues it for the specific connection.
  /// </summary>
  /// <param name="connection">The connection that sent the message</param>
  /// <param name="identity">The sender's Steam network identity</param>
  /// <param name="data">Unmanaged pointer to the message data</param>
  /// <param name="size">Size of the message in bytes</param>
  /// <param name="recvTime">Steam's timestamp for when the message was received</param>
  /// <param name="messageNum">Message sequence number</param>
  /// <param name="channel">The channel this message was received on</param>
  public override void OnMessage(Connection connection, NetIdentity identity, IntPtr data, int size, long recvTime, long messageNum, int channel) {
    base.OnMessage(connection, identity, data, size, messageNum, recvTime, channel);

    byte[] managedArray = new byte[size];
    Marshal.Copy(data, managedArray, 0, size);

    _connectionMessages[connection].Enqueue(new SteamNetworkingMessage(managedArray, identity.SteamId, MultiplayerPeer.TransferModeEnum.Reliable, recvTime));
  }

  /// <summary>
  /// Retrieves and removes all currently pending messages for a specific connection.
  /// </summary>
  /// <param name="connection">The connection to retrieve messages from</param>
  /// <returns>An enumerable of all queued messages for this connection. The queue is emptied as messages are yielded.</returns>
  /// <remarks>
  /// The caller must enumerate the entire sequence to clear the queue, otherwise messages remain queued.
  /// </remarks>
  public IEnumerable<SteamNetworkingMessage> ReceiveMessagesOnConnection(Connection connection) {
    int messageCount = _connectionMessages[connection].Count;
    for (int i = 0; i < messageCount; i++) {
      yield return _connectionMessages[connection].Dequeue();
    }
  }
}
