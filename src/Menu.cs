using Godot;

public partial class Menu : Control {
  [Export]
  public PackedScene LevelScene;

  [Export]
  public Button StartButton;

  [Export]
  public Control AvatarsContainer;

  public override void _EnterTree() {
    StartButton.Pressed += ChangeLevel;
  }

  public override void _ExitTree() {
    StartButton.Pressed -= ChangeLevel;
  }

  public override void _Process(double delta) {
    Visible = Input.MouseMode == Input.MouseModeEnum.Visible;
  }

  private void ChangeLevel() {
    LevelManager.ChangeLevel(LevelScene);
	StartButton.Disabled = true;
  }
}
