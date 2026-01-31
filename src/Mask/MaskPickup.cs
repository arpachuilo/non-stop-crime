using Godot;

[Tool]
public partial class MaskPickup : Area3D
{
    [Export] public MaskData MaskData { get; set; }
    [Export] public Vector3 PickupSize { get; set; } = new Vector3(1, 1, 1);

    [Signal] public delegate void PickedUpEventHandler();

    private MeshInstance3D _visualMesh;
    private CollisionShape3D _collisionShape;

    public override void _Ready()
    {
        SetupVisuals();
        SetupCollision();
        BodyEntered += OnBodyEntered;
    }

    private void SetupVisuals()
    {
        _visualMesh = GetNodeOrNull<MeshInstance3D>("VisualMesh");
        if (_visualMesh == null)
        {
            _visualMesh = new MeshInstance3D();
            _visualMesh.Name = "VisualMesh";
            AddChild(_visualMesh);
        }

        var sphere = new SphereMesh();
        sphere.Radius = PickupSize.X / 2;
        sphere.Height = PickupSize.Y;
        _visualMesh.Mesh = sphere;

        UpdateVisualColor();
    }

    private void SetupCollision()
    {
        _collisionShape = GetNodeOrNull<CollisionShape3D>("CollisionShape");
        if (_collisionShape == null)
        {
            _collisionShape = new CollisionShape3D();
            _collisionShape.Name = "CollisionShape";
            AddChild(_collisionShape);
        }

        var shape = new SphereShape3D();
        shape.Radius = PickupSize.X / 2;
        _collisionShape.Shape = shape;
    }

    private void UpdateVisualColor()
    {
        if (_visualMesh == null) return;

        var material = new StandardMaterial3D();
        material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;

        if (MaskData != null)
        {
            var color = MaskData.Color;
            color.A = 0.8f;
            material.AlbedoColor = color;
        }
        else
        {
            material.AlbedoColor = new Color(1, 1, 1, 0.8f);
        }

        _visualMesh.MaterialOverride = material;
    }

    private void OnBodyEntered(Node3D body)
    {
        if (Engine.IsEditorHint()) return;

        if (body is Player player)
        {
            player.EquipMask(MaskData);
            EmitSignal(SignalName.PickedUp);
            QueueFree();
        }
    }
}
