using Godot;

public partial class Player : Character {
  [Export]
  public Label3D NamePlate;

  [Export]
  public PlayerController PlayerController { get; set; }

  [Export]
  public SpriteParent SpriteParent { get; set; }

  [Export]
  public int Mask { get; set; } = 0;

  [Export]
  public int Score { get; set; } = 0;

  [Export]
  private Camera3D _camera;

  [Export]
  public Vector3 Spawn;

  [Export]
  public MaskData CurrentMask { get; set; }

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
    SpriteParent ??= GetNode<SpriteParent>("SpriteParent");
    _camera ??= GetViewport().GetCamera3D();
    _baseMaxSpeed = MaxSpeed;

  Visible = false;
  SetProcess(false);
  SetPhysicsProcess(false);
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
  }

  public override void _ExitTree() {
    base._EnterTree();
  }

  public void Reset() {
    GlobalPosition = Spawn;
  }

  public void AddScore(int points) {
    Score += points;
  PlayerInfo.ScoreOrReadyStatus.Text = Score.ToString();
    GD.Print($"Player {PlayerController?.DeviceId} scored {points} points (Total: {Score})");
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

    CurrentMask = mask;
    Mask = mask?.MaskBits ?? 0;  // Update zone access mask

    SpriteParent.ApplyMaskTexture(mask?.Sprite);

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
      AddChild(_projectileEmitter);
    }
  }

  private void RemoveMaskAbilities() {
    // Reset speed
    MaxSpeed = _baseMaxSpeed;

    // Remove projectile emitter if exists
    if (_projectileEmitter != null) {
      _projectileEmitter.QueueFree();
      _projectileEmitter = null;
    }
  }
}
