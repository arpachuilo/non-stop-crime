using Godot;
using System.Linq;

/// <summary>
/// Utility functions extension for nodes.
/// </summary>
public static class NodeUtil {

  /// <summary>
  /// Sort children by name.
  /// </summary>
  public static void Sort(this Node node) {
    if (node.GetChildren().Count == 0)
      return;

    var children = node.GetChildren().OrderBy(child => child.Name.ToString()).ToList();
    for (int i = 0; i < children.Count; i++) {
      node.MoveChild(children[i], i);
    }
  }
}
