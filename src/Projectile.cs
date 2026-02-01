using Godot;

public partial class Projectile : Area3D {
  [Signal] public delegate void HitPlayerEventHandler(Player shooter, Player victim);
  [Export]
  public float Duration = 5.0f;

  [Export]
  public float Speed = 3.0f;

  [Export]
  public AudioStreamPlayer Sfx;

  [Export]
  public Player PlayerOwner;

  [Export]
  public bool DespawnOnCollide = false;

  private Poller _lifetimePoller = new(5.0f);

  public void Own(Player player) {
    PlayerOwner = player;
  }

  public override void _Ready() {
    Sfx?.Play();
    UpdateSpriteDirection();
  }

  private void UpdateSpriteDirection() {
    var sprite = GetNodeOrNull<Sprite3D>("Sprite3D");
    if (sprite == null) return;

    // Check if projectile is moving left (negative X component of forward)
    Vector3 forward = -GlobalTransform.Basis.Z;
    sprite.FlipH = forward.X < 0;
  }

  public override void _EnterTree() {
    _lifetimePoller.Interval = Duration;
    AreaEntered += OnAreaEntered;
    BodyEntered += OnBodyEntered;
  }

  public override void _ExitTree() {
    AreaEntered -= OnAreaEntered;
    BodyEntered -= OnBodyEntered;
  }

  public override void _PhysicsProcess(double delta) {
    base._PhysicsProcess(delta);
    MoveTowards(delta);
    _lifetimePoller?.Poll(QueueFree);
  }

  protected virtual void MoveTowards(double delta) {
    Vector3 forward = -GlobalTransform.Basis.Z;
    GlobalPosition += forward * Speed * (float)delta;
  }

  protected virtual void OnAreaEntered(Area3D area) {
  }

  protected virtual void OnBodyEntered(Node3D body) {
    if (body is Player owner) {
      if (owner == PlayerOwner) return; // Ignore self-hit
    }

    if (body is Player player) {
      PlayerOwner?.AddScoreForPlayerKill();
      EmitSignal(SignalName.HitPlayer, PlayerOwner, player);
      player.Reset();
    } else if (body is NPC npc) {
      if (npc.IsCaptured) return; // Already captured

      PlayerOwner?.AddScoreForNPCCapture();
      npc.OnGoalCaptured(PlayerOwner);
    }

    if (DespawnOnCollide) {
      QueueFree();
    }
  }
}
