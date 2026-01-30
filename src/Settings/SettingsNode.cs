using Godot;

public partial class SettingsNode : Node {
  public override void _Ready() {
    Settings.Load();
    Settings.Save();
  }
}

