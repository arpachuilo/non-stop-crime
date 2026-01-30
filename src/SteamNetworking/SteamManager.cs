using Godot;
using Steamworks;
using System;
using System.Threading.Tasks;

using SteamLobby = Steamworks.Data.Lobby;

namespace Steam;

/// <summary>
/// High-level manager for Steam integration, handling initialization, lobby management, and Steam events.
/// Provides a singleton-style interface for Steam functionality throughout the application.
/// </summary>
/// <remarks>
/// This class manages:
/// - Steam client initialization
/// - Lobby creation and joining
/// - Lobby member tracking
/// - Steam overlay integration
///
/// IMPORTANT: Call Load() before using any Steam functionality. It initializes the Steam client
/// and registers event handlers. The class maintains static state, so there's only one Steam
/// session per application instance.
/// </remarks>
public static class SteamManager {
  /// <summary>
  /// Maximum number of players allowed in a lobby.
  /// </summary>
  [Export]
  public static int MaxLobbySize = 16;

  /// <summary>
  /// Fired when Steam is successfully initialized.
  /// </summary>
  public static Action OnSteamInitialized;

  /// <summary>
  /// Fired when a lobby is successfully created by the local player.
  /// </summary>
  public static Action<SteamLobby> OnLobbySuccessfullyCreated;

  /// <summary>
  /// Fired when a lobby's game server is created (when SetGameServer is called).
  /// </summary>
  public static Action<SteamLobby> OnLobbyGameCreated;

  /// <summary>
  /// Fired when a player joins any lobby the local player is in.
  /// </summary>
  public static Action<SteamLobby, Friend> OnPlayerJoinLobby;

  /// <summary>
  /// Fired when a player leaves or disconnects from a lobby the local player is in.
  /// </summary>
  public static Action<SteamLobby, Friend> OnPlayerLeftLobby;

  /// <summary>
  /// The local Steam user's display name.
  /// </summary>
  public static string PlayerName => SteamClient.Name;

  /// <summary>
  /// The local Steam user's unique Steam ID.
  /// </summary>
  public static SteamId PlayerSteamID => SteamClient.SteamId;

  /// <summary>
  /// Whether Steam has been successfully initialized.
  /// </summary>
  public static bool IsActive { get; private set; }

  private static SteamLobby _hostedLobby;

  /// <summary>
  /// The currently active lobby (either hosting or joined).
  /// </summary>
  public static SteamLobby HostedLobby => _hostedLobby;

  /// <summary>
  /// Initializes the Steam client and registers all event handlers.
  /// Must be called before using any other Steam functionality.
  /// </summary>
  /// <remarks>
  /// EXCEPTION HANDLING: Catches and logs exceptions to GD.PrintErr instead of propagating them.
  /// This means Steam initialization failures won't crash the application, but IsActive will
  /// remain false. Always check IsActive before using Steam features.
  ///
  /// ASYNC CALLBACKS: Initializes Steam with asyncCallbacks: true, which means Steam events
  /// are delivered on a background thread. Event handlers must be thread-safe.
  ///
  /// IDEMPOTENCY: Can be called multiple times, but will re-register event handlers each time.
  /// This could lead to duplicate event invocations if called repeatedly.
  /// </remarks>
  public static void Load() {
    try {
      SteamClient.Init(SteamSettings.AppID, asyncCallbacks: true);

      SteamMatchmaking.OnLobbyGameCreated += OnLobbyGameCreatedWithSteamId;
      SteamMatchmaking.OnLobbyCreated += OnLobbyCreated;
      SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoined;
      SteamMatchmaking.OnLobbyMemberDisconnected += OnLobbyMemberDisconnected;
      SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberLeave;
      SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;
      SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequested;

      OnSteamInitialized?.Invoke();
      IsActive = true;
    } catch (Exception ex) {
      GD.PrintErr(ex.Message);
    }
  }

  /// <summary>
  /// Handles disconnection events (hard disconnect from Steam, network loss, etc.)
  /// </summary>
  private static void OnLobbyMemberDisconnected(SteamLobby lobby, Friend friend) {
    OnPlayerLeftLobby?.Invoke(lobby, friend);
  }

  /// <summary>
  /// Handles graceful leave events (player explicitly left the lobby).
  /// </summary>
  private static void OnLobbyMemberLeave(SteamLobby lobby, Friend friend) {
    OnPlayerLeftLobby?.Invoke(lobby, friend);
  }

  /// <summary>
  /// Handles new members joining the lobby.
  /// </summary>
  private static void OnLobbyMemberJoined(SteamLobby lobby, Friend friend) {
    GD.Print("Player Joined Lobby: " + friend.Name);
    OnPlayerJoinLobby?.Invoke(lobby, friend);
  }

  /// <summary>
  /// Handles successful lobby creation result.
  /// </summary>
  /// <remarks>
  /// Only fires the event on Result.OK. Failures are silently ignored.
  /// </remarks>
  private static void OnLobbyCreated(Result result, SteamLobby lobby) {
    if (result == Result.OK) {
      OnLobbySuccessfullyCreated?.Invoke(lobby);
    }
  }

  /// <summary>
  /// Adapter for OnLobbyGameCreated that discards the extra parameters.
  /// </summary>
  private static void OnLobbyGameCreatedWithSteamId(SteamLobby lobby, uint id, ushort port, SteamId steamId) {
    OnLobbyGameCreated?.Invoke(lobby);
  }

  /// <summary>
  /// Creates a new Steam lobby with the local player as host.
  /// </summary>
  /// <returns>Task that completes when the lobby is created</returns>
  /// <remarks>
  /// LOBBY CONFIGURATION:
  /// - Joinable: true (friends can see and join)
  /// - Friends only: true (not visible in public lobby browser)
  /// - Stores host name in lobby metadata as "ownerNameDataString"
  /// </remarks>
  public static async Task CreateLobby() {
    SteamLobby? createLobbyOutput = await SteamMatchmaking.CreateLobbyAsync(MaxLobbySize);

    if (!createLobbyOutput.HasValue) {
      GD.PrintErr("Failed to create lobby: null result");
      return;
    }

    _hostedLobby = createLobbyOutput.Value;
    _hostedLobby.SetJoinable(true);
    _hostedLobby.SetFriendsOnly();
    _hostedLobby.SetData("ownerNameDataString", PlayerName);
  }

  /// <summary>
  /// Handles the local player successfully entering a lobby (either after creating or joining).
  /// </summary>
  /// <remarks>
  /// IMPORTANT BEHAVIOR:
  /// - Fires OnPlayerJoinLobby for ALL existing members, including the local player
  /// - Sets the game server to the lobby owner's Steam ID
  ///
  /// This means OnPlayerJoinLobby will be called multiple times when joining an existing lobby,
  /// once for each member already in the lobby. Subscribers need to handle this.
  /// </remarks>
  private static void OnLobbyEntered(SteamLobby lobby) {
    if (lobby.MemberCount > 0) {
      _hostedLobby = lobby;
      foreach (var item in lobby.Members) {
        OnPlayerJoinLobby?.Invoke(lobby, item);
      }

      lobby.SetGameServer(lobby.Owner.Id);
    }
  }

  /// <summary>
  /// Leaves the current lobby and resets host status.
  /// </summary>
  /// <remarks>
  public static void LeaveLobby() {
    _hostedLobby.Leave();
  }

  /// <summary>
  /// Handles Steam overlay "join game" requests from friends.
  /// </summary>
  /// <remarks>
  /// TODO: Test this case
  /// DUPLICATE EVENTS: After joining, fires OnPlayerJoinLobby for all existing members.
  /// This is the same behavior as OnLobbyEntered, so both events might fire for the same join.
  /// Consider consolidating this logic.
  /// </remarks>
  private static async void OnGameLobbyJoinRequested(SteamLobby lobby, SteamId id) {
    RoomEnter joinSuccessful = await lobby.Join();
    if (joinSuccessful != RoomEnter.Success) {
      GD.PrintErr("Failed to Join Lobby: " + joinSuccessful.ToString());
      return;
    } else {
      _hostedLobby = lobby;

      foreach (Friend friend in lobby.Members) {
        OnPlayerJoinLobby?.Invoke(lobby, friend);
      }
    }
  }

  /// <summary>
  /// Opens the Steam overlay to allow inviting friends to the current lobby.
  /// </summary>
  public static void OpenFriendOverlayForInvite() {
    if (_hostedLobby.Id == 0) {
      GD.PrintErr("Cannot open invite overlay: no active lobby.");
      return;
    }

    SteamFriends.OpenGameInviteOverlay(_hostedLobby.Id);
  }
}
