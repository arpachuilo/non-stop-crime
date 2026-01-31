using Godot;

public partial class WinnerScreen : CanvasLayer
{
  [Export] public Label WinnerLabel;
  [Export] public Button MainMenuButton;

  private const string MainMenuPath = "res://menus/MainMenu.tscn";
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
    GetTree().ChangeSceneToFile(MainMenuPath);
  }
}
