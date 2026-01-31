using Godot;

public partial class PlayerInfo : Control {
  [Export]
  public Label NameLabel { get; set; }

  [Export]
  public TextureRect Avatar { get; set; }

  [Export]
  public Label Score { get; set; }
}
