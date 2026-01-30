using Godot;

public partial class SteamSettings : Node {
  private static SteamSettings _this;
  [Export]
  public uint SteamAppID = 480;

  public static uint AppID {
    get => _this.SteamAppID;
  }

  public override void _EnterTree() {
    base._EnterTree();
    _this = this;
  }
}
