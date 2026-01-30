#nullable enable
using Godot;
using Steamworks.Data;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Steam;

/// <summary>
/// Manages outgoing Steam relay connections for clients connecting to a host.
/// Extends Steamworks.ConnectionManager to handle connection lifecycle and message buffering.
/// </summary>
/// <remarks>
/// This class is typically used by clients when connecting to a host via ConnectRelay.
/// It queues incoming messages until they can be processed by the multiplayer peer.
///
/// IMPORTANT: Messages are buffered in a queue and must be retrieved via GetPendingMessages()
/// during the poll cycle, otherwise they will accumulate in memory indefinitely.
/// </remarks>
public class SteamConnectionManager : ConnectionManager {
  /// <summary>
  /// Fired when a connection is successfully established and ready for data transmission.
  /// </summary>
  public event Action<ConnectionInfo>? OnConnectionEstablished;

  /// <summary>
  /// Fired when a connection is lost or disconnected.
  /// </summary>
  public event Action<ConnectionInfo>? OnConnectionLost;

  /// <summary>
  /// Queue of messages received but not yet processed by the multiplayer system.
  /// Messages are added during OnMessage callbacks and consumed via GetPendingMessages().
  /// </summary>
  private Queue<SteamNetworkingMessage> _pendingMessages { get; } = new Queue<SteamNetworkingMessage>();

  /// <summary>
  /// Called when connection status changes. Currently just passes through to base implementation.
  /// </summary>
  public override void OnConnectionChanged(ConnectionInfo info) {
    base.OnConnectionChanged(info);
  }

  /// <summary>
  /// Called when the connection is fully established and ready for communication.
  /// Raises the OnConnectionEstablished event for external listeners.
  /// </summary>
  public override void OnConnected(ConnectionInfo info) {
    base.OnConnected(info);
    OnConnectionEstablished?.Invoke(info);
  }

  /// <summary>
  /// Called during the connection process before it's fully established.
  /// Currently just passes through to base implementation.
  /// </summary>
  public override void OnConnecting(ConnectionInfo info) {
    base.OnConnecting(info);
  }

  /// <summary>
  /// Called when the connection is closed or lost.
  /// Raises the OnConnectionLost event for external listeners.
  /// </summary>
  public override void OnDisconnected(ConnectionInfo info) {
    base.OnDisconnected(info);
    OnConnectionLost?.Invoke(info);
  }

  /// <summary>
  /// Called when a message is received from the remote peer.
  /// Copies the unmanaged data to managed memory and queues it for processing.
  /// </summary>
  /// <remarks>
  /// CRITICAL: The data pointer is unmanaged memory that Steam will free after this callback returns.
  /// We MUST copy it to managed memory (byte array) before returning, otherwise we'll have a dangling pointer.
  ///
  /// NUANCE: Always marks messages as Reliable regardless of how they were actually sent.
  /// This may cause issues if transfer mode matters for message processing.
  /// </remarks>
  public override void OnMessage(IntPtr data, int size, long recvTime, long messageNum, int channel) {
    base.OnMessage(data, size, messageNum, recvTime, channel);

    byte[] managedArray = new byte[size];
    Marshal.Copy(data, managedArray, 0, size);

    _pendingMessages.Enqueue(new SteamNetworkingMessage(managedArray, ConnectionInfo.Identity.SteamId, MultiplayerPeer.TransferModeEnum.Reliable, recvTime));
  }

  /// <summary>
  /// Retrieves and removes all currently pending messages from the queue.
  /// </summary>
  /// <returns>An enumerable of all queued messages. The queue is emptied as messages are yielded.</returns>
  /// <remarks>
  /// IMPORTANT: This method captures the count at the start and only processes that many messages.
  /// If new messages arrive during enumeration (e.g., from another thread), they won't be included
  /// in this batch. This prevents infinite loops if messages continue arriving during processing.
  ///
  /// The caller must enumerate the entire sequence to clear the queue, otherwise messages remain queued.
  /// </remarks>
  public IEnumerable<SteamNetworkingMessage> GetPendingMessages() {
    int maxMessageCount = _pendingMessages.Count;
    for (int i = 0; i < maxMessageCount; i++) {
      yield return _pendingMessages.Dequeue();
    }
  }
}
