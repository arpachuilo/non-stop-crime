using Godot;

public partial class Player : Character {
  [Export]
  public Label3D NamePlate;

  [Export]
  public PlayerController PlayerController { get; set; }

  [Export]
  public int Mask { get; set; } = 0;

  [Export]
  private Camera3D _camera;

  [Export]
  public Vector3 _spawn;

  public override void _Ready() {
    _camera ??= GetViewport().GetCamera3D();
  }

  public override void _Process(double delta) {
  }

  public override void _EnterTree() {
    base._EnterTree();
  }

  public override void _ExitTree() {
    base._EnterTree();
  }

  public void Reset() {
  GlobalPosition = _spawn;
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
}
