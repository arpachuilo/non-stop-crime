using Godot;
using System.Collections.Generic;

/// <summary>
/// Handle spawning in players for level based joypad
/// </summary>
public partial class LocalMultiplayerLevel : Node
{
	[Export]
	public PackedScene PlayerScene;

	[Export]
	public Node3D SpawnLocation;

	[Export]
	public Node3D PlayerContainer;

	public Dictionary<int, Node> JoypadToPlayer = new();

	public Node KBPlayer = null;

	public override void _Input(InputEvent @event)
	{
		// Handle potential new joypad player
		if (@event is InputEventJoypadButton joypadEvent)
		{
			// Not start
			if (joypadEvent.ButtonIndex != JoyButton.Start)
			{
				return;
			}

			// Already assigned
			if (JoypadToPlayer.ContainsKey(joypadEvent.Device))
			{
				return;
			}

			var player = AddPlayer(joypadEvent.Device);
			JoypadToPlayer[joypadEvent.Device] = player;
		}

		if (@event is InputEventKey keyEvent)
		{
			// Not escape
			if (keyEvent.Keycode != Key.Enter)
			{
				return;
			}

			// Already assigned
			if (KBPlayer != null)
			{
				return;
			}

			var player = AddPlayer(keyEvent.Device, true);
			KBPlayer = player;
		}
	}

	private Node AddPlayer(int deviceId, bool isKB = false)
	{
		var player = PlayerScene.Instantiate() as Player;
		player.PlayerController.DeviceId = deviceId;
		player.PlayerController.IsKB = isKB;
		player.Position = SpawnLocation.Position;
		PlayerContainer.AddChild(player, true);
		return player;
	}

	private void RemovePlayer(int id, bool isKB = false)
	{
		// Handle KB removal
		if (isKB)
		{
			PlayerContainer.RemoveChild(KBPlayer);
			KBPlayer = null;
			return;
		}

		// Handle joypad removal
		if (!JoypadToPlayer.ContainsKey(id)) return;
		var player = JoypadToPlayer[id];
		PlayerContainer.RemoveChild(player);
	}
}
