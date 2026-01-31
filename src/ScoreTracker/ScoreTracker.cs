using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class ScoreTracker : CanvasLayer
{
	[Export] public VBoxContainer ScoreContainer;
	[Export] public Label TimerLabel;
	[Export] public Label WinnerLabel;
	[Export] public float GameDurationSeconds = 600f; // 10 minutes default

	private Dictionary<Player, Label> _playerLabels = new();
	private float _timeRemaining;
	private bool _timerStarted = false;
	private bool _gameEnded = false;

	public override void _Ready()
	{
		_timeRemaining = GameDurationSeconds;
		WinnerLabel.Visible = false;
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

		// Add labels for new players
		foreach (var player in players)
		{
			if (!_playerLabels.ContainsKey(player))
			{
				var label = new Label();
				ScoreContainer.AddChild(label);
				_playerLabels[player] = label;
			}

			// Update score display
			_playerLabels[player].Text = $"{player.NamePlate.Text}: {player.Score}";
		}

		// Remove labels for removed players
		var toRemove = _playerLabels.Keys.Where(p => !players.Contains(p)).ToList();
		foreach (var player in toRemove)
		{
			_playerLabels[player].QueueFree();
			_playerLabels.Remove(player);
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
		if (players.Count == 0)
		{
			WinnerLabel.Text = "No players!";
		}
		else
		{
			var winner = players.OrderByDescending(p => p.Score).First();
			var topScore = winner.Score;
			var tied = players.Where(p => p.Score == topScore).ToList();

			if (tied.Count > 1)
			{
				WinnerLabel.Text = "TIE!";
			}
			else
			{
				WinnerLabel.Text = $"Winner: {winner.NamePlate.Text}!";
			}
		}
		WinnerLabel.Visible = true;
	}
}
