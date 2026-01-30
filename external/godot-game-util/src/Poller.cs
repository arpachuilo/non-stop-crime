using Godot;
using System;

/// <summary>
/// Helper class to execute an action at regular intervals.
/// </summary>
public class Poller(float interval) {
  public float Interval = interval;

  private float _timeAccumulator = 0.0f;

  /// <summary>
  /// Call this method every frame to check if the interval has passed.
  ///
  /// Executes the provided action at each interval.
  /// </summary>
  public void Poll(Action f) {
    var tree = Engine.GetMainLoop() as SceneTree;
    var delta = tree.Root.GetProcessDeltaTime();

    _timeAccumulator += (float) delta;
    if (_timeAccumulator >= Interval) {
      _timeAccumulator -= 0;
      f();
    }
  }
}
