using Godot;
using System;
using Steamworks;
using Steam;

using MembersList = Godot.Collections.Array<Godot.Collections.Dictionary>;
using System.Collections.Generic;

/// <summary>
/// Manages multiplayer lobby functionality
/// - Hosting
/// - Joining
/// - Member Synchronization
///
/// Supports both Steam networking and traditional IP-based connections via ENet.
/// Supports UPnP
/// </summary>
public partial class Lobby : Node {
  private static Lobby _this;

  /// <summary>
  /// Invoked when the local player successfully hosts a game.
  /// </summary>
  public static Action OnHost;

  /// <summary>
  /// Invoked when the local player successfully joins a game.
  /// </summary>
  public static Action OnJoin;

  /// <summary>
  /// Invoked when the local player disconnects from a game.
  /// </summary>
  public static Action OnDisconnect;

  /// <summary>
  /// Invoked when the member list is updated (players join/leave).
  ///
  /// NOTE: This is kicked off by the host
  /// </summary>
  public static Action OnMembersUpdated;

  public static bool IsHost { get; private set; } = false;

  public static bool IsSteam { get; private set; } = false;

  /// <summary>
  /// Only populated when using Steam networking.
  /// </summary>
  public static Dictionary<ulong, Member> MemberBySteamID = [];

  /// <summary>
  /// Dictionary mapping Godot multiplayer peer IDs to Member objects.
  /// Used for both Steam and ENet connections.
  /// </summary>
  public static Dictionary<int, Member> MemberByPeerID = [];

  private Upnp _upnp = null;

  /// <summary>
  /// Gets the external IP address for hosting.
  /// Returns UPnP-discovered address if available, otherwise falls back to Settings.IPAddress.
  /// </summary>
  private static string IPAddress() {
    if (_this._upnp == null) {
      return Settings.IPAddress.Value;
    }

    return _this._upnp.QueryExternalAddress();
  }

  /// <summary>
  /// Synchronizes the member list across all clients in the lobby.
  /// Called via RPC by the host when players join/leave.
  ///
  /// IMPORTANT: Only the host maintains the authoritative member list.
  /// Clients receive updates through this RPC method.
  ///
  /// Will also attempt to load avatars into lobby member objects
  /// </summary>
  /// <param name="members">Serialized member list from the host</param>
  [Rpc(CallLocal = true)]
  private async void SyncMembers(MembersList members) {
    var memberArray = members.FromMembersList();
    MemberBySteamID.Clear();
    MemberByPeerID.Clear();
    foreach (var member in memberArray) {
      if (IsSteam) MemberBySteamID[member.SteamID] = member;
      MemberByPeerID[member.PeerID] = member;

      if (member.SteamID == default) {
        await member.TryLoadAvatar(null);
      }
    }

    if (IsSteam) {
      foreach (var steamMember in SteamManager.HostedLobby.Members) {
        if (!MemberBySteamID.ContainsKey(steamMember.Id.Value)) continue;
        var member = MemberBySteamID[steamMember.Id.Value];
        await member?.TryLoadAvatar(steamMember);
      }
    }

    OnMembersUpdated?.Invoke();
  }

  /// <summary>
  /// Attempt to establish UPnP port mapping for hosting without port forwarding.
  /// Maps both UDP and TCP protocols for the configured port.
  /// </summary>
  private void TryPrepareUpnp() {
    var _upnp = new Upnp();
    var result = _upnp.Discover();

    if (result != (int)Upnp.UpnpResult.Success) {
      GD.PrintErr("UPnP discovery failed: " + result.ToString());
      return;
    }

    var gateway = _upnp.GetGateway();
    if (!gateway.IsValidGateway()) {
      GD.PrintErr("No valid UPnP gateway found.");
      return;
    }

    var mapResultUDP = _upnp.AddPortMapping(Settings.Port.Value, Settings.Port.Value, "Friend Slop Game Port", "UDP");
    var mapResultTCP = _upnp.AddPortMapping(Settings.Port.Value, Settings.Port.Value, "Friend Slop Game Port", "TCP");

    if (mapResultUDP != (int)Upnp.UpnpResult.Success || mapResultTCP != (int)Upnp.UpnpResult.Success) {
      GD.PrintErr("Failed to add UPnP port mapping: " + mapResultUDP.ToString() + ", " + mapResultTCP.ToString());
      return;
    }

    GD.Print("UPnP port mapping added successfully.");
    GD.Print("External IP Address: " + _upnp.QueryExternalAddress());
  }

  /// <summary>
  /// Initializes the Lobby singleton when entering the scene tree.
  /// Initializes Steam client if available.
  /// Initializes UPnP
  /// </summary>
  public override void _EnterTree() {
    if (_this == null) {
      _this = this;
      GD.Print("Lobby initialized.");
    } else {
      QueueFree();
      return;
    }

    TryPrepareUpnp();

    try {
      SteamInitializer.Init();
      SteamManager.Load();
      GD.Print("Hello ", SteamClient.Name);
    } catch (Exception e) {
      GD.PrintErr("Steam initialization failed: " + e.Message);
    }
  }

  /// <summary>
  /// Shuts down Steam client if active.
  /// Removes UPnP port mappings if they were established.
  /// </summary>
  public override void _ExitTree() {
    if (SteamClient.IsValid) {
      SteamClient.Shutdown();
    }

    if (_upnp != null) {
      _upnp.DeletePortMapping(Settings.Port.Value, "UDP");
      _upnp.DeletePortMapping(Settings.Port.Value, "TCP");
    }
  }

  /// <summary>
  /// Disconnect from current game session.
  /// </summary>
  public static void Disconnect() {
    _this.Multiplayer.MultiplayerPeer = null;
    OnDisconnect?.Invoke();
  }
}
