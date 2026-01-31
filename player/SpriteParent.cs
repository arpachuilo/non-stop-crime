using System.ComponentModel;
using Godot;

public partial class SpriteParent : Node3D {
  [Description("Lateral speed below which the animation will be paused")]
  [Export(PropertyHint.Range, "0,0.5,0.01")]
  public float Deadzone = 0.05f;

  [Description("Minimum FPS for the run cycle animation at low speed.")]
  [ExportGroup("Animation Tuning")]
  [Export(PropertyHint.Range, "1,20,1")]
  public float MinFps = 4f;

  [Description("Maximum FPS for the run cycle animation at high speed.")]
  [ExportGroup("Animation Tuning")]
  [Export(PropertyHint.Range, "1,20,1")]
  public float MaxFps = 10f;

  [Description("Speed at which the run cycle reaches maximum FPS.")]
  [ExportGroup("Animation Tuning")]
  [Export(PropertyHint.Range, "1,20,1")]
  public float SpeedForMaxFps = 12f;

  [Export]
  private Player _player;

  [Export]
  private Sprite3D _head;

  [Export]
  private AnimatedSprite3D _run;

  public override void _Ready() {
  }

  public override void _Process(double delta) {
    var v = _player.Velocity;

    float lateralSpeed = v.X;
    float planarSpeed = new Vector3(v.X, 0f, v.Z).Length();

    if (Mathf.Abs(lateralSpeed) > Deadzone) {
      bool facingLeft = lateralSpeed < 0f;

      _head.FlipH = facingLeft;
      _run.FlipH = facingLeft;
    }

    if (planarSpeed <= Deadzone) {
      _run.Pause();
      return;
    }

    _run.Play();

	DebugDraw2D.SetText(_player.NamePlate.Text, $"Speed: {planarSpeed}");

    float t = Mathf.Clamp(planarSpeed / SpeedForMaxFps, 0f, 1f);
    float targetFps = Mathf.Lerp(MinFps, MaxFps, t);

    _run.SpeedScale = targetFps / Mathf.Max(MinFps, 0.001f);
  }
}
