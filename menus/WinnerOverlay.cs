using Godot;

public partial class WinnerOverlay : Control {
  [Export] public Label WinnerLabel;
  [Export] public TextureRect WinnerGraphic;
  [Export] public Button MainMenuButton;

  [Export] public string MainMenuPath = "res://menus/MainMenu.tscn";
  private static string _winnerText = "Winner!";
  private static bool _isTie = false;
  private static bool _hasWinner = false;

  public static void SetWinState(string winnerName, bool tie, bool hasWinner) {
    _isTie = tie;
    _hasWinner = hasWinner;

    if (tie) {
      _winnerText = "TIE!";
    } else if (hasWinner) {
      _winnerText = winnerName;
    } else {
      _winnerText = "No players!";
    }
  }

  public override void _Ready() {
    WinnerLabel.Text = _winnerText;
    MainMenuButton.Pressed += OnMainMenuPressed;

    if (_isTie || !_hasWinner) {
      WinnerGraphic.Visible = false;
    }
  }

  public override void _ExitTree() {
    MainMenuButton.Pressed -= OnMainMenuPressed;
  }

  private void OnMainMenuPressed() {
    GetTree().ChangeSceneToFile(MainMenuPath);
  }
}
