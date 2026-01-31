using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class Lobby : Node {
    [Signal] public delegate void GameStartedEventHandler();

    [Export] public PackedScene PlayerScene;
    [Export] public Node3D PlayerContainer;
    [Export] public PackedScene PlayerInfoScene;
    [Export] public Control PlayerInfoContainer;
    [Export] public int MinPlayersToStart { get; set; } = 2;

    [Export] public Godot.Collections.Array<Node3D> SpawnPoints { get; set; } = new();

    [Export] public GoalZoneSpawner GoalZoneSpawner;
    [Export] public MaskSpawner MaskSpawner;
    [Export] public Control LobbyOverlay;

    public Dictionary<int, Player> JoypadToPlayer = new();
    public Player KBPlayer = null;
    public List<Player> Players {
        get => [.. JoypadToPlayer.Values, .. KBPlayer != null ? new[] { KBPlayer } : []];
    }

    private bool _gameStarted = false;
    private int _nextSpawnIndex = 0;

    public override void _Ready() {
        if (GoalZoneSpawner != null)
            GoalZoneSpawner.PlayerSpawnLocations = SpawnPoints;
        if (MaskSpawner != null)
            MaskSpawner.PlayerSpawnLocations = SpawnPoints;
    }

    public override void _Input(InputEvent @event) {
        if (_gameStarted) return;

        if (@event is InputEventJoypadButton joypadEvent && joypadEvent.ButtonIndex == JoyButton.Start && joypadEvent.Pressed) {
            HandleJoypadInput(joypadEvent.Device);
        } else if (@event is InputEventKey keyEvent && keyEvent.Keycode == Key.Enter && keyEvent.Pressed) {
            HandleKeyboardInput();
        }
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
        CheckStartCondition();
    }

    private void AddPlayer(int deviceId, bool isKB) {
        var spawnPosition = GetNextSpawnPosition();

        var playerInfo = PlayerInfoScene.Instantiate<PlayerInfo>();
        var image = AvatarGenerator.NextAvatar(playerInfo.NameLabel.Text);
        image.Resize(64, 64);
        playerInfo.Avatar.Texture = ImageTexture.CreateFromImage(image);
        PlayerInfoContainer.AddChild(playerInfo);

        var player = PlayerScene.Instantiate<Player>();
        player.PlayerController.DeviceId = deviceId;
        player.PlayerController.IsKB = isKB;
        player.Position = spawnPosition;
        player.Spawn = spawnPosition;
        player.NamePlate.Text = GetUniqueName();
        playerInfo.NameLabel.Text = player.NamePlate.Text;
        player.PlayerInfo = playerInfo;
        PlayerContainer.AddChild(player);

        if (isKB)
            KBPlayer = player;
        else
            JoypadToPlayer[deviceId] = player;
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

    private void CheckStartCondition() {
        if (Players.Count < MinPlayersToStart) return;
        if (!Players.All(p => p.PlayerInfo.IsReady)) return;

        StartGame();
    }

    private void StartGame() {
        _gameStarted = true;

        if (LobbyOverlay != null)
            LobbyOverlay.Visible = false;

        foreach (var player in Players) {
            player.StartPlaying();
        }

        EmitSignal(SignalName.GameStarted);
    }
}
