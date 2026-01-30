using Godot;
using System.Collections.Generic;

/// <summary>
/// Utilities for Node3D
/// </summary>
public static class Node3DUtil {
  /// <summary>
  /// Get forward direction of Node3D
  /// </summary>
  public static Vector3 Forward(this Node3D node) {
    return -node.GlobalTransform.Basis.Z;
  }

  /// <summary>
  /// Distance between nodes
  /// <summary>
  public static float DistanceTo(this Node3D a, Node3D b) {
    var from = a.GlobalPosition;
    var to = b.GlobalPosition;

    return from.DistanceTo(to);
  }

  /// <summary>
  /// Get closest Node3D from given list
  /// </summary>
  public static Node3D Closest(this Node3D node, IEnumerable<Node3D> nodes) {
    Node3D closest = null;
    float closestDist = float.MaxValue;
    foreach (var n in nodes) {
      var dist = node.GlobalPosition.DistanceTo(n.GlobalPosition);
      if (dist < closestDist) {
        closestDist = dist;
        closest = n;
      }
    }

    return closest;
  }
}
