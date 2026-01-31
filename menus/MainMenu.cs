using Godot;

public partial class MainMenu : Control {
  [Export] private PackedScene gameScene;
  [Export] private PackedScene demoScene;
  [Export] private Button startButton;
  [Export] private Button demoButton;
  [Export] private Button exitButton;
  [Export] private AudioStream bgmStart;

  public override void _Ready() {
    // Re-enable once we have bgm
    // AudioManager.PlaySFX(bgmStart, 1, false, GlobalPosition);
    startButton.ButtonDown += OnStartButtonPressed;
    demoButton.ButtonDown += OnDemoButtonPressed;
    exitButton.ButtonDown += OnExitButtonPressed;
    startButton.GrabFocus();
  }

  private void OnStartButtonPressed() {
    GetTree().ChangeSceneToPacked(gameScene);
  }

  private void OnDemoButtonPressed() {
    GetTree().ChangeSceneToPacked(demoScene);
  }

  private void OnExitButtonPressed() {
    GetTree().Quit();
  }

  public override void _Input(InputEvent @event) {
    if (@event is InputEventJoypadButton joypadEvent) {
      if (joypadEvent.ButtonIndex == JoyButton.A && joypadEvent.Pressed && startButton.HasFocus()) {
        OnStartButtonPressed();
      } else if (joypadEvent.ButtonIndex == JoyButton.A && joypadEvent.Pressed && demoButton.HasFocus()) {
        OnDemoButtonPressed();
      } else if (joypadEvent.ButtonIndex == JoyButton.A && joypadEvent.Pressed && exitButton.HasFocus()) {
        OnExitButtonPressed();
      }
    }

    if (@event.IsActionPressed("ui_accept")) {
      OnStartButtonPressed();
    } else if (@event.IsActionPressed("ui_cancel")) {
      OnExitButtonPressed();
    }
  }
}
