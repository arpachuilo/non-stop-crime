using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class Lobby : Node {
  [Signal] public delegate void GameStartedEventHandler();

  [Export] public PackedScene PlayerScene;
  [Export] public Node3D PlayerContainer;
  [Export] public PackedScene PlayerInfoScene;
  [Export] public Control PlayerInfoContainer;
  [Export] public Label Safetymode;
  [Export] public int MinPlayersToStart { get; set; } = 2;
  [Export] public Godot.Collections.Array<Color> PlayerColors { get; set; } = [
    Colors.IndianRed,
    Colors.Azure,
    Colors.LimeGreen,
    Colors.LightYellow
  ];

  [Export] public Godot.Collections.Array<Node3D> SpawnPoints { get; set; } = new();

  [Export] public GoalZoneSpawner GoalZoneSpawner;
  [Export] public MaskSpawner MaskSpawner;
  [Export] public LobbyOverlay LobbyOverlay;

  public Dictionary<int, Player> JoypadToPlayer = new();
  public Player KBPlayer = null;
  public List<Player> Players {
    get => [.. JoypadToPlayer.Values, .. KBPlayer != null ? new[] { KBPlayer } : []];
  }

  private bool _gameStarted = false;
  private int _nextSpawnIndex = 0;
  private List<Color> _availableColors = new();
  private RandomNumberGenerator _rng = new();

  public override void _Ready() {
    _rng.Randomize();
    _availableColors = new List<Color>(PlayerColors);
    Safetymode.Text = Canned.UseSafeNames ? "S" : "";
    if (GoalZoneSpawner != null)
      GoalZoneSpawner.PlayerSpawnLocations = SpawnPoints;
    if (MaskSpawner != null)
      MaskSpawner.PlayerSpawnLocations = SpawnPoints;

    // Hide the in-round playerinfo until we start
    PlayerInfoContainer.Visible = false;
  }

  public override void _Input(InputEvent @event) {
    if (@event is InputEventKey hiddenEvent && hiddenEvent.Keycode == Key.Key1) {
      if (hiddenEvent.IsReleased()) {
        Canned.UseSafeNames = !Canned.UseSafeNames;
        Safetymode.Text = Canned.UseSafeNames ? "S" : "";
      }

      return;
    }

    if (@event is InputEventJoypadButton joypadEvent && joypadEvent.ButtonIndex == JoyButton.Start && joypadEvent.Pressed) {
      if (_gameStarted) {
        ReturnToLobby();
        return;
      }
      HandleJoypadInput(joypadEvent.Device);
    } else if (@event is InputEventKey keyEvent && keyEvent.Pressed) {
      if (keyEvent.Keycode == Key.Escape && _gameStarted) {
        ReturnToLobby();
        return;
      }
      if (keyEvent.Keycode == Key.Enter && !_gameStarted) {
        HandleKeyboardInput();
      }
    }
  }

  private void ReturnToLobby() {
    GetTree().ReloadCurrentScene();
  }

  private void HandleJoypadInput(int deviceId) {
    if (JoypadToPlayer.TryGetValue(deviceId, out var player)) {
      ToggleReady(player);
    } else {
      AddPlayer(deviceId, false);
    }
  }

  private void HandleKeyboardInput() {
    if (KBPlayer != null) {
      ToggleReady(KBPlayer);
    } else {
      AddPlayer(-1, true);
    }
  }

  private void ToggleReady(Player player) {
    player.PlayerInfo.IsReady = !player.PlayerInfo.IsReady;

    LobbyOverlay?.SetPlayerReadyState(player, player.PlayerInfo.IsReady);

    CheckStartCondition();
  }

  private void AddPlayer(int deviceId, bool isKB) {
    var spawnPosition = GetNextSpawnPosition();

    var playerInfo = PlayerInfoScene.Instantiate<PlayerInfo>();
    PlayerInfoContainer.AddChild(playerInfo);

    var player = PlayerScene.Instantiate<Player>();
    player.PlayerController.DeviceId = deviceId;
    player.PlayerController.IsKB = isKB;
    player.Position = spawnPosition;
    player.Spawn = spawnPosition;
    player.SpawnPoints = new System.Collections.Generic.List<Node3D>(SpawnPoints);

    string uniqueName = GetUniqueName();
    player.NamePlate.Text = uniqueName;
    playerInfo.NameLabel.Text = uniqueName;
    playerInfo.ScoreOrReadyStatus.Text = "Not Ready";
    player.PlayerInfo = playerInfo;

    var playerColor = GetNextColor();
    if (!isKB) Input.SetJoyLight(deviceId, playerColor);
    player.PlayerInfo.UIColor = playerColor;
    player.color = playerColor;
    PlayerContainer.AddChild(player);

    if (isKB) {
      KBPlayer = player;
    } else {
      JoypadToPlayer[deviceId] = player;
    }

    LobbyOverlay?.SetPlayerActiveState(player, true);
    LobbyOverlay?.SetPlayerReadyState(player, false);
    LobbyOverlay?.SetPlayerName(player, uniqueName);
    LobbyOverlay?.SetPlayerColor(player, playerColor);
    LobbyOverlay?.SetPlayerPortrait(player);
  }

  private Vector3 GetNextSpawnPosition() {
    if (SpawnPoints.Count == 0)
      return new Vector3(0, 1, 0);

    var pos = SpawnPoints[_nextSpawnIndex % SpawnPoints.Count].GlobalPosition;
    _nextSpawnIndex++;
    return pos;
  }

  private string GetUniqueName() {
    var usedNames = Players.Select(p => p.NamePlate.Text);
    return RandomUtil.FromList(Canned.PlayerNames.Except(usedNames));
  }

  private Color GetNextColor() {
    if (_availableColors.Count == 0)
      _availableColors = new List<Color>(PlayerColors);

    int index = _rng.RandiRange(0, _availableColors.Count - 1);
    var color = _availableColors[index];
    _availableColors.RemoveAt(index);
    return color;
  }

  private void CheckStartCondition() {
    if (Players.Count < MinPlayersToStart) return;
    if (!Players.All(p => p.PlayerInfo.IsReady)) return;

    StartGame();
  }

  private void StartGame() {
    _gameStarted = true;

    if (LobbyOverlay != null)
      LobbyOverlay.Visible = false;

    PlayerInfoContainer.Visible = true;

    foreach (var player in Players) {
      player.StartPlaying();
    }

    EmitSignal(SignalName.GameStarted);
  }
}
