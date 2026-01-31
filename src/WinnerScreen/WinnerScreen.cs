using Godot;

public partial class WinnerScreen : CanvasLayer
{
  [Export] public Label WinnerLabel;
  [Export] public Button MainMenuButton;
  [Export] public PackedScene MainMenuScene;

  private static string _winnerText = "Winner!";

  public static void SetWinnerText(string text)
  {
    _winnerText = text;
  }

  public override void _Ready()
  {
    WinnerLabel.Text = _winnerText;
    MainMenuButton.Pressed += OnMainMenuPressed;
  }

  public override void _ExitTree()
  {
    MainMenuButton.Pressed -= OnMainMenuPressed;
  }

  private void OnMainMenuPressed()
  {
    GD.Print(MainMenuScene.ResourcePath);
    GetTree().ChangeSceneToPacked(MainMenuScene);
  }
}
