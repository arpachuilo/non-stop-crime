using Godot;

public partial class Projectile : Area3D
{
	[Export]
	public float Duration = 5.0f;

	[Export]
	public float Speed = 10.0f;

	[Export]
	public Player Owner;

	private Poller _lifetimePoller = new(5.0f);

	public void Own(Player player)
	{
		Owner = player;
	}

	public override void _EnterTree()
	{
		_lifetimePoller.Interval = Duration;
		AreaEntered += OnAreaEntered;
		BodyEntered += OnBodyEntered;
	}

	public override void _ExitTree()
	{
		AreaEntered -= OnAreaEntered;
		BodyEntered -= OnBodyEntered;
	}

	public override void _PhysicsProcess(double delta)
	{
		MoveTowards(delta);
		_lifetimePoller?.Poll(QueueFree);
	}

	protected virtual void MoveTowards(double delta)
	{
		Vector3 forward = -GlobalTransform.Basis.Z;
		GlobalPosition += forward * Speed * (float)delta;
	}

	protected virtual void OnAreaEntered(Area3D area)
	{

	}

	protected virtual void OnBodyEntered(Node3D body)
	{
		GD.Print("Projectile hit body: " + body.Name);
		if (body is Player player)
		{
			player.Reset();
		}
	}
}
