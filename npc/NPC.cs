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

    return Vector3.Zero;
  }

  public void Kill() {
    var timer = new Timer
    {
      Autostart = true,
      WaitTime = 0.5f
    };

    timer.Timeout += () => {
      QueueFree();
    };
  }
}
