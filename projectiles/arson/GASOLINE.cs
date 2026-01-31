using Godot;

public partial class GASOLINE : AnimatedSprite3D {
  [Export]
  public float DecayTime { get; set; } = 0.5f;

  private float _timer = 0f;

  public override void _Ready() {
    _timer = DecayTime;
  }

  public override void _Process(double delta) {
    _timer -= (float)delta;
    SetInstanceShaderParameter("scale", Mathf.Clamp(_timer / DecayTime, 0.2f, 1.0f));
  }
}
