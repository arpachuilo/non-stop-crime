using Godot;
using System.Linq;

public partial class DynamicCamera : Camera3D {
  [Export]
  public Vector3 BaseOffset = new Vector3(0, 6, 8);

  [Export]
  public float MinDistance = 10.0f;

  [Export]
  public float MaxDistance = 30.0f;

  [Export]
  public float SmoothSpeed = 5.0f;

  private Vector3 _basePosition;

  public override void _Ready() {
    _basePosition = GlobalPosition;
  }

  public override void _Process(double delta) {
    var players = GetTree().GetNodesInGroup(Group.Player).Cast<Node3D>().ToList();

    if (!players.Any()) return;

    Vector3 averagePosition = Vector3.Zero;
    float maxSpread = 0f;

    foreach (var player in players) {
      averagePosition += player.GlobalPosition;
    }

    averagePosition /= players.Count();

    // Calculate max distance between players for dynamic zoom
    foreach (var player in players) {
      float distance = player.GlobalPosition.DistanceTo(averagePosition);
      maxSpread = Mathf.Max(maxSpread, distance);
    }

    // Calculate dynamic distance based on player spread
    float targetDistance = Mathf.Clamp(maxSpread * 2.0f, MinDistance, MaxDistance);

    // Calculate target position with base offset
    Vector3 targetPosition = averagePosition + BaseOffset.Normalized() * targetDistance;

    // Smooth movement
    GlobalPosition = GlobalPosition.Lerp(targetPosition, (float)(SmoothSpeed * delta));
  }
}
