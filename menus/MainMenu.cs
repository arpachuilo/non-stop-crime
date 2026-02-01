using Godot;

public partial class MainMenu : Control {
  [Export] private PackedScene gameScene;
  [Export] private PackedScene demoScene;
  [Export] private BaseButton startButton;
  [Export] private BaseButton demoButton;
  [Export] private BaseButton exitButton;
  [Export] private AudioStream bgmStart;

  public override void _Ready() {
    // Re-enable once we have bgm
    // AudioManager.PlaySFX(bgmStart, 1, false, GlobalPosition);
    if (startButton != null) startButton.ButtonDown += OnStartButtonPressed;
    if (demoButton != null) demoButton.ButtonDown += OnDemoButtonPressed;
    if (exitButton != null) exitButton.ButtonDown += OnExitButtonPressed;
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
      } else if (joypadEvent.ButtonIndex == JoyButton.A && joypadEvent.Pressed && demoButton != null && demoButton.HasFocus()) {
        OnDemoButtonPressed();
      } else if (joypadEvent.ButtonIndex == JoyButton.A && joypadEvent.Pressed && exitButton != null && exitButton.HasFocus()) {
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
