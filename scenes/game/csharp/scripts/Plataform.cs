using Godot;
using System;

public partial class Plataform : AnimatableBody2D
{
	[Export] public float MoveSpeed = 80.0f;              // pixels/segundo
	[Export] public int MovementPatternIndex = 1;         // 0 = parado, 1-6 = padrões
	[Export] public Vector2 Offset = new Vector2(0, -75f);
	[Export] public float PauseAfterUp = 0f;
	[Export] public float PauseAfterDown = 0f;
	[Export] public float PauseAfterRight = 0f;
	[Export] public float PauseAfterLeft = 0f;
	[Export] public bool IsLooping = true;

	private bool isMoving = false;
	private float currentPause = 0f;
	private bool goingToTarget = true;
	private Vector2 startPosition;
	private Vector2 targetPosition;
	private Vector2 currentOffset;

	public override void _Ready()
	{
		startPosition = Position;  // ← use Position (local), mais seguro
		SyncToPhysics = true;      // garante sincronia com física (pode marcar no Inspector também)

		if (MovementPatternIndex >= 1 && MovementPatternIndex <= 6)
		{
			ConfigureFromPattern(MovementPatternIndex);
			isMoving = true;
		}
		else
		{
			currentOffset = Offset;
			targetPosition = startPosition + currentOffset;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (currentPause > 0f)
		{
			currentPause -= (float)delta;
			if (currentPause <= 0f && IsLooping)
			{
				SwitchDirection();
				isMoving = true;
			}
			return;
		}

		if (!isMoving) return;

		Vector2 currentTarget = goingToTarget ? targetPosition : startPosition;
		Vector2 toTarget = currentTarget - Position;
		float distance = toTarget.Length();

		if (distance < 1.0f)
		{
			Position = currentTarget;  // snap para precisão
			currentPause = GetPauseForDirection(goingToTarget);

			if (currentPause <= 0f && IsLooping)
			{
				SwitchDirection();
				isMoving = true;
			}
			else
			{
				isMoving = false;  // pausa real → para movimento
			}
			return;
		}

		// Movimento linear suave (delta-timed)
		Vector2 direction = toTarget.Normalized();
		Vector2 moveThisFrame = direction * MoveSpeed * (float)delta;

		// Importante: use Position += (local) ou GlobalPosition += se necessário
		Position += moveThisFrame;
	}

	private void SwitchDirection()
	{
		goingToTarget = !goingToTarget;
	}

	private float GetPauseForDirection(bool toTarget)
	{
		Vector2 dir = toTarget ? currentOffset : -currentOffset;

		// Prioriza horizontal se houver componente X maior
		if (Mathf.Abs(dir.X) > Mathf.Abs(dir.Y))
		{
			return dir.X > 0 ? PauseAfterRight : PauseAfterLeft;
		}
		else
		{
			return dir.Y < 0 ? PauseAfterUp : PauseAfterDown;
		}
	}

	private void ConfigureFromPattern(int index)
	{
		switch (index)
		{
			case 1:
				currentOffset = new Vector2(0, -75f);
				PauseAfterUp = 0f; PauseAfterDown = 0f;
				break;
			case 2:
				currentOffset = new Vector2(0, -75f);
				PauseAfterUp = 1f; PauseAfterDown = 1f;
				break;
			case 3:
				currentOffset = new Vector2(0, -75f);
				PauseAfterUp = 2f; PauseAfterDown = 0f;
				break;
			case 4:
				currentOffset = new Vector2(75f, 0);
				PauseAfterRight = 0f; PauseAfterLeft = 0f;
				break;
			case 5:
				currentOffset = new Vector2(75f, 0);
				PauseAfterRight = 1f; PauseAfterLeft = 1f;
				break;
			case 6:
				currentOffset = new Vector2(75f, 0);
				PauseAfterRight = 2f; PauseAfterLeft = 0f;
				break;
			default:
				GD.PushError("Padrão inválido (1-6)");
				return;
		}

		targetPosition = startPosition + currentOffset;
		IsLooping = true;
	}

	// Métodos públicos (mantidos, mas ajustados para Position)
	public void SetMovementPattern(int index)
	{
		if (index < 1 || index > 6)
		{
			GD.PushError("Padrão deve ser entre 1 e 6");
			return;
		}

		MovementPatternIndex = index;
		ConfigureFromPattern(index);
		goingToTarget = true;
		currentPause = 0f;
		isMoving = true;
	}

	public void ActivateMovement()
	{
		isMoving = true;
	}

	public void ReturnToStart()
	{
		goingToTarget = false;
		currentPause = 0f;
		isMoving = true;
	}

	public void Stop()
	{
		isMoving = false;
		currentPause = 0f;
	}

	public void SetNewOffset(Vector2 newOffset)
	{
		Offset = newOffset;
		currentOffset = newOffset;
		targetPosition = startPosition + currentOffset;
		goingToTarget = true;
		currentPause = 0f;
		isMoving = true;
	}
}