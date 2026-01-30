using Godot;

public partial class PlayerController : MultiplayerSynchronizer {
  [Export]
  public Vector3 Direction { get; set; } = Vector3.Zero;

  [Export]
  public Vector3 Look { get; set; } = Vector3.Zero;

  [Export]
  public bool Jumping { get; set; } = false;

  [ExportGroup("Camera Settings")]
  [Export] private float _sensX = 100.0f;
  [Export] private float _sensY = 100.0f;

  [Export(PropertyHint.Range, "-360,360,radians_as_degrees")]
  private float _lookAngleMin = Mathf.DegToRad(-70f);

  [Export(PropertyHint.Range, "-360,360,radians_as_degrees")]
  private float _lookAngleMax = Mathf.DegToRad(70f);

  private Vector2 _cameraRotation = Vector2.Zero;
  private float _pitch = 0.0f;
  private float _yaw = 0.0f;

  public override void _Ready() {
    SetProcess(GetMultiplayerAuthority() == Multiplayer.GetUniqueId());
    SetPhysicsProcess(GetMultiplayerAuthority() == Multiplayer.GetUniqueId());
    SetProcessInput(GetMultiplayerAuthority() == Multiplayer.GetUniqueId());
  }

  public void InitializeYawPitch(float x, float y) {
    _pitch = x;
    _yaw = y;
  }

  public override void _Input(InputEvent @event) {
    if (@event is InputEventMouseMotion mouseMotion) {
      if (Input.MouseModeEnum.Captured == Input.MouseMode) {
        _cameraRotation.X = -mouseMotion.Relative.X * 0.005f;
        _cameraRotation.Y = -mouseMotion.Relative.Y * 0.005f;
      }
    }
  }

  [Rpc(CallLocal = true)]
  private void Jump() {
    Jumping = true;
  }

  private void MoveCamera(float delta) {
    // Update pitch and yaw angles
    var x = _cameraRotation.X;
    var y = _cameraRotation.Y;
    _pitch = (_pitch + x * _sensX * delta) % Mathf.Tau;
    _yaw = Mathf.Clamp(_yaw + y * _sensY * delta, _lookAngleMin, _lookAngleMax);

    var rotation = Quaternion.FromEuler(new Vector3(_yaw, _pitch, 0));
    Look = rotation.GetEuler();
    _cameraRotation = Vector2.Zero;
  }

  public override void _Process(double delta) {
    if (Input.IsActionJustPressed("ui_cancel")) {
      Input.MouseMode = Input.MouseModeEnum.Captured == Input.MouseMode
        ? Input.MouseModeEnum.Visible
        : Input.MouseModeEnum.Captured;
    }

    // Handle jump
    if (Input.IsActionJustPressed(InputActions.jump)) {
      Rpc(MethodName.Jump);
    }

    // Get movement input
    Vector2 movementInput = Input.GetVector(
        InputActions.move_left, InputActions.move_right,
        InputActions.move_fwd, InputActions.move_back
    );

    Direction = new Vector3(movementInput.X, 0, movementInput.Y);

    // Get movement input for WASD
    var lookInput = Input.GetVector(
        InputActions.look_left, InputActions.look_right,
        InputActions.look_up, InputActions.look_down
    );

    _cameraRotation += lookInput;

    MoveCamera((float)delta);
  }
}
