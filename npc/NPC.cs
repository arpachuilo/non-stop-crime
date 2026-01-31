using Godot;
using System.Linq;

public partial class NPC : Character
{
  [Export]
  public SpriteParent SpriteParent;

  [Export]
  public string NPCName = "NPC";

  [Export]
  public float AggroRadius = 10f;

  public override void _Ready()
  {
    SpriteParent ??= GetNode<SpriteParent>("SpriteParent");
  }

  public override Vector3 GetDirection()
  {
    var players = GetTree().GetNodesInGroup(Group.Player).Cast<Player>().ToList();
    Player closest = null;
    var minRadius = AggroRadius * 2f;
    foreach (var player in players)
    {
      var distance = GlobalPosition.DistanceTo(player.GlobalPosition);
      if (distance < AggroRadius && distance < minRadius)
      {
        closest = player;
        minRadius = distance;
      }
    }

    if (closest != null)
    {
      return -(closest.GlobalPosition - GlobalPosition).Normalized();
    }

    // Do a random walk if no player is within aggro radius
    return RandomUtil.RandomDirection().Normalized()._X0Y();
  }

  public bool IsCaptured { get; private set; } = false;
  public Player CapturedBy { get; private set; } = null;

  public void OnGoalCaptured(Player player)
  {
    IsCaptured = true;
    CapturedBy = player;
    // TODO: Change sprite/state when captured
    GD.Print($"NPC {NPCName} was captured by player {player.PlayerController.DeviceId}");
  }

  public void Kill()
  {
    var timer = new Timer
    {
      Autostart = true,
      WaitTime = 0.5f
    };

    timer.Timeout += () =>
    {
      QueueFree();
    };
  }
}
