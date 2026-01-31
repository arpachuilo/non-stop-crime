using Godot;

/// <summary>
/// Canned list of things generated from .txt files
/// </summary>
public static class Canned {
  public static string[] SpicyNames { get; private set; }
  public static string[] SafeNames { get; private set; }

  public static string[] PlayerNames {
    get {
      if (UseSafeNames) {
        return SafeNames;
      } else {
        return SpicyNames;
      }
    }
  }

  public static bool UseSafeNames { get; set; } = false;

  private static string[] LoadNames(string filepath) {
    using var file = FileAccess.Open(filepath, FileAccess.ModeFlags.Read);
    string content = file.GetAsText();
    return content.Split("\n");
  }

  static Canned() {
    SpicyNames = LoadNames("res://src/Canned/player_names.txt");
    SafeNames = LoadNames("res://src/Canned/safe_names.txt");
  }
}
