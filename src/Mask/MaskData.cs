using Godot;

[GlobalClass]
[Tool]
public partial class MaskData : Resource
{
    [Export] public string Name { get; set; } = "Default Mask";
    [Export] public int MaskBits { get; set; } = 0;  // For zone access
    [Export] public Color Color { get; set; } = Colors.White;
    [Export] public Texture2D Icon { get; set; }

    // Ability modifiers
    [Export] public float SpeedMultiplier { get; set; } = 1.0f;

    // Projectile ability (integrates with existing ProjectileEmitter)
    [Export] public bool HasProjectile { get; set; } = false;
    [Export] public PackedScene ProjectileScene { get; set; }
    [Export] public float ProjectileFireRate { get; set; } = 0.5f;
    [Export] public float ProjectileSpeed { get; set; } = 10.0f;
}
