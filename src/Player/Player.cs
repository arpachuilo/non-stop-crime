using Godot;

public partial class Player : Character {
  [Export]
  public Label3D NamePlate;

  [Export]
  public Sprite3D Avatar;

  [Export]
  public PlayerController PlayerController { get; set; }

  [Export]
  private Node3D _cameraPivot;

  [Export]
  private SpringArm3D _cameraSpring;

  [Export]
  private Camera3D _camera;

  public override void _Ready() {
    PlayerController.InitializeYawPitch(
        _camera.Rotation.X,
        _camera.Rotation.Y
    );
  }

  public override void _Process(double delta) {
    _cameraPivot.Rotation = PlayerController.Look;
  }

  public override void _EnterTree() {
    base._EnterTree();
  }

  public override void _ExitTree() {
    base._EnterTree();
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

  public override void LookAt() {
    // Create a normalized quaternion for the target direction
    Vector3 visualForward = -_camera.GlobalTransform.Basis.Z;
    var projectedForward = visualForward.Slide(UpDirection).Normalized();

    if (projectedForward == Vector3.Zero) {
      return;
    }

    var projectedPosition = GlobalPosition + projectedForward * 5.0f;
    Visual.LookAt(projectedPosition, UpDirection);
  }
}
