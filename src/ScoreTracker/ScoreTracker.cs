using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class ScoreTracker : CanvasLayer
{
  [Export] public VBoxContainer ScoreContainer;
  [Export] public Label TimerLabel;
  [Export] public PackedScene WinnerScreenScene;
  [Export] public float GameDurationSeconds = 600f; // 10 minutes default

  private float _timeRemaining;
  private bool _timerStarted = false;
  private bool _gameEnded = false;

  public override void _Ready()
  {
    _timeRemaining = GameDurationSeconds;
    UpdateTimerDisplay();
  }

  public override void _Process(double delta)
  {
    var players = GetTree().GetNodesInGroup(Group.Player).Cast<Player>().ToList();

    // Start timer when first player spawns
    if (!_timerStarted && players.Count > 0)
    {
      _timerStarted = true;
    }

    // Countdown logic
    if (_timerStarted && !_gameEnded)
    {
      _timeRemaining -= (float)delta;
      if (_timeRemaining <= 0)
      {
        _timeRemaining = 0;
        _gameEnded = true;
        DeclareWinner(players);
      }
      UpdateTimerDisplay();
    }
  }

  private void UpdateTimerDisplay()
  {
    int minutes = (int)(_timeRemaining / 60);
    int seconds = (int)(_timeRemaining % 60);
    TimerLabel.Text = $"{minutes:D2}:{seconds:D2}";
  }

  private void DeclareWinner(List<Player> players)
  {
    string winnerText;
    if (players.Count == 0)
    {
      winnerText = "No players!";
    }
    else
    {
      var winner = players.OrderByDescending(p => p.Score).First();
      var topScore = winner.Score;
      var tied = players.Where(p => p.Score == topScore).ToList();

      if (tied.Count > 1)
      {
        winnerText = "TIE!";
      }
      else
      {
        winnerText = $"Winner: {winner.NamePlate.Text}!";
      }
    }

    WinnerScreen.SetWinnerText(winnerText);
    GetTree().ChangeSceneToPacked(WinnerScreenScene);
  }
}
