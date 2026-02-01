using Godot;

[GlobalClass]
[Tool]
public partial class MaskData : Resource
{
    [Export] public string Name { get; set; } = "Default Mask";
    [Export] public int MaskBits { get; set; } = 0;  // For zone access
    [Export] public Color Color { get; set; } = Colors.White;
    [Export] public Texture2D Icon { get; set; }
    [Export] public Texture2D Sprite { get; set; }

    // Ability modifiers
    [Export] public float SpeedMultiplier { get; set; } = 1.0f;
    [Export] public bool HasProjectile { get; set; } = false;
    [Export] public PackedScene ProjectileScene { get; set; }
    [Export] public float FireRate { get; set; } = 0.5f;
    [Export] public bool Scorable {get; set; }
    [Export] public bool HasAnimation { get; set; } = false;
    [Export] public string Animation { get; set; } = null;
    [Export] public bool ContactCapture { get; set; } = false;
}
