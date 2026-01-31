using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class ScoreTracker : CanvasLayer
{
	[Export] public VBoxContainer ScoreContainer;

	private Dictionary<Player, Label> _playerLabels = new();

	public override void _Process(double delta)
	{
		var players = GetTree().GetNodesInGroup(Group.Player).Cast<Player>().ToList();

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
}
