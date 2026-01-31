using Chickensoft.Collections;
using Godot;

[Tool]
public partial class GoalZone : Area3D {
  [Signal] public delegate void CapturedEventHandler(Player player);
  private MeshInstance3D _visualMesh;
  private CollisionShape3D _collisionShape;

  [Export] public Color NeutralColor { get; set; } = new Color(0.5f, 0.5f, 0.5f, 0.5f);
  [Export] public Vector3 ZoneSize { get; set; } = new Vector3(3, 1, 3);
  [Export] public int Mask { get; set; } = 0;
  public int OwnerPlayerId { get; private set; } = -1;  // -1 = neutral
  public bool IsCompleted { get; private set; } = false;

  public override void _Ready() {
    SetupVisuals();
    SetupCollision();

    BodyEntered += OnBodyEntered;
    AreaEntered += OnAreaEntered;
  }

  private void SetupVisuals() {
    _visualMesh = GetNodeOrNull<MeshInstance3D>("VisualMesh");
    if (_visualMesh == null) {
      _visualMesh = new MeshInstance3D();
      _visualMesh.Name = "VisualMesh";
      AddChild(_visualMesh);
    }

    var box = new BoxMesh();
    box.Size = ZoneSize;
    _visualMesh.Mesh = box;

    UpdateVisualColor(NeutralColor);
  }

  private void SetupCollision() {
    _collisionShape = GetNodeOrNull<CollisionShape3D>("CollisionShape");
    if (_collisionShape == null) {
      _collisionShape = new CollisionShape3D();
      _collisionShape.Name = "CollisionShape";
      AddChild(_collisionShape);
    }

    var shape = new BoxShape3D();
    shape.Size = ZoneSize;
    _collisionShape.Shape = shape;
  }

  private void OnBodyEntered(Node3D body) {
    // Don't allow re-capture of completed zones
    if (IsCompleted) return;

    if (body is Player player) {
      // Check if player's mask matches zone's mask using bitwise AND
      // Zone mask 0 = any player can complete (default)
      // Otherwise, player must have all bits set that the zone requires
      if (Mask != 0 && (player.Mask & Mask) != Mask) return;

      int playerId = player.PlayerController.DeviceId;
      if (playerId != OwnerPlayerId) {
        OwnerPlayerId = playerId;
        IsCompleted = true;
        UpdateVisualColor(player.color);
        player.AddScore(1);
        EmitSignal(SignalName.Captured, player);
        GD.Print($"Zone captured by Player {playerId}");
      }
    }
  }

  private void OnAreaEntered(Area3D area) {
    // Don't allow re-capture of completed zones
    if (IsCompleted) return;

    if (area is Projectile projectie) {
      var player = projectie.PlayerOwner;
      // Check if player's mask matches zone's mask using bitwise AND
      // Zone mask 0 = any player can complete (default)
      // Otherwise, player must have all bits set that the zone requires
      if (Mask != 0 && (player.Mask & Mask) != Mask) return;

      int playerId = player.PlayerController.DeviceId;
      if (playerId != OwnerPlayerId) {
        OwnerPlayerId = playerId;
        IsCompleted = true;
        UpdateVisualColor(player.color);
        player.AddScore(1);
        EmitSignal(SignalName.Captured, player);
        GD.Print($"Zone captured by Player {playerId}");
      }
    }
  }

  private void UpdateVisualColor(Color playerColor) {
    if (_visualMesh == null) return;

    var material = new StandardMaterial3D();
    material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;

    if (OwnerPlayerId >= 0) {
      material.AlbedoColor = playerColor;
    } else {
      material.AlbedoColor = NeutralColor;
    }

    _visualMesh.MaterialOverride = material;
  }
}
