using Godot;

public partial class LevelManager : Node
{
	private static LevelManager _this;

	[Export]
	public Node LevelContainer;

	public override void _EnterTree()
	{
		_this = this;
	}

	public override void _ExitTree()
	{
		_this = null;
	}

	public static void ChangeLevel(PackedScene levelScene)
	{
		if (_this == null)
		{
			GD.PrintErr("LevelManager instance not found in the scene tree.");
			return;
		}

		// Remove existing level if any
		foreach (Node child in _this.LevelContainer.GetChildren())
		{
			child.QueueFree();
		}

		// Instance and add the new level
		Node3D newLevel = levelScene.Instantiate<Node3D>();
		_this.LevelContainer.AddChild(newLevel);
	}
}
