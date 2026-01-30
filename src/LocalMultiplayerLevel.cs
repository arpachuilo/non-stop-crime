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

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventJoypadButton joypadEvent)
		{
		  GD.Print($"Joypad Event: Device={joypadEvent.Device}, ButtonIndex={joypadEvent.ButtonIndex}, Pressed={joypadEvent.Pressed}");
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

			AddPlayer(joypadEvent.Device);
		}
	}

	private void AddPlayer(int deviceId)
	{
		var player = PlayerScene.Instantiate() as Player;
		player.PlayerController.DeviceId = deviceId;
		player.Position = SpawnLocation.Position;
		PlayerContainer.AddChild(player, true);
		JoypadToPlayer[deviceId] = player;
	}

	private void RemovePlayer(int id)
	{
		if (!JoypadToPlayer.ContainsKey(id)) return;

		var player = JoypadToPlayer[id];
		PlayerContainer.RemoveChild(player);
	}
}
