using Godot;
using System.Collections.Generic;

public partial class Player : Character {
  [Signal] public delegate void PlayerResetEventHandler();

  // Scoring constants - all point values defined here
  public const int PointsPerPlayerKill = 5;
  public const int PointsPerNPCCapture = 1;

  [Export]
  public Label3D NamePlate;

  [Export]
  public PlayerController PlayerController { get; set; }

  [Export]
  public SpriteParent SpriteParent { get; set; }

  [Export]
  public int Mask { get; set; } = 0;

  private int _score = 0;
  [Export]
  public int Score {
    get => _score;
    set {
        int diff = value - _score;
        _score = value;
        PlayerInfo.AnimateScore(diff);
    }
  }

  [Export]
  private Camera3D _camera;

  [Export]
  public Vector3 Spawn;

  public List<Node3D> SpawnPoints { get; set; }
  private RandomNumberGenerator _rng = new();

  [Export]
  public MaskData CurrentMask { get; set; }

  [Export]
  public OmniLight3D Light;

  [Export]
  public AudioStreamPlayer3D OuchSfx;

  [Export]
  public AudioStreamPlayer3D PickupSfx;

  private ProjectileEmitter _projectileEmitter;
  private float _baseMaxSpeed;

  [ExportGroup("Appearance")]
  [Export]
  public Color color = Colors.Gray;

  [ExportGroup("Appearance")]
  [Export]
  public Color labelColor = Colors.White;

  public PlayerInfo PlayerInfo { get; set; }

  public override void _Ready() {
    _rng.Randomize();
    SpriteParent ??= GetNode<SpriteParent>("SpriteParent");
    _camera ??= GetViewport().GetCamera3D();
    _baseMaxSpeed = MaxSpeed;

    if (PlayerInfo.IsPlaying) {
      StartPlaying();
    } else {
      Visible = false;
      SetProcess(false);
      SetPhysicsProcess(false);
    }
  }

  public void StartPlaying() {
    PlayerInfo.IsReady = true;
    PlayerInfo.IsPlaying = true;
    PlayerInfo.ScoreOrReadyStatus.Text = Score.ToString();
    Visible = true;
    SetProcess(true);
    SetPhysicsProcess(true);
  }

  public override void _Process(double delta) {
  }

  public override void _EnterTree() {
    base._EnterTree();
    SpriteParent.color = color;
    NamePlate.Modulate = labelColor;
    Light.LightColor = PlayerInfo.UIColor;
  }

  public override void _ExitTree() {
    base._EnterTree();
  }

  private bool _isDead = false;

  public void Reset() {
    if (_isDead) return;

    _isDead = true;
    SpawnCorpse();
    GlobalPosition = Spawn;
    EquipMask(null);
    EmitSignal(SignalName.PlayerReset);
    OuchSfx?.Play();

    // Hide player and disable light
    SpriteParent.Visible = false;
    Light.Visible = false;
    SetPhysicsProcess(false);

    // Controller vibration
    if (!PlayerController.IsKB) {
      Input.StartJoyVibration(PlayerController.DeviceId, 0.5f, 0.5f, 1.2f);
    }

    // Respawn after 3 seconds
    var timer = GetTree().CreateTimer(3.0f);
    timer.Timeout += Respawn;
  }

  private void SpawnCorpse() {
    var corpse = new PlayerCorpse();
    corpse.Color = color;
    GetTree().CurrentScene.AddChild(corpse);
    corpse.GlobalPosition = GlobalPosition;
    GD.Print($"Corpse placed at {corpse.GlobalPosition}");
  }

  private void Respawn() {
    GlobalPosition = GetRandomSpawnPosition();
    SpriteParent.Visible = true;
    Light.Visible = true;
    SetPhysicsProcess(true);
    _isDead = false;
  }

  private Vector3 GetRandomSpawnPosition() {
    if (SpawnPoints == null || SpawnPoints.Count == 0)
      return Spawn;

    int index = _rng.RandiRange(0, SpawnPoints.Count - 1);
    return SpawnPoints[index].GlobalPosition;
  }

  public void AddScore(int points) {
    Score += points;
    PlayerInfo.ScoreOrReadyStatus.Text = Score.ToString();
    GD.Print($"Player {PlayerController?.DeviceId} scored {points} points (Total: {Score})");
  }

  public void AddScoreForPlayerKill() {
    AddScore(PointsPerPlayerKill);
  }

  public void AddScoreForNPCCapture() {
    AddScore(PointsPerNPCCapture);
  }

  public override Vector3 GetDirection() {
    var direction = PlayerController.Direction;

    // Get camera's forward and right vectors
    Vector3 cameraForward = _camera.GlobalTransform.Basis.Z;
    Vector3 cameraRight = _camera.GlobalTransform.Basis.X;

    // Project camera vectors onto the plane perpendicular to gravity
    Vector3 forward = cameraForward.Slide(UpDirection).Normalized();
    Vector3 right = cameraRight.Slide(UpDirection).Normalized();

    // Calculate movement direction in the gravity plane
    return (forward * direction.Z + right * direction.X).Normalized();
  }

  public void EquipMask(MaskData mask) {
    // Remove previous abilities
    RemoveMaskAbilities();

    PickupSfx?.Play();

    CurrentMask = mask;
    Mask = mask?.MaskBits ?? 0;  // Update zone access mask

    SpriteParent.ApplyMaskTexture(mask?.Sprite);
    PlayerInfo?.UpdateMaskIcon(mask?.Icon);

    ApplyMaskAbilities();
  }

  private void ApplyMaskAbilities() {
    if (CurrentMask == null) return;

    // Speed modifier
    MaxSpeed = _baseMaxSpeed * CurrentMask.SpeedMultiplier;

    // Projectile ability
    if (CurrentMask.HasProjectile && CurrentMask.ProjectileScene != null) {
      _projectileEmitter = new ProjectileEmitter();
      _projectileEmitter.PlayerOwner = this;
      _projectileEmitter.ProjectileScene = CurrentMask.ProjectileScene;
      _projectileEmitter.FireRate = CurrentMask.FireRate;
      AddChild(_projectileEmitter);
    }

    if (CurrentMask.HasAnimation && CurrentMask.Animation != null) {
      SpriteParent.AnimateBody(CurrentMask.Animation);
    }
  }

  private void RemoveMaskAbilities() {
    SpriteParent.AnimateBody();

    // Reset speed
    MaxSpeed = _baseMaxSpeed;

    // Remove projectile emitter if exists
    if (_projectileEmitter != null) {
      _projectileEmitter.QueueFree();
      _projectileEmitter = null;
    }
  }
}
