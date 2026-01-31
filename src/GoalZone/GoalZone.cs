using System.ComponentModel;
using Chickensoft.Collections;
using Godot;

[Tool]
public partial class GoalZone : Area3D {
  const int NEUTRAL_OWNER_ID = -1;

  [Signal] public delegate void CapturedEventHandler(Player player);

  [Description("Mesh representing the visual size of the goal zone")]
  [Export]
  public MeshInstance3D VisualMesh;

  [Description("Collision shape for the goal zone that triggers captures")]
  [Export]
  public CollisionShape3D CollisionShape;

  [Export]
  public Color NeutralColor { get; set; } = new Color(0.5f, 0.5f, 0.5f, 0.5f);

  [Export]
  public Vector3 ZoneSize { get; set; } = new Vector3(3, 1, 3);

  [Export]
  public int Mask { get; set; } = 0;

  public int OwnerPlayerId { get; private set; } = NEUTRAL_OWNER_ID;
  public bool IsCompleted { get; private set; } = false;

  public override void _Ready() {
    SetupVisuals();
    SetupCollision();

    BodyEntered += OnBodyEntered;
    AreaEntered += OnAreaEntered;
  }

  private void SetupVisuals() {
    VisualMesh ??= GetNodeOrNull<MeshInstance3D>("VisualMesh");

    UpdateVisualColor(NeutralColor);
  }

  private void SetupCollision() {
    CollisionShape ??= GetNodeOrNull<CollisionShape3D>("CollisionShape");
  }

  private void OnBodyEntered(Node3D body) {
    // Don't allow re-capture of completed zones
    if (IsCompleted) return;

    if (body is Player player) {
      Claim(player);
    }
  }

  private void OnAreaEntered(Area3D area) {
    // Don't allow re-capture of completed zones
    if (IsCompleted) return;

    if (area is Projectile projectie) {
      var player = projectie.PlayerOwner;
      Claim(player);
    }
  }

  private void Claim(Player player) {
    // Check if player's mask matches zone's mask using bitwise AND
    // Zone mask 0 = any player can complete (default)
    // Otherwise, player must have all bits set that the zone requires
    if (Mask != 0 && (player.Mask & Mask) != Mask) return;

    int playerId = player.PlayerController.DeviceId;
    if (playerId != OwnerPlayerId) {
      OwnerPlayerId = playerId;
      IsCompleted = true;
      UpdateVisualColor(player.PlayerInfo.UIColor);
      player.AddScore(1);
      EmitSignal(SignalName.Captured, player);
      GD.Print($"Zone captured by Player {playerId}");
    }
  }

  private void UpdateVisualColor(Color playerColor) {
    if (VisualMesh == null) return;

    var material = new StandardMaterial3D();
    material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;

    if (OwnerPlayerId >= 0) {
      material.AlbedoColor = playerColor;
    } else {
      material.AlbedoColor = NeutralColor;
    }

    VisualMesh.MaterialOverride = material;
  }
}
