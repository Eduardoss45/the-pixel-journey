using Godot;
using System;

public partial class Player : CharacterBody2D
{
	private AnimatedSprite2D pixelAnimation;
	private bool canMove = true;

	public const float Speed = 80.0f;
	public const float JumpVelocity = -300.0f;

	public override void _Ready()
	{
		pixelAnimation = GetNode<AnimatedSprite2D>("PixelAnimation");
		base._Ready();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!canMove && IsOnFloor())
		{
			Velocity = Vector2.Zero;
			UpdateAnimation();
			return;
		}

		Vector2 velocity = Velocity;


		if (!IsOnFloor())
			velocity += GetGravity() * (float)delta;


		if (Input.IsActionJustPressed("jump") && IsOnFloor())
			velocity.Y = JumpVelocity;

		float direction = Input.GetAxis("left", "right");

		if (direction != 0)
		{
			velocity.X = direction * Speed;
		}
		else
		{
			velocity.X = 0;
		}

		Velocity = velocity;
		MoveAndSlide();
		UpdateAnimation();
	}

	private enum PlayerState
	{
		Idle,
		Walking,
		Jumping,
		// ! ADICIONAR OUTROS ESTADOS SE NECESSÁRIO
	}

	private PlayerState currentState = PlayerState.Idle;

	private void UpdateAnimation()
	{
		PlayerState newState = DetermineState();

		if (newState != currentState)
		{
			currentState = newState;
			PlayAnimationBasedOnState();
		}

		if (currentState == PlayerState.Walking || currentState == PlayerState.Jumping)
		{
			if (Mathf.Abs(Velocity.X) > 0.1f)
			{
				pixelAnimation.FlipH = Velocity.X < 0;
			}
		}
	}

	private PlayerState DetermineState()
	{
		if (!IsOnFloor())
			return PlayerState.Jumping;

		if (Mathf.Abs(Velocity.X) > 0.1f)
			return PlayerState.Walking;

		return PlayerState.Idle;
	}

	private void PlayAnimationBasedOnState()
	{
		string anim = currentState switch
		{
			PlayerState.Idle => "idle",
			PlayerState.Walking => "walk",
			PlayerState.Jumping => "jump",
			_ => "idle"
		};

		if (pixelAnimation.Animation != anim)
		{
			pixelAnimation.Play(anim);
		}
	}

	public void SetCanMove(bool value)
	{
		canMove = value;
	}
}