using System.Collections.Generic;
using Godot;

/// <summary>
/// Vector3D utilities
/// </summary>
public static class Vector3DUtil {
  /// <summary>
  /// Check if vectors are opposite of each other
  /// </summary>
  public static bool AreOpposite(this Vector3 a, Vector3 b) {
    // Normalize the vectors to ensure we're only comparing directions
    Vector3 normA = a.Normalized();
    Vector3 normB = b.Normalized();

    // Calculate the dot product
    float dot = normA.Dot(normB);

    // If dot product is negative, vectors are pointing in opposite directions
    return dot < 0;
  }

  /// <summary>
  /// Get closest node within list
  /// </summary>
  public static Node3D Closest(this Vector3 a, IEnumerable<Node3D> nodes) {
    Node3D closest = null;
    var closestDistance = float.MaxValue;
    foreach (var point in nodes) {
      if (!point.IsInsideTree())
        continue;
      var dist = a.DistanceTo(point.GlobalPosition);
      if (dist < closestDistance) {
        closestDistance = dist;
        closest = point;
      }
    }

    return closest;
  }
}
