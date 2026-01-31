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

  private ShaderMaterial _materialInstance;

  public override void _Ready() {
    SetupVisuals();
    SetupCollision();

    BodyEntered += OnBodyEntered;
    AreaEntered += OnAreaEntered;
  }

  private void SetupVisuals() {
    VisualMesh ??= GetNodeOrNull<MeshInstance3D>("VisualMesh");
    if (VisualMesh == null) return;

    // Get the material currently assigned to the mesh
    var originalMaterial = VisualMesh.GetActiveMaterial(0) as ShaderMaterial;
    if (originalMaterial == null) {
      GD.PushWarning("GoalZone VisualMesh does not have a ShaderMaterial.");
      return;
    }

    // Duplicate so this zone has its own instance
    _materialInstance = (ShaderMaterial)originalMaterial.Duplicate();
    VisualMesh.SetSurfaceOverrideMaterial(0, _materialInstance);

    UpdateVisualColor(NeutralColor);
  }

  private void SetupCollision() {
    CollisionShape ??= GetNodeOrNull<CollisionShape3D>("CollisionShape");
  }

  private void OnBodyEntered(Node3D body) {
    if (IsCompleted) return;

    if (body is Player player) {
      Claim(player);
    }
  }

  private void OnAreaEntered(Area3D area) {
    if (IsCompleted) return;

    if (area is Projectile projectile) {
      var player = projectile.PlayerOwner;
      Claim(player);
    }
  }

  private void Claim(Player player) {
    // Mask logic
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

  private void UpdateVisualColor(Color color) {
    if (_materialInstance == null) return;

    // Shader uniform name must match the shader exactly
    _materialInstance.SetShaderParameter("base_color", color);
  }
}
