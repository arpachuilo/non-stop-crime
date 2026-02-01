using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class ScoreTracker : Control {
  [Export] public VBoxContainer ScoreContainer;
  [Export] public Label TimerLabel;
  [Export] public PackedScene WinnerScreenScene;
  [Export] public float GameDurationSeconds = 600f; // 10 minutes default
  [Export] public int RequiredNumberOfPlayers = 1;

  private float _timeRemaining;
  private bool _gameEnded = false;

  [Export] public bool TimerActive { get; set; } = false;

  public override void _Ready() {
    _timeRemaining = GameDurationSeconds;
    UpdateTimerDisplay();
  }

  public override void _Process(double delta) {
    if (!TimerActive || _gameEnded) return;

    _timeRemaining -= (float)delta;
    if (_timeRemaining <= 0) {
      _timeRemaining = 0;
      _gameEnded = true;
      var players = GetTree().GetNodesInGroup(Group.Player).Cast<Player>().ToList();
      DeclareWinner(players);
    }
    UpdateTimerDisplay();
  }

  public void StartTimer() {
    TimerActive = true;
  }

  private void _on_lobby_game_started() {
    StartTimer();
  }

  private void UpdateTimerDisplay() {
    int minutes = (int)(_timeRemaining / 60);
    int seconds = (int)(_timeRemaining % 60);
    TimerLabel.Text = $"{minutes:D2}:{seconds:D2}";
  }

  private void DeclareWinner(List<Player> players) {
    string winnerName = "";
    bool hasTie = false;
    bool hasWinner = false;

    var color = Colors.Black;
    if (players.Count == 0) {
      winnerName = "No players!";
    } else {
      var winner = players.OrderByDescending(p => p.Score).First();
      var topScore = winner.Score;
      var tied = players.Where(p => p.Score == topScore).ToList();

      if (tied.Count > 1) {
        hasTie = true;
      } else {
        winnerName = winner.NamePlate.Text;
        hasWinner = true;
        color = winner.PlayerInfo.UIColor;
      }
    }

    WinnerOverlay.SetWinState(winnerName, hasTie, hasWinner, color);
    GetTree().ChangeSceneToPacked(WinnerScreenScene);
  }
}
