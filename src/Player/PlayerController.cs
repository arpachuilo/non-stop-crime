using Godot;

public partial class PlayerController : Node
{
	[Export]
	public Vector3 Direction { get; set; } = Vector3.Zero;

	public int DeviceId { get; set; } = -1;

	private Vector3 _previousDirection = Vector3.Zero;

	public override void _Ready() { }

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventJoypadButton joypadEvent)
		{
			if (joypadEvent.Device != DeviceId) return;
		}
	}

	public override void _Process(double delta)
	{
		float x = Input.GetJoyAxis(DeviceId, JoyAxis.LeftX);
		float y = Input.GetJoyAxis(DeviceId, JoyAxis.LeftY);
		Vector2 movementInput = new(x, y);

		if (movementInput.Length() > 0.1f)
		{
			Direction = new Vector3(movementInput.X, 0f, movementInput.Y).Normalized();
			_previousDirection = Direction.Normalized();
		}
		else
		{
			Direction = _previousDirection;
		}
	}
}
