using Godot;
using System;
using Steamworks;
using Steam;

using SteamLobby = Steamworks.Data.Lobby;

public partial class Lobby : Node {
  /// <summary>
  /// Join a multiplayer game as a client using ENet (IP-based connection).
  ///
  /// Creates an ENet multiplayer peer and connects to the specified IP address.
  /// Sets IsSteam to false.
  /// </summary>
  /// <param name="ip">IP address of the host to connect to</param>
  public static void ENetJoin(string ip) {
    var peer = new ENetMultiplayerPeer();
    var error = peer.CreateClient(ip, Settings.Port.Value);
    if (error != Error.Ok) {
      GD.PrintErr("Failed to join host: " + error.ToString());
      return;
    }

    if (peer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Disconnected) {
      GD.PrintErr("Failed to join game.");
      return;
    }

    IsSteam = false;
    EstablishClientPeer(peer);
  }

  /// <summary>
  /// Join a multiplayer game as a client using Steam networking.
  ///
  /// Creates a Steam multiplayer peer and connects to the specified lobby's owner.
  /// Sets IsSteam to true.
  /// </summary>
  /// <param name="lobby">The Steam lobby to join</param>
  private static void SteamJoinLobby(SteamLobby lobby) {
    var peer = new SteamMultiplayerPeer();
    GD.Print("Joining lobby: ", lobby.Id);
    try {
      var error = peer.CreateClient(SteamManager.PlayerSteamID, lobby.Owner.Id);
      if (error != Error.Ok) {
        GD.PrintErr("Failed to join host: " + error.ToString());
        return;
      }
    } catch (Exception e) {
      GD.PrintErr("Error joining lobby: " + e.Message);
      return;
    }

    if (peer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Disconnected) {
      GD.PrintErr("Failed to join game.");
      return;
    }

    IsSteam = true;
    EstablishClientPeer(peer);
  }

  /// <summary>
  /// Sets multiplayer peer
  /// Invokes OnJoin action
  /// </summary>
  private static void EstablishClientPeer(MultiplayerPeer peer) {
    IsHost = false;
    _this.Multiplayer.MultiplayerPeer = peer;

    OnJoin?.Invoke();

    GD.Print("Joined game");
  }

  /// <summary>
  /// Event handler for successful Steam lobby join.
  /// </summary>
  /// <param name="lobby">The joined Steam lobby</param>
  /// <param name="friend">The Steam friend who owns the lobby (the host)</param>
  private static void HandleSteamLobbyJoin(SteamLobby lobby, Friend friend) {
    SteamJoinLobby(lobby);

    SteamManager.OnPlayerJoinLobby -= HandleSteamLobbyJoin;
  }

  /// <summary>
  /// Join a Steam lobby by its ID.
  /// </summary>
  /// <param name="id">The Steam lobby ID to join</param>
  public static async void SteamJoin(ulong id) {
    SteamManager.OnPlayerJoinLobby += HandleSteamLobbyJoin;

    await SteamMatchmaking.JoinLobbyAsync(id);
  }
}
