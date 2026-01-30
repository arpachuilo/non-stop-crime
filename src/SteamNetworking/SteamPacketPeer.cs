using Godot;
using static Godot.MultiplayerPeer;

namespace Steam;

/// <summary>
/// Represents a packet to be sent or received over a Steam connection.
/// Wraps packet data along with metadata like sender and transfer mode.
/// </summary>
/// <remarks>
/// Constructs a new packet.
/// </remarks>
/// <param name="data">The packet payload bytes</param>
/// <param name="transferMode">The transfer mode to use (defaults to Reliable)</param>
public partial class SteamPacketPeer(byte[] data, TransferModeEnum transferMode = MultiplayerPeer.TransferModeEnum.Reliable) : RefCounted {
  /// <summary>
  /// The packet payload data.
  /// </summary>
  public byte[] Data { get; private set; } = data;

  /// <summary>
  /// The Steam ID of the sender. Set to 0 for outgoing packets, populated for incoming packets.
  /// </summary>
  /// <remarks>
  /// MUTABLE: This is the only mutable property. It's set after construction when processing
  /// received messages in SteamMultiplayerPeer.ProcessMessage.
  /// </remarks>
  public ulong SenderSteamId { get; set; }

  /// <summary>
  /// The intended transfer mode for this packet.
  /// </summary>
  public TransferModeEnum TransferMode { get; private set; } = transferMode;
}
