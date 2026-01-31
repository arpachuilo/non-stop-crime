using Godot;
using System.Collections.Generic;
using System.Linq;

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

  [Export]
  public PackedScene PlayerInfoScene;

  [Export]
  public Control PlayerInfoContainer;

  public Dictionary<Player, PlayerInfo> PlayerToInfo = [];

  public Dictionary<int, Player> JoypadToPlayer = [];

  public Player KBPlayer = null;

  public List<Player> Players
  {
    get => [.. JoypadToPlayer.Values, .. KBPlayer != null ? new[] { KBPlayer } : []];
  }

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

  private Player AddPlayer(int deviceId, bool isKB = false)
  {
    // Add player
    var player = PlayerScene.Instantiate() as Player;
    player.PlayerController.DeviceId = deviceId;
    player.PlayerController.IsKB = isKB;
    player.Position = SpawnLocation.Position;
    player.NamePlate.Text = RandomUtil.FromList(Canned.PlayerNames.Except(Players.Select(p => p.NamePlate.Text)));
    PlayerContainer.AddChild(player, true);

    // Add player info
    var playerInfo = PlayerInfoScene.Instantiate() as PlayerInfo;
    playerInfo.NameLabel.Text = player.NamePlate.Text;
    var image = AvatarGenerator.NextAvatar(playerInfo.NameLabel.Text);
    image.Resize(64, 64);
    playerInfo.Avatar.Texture = ImageTexture.CreateFromImage(image);
    PlayerToInfo[player] = playerInfo;
    PlayerInfoContainer.AddChild(playerInfo);

	player.PlayerInfo = playerInfo;
    return player;
  }

  private void RemovePlayer(int id, bool isKB = false)
  {
    var player = isKB ? KBPlayer : JoypadToPlayer.GetValueOrDefault(id, null);

    if (player == null) return;

    var info = PlayerToInfo[player];
    PlayerInfoContainer.RemoveChild(info); // Remove info from scene
    PlayerContainer.RemoveChild(player); // Remove player from scene

    // Cleanup supporting structures
    PlayerToInfo.Remove(player);
    if (isKB)
    {
      KBPlayer = null;
    }
    else
    {
      JoypadToPlayer.Remove(id);
    }
  }
}
