using Godot;

/// <summary>
/// Allows host to change level
/// </summary>
public partial class MultiplayerLevelManager : Node {
  private static MultiplayerLevelManager _this;

  [Export]
  private Node _levelContainer;

  [Export]
  private MultiplayerSpawner _spawner;

  public override void _Ready() {
    _this = this;
  }

  /// <summary>
  /// Attempt to change the requested scene
  /// </summary>
  public static void ChangeLevel(PackedScene scene) {
    if (!_this.Multiplayer.IsServer()) return;

    var tryLoad = false;
    var spawner = _this._spawner;
    for (int i = 0; i < spawner.GetSpawnableSceneCount(); i++) {
      var spawnableScene = spawner.GetSpawnableScene(i);
      if (scene.ResourcePath == spawnableScene) {
        tryLoad = true;
      }
    }

    if (!tryLoad) {
      GD.PrintErr("Tried to load a scene that is not registered as spawnable: " + scene.ResourcePath);
      return;
    }

    Callable.From(() => {
      var level = _this._levelContainer;
      foreach (Node child in level.GetChildren()) {
        level.RemoveChild(child);
        child.QueueFree();
      }

      level.AddChild(scene.Instantiate());
    }).CallDeferred();
  }
}
