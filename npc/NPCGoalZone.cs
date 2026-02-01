using Godot;

public partial class NPCGoalZone : Area3D
{
    [Signal] public delegate void CapturedEventHandler(Player player);

    [Export] public Vector3 ZoneSize { get; set; } = new Vector3(2, 1, 2);

    private CollisionShape3D _collisionShape;
    private NPC _parentNPC;
    public bool IsCompleted { get; private set; } = false;

    public override void _Ready()
    {
        _parentNPC = GetParent<NPC>();
        SetupCollision();
        BodyEntered += OnBodyEntered;
        AreaEntered += OnAreaEntered;
    }

    private void SetupCollision()
    {
        _collisionShape = new CollisionShape3D { Name = "CollisionShape" };
        var shape = new BoxShape3D { Size = ZoneSize };
        _collisionShape.Shape = shape;
        AddChild(_collisionShape);
    }

    private void OnBodyEntered(Node3D body)
    {
        if (IsCompleted) return;

        if (body is Player player)
        {
            // Only allow contact capture if the player's mask has ContactCapture enabled
            if (player.CurrentMask == null || !player.CurrentMask.ContactCapture) return;

            Capture(player);
        }
    }

    private void OnAreaEntered(Area3D area)
    {
        if (IsCompleted) return;

        if (area is Projectile projectile && projectile.PlayerOwner != null)
        {
            Capture(projectile.PlayerOwner);
        }
    }

    private void Capture(Player player)
    {
        IsCompleted = true;
        EmitSignal(SignalName.Captured, player);
        _parentNPC?.OnGoalCaptured(player);
    }
}
