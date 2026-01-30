using Godot;

public partial class PlayerController : Node
{
	[Export]
	public Vector3 Direction { get; set; } = Vector3.Zero;

	public int DeviceId { get; set; } = -1;
	public bool IsKB { get; set; } = false;

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
		var movement = GetMovement();
		if (movement.Length() > 0.0f)
		{
			Direction = new Vector3(movement.X, 0f, movement.Y).Normalized();
			_previousDirection = Direction.Normalized();
		}
		else
		{
			Direction = _previousDirection;
		}
	}

	public Vector2 GetMovement()
	{
		if (IsKB)
		{
			return new(
				Input.GetAxis(InputActions.move_left_kb, InputActions.move_right_kb),
				Input.GetAxis(InputActions.move_fwd_kb, InputActions.move_back_kb)
			);
		}

		return new(
			Input.GetJoyAxis(DeviceId, JoyAxis.LeftX),
			Input.GetJoyAxis(DeviceId, JoyAxis.LeftY)
		);
	}
}
