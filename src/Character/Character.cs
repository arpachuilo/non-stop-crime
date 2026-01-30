using Godot;

/// <summary>
/// Character class that supports the following physics:
/// - Multiple jumps
/// - Ceiling detection to stop vertical momentum
/// - Terminal velocity
/// - Arbitrary gravity axis alignment
///
/// Implementations are expected to override the following:
/// - GetDirection()
/// - GetJump()
/// - LookAt()
/// </summary>
public partial class Character : CharacterBody3D {
  [Export]
  public Node3D Visual;

  [Export]
  public float Speed { get; set; } = 8.0f;

  [Export]
  public Vector3 Drag { get; set; } = Vector3.One * 10f;

  [Export] private Vector3 _gravity = Vector3.Down * 9.8f;
  [Export] private Vector3 _velocity; // Helper to sync velocity
  [Export] private float _terminalVelocity = 30f;
  protected Vector3 _previousVelocity = Vector3.Zero;
  protected Vector3 _previousPosition = Vector3.Zero;

  [ExportGroup("Debug Settings")]
  [Export] private bool _displayVelocity = false;
  [Export] private bool _displayGravity = false;
  [Export] private bool _displayForward = false;

  public Vector3 PreviousLocation {
    get => _previousPosition;
  }

  public virtual Vector3 GetDirection() {
    return Vector3.Zero;
  }

  public virtual void LookAt() {
    // Create a normalized quaternion for the target direction
    var projectedForward = Vector3.Zero;
    if (_velocity.Length() > 0f) {
      projectedForward = _velocity.Slide(UpDirection).Normalized();
    }

    if (projectedForward == Vector3.Zero) {
      return;
    }

    var projectedPosition = GlobalPosition + projectedForward * 5.0f;
    Visual.LookAt(projectedPosition, UpDirection);
  }

  private void AlignToGravity(float delta) {
    // Get current forward direction (before rotation)
    Vector3 currentForward = -GlobalTransform.Basis.Z;

    // Project current forward onto the plane perpendicular to gravity
    // This preserves the player's forward direction as much as possible
    Vector3 projectedForward = currentForward.Slide(UpDirection).Normalized();

    // If the projection resulted in a zero vector (current forward parallel to gravity)
    // then we need to choose an arbitrary forward direction
    if (projectedForward.LengthSquared() < 0.001f) {
      // Choose an arbitrary reference vector
      Vector3 reference = Vector3.Right;

      // If gravity is nearly parallel to our reference, use a different reference
      if (Mathf.Abs(-UpDirection.Dot(reference)) > 0.9f) {
        reference = Vector3.Forward;
      }

      // Calculate right vector (perpendicular to both up and reference)
      Vector3 right1 = reference.Cross(UpDirection).Normalized();

      // Calculate forward vector (perpendicular to both up and right)
      projectedForward = right1.Cross(UpDirection).Normalized();
    }

    // Create a basis with the right, up, and forward vectors
    Vector3 right = projectedForward.Cross(UpDirection).Normalized();
    Basis newBasis = new(right, UpDirection, -projectedForward);

    // Apply the rotation to the player
    Transform3D newTransform = new(newBasis, GlobalPosition);
    GlobalTransform = GlobalTransform.InterpolateWith(newTransform, delta * 1.5f);
  }

  public override void _PhysicsProcess(double delta) {
    // Get what our "up" vector is
    var up = -GetGravity().Normalized();
    UpDirection = up.IsZeroApprox() ? Vector3.Up : up;

    // Get what gravity should be
    _gravity = GetGravity();
    if (_gravity.IsZeroApprox())
      _gravity = Vector3.Up;

    // Decompose vertical and horizontal motion
    Vector3 horizontalVelocity = _velocity.Slide(UpDirection);
    Vector3 verticalVelocity = _velocity - horizontalVelocity;

    // Handle Ceiling Hits
    if (IsOnCeiling()) {
      verticalVelocity = Vector3.Zero;
    }

    // Handle gravity with terminal velocity
    if (!IsOnFloor()) {
      Vector3 gravityDir = _gravity.Normalized();
      float currentFallSpeed = verticalVelocity.Dot(gravityDir); // Negative because we're falling

      // Only apply more gravity if we haven't reached terminal velocity
      if (currentFallSpeed < _terminalVelocity) {
        float gravityMultiplier = 2.5f; // Increased initial acceleration for snappier feel
        verticalVelocity += gravityMultiplier * _gravity * (float)delta;

        // Clamp to terminal velocity
        if (currentFallSpeed > _terminalVelocity) {
          verticalVelocity = gravityDir * _terminalVelocity;
        }
      }
    }

    // Update horizontal velocity with input direction
    horizontalVelocity = horizontalVelocity / 1.0f + GetDirection() * Speed;

    // Apply drag
    horizontalVelocity /= Vector3.One + Drag * (float)delta;

    // Recombine velocities
    _velocity = horizontalVelocity + verticalVelocity;

    // Update previous and current velocity
    _previousPosition = GlobalPosition;
    _previousVelocity = Velocity;

    // Commit move
    Velocity = _velocity;
    MoveAndSlide();

    // Perform gravity alignment
    AlignToGravity((float)delta);

    // Update visual rotation
    LookAt();

    // Debug
    if (_displayGravity) {
      DebugDraw3D.DrawArrow(GlobalPosition, GlobalPosition + _gravity.LimitLength(2f), Colors.Orange);
    }

    if (_displayVelocity) {
      DebugDraw3D.DrawArrow(GlobalPosition, GlobalPosition + horizontalVelocity.LimitLength(2f), Colors.Red);
      DebugDraw3D.DrawArrow(GlobalPosition, GlobalPosition + verticalVelocity.LimitLength(2f), Colors.Green);
    }

    if (_displayForward) {
      DebugDraw3D.DrawArrow(GlobalPosition, GlobalPosition + Visual.Forward(), Colors.Blue);
    }
  }
}
