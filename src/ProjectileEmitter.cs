using Godot;

public partial class ProjectileEmitter : Node3D {
  [Export]
  public PackedScene ProjectileScene;

  [Export]
  public float FireRate = 0.5f;

  [Export]
  public Player PlayerOwner;

  private Poller _firePoller = new(1.0f);

  public override void _Ready() {
    _firePoller.Interval = FireRate;
  }

  public override void _PhysicsProcess(double delta) {
    _firePoller.Poll(FireProjectile);
  }

  protected virtual void FireProjectile() {
    if (ProjectileScene == null) return;

    var projectileInstance = ProjectileScene.Instantiate<Projectile>();
    projectileInstance.Own(PlayerOwner);

    // Spawn transform based on player velocity
    Vector3 forward = -PlayerOwner.Velocity.Normalized();
    if (forward.Length() < 0.01f)
      forward = -projectileInstance.Transform.Basis.Z;

    Vector3 up = Vector3.Up;
    if (Mathf.Abs(forward.Dot(up)) > 0.99f) // Prevent degenerate cross product
      up = Vector3.Right;

    Vector3 right = up.Cross(forward).Normalized();
    up = forward.Cross(right).Normalized();

    projectileInstance.Transform = new Transform3D(
        new Basis(right, up, forward),
        PlayerOwner.GlobalPosition
    );

    GetTree().CurrentScene.AddChild(projectileInstance);
  }
}
