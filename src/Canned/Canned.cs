using Godot;

/// <summary>
/// Canned list of things generated from .txt files
/// </summary>
public static class Canned {
  public static string[] PlayerNames { get; private set; }
  public static string[] SafeNames { get; private set; }

  private static string[] LoadNames(string filepath) {
    using var file = FileAccess.Open(filepath, FileAccess.ModeFlags.Read);
    string content = file.GetAsText();
    return content.Split("\n");
  }

  static Canned() {
    PlayerNames = LoadNames("res://src/Canned/player_names.txt");
    SafeNames = LoadNames("res://src/Canned/safe_names.txt");
  }
}
