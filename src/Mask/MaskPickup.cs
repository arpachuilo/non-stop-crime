using Godot;

[Tool]
public partial class MaskPickup : Area3D {
  private MaskData _maskData;

  [Export]
  public MaskData MaskData {
    get => _maskData;
    set {
      _maskData = value;
      ApplyDefinition();
    }
  }

  private Vector3 _pickupSize = new Vector3(1, 1, 1);

  [Export]
  public Vector3 PickupSize {
    get => _pickupSize;
    set {
      _pickupSize = value;
      ApplyDefinition();
    }
  }

  [Signal] public delegate void PickedUpEventHandler(Player player);

  private MeshInstance3D _visualMesh;
  private CollisionShape3D _collisionShape;
  private Sprite3D _iconSprite;

  public override void _Ready() {
    SetupVisuals();
    SetupIconSprite();
    SetupCollision();

    ApplyDefinition();

    BodyEntered += OnBodyEntered;
  }

  private void SetupVisuals() {
    _visualMesh = GetNodeOrNull<MeshInstance3D>("VisualMesh");
    if (_visualMesh == null) {
      _visualMesh = new MeshInstance3D { Name = "VisualMesh" };
      AddChild(_visualMesh);
    }
  }

  private void SetupIconSprite() {
    _iconSprite = GetNodeOrNull<Sprite3D>("IconSprite3D");
  }

  private void SetupCollision() {
    _collisionShape = GetNodeOrNull<CollisionShape3D>("CollisionShape");
    if (_collisionShape == null) {
      _collisionShape = new CollisionShape3D { Name = "CollisionShape" };
      AddChild(_collisionShape);
    }

  }

  private void ApplyDefinition() {
    if (!IsInsideTree())
      return;

    if (_visualMesh == null || _collisionShape == null || _iconSprite == null) {
      CallDeferred(nameof(ApplyDefinition));
      return;
    }

    var sphere = new SphereMesh {
      Radius = _pickupSize.X / 2f,
      Height = _pickupSize.Y
    };
    _visualMesh.Mesh = sphere;

    var shape = new SphereShape3D {
      Radius = _pickupSize.X / 2f
    };
    _collisionShape.Shape = shape;

    UpdateVisualColor();

    _iconSprite.Texture = _maskData?.Icon;
  }

  private void UpdateVisualColor() {
    if (_visualMesh == null) return;

    var material = new StandardMaterial3D {
      Transparency = BaseMaterial3D.TransparencyEnum.Alpha
    };

    if (_maskData != null) {
      var color = _maskData.Color;
      color.A = 0.8f;
      material.AlbedoColor = color;
    } else {
      material.AlbedoColor = new Color(1, 1, 1, 0.8f);
    }

    _visualMesh.MaterialOverride = material;
  }

  private void OnBodyEntered(Node3D body) {
    if (Engine.IsEditorHint()) return;

    if (body is Player player) {
      player.EquipMask(_maskData);
      EmitSignal(SignalName.PickedUp, player);
      QueueFree();
    }
  }
}
