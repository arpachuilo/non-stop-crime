using System.ComponentModel;
using Godot;

public partial class SpriteParent : Node3D {
  [Export]
  public NodePath PlayerPath;

  [Export]
  public Color color = Colors.WhiteSmoke;

  [Description("Lateral speed below which the animation will be paused")]
  [ExportGroup("Animation Tuning")]
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

  [ExportGroup("Sprite Refs")]
  [Export]
  private Player _player;

  [ExportGroup("Sprite Refs")]
  [Export]
  private Sprite3D _head;

  [ExportGroup("Sprite Refs")]
  [Export]
  private AnimatedSprite3D _run;

  [ExportGroup("Sprite Refs")]
  [Export]
  private Sprite3D _mask;

  public override void _Ready() {
    _player = GetNode<Player>(PlayerPath);
    _head = GetNode<Sprite3D>("HeadSprite");
    _run = GetNode<AnimatedSprite3D>("RunCycleSprite");
    _mask = GetNode<Sprite3D>("MaskSprite");

    _head.Modulate = color;
    _run.Modulate = color;
  }

  public override void _Process(double delta) {
    var v = _player.Velocity;

    float lateralSpeed = v.X;
    float planarSpeed = new Vector3(v.X, 0f, v.Z).Length();

    if (Mathf.Abs(lateralSpeed) > Deadzone) {
      bool facingLeft = lateralSpeed < 0f;

      _head.FlipH = facingLeft;
      _run.FlipH = facingLeft;
      _mask.FlipH = facingLeft;
    }

    if (planarSpeed <= Deadzone) {
      _run.Pause();
      return;
    }

    _run.Play();

    float t = Mathf.Clamp(planarSpeed / SpeedForMaxFps, 0f, 1f);
    float targetFps = Mathf.Lerp(MinFps, MaxFps, t);

    _run.SpeedScale = targetFps / Mathf.Max(MinFps, 0.001f);
  }

  public void ApplyMaskTexture(Texture2D maskTexture) {
    if (maskTexture != null) {
      _mask.Texture = maskTexture;
      _mask.Visible = true;
    } else {
      _mask.Visible = false;
    }
  }
}
