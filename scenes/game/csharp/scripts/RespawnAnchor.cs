using Godot;

public partial class RespawnAnchor : Area2D
{
	[Export] public bool OnlyOnce { get; set; } = false;
	[Export] public bool UseXTrigger { get; set; } = false;
	[Export] public float TriggerXRange { get; set; } = 8.0f;

	private bool _activated;
	private float _lastPlayerX;
	private bool _hasLastPlayerX;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
		if (UseXTrigger)
		{
			Monitoring = false;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!UseXTrigger)
			return;

		if (_activated && OnlyOnce)
			return;

		Player player = GetTree().GetFirstNodeInGroup("Player") as Player;
		if (player == null)
			return;

		float playerX = player.GlobalPosition.X;
		float anchorX = GlobalPosition.X;

		if (!_hasLastPlayerX)
		{
			_lastPlayerX = playerX;
			_hasLastPlayerX = true;
			return;
		}

		bool crossed = (_lastPlayerX < anchorX && playerX >= anchorX) ||
					   (_lastPlayerX > anchorX && playerX <= anchorX);
		bool inRange = Mathf.Abs(playerX - anchorX) <= TriggerXRange;

		if (crossed || inRange)
		{
			GameSession.Instance?.SetRespawnPosition(GlobalPosition);
			_activated = true;
		}

		_lastPlayerX = playerX;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (UseXTrigger)
			return;

		if (_activated && OnlyOnce)
			return;

		if (body is not Player)
			return;

		GameSession.Instance?.SetRespawnPosition(GlobalPosition);
		_activated = true;
	}
}
