using Godot;

public partial class MaskSpriteBob : Sprite3D {
  [Export] public float RotationSpeed = 1.0f;   // radians per second
  [Export] public float BobAmplitude = 0.20f;   // units
  [Export] public float BobFrequency = 0.5f;    // cycles per second

  private Vector3 _basePosition;
  private float _time;

  public override void _Ready() {
    _basePosition = Position;
  }

  public override void _Process(double delta) {
    float dt = (float)delta;
    _time += dt;

    RotateY(RotationSpeed * dt);

    float bobOffset = Mathf.Sin(_time * Mathf.Tau * BobFrequency) * BobAmplitude;
    Position = _basePosition + Vector3.Up * bobOffset;
  }
}
