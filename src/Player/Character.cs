using Godot;

/// <summary>
/// Character class that supports the following physics:
/// - Ceiling detection to stop vertical momentum
/// - Terminal velocity
/// - Arbitrary gravity axis alignment
///
/// Implementations are expected to override the following:
/// - GetDirection()
/// - LookAt()
/// </summary>
public partial class Character : CharacterBody3D
{
  [Export]
  public Vector3 Drag { get; set; } = Vector3.One * 10f;

  [Export] private Vector3 _gravity = Vector3.Down * 9.8f;
  [Export] private Vector3 _velocity; // Helper to sync velocity
  [Export] private float _terminalVelocity = 30f;
  protected Vector3 _previousVelocity = Vector3.Zero;
  protected Vector3 _previousPosition = Vector3.Zero;

  [Export] public float MaxSpeed { get; set; } = 8.0f;
  [Export] public float MinSpeed { get; set; } = 2.0f;
  [Export] public float AccelerationRate { get; set; } = 1.0f;
  [Export] public float CollisionSpeedPenalty { get; set; } = 0.1f;
  [Export] public float CollisionMinSpeedThreshold { get; set; } = 3.0f;

  [ExportGroup("Debug Settings")]
  [Export] private bool _displayVelocity = false;
  [Export] private bool _displayGravity = false;

  protected float _currentSpeed = 2.0f;
  protected float _collisionCooldown = 0f;
  private const float CollisionCooldownDuration = 0.3f;

  public Vector3 PreviousLocation
  {
    get => _previousPosition;
  }

  public virtual Vector3 GetDirection()
  {
    return Vector3.Zero;
  }

  public override void _PhysicsProcess(double delta)
  {
    // Get what our "up" vector is
    var up = -GetGravity().Normalized();
    var direction = GetDirection();

    // Acceleration
    if (direction != Vector3.Zero)
    {
      _currentSpeed = Mathf.Clamp(Mathf.MoveToward(_currentSpeed, MaxSpeed, AccelerationRate * (float)delta), MinSpeed, MaxSpeed);
    }

    UpDirection = up.IsZeroApprox() ? Vector3.Up : up;

    // Get what gravity should be
    _gravity = GetGravity();
    if (_gravity.IsZeroApprox())
      _gravity = Vector3.Up;

    // Decompose vertical and horizontal motion
    Vector3 horizontalVelocity = _velocity.Slide(UpDirection);
    Vector3 verticalVelocity = _velocity - horizontalVelocity;

    // Handle Ceiling Hits
    if (IsOnCeiling())
    {
      verticalVelocity = Vector3.Zero;
    }

    // Handle gravity with terminal velocity
    if (!IsOnFloor())
    {
      Vector3 gravityDir = _gravity.Normalized();
      float currentFallSpeed = verticalVelocity.Dot(gravityDir); // Negative because we're falling

      // Only apply more gravity if we haven't reached terminal velocity
      if (currentFallSpeed < _terminalVelocity)
      {
        float gravityMultiplier = 2.5f; // Increased initial acceleration for snappier feel
        verticalVelocity += gravityMultiplier * _gravity * (float)delta;

        // Clamp to terminal velocity
        if (currentFallSpeed > _terminalVelocity)
        {
          verticalVelocity = gravityDir * _terminalVelocity;
        }
      }
    }

    // Update horizontal velocity with input direction
    horizontalVelocity = horizontalVelocity / 1.0f + GetDirection().Normalized() * _currentSpeed;

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

    // Deceleration collision detection
    if (_collisionCooldown > 0)
    {
      _collisionCooldown -= (float)delta;
    }

    if (GetSlideCollisionCount() > 0 && _collisionCooldown <= 0)
    {
      for (int i = 0; i < GetSlideCollisionCount(); i++)
      {
        var collision = GetSlideCollision(i);
        var normal = collision.GetNormal();
        float verticalComponent = Mathf.Abs(normal.Dot(UpDirection));

        if (verticalComponent < 0.5f)
        {  // Wall (not floor/ceiling)
          _currentSpeed = Mathf.Max(_currentSpeed * CollisionSpeedPenalty, CollisionMinSpeedThreshold);
          _collisionCooldown = CollisionCooldownDuration;
          break;
        }
      }
    }

    // Debug
    if (_displayGravity)
    {
      DebugDraw3D.DrawArrow(GlobalPosition, GlobalPosition + _gravity.LimitLength(2f), Colors.Orange);
    }

    if (_displayVelocity)
    {
      DebugDraw3D.DrawArrow(GlobalPosition, GlobalPosition + horizontalVelocity.LimitLength(2f), Colors.Red);
      DebugDraw3D.DrawArrow(GlobalPosition, GlobalPosition + verticalVelocity.LimitLength(2f), Colors.Green);
    }
  }
}
