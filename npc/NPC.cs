using Godot;

public partial class NPC : Character
{
  [Export]
  public SpriteParent SpriteParent;

  [Export]
  public string NPCName = "NPC";

  public override void _Ready()
  {
    SpriteParent ??= GetNode<SpriteParent>("SpriteParent");
  }
}
