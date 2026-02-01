using Godot;

public partial class AnimatedScore : Label {
  [Export] public float MoveSpeed = 50.0f;
  [Export] public float DespawnTimer = 2.0f;

  private float _elapsedTime = 0.0f;

  public override void _Process(double delta) {
    // Move upward
    Position += new Vector2(0, -MoveSpeed * (float)delta);

    Modulate = Modulate.Opacity((DespawnTimer - _elapsedTime) / DespawnTimer);

    // Track elapsed time
    _elapsedTime += (float)delta;

    // Despawn after timer expires
    if (_elapsedTime >= DespawnTimer) {
      QueueFree();
    }
  }
}
