using Godot;

public static class ColorUtil {
  public static Color Opacity(this Color c, float opacity) {
    c.A = opacity;
    return c;
  }

  public static Vector4 ToVector4(this Color c) {
    return new Vector4(c.R, c.G, c.B, c.A);
  }
}
