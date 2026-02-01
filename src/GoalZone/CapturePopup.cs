using Godot;

public partial class CapturePopup : Node3D {
  public Color Color { get; set; } = Colors.White;
  public float Duration { get; set; } = 2.0f;

  private float _elapsed = 0f;
  private MeshInstance3D _mesh;
  private StandardMaterial3D _material;

  public override void _Ready() {
    CreateVisual();
  }

  public override void _Process(double delta) {
    _elapsed += (float)delta;

    float t = _elapsed / Duration;

    if (t >= 1f) {
      QueueFree();
      return;
    }

    // Animation: scale up quickly, then fade out
    float scaleT = Mathf.Min(t * 4f, 1f); // Scale up in first 25% of duration
    float scale = Mathf.Lerp(0.5f, 2.5f, EaseOutBack(scaleT));
    _mesh.Scale = new Vector3(scale, scale, scale);

    // Rise up slightly
    float rise = Mathf.Lerp(0f, 1.5f, EaseOutQuad(t));
    Position = new Vector3(Position.X, Position.Y + rise * (float)delta, Position.Z);

    // Fade out in second half
    float fadeT = Mathf.Max(0f, (t - 0.5f) * 2f);
    float alpha = Mathf.Lerp(1f, 0f, EaseInQuad(fadeT));
    _material.AlbedoColor = new Color(Color.R, Color.G, Color.B, alpha);
    _material.Emission = new Color(Color.R, Color.G, Color.B) * alpha * 3f;
  }

  private void CreateVisual() {
    _mesh = new MeshInstance3D();

    // Create a torus (ring) mesh
    var torus = new TorusMesh();
    torus.InnerRadius = 1.5f;
    torus.OuterRadius = 2.0f;
    torus.Rings = 32;
    torus.RingSegments = 16;
    _mesh.Mesh = torus;

    // Create glowing material
    _material = new StandardMaterial3D();
    _material.AlbedoColor = Color;
    _material.Emission = new Color(Color.R, Color.G, Color.B) * 3f;
    _material.EmissionEnabled = true;
    _material.EmissionEnergyMultiplier = 2f;
    _material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
    _material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
    _material.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
    _mesh.MaterialOverride = _material;

    // Rotate to be horizontal
    _mesh.RotationDegrees = new Vector3(90, 0, 0);
    _mesh.Scale = new Vector3(0.5f, 0.5f, 0.5f);

    AddChild(_mesh);
  }

  private float EaseOutBack(float t) {
    float c1 = 1.70158f;
    float c3 = c1 + 1f;
    return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
  }

  private float EaseOutQuad(float t) {
    return 1f - (1f - t) * (1f - t);
  }

  private float EaseInQuad(float t) {
    return t * t;
  }
}
