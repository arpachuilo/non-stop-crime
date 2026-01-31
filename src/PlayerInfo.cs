using Godot;

public partial class PlayerInfo : Control {
  [Export]
  public Label NameLabel { get; set; }

  [Export]
  public TextureRect Avatar { get; set; }

  [Export]
  public Color UIColor { get; set; }

  [Export]
  public Label ScoreOrReadyStatus { get; set; }

  public bool IsPlaying = false;

  public bool _isReady = false;
  public bool IsReady {
	get => _isReady;
	set {
	  _isReady = value;
	  ScoreOrReadyStatus.Text = _isReady ? "Ready" : "Not Ready";
	}
  }
}
