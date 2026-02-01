using Godot;

public partial class PlayerInfo : Control {
  [Export]
  public Label NameLabel { get; set; }

  private Color _uiColor;
  [Export]
  public Color UIColor {
    get => _uiColor;
    set {
      _uiColor = value;
      UpdateColors();
    }
  }

  [Export]
  public Label ScoreOrReadyStatus { get; set; }

  [Export]
  public TextureRect MaskIcon { get; set; }

  [Export]
  public TextureRect DefaultMask { get; set; }

  [Export]
  public Sprite2D PlayerHUD { get; set; }

  [Export]
  public PackedScene ScoreAnimationScene { get; set; }

  public bool IsPlaying = false;

  public bool _isReady = false;
  public bool IsReady {
    get => _isReady;
    set {
      _isReady = value;
      ScoreOrReadyStatus.Text = _isReady ? "Ready" : "Not Ready";
    }
  }

  public void AnimateScore(int diff) {
    var animatedScore = ScoreAnimationScene.Instantiate<AnimatedScore>();
    animatedScore.Text = diff > 0 ? $"+{diff}" : diff.ToString();
    animatedScore.Modulate = UIColor;
    AddChild(animatedScore);
  }

  public void UpdateColors() {
    DefaultMask.Modulate = UIColor;
    PlayerHUD.Modulate = UIColor;
    ScoreOrReadyStatus.Modulate = UIColor;
  }

  public void UpdateMaskIcon(Texture2D icon) {
    if (MaskIcon == null) return;

    if (icon != null) {
      DefaultMask.Visible = false;
      MaskIcon.Texture = icon;
      MaskIcon.Visible = true;
      MaskIcon.Modulate = UIColor;
    } else {
      MaskIcon.Visible = false;
      DefaultMask.Visible = true;
    }
  }
}
