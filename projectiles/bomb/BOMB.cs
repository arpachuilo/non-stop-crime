using Godot;

public partial class BOMB : Sprite3D {
  private float _timer = 0f;

  public override void _Process(double delta) {
    _timer += (float)delta;
    SetInstanceShaderParameter("scale", Mathf.Clamp(_timer, 0.0f, 1.0f));
  }
}
