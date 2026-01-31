using Godot;

public partial class SpriteParent : Node3D {
  [Export]
  public NodePath PlayerPath;

  [Export]
  public float Deadzone = 0.05f;

  // Animation tuning
  [Export]
  public float MinFps = 6f;
  [Export]
  public float MaxFps = 18f;
  [Export]
  public float SpeedForMaxFps = 6f;

  private CharacterBody3D _player;
  private Sprite3D _head;
  private AnimatedSprite3D _run;

  public override void _Ready() {
    _player = GetNode<CharacterBody3D>(PlayerPath);
    _head = GetNode<Sprite3D>("HeadSprite");
    _run = GetNode<AnimatedSprite3D>("RunCycleSprite");
  }

  public override void _Process(double delta) {
    var v = _player.Velocity;

    // Choose the axis that represents "left/right" in your game.
    // Commonly X. If your game uses Z for left/right, swap to v.Z.
    float lateral = v.X;

    // 1) Flip rig by mirroring parent scale on X so head+body stay consistent
    if (Mathf.Abs(lateral) > Deadzone) {
      bool facingLeft = lateral < 0f;

      _head.FlipH = facingLeft;
      _run.FlipH = facingLeft;
      // Scaling-based solution
      // var s = Scale;
      // s.X = facingLeft ? -Mathf.Abs(s.X) : Mathf.Abs(s.X);
      // Scale = s;
    }

    // 2) Animation rate based on speed
    float speed = Mathf.Abs(lateral);

    if (speed <= Deadzone) {
      _run.Pause();
      return;
    }

    _run.Play();

    float t = Mathf.Clamp(speed / SpeedForMaxFps, 0f, 1f);
    float targetFps = Mathf.Lerp(MinFps, MaxFps, t);

    // AnimatedSprite3D uses SpeedScale as a multiplier.
    // If your SpriteFrames are authored at some base fps (e.g. 12),
    // set SpeedScale relative to that. If you authored at MinFps,
    // this works as-is:
    _run.SpeedScale = targetFps / Mathf.Max(MinFps, 0.001f);
  }
}
