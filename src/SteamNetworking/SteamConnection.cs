using Godot;
using Steamworks;
using Steamworks.Data;

namespace Steam;

/// <summary>
/// Represents a connection to a remote peer via Steam's networking system.
/// Wraps the low-level Steam Connection and manages peer identification during the handshake phase.
/// </summary>
public partial class SteamConnection : RefCounted {
  /// <summary>
  /// The Steam ID of the remote peer.
  /// </summary>
  public SteamId SteamIdRaw { get; set; }

  /// <summary>
  /// The raw ulong value of the Steam ID for easier comparison and dictionary lookups.
  /// </summary>
  public ulong SteamId => SteamIdRaw.Value;

  /// <summary>
  /// The underlying Steam networking connection handle.
  /// </summary>
  public Connection Connection { get; set; }

  /// <summary>
  /// The Godot multiplayer peer ID assigned to this connection.
  /// Starts at -1 until the peer handshake completes via SetupPeerPayload.
  /// </summary>
  /// <remarks>
  /// -1 indicates the peer is not yet identified in the multiplayer system.
  /// 1 is always the host/server.
  /// Other positive values are client peer IDs.
  /// </remarks>
  public int PeerId { get; set; } = -1;

  /// <summary>
  /// Payload structure sent during the initial peer handshake to exchange peer IDs.
  /// </summary>
  /// </remarks>
  public struct SetupPeerPayload {
    /// <summary>
    /// The peer ID to assign. -1 indicates unassigned.
    /// </summary>
    public int PeerId = -1;

    public SetupPeerPayload() {
    }
  }

  /// <summary>
  /// Sends a packet to the remote peer over this connection.
  /// </summary>
  /// <param name="packet">The packet to send</param>
  /// <returns>Error.Ok on success, otherwise an error code</returns>
  /// </remarks>
  public Error Send(SteamPacketPeer packet) {
    Error errorCode = RawSend(packet);
    if (errorCode != Error.Ok) {
      return errorCode;
    }

    return Error.Ok;
  }

  /// <summary>
  /// Sends the packet data over the Steam connection.
  /// </summary>
  /// </remarks>
  private Error RawSend(SteamPacketPeer packet) {
    return GetErrorFromResult(Connection.SendMessage(packet.Data, SendType.Reliable));
  }

  /// <summary>
  /// Converts Steam Result codes to Godot Error codes.
  /// </summary>
  /// <remarks>
  /// Maps Steam's extensive Result enum to Godot's more limited Error enum.
  /// Some Steam-specific errors (like authentication/account issues) don't have
  /// direct Godot equivalents and are mapped to the closest semantic match.
  ///
  /// Unmapped Steam errors return Error.Bug to indicate an unexpected/unhandled case.
  /// </remarks>
  private Error GetErrorFromResult(Result result) => result switch {
    //TODO - IMPLEMENT OTHER ERROR MESSAGES
    Result.OK => Error.Ok,
    Result.Fail => Error.Failed,
    Result.NoConnection => Error.ConnectionError,
    Result.InvalidParam => Error.InvalidParameter,
    _ => Error.Bug
  };

  /// <summary>
  /// Sends the peer ID to the remote peer as part of the handshake protocol.
  /// </summary>
  /// <param name="uniqueId">The peer ID to send (typically the local peer's ID)</param>
  /// <returns>Error.Ok on success, otherwise an error code</returns>
  public Error SendPeer(int uniqueId) {
    SetupPeerPayload payload = new SetupPeerPayload();
    payload.PeerId = uniqueId;
    return SendSetupPeer(payload);
  }

  /// <summary>
  /// Serializes and sends a SetupPeerPayload to the remote peer.
  /// </summary>
  private Error SendSetupPeer(SetupPeerPayload payload) {
    return Send(new SteamPacketPeer(payload.ToBytes(), MultiplayerPeer.TransferModeEnum.Reliable));
  }
}
