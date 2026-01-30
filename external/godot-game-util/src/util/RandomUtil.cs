using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Various random generation utilities
///
/// NOTE: Seed is set to 1176213204 when DEBUG is true
/// </summary>
public static class RandomUtil {
  public static RandomNumberGenerator Rng = new RandomNumberGenerator();

  static RandomUtil() {
#if DEBUG
    Rng.Seed = 1176213204;
    GD.Print("Seed (debug): ", Rng.Seed);
#else
      Rng.Seed = GD.Randi();
      GD.Print("Seed: ", Rng.Seed);
#endif
  }

  /// <summary>
  /// Return a new RNG with given seed
  /// </summary>
  public static RandomNumberGenerator WithSeed(ulong seed) {
    return new RandomNumberGenerator() { Seed = seed };
  }

  /// <summary>
  /// Pick random from given list
  /// </summary>
  public static T FromList<T>(IEnumerable<T> list) {
    return Rng.FromList(list);
  }

  /// <summary>
  /// Pick random from given list
  /// </summary>
  public static T FromList<T>(this RandomNumberGenerator rng, IEnumerable<T> list) {
    var i = rng.RandiRange(0, list.Count() - 1);
    return list.Skip(i).First();
  }

  /// <summary>
  /// Generate random vec2 inside circle
  /// </summary>
  /// <param name="radius">Radius of point</param>
  public static Vector2 RandomPointInCircle(float radius) {
    return Rng.RandomPointInCircle(radius);
  }

  /// <summary>
  /// Generate random vec2 inside circle
  /// </summary>
  /// <param name="radius">Radius of point</param>
  public static Vector2 RandomPointInCircle(this RandomNumberGenerator rng, float radius) {
    var theta = rng.Randf() * 2 * Mathf.Pi;
    return new Vector2(Mathf.Cos(theta), Mathf.Sin(theta)) * Mathf.Sqrt(rng.Randf()) * radius;
  }

  /// <summary>
  /// Generate random vec2 inside a square
  /// </summary>
  /// <param name="radius">Size of square</param>
  public static Vector2 RandomPointInSquare(float s) {
    return Rng.RandomPointInRectangle(s, s);
  }

  /// <summary>
  /// Generate random vec2 inside a rectangle
  /// </summary>
  /// <param name="width">Width of the rectangle</param>
  /// <param name="height">Height of the rectangle</param>
  public static Vector2 RandomPointInRectangle(float dx, float dy) {
    return Rng.RandomPointInRectangle(dx, dy);
  }

  /// <summary>
  /// Generate random vec2 inside a rectangle
  /// </summary>
  /// <param name="width">Width of the rectangle</param>
  /// <param name="height">Height of the rectangle</param>
  public static Vector2 RandomPointInRectangle(this RandomNumberGenerator rng, float width, float height) {
    float x = (rng.Randf() - 0.5f) * width;
    float y = (rng.Randf() - 0.5f) * height;
    return new Vector2(x, y);
  }

  /// <summary>
  /// Generate random vec3 inside sphere
  /// </summary>
  /// <param name="radius">Radius of sphere</param>
  public static Vector3 RandomPointInSphere(float radius) {
    return Rng.RandomPointInSphere(radius);
  }

  /// <summary>
  /// Generate random vec3 inside sphere
  /// </summary>
  /// <param name="radius">Radius of sphere</param>
  public static Vector3 RandomPointInSphere(this RandomNumberGenerator rng, float radius) {
    // Generate random spherical coordinates
    float theta = rng.RandfRange(0, Mathf.Pi * 2); // Azimuthal angle
    float phi = Mathf.Acos(rng.RandfRange(-1.0f, 1.0f)); // Polar angle
    float r = Mathf.Pow(rng.Randf(), 1.0f / 3.0f) * radius; // Radius with cubic root for uniform distribution

    // Convert to Cartesian coordinates
    return new Vector3(
        r * Mathf.Sin(phi) * Mathf.Cos(theta),
        r * Mathf.Sin(phi) * Mathf.Sin(theta),
        r * Mathf.Cos(phi)
    );
  }

  /// <summary>
  /// Generate a random vector direction
  /// </summary>
  public static Vector2 RandomDirection() {
    return Rng.RandomDirection();
  }

  /// <summary>
  /// Generate a random vector direction
  /// </summary>
  public static Vector2 RandomDirection(this RandomNumberGenerator rng) {
    float z = rng.RandfRange(-1.0f, 1.0f);
    float theta = rng.RandfRange(0, Mathf.Pi * 2);
    float r = Mathf.Sqrt(1 - z * z);
    return new Vector2(r * Mathf.Cos(theta), r * Mathf.Sin(theta));
  }

  /// <summary>
  /// Shuffle a list in place
  /// </summary>
  public static void Shuffle<T>(this IList<T> list) {
    int n = list.Count;
    while (n > 1) {
      n--;
      int k = Rng.RandiRange(0, n);
      T value = list[k];
      list[k] = list[n];
      list[n] = value;
    }
  }
}
