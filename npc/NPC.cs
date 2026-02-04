using Godot;
using System.Linq;

public partial class NPC : Character {
  [Export]
  public SpriteParent SpriteParent;

  [Export]
  public string NPCName = "NPC";

  [Export]
  public float AggroRadius = 10f;

  [Export]
  public Godot.Collections.Array<AudioStream> VoiceClips { get; set; } = new();

  [Export]
  public AudioStreamPlayer3D VoicePlayer { get; set; }

  [Export]
  public AudioStreamPlayer3D OofSfx { get; set; }

  private static Texture2D _deathTexture;
  private static Texture2D _happyFaceTexture;
  private static Texture2D _scaredFaceTexture;
  private static RandomNumberGenerator _rng;

  private bool _isScared = false;

  static NPC() {
    _rng = new RandomNumberGenerator();
    _rng.Randomize();
  }

  public override void _Ready() {
    SpriteParent ??= GetNode<SpriteParent>("SpriteParent");
    VoicePlayer ??= GetNodeOrNull<AudioStreamPlayer3D>("VoicePlayer");
    _deathTexture ??= GD.Load<Texture2D>("res://player/assets/death.png");
    _happyFaceTexture ??= GD.Load<Texture2D>("res://npc/faces/npc_happy.png");
    _scaredFaceTexture ??= GD.Load<Texture2D>("res://npc/faces/npc_scared.png");

    // Apply happy face on spawn
    SpriteParent?.ApplyMaskTexture(_happyFaceTexture);

    PlayRandomVoice();
  }

  private void PlayRandomVoice() {
    if (VoiceClips.Count == 0 || VoicePlayer == null) return;

    int index = _rng.RandiRange(0, VoiceClips.Count - 1);
    VoicePlayer.Stream = VoiceClips[index];
    VoicePlayer.PitchScale = _rng.RandfRange(0.8f, 1.3f);
    VoicePlayer.Play();
  }

  private void UpdateFaceExpression(bool playerNearby) {
    if (playerNearby && !_isScared) {
      _isScared = true;
      SpriteParent?.ApplyMaskTexture(_scaredFaceTexture);
    } else if (!playerNearby && _isScared) {
      _isScared = false;
      SpriteParent?.ApplyMaskTexture(_happyFaceTexture);
    }
  }

  public override Vector3 GetDirection() {
    if (IsCaptured)
      return Vector3.Zero;

    var players = GetTree().GetNodesInGroup(Group.Player).Cast<Player>().ToList();
    Player closest = null;
    var minRadius = AggroRadius * 2f;
    foreach (var player in players) {
      var distance = GlobalPosition.DistanceTo(player.GlobalPosition);
      if (distance < AggroRadius && distance < minRadius) {
        closest = player;
        minRadius = distance;
      }
    }

    if (closest != null) {
      UpdateFaceExpression(true);
      return -(closest.GlobalPosition - GlobalPosition).Normalized();
    }

    // Do a random walk if no player is within aggro radius
    UpdateFaceExpression(false);
    return RandomUtil.RandomDirection().Normalized()._X0Y();
  }

  public bool IsCaptured { get; private set; } = false;
  public Player CapturedBy { get; private set; } = null;

  public override void _PhysicsProcess(double delta) {
    if (_lockOnFloor && IsOnFloor()) {
      AxisLockLinearX = true;
      AxisLockLinearY = true;
      AxisLockLinearZ = true;
      SetPhysicsProcess(false);
      SetProcess(false);
    }

    base._PhysicsProcess(delta);
  }

  private bool _lockOnFloor = false;
  public void OnGoalCaptured(Player player) {
    OofSfx?.Play();
    IsCaptured = true;
    CapturedBy = player;
    SpriteParent?.ShowDeath(_deathTexture);
    _lockOnFloor = true;
    CollisionLayer = 0;
    CollisionMask = 0;
    GD.Print($"NPC {NPCName} was captured by player {player.PlayerController.DeviceId}");
  }

  public void Kill() {
    var timer = new Timer {
      Autostart = true,
      WaitTime = 0.5f
    };

    timer.Timeout += () => {
      QueueFree();
    };
  }
}
