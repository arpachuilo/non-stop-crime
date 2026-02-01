using Godot;

public partial class MaskSpriteBob : Sprite3D {
  [Export] public float RotationSpeed = 1.0f;   // radians per second
  [Export] public float BobAmplitude = 0.20f;   // units
  [Export] public float BobFrequency = 0.5f;    // cycles per second

  private Vector3 _basePosition;
  private float _time;
  private float _offsetRotate;
  private float _offsetBob;

  public override void _Ready() {
    _basePosition = Position;

    // Random offset so not all the masks are bobbing in sync
    _offsetRotate = (0.5f - GD.Randf()) / 10;
    _offsetBob = (0.5f - GD.Randf()) / 10;
  }

  public override void _Process(double delta) {
    float dt = (float)delta;
    _time += dt;

    RotateY((RotationSpeed + _offsetRotate) * dt);

    float bobOffset = Mathf.Sin(_time * Mathf.Tau * (BobFrequency + _offsetBob)) * BobAmplitude;
    Position = _basePosition + Vector3.Up * bobOffset;
  }
}
