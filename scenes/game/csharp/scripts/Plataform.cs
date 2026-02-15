using Godot;
using System;

public partial class Plataform : AnimatableBody2D, IGameMechanism
{
	[Export] public string MechanismId { get; set; } = "";
	[Export] public float MoveSpeed = 80.0f;
	[Export] public int MovementPatternIndex = 0; // 0 = parado, 1 = vertical, 2 = horizontal
	[Export] public float Distance = 75f;         // distância do movimento
	[Export] public bool StartActive = false;

	private bool isMoving = false;
	private bool goingToTarget = true;

	private Vector2 startPosition;
	private Vector2 targetPosition;
	private Vector2 currentOffset;


	public override void _Ready()
	{
		startPosition = Position;
		SyncToPhysics = true;

		MechanismId = MechanismId.Trim();

		ObjectManager.Instance?.Register(this);

		if (MovementPatternIndex == 1)
			SetVerticalLoop();
		else if (MovementPatternIndex == 2)
			SetHorizontalLoop();

		isMoving = StartActive;
	}


	public override void _PhysicsProcess(double delta)
	{
		if (!isMoving)
			return;

		Vector2 currentTarget = goingToTarget ? targetPosition : startPosition;
		Vector2 toTarget = currentTarget - Position;

		if (toTarget.Length() < 1.0f)
		{
			Position = currentTarget;
			goingToTarget = !goingToTarget;
			return;
		}

		Vector2 direction = toTarget.Normalized();
		Position += direction * MoveSpeed * (float)delta;
	}

	private void SetVerticalLoop()
	{
		currentOffset = new Vector2(0, -Distance);
		targetPosition = startPosition + currentOffset;
	}

	private void SetHorizontalLoop()
	{
		currentOffset = new Vector2(Distance, 0);
		targetPosition = startPosition + currentOffset;
	}

	// ========= API Pública =========

	public void SetMovementPattern(int index)
	{
		if (index != 1 && index != 2)
		{
			GD.PushError("Padrão inválido. Use 1 (vertical) ou 2 (horizontal).");
			return;
		}

		MovementPatternIndex = index;

		if (index == 1)
			SetVerticalLoop();
		else
			SetHorizontalLoop();

		goingToTarget = true;
		isMoving = true;
	}

	public void ActivateMovement()
	{
		isMoving = true;
	}

	public void Stop()
	{
		isMoving = false;
	}

	public void ReturnToStart()
	{
		Position = startPosition;
		goingToTarget = true;
	}

	public void ApplyEffect(string effectId, Variant? value = null)
	{
		switch (effectId)
		{
			case "set_pattern":
				if (value.HasValue)
				{
					int pattern = (int)value.Value.AsDouble();
					SetMovementPattern(pattern);
				}
				break;

			case "activate":
				ActivateMovement();
				break;

			case "stop":
				Stop();
				break;
		}
	}
}
