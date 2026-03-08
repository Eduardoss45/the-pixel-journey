using Godot;
using System;

public partial class Plataform : AnimatableBody2D, IGameMechanism
{
	[Export] public string MechanismId { get; set; } = "";
	[Export] public float MoveSpeed = 80.0f;
	[Export] public int MovementPatternIndex = 0;
	[Export] public float Distance = 75f;
	[Export] public bool StartActive = false;

	private bool isMoving = false;
	private bool goingToTarget = true;
	private bool isOneWay = false; 

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
		else if (MovementPatternIndex == 3)
			SetOneWayUp();
		else if (MovementPatternIndex == 4)
			SetOneWayDown();

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
			
			if (isOneWay)
			{
				
				isMoving = false;
			}
			else
			{
				
				goingToTarget = !goingToTarget;
			}
			return;
		}

		Vector2 direction = toTarget.Normalized();
		Position += direction * MoveSpeed * (float)delta;
	}

	private void SetVerticalLoop()
	{
		currentOffset = new Vector2(0, -Distance);
		targetPosition = startPosition + currentOffset;
		isOneWay = false;
	}

	private void SetHorizontalLoop()
	{
		currentOffset = new Vector2(Distance, 0);
		targetPosition = startPosition + currentOffset;
		isOneWay = false;
	}

	
	private void SetOneWayUp()
	{
		currentOffset = new Vector2(0, -Distance);
		targetPosition = startPosition + currentOffset;
		isOneWay = true;
		goingToTarget = true; 
	}

	
	private void SetOneWayDown()
	{
		currentOffset = new Vector2(0, Distance);
		targetPosition = startPosition + currentOffset;
		isOneWay = true;
		goingToTarget = true; 
	}



	public void SetMovementPattern(int index)
	{
		if (index < 1 || index > 4)
		{
			GD.PushError("Padrão inválido. Use 1 (vertical loop), 2 (horizontal loop), 3 (one-way up) ou 4 (one-way down).");
			return;
		}

		MovementPatternIndex = index;

		if (index == 1)
			SetVerticalLoop();
		else if (index == 2)
			SetHorizontalLoop();
		else if (index == 3)
			SetOneWayUp();
		else if (index == 4)
			SetOneWayDown();

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