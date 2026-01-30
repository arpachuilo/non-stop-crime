using static Godot.MultiplayerPeer;
using Steamworks;

namespace Steam;

/// <summary>
/// Immutable container for a received Steam networking message.
/// Holds the message data, sender info, transfer mode, and receive timestamp.
/// </summary>
/// <remarks>
/// Constructs a new message container.
/// </remarks>
/// <param name="data">The message payload bytes (already copied to managed memory)</param>
/// <param name="sender">The Steam ID of the peer who sent this message</param>
/// <param name="transferMode">The transfer mode used (Reliable, Unreliable, etc.)</param>
/// <param name="receiveTime">Steam's timestamp for when the message was received</param>
public class SteamNetworkingMessage(byte[] data, SteamId sender, TransferModeEnum transferMode, long receiveTime) {
  /// <summary>
  /// The message payload bytes.
  /// </summary>
  public byte[] Data { get; private set; } = data;

  /// <summary>
  /// The Steam ID of the sender.
  /// </summary>
  public SteamId Sender { get; private set; } = sender;

  /// <summary>
  /// The transfer mode used for this message.
  /// </summary>
  public TransferModeEnum TransferMode { get; private set; } = transferMode;

  /// <summary>
  /// The size of the message data in bytes.
  /// </summary>
  public int Size => Data.Length;

  /// <summary>
  /// Steam's timestamp for when the message was received (in microseconds since connection).
  /// </summary>
  public long ReceiveTime { get; private set; } = receiveTime;
}
