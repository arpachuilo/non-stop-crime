using Godot;

public partial class ProjectileEmitter : Node3D
{
	[Export]
	public PackedScene ProjectileScene;

	[Export]
	public float FireRate = 0.1f;

	[Export]
	public float ProjectileSpeed = 10.0f;

	[Export]
	public Player Owner;

	private Poller _firePoller = new(1.0f);

	public override void _Ready()
	{
		_firePoller.Interval = FireRate;
	}

	public override void _PhysicsProcess(double delta)
	{
		_firePoller.Poll(FireProjectile);
	}

	protected virtual void FireProjectile()
	{
		if (ProjectileScene == null) return;

		var projectileInstance = ProjectileScene.Instantiate<Projectile>();
		projectileInstance.Own(Owner);
		projectileInstance.Speed = ProjectileSpeed;
		projectileInstance.GlobalTransform = GlobalTransform;
		GetTree().CurrentScene.AddChild(projectileInstance);
	}
}
