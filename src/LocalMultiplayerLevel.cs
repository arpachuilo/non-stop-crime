using Godot;

/// <summary>
/// Handle spawning in players for level based joypad
/// </summary>
public partial class LocalMultiplayerLevel : Node {
  [Export]
  public PackedScene PlayerScene;

  [Export]
  public Node3D SpawnLocation;

  [Export]
  public Node3D PlayerContainer;

  public override void _Ready() {
    if (!Multiplayer.IsServer()) return;

    // Spawn already connected players
    foreach (var peerID in Multiplayer.GetPeers()) {
      AddPlayer(peerID);
    }

    // Spawn local player
    if (!OS.HasFeature("dedicated_server")) {
      AddPlayer(Multiplayer.GetUniqueId());
    }
  }

  public override void _EnterTree() {
    if (!Multiplayer.IsServer()) return;

    Multiplayer.PeerConnected += AddPlayer;
    Multiplayer.PeerDisconnected += RemovePlayer;
  }

  public override void _ExitTree() {
    if (!Multiplayer.IsServer()) return;

    Multiplayer.PeerConnected -= AddPlayer;
    Multiplayer.PeerDisconnected -= RemovePlayer;
  }

  private void AddPlayer(long id) {
    var player = PlayerScene.Instantiate() as Player;
    player.Name = id.ToString();
    player.Position = SpawnLocation.Position;
    PlayerContainer.AddChild(player);
  }

  private void RemovePlayer(long id) {
    if (!PlayerContainer.HasNode(id.ToString())) return;

    PlayerContainer.GetNode(id.ToString()).QueueFree();
  }
}
