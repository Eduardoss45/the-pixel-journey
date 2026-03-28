using Godot;

public partial class Player : CharacterBody2D
{
	private enum PlayerState
	{
		Idle,
		Walk,
		Jump,
		Fall,
		Duck,
		Slide,
		Wall,
		Swimming,
		Hurt
	}

	private AnimatedSprite2D _anim = null!;
	private CollisionShape2D _collisionShape = null!;
	private CollisionShape2D _hitboxCollision = null!;
	private RayCast2D _leftWallDetector = null!;
	private RayCast2D _rightWallDetector = null!;
	private Timer _reloadTimer = null!;

	private bool _canMove = true;
	private bool _deathTriggered;
	private int _jumpCount;
	private float _direction;
	private PlayerState _status;
	private float _forceAirRemaining;

	[Signal]
	public delegate void DeathTriggeredEventHandler();

	[Export] public float DeathYThreshold { get; set; } = 300.0f;

	[Export] public float MaxSpeed { get; set; } = 150.0f;
	[Export] public float Acceleration { get; set; } = 400.0f;
	[Export] public float Deceleration { get; set; } = 400.0f;
	[Export] public float SlideDeceleration { get; set; } = 100.0f;
	[Export] public float WallAcceleration { get; set; } = 40.0f;
	[Export] public float WallJumpVelocity { get; set; } = 240.0f;
	[Export] public float WaterMaxSpeed { get; set; } = 100.0f;
	[Export] public float WaterAcceleration { get; set; } = 200.0f;
	[Export] public float WaterJumpForce { get; set; } = -100.0f;
	[Export] public int MaxJumpCount { get; set; } = 2;
	[Export] public float JumpGraceSeconds { get; set; } = 0.1f;

	public const float JumpVelocity = -300.0f;

	public override void _Ready()
	{
		_anim = GetNode<AnimatedSprite2D>("PixelAnimation");
		_collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
		_hitboxCollision = GetNode<CollisionShape2D>("Hitbox/CollisionShape2D");
		_leftWallDetector = GetNode<RayCast2D>("LeftWallDetector");
		_rightWallDetector = GetNode<RayCast2D>("RightWallDetector");
		_reloadTimer = GetNode<Timer>("ReloadTimer");

		GoToIdleState();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_forceAirRemaining > 0.0f)
		{
			_forceAirRemaining = Mathf.Max(0.0f, _forceAirRemaining - (float)delta);
		}

		if (!_canMove)
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

		switch (_status)
		{
			case PlayerState.Idle:
				IdleState((float)delta);
				break;
			case PlayerState.Walk:
				WalkState((float)delta);
				break;
			case PlayerState.Jump:
				JumpState((float)delta);
				break;
			case PlayerState.Fall:
				FallState((float)delta);
				break;
			case PlayerState.Duck:
				DuckState((float)delta);
				break;
			case PlayerState.Slide:
				SlideState((float)delta);
				break;
			case PlayerState.Wall:
				WallState((float)delta);
				break;
			case PlayerState.Swimming:
				SwimmingState((float)delta);
				break;
			case PlayerState.Hurt:
				HurtState((float)delta);
				break;
		}

		MoveAndSlide();

		if (!_deathTriggered && GlobalPosition.Y > DeathYThreshold)
		{
			_deathTriggered = true;
			_canMove = false;
			EmitSignal(SignalName.DeathTriggered);
		}

		if (_deathTriggered)
		{
			ZIndex = GlobalPosition.Y > 178f ? -4 : 0;
		}
		else
		{
			ZIndex = 0;
		}
	}

	private void GoToIdleState()
	{
		_status = PlayerState.Idle;
		_anim.Play("idle");
	}

	private void GoToWalkState()
	{
		_status = PlayerState.Walk;
		_anim.Play("walk");
	}

	private void GoToJumpState()
	{
		_status = PlayerState.Jump;
		_anim.Play("jump");
		Velocity = new Vector2(Velocity.X, JumpVelocity);
		_jumpCount += 1;
		_forceAirRemaining = JumpGraceSeconds;
	}

	private void GoToFallState()
	{
		_status = PlayerState.Fall;
		_anim.Play("fall");
	}

	private void GoToDuckState()
	{
		_status = PlayerState.Duck;
		_anim.Play("duck");
		SetSmallCollider();
	}

	private void ExitFromDuckState()
	{
		SetLargeCollider();
	}

	private void GoToSlideState()
	{
		_status = PlayerState.Slide;
		_anim.Play("slide");
		SetSmallCollider();
	}

	private void ExitFromSlideState()
	{
		SetLargeCollider();
	}

	private void GoToWallState()
	{
		_status = PlayerState.Wall;
		_anim.Play("wall");
		Velocity = Vector2.Zero;
		_jumpCount = 0;
	}

	private void GoToSwimmingState()
	{
		_status = PlayerState.Swimming;
		_anim.Play("swimming");
		Velocity = new Vector2(Velocity.X, Mathf.Min(Velocity.Y, 150));
	}

	private void GoToHurtState()
	{
		if (_status == PlayerState.Hurt)
			return;

		_status = PlayerState.Hurt;
		_anim.Play("hurt");
		Velocity = new Vector2(0.0f, Velocity.Y);

		if (!_deathTriggered)
		{
			_deathTriggered = true;
			_canMove = false;
			EmitSignal(SignalName.DeathTriggered);
		}
	}

	private void IdleState(float delta)
	{
		ApplyGravity(delta);
		MoveHorizontal(delta);

		if (Mathf.Abs(Velocity.X) > 0.01f)
		{
			GoToWalkState();
			return;
		}

		if (Input.IsActionJustPressed("jump"))
		{
			GoToJumpState();
			return;
		}

		if (Input.IsActionPressed("duck"))
		{
			GoToDuckState();
			return;
		}
	}

	private void WalkState(float delta)
	{
		ApplyGravity(delta);
		MoveHorizontal(delta);

		if (Mathf.Abs(Velocity.X) <= 0.01f)
		{
			GoToIdleState();
			return;
		}

		if (Input.IsActionJustPressed("jump"))
		{
			GoToJumpState();
			return;
		}

		if (Input.IsActionJustPressed("duck"))
		{
			GoToSlideState();
			return;
		}

		if (!IsGrounded())
		{
			_jumpCount += 1;
			GoToFallState();
			return;
		}
	}

	private void JumpState(float delta)
	{
		ApplyGravity(delta);
		MoveHorizontal(delta);

		if (!IsOnFloor() && Mathf.IsEqualApprox(Velocity.Y, 0.0f))
		{
			Velocity = new Vector2(Velocity.X, 1.0f);
		}

		if (Input.IsActionJustPressed("jump") && CanJump())
		{
			GoToJumpState();
			return;
		}

		if (Velocity.Y >= 0.0f)
		{
			GoToFallState();
		}
	}

	private void FallState(float delta)
	{
		ApplyGravity(delta);
		MoveHorizontal(delta);

		if (!IsOnFloor() && Mathf.IsEqualApprox(Velocity.Y, 0.0f))
		{
			Velocity = new Vector2(Velocity.X, 1.0f);
		}

		if (Input.IsActionJustPressed("jump") && CanJump())
		{
			GoToJumpState();
			return;
		}

		if (IsGrounded())
		{
			_jumpCount = 0;
			if (Mathf.Abs(Velocity.X) <= 0.01f)
				GoToIdleState();
			else
				GoToWalkState();
			return;
		}

		if ((_leftWallDetector.IsColliding() || _rightWallDetector.IsColliding()) && IsOnWall())
		{
			GoToWallState();
		}
	}

	private void DuckState(float delta)
	{
		ApplyGravity(delta);
		UpdateDirection();

		if (Input.IsActionJustReleased("duck"))
		{
			ExitFromDuckState();
			GoToIdleState();
		}
	}

	private void SlideState(float delta)
	{
		ApplyGravity(delta);
		Velocity = new Vector2(Mathf.MoveToward(Velocity.X, 0.0f, SlideDeceleration * delta), Velocity.Y);

		if (Input.IsActionJustReleased("duck"))
		{
			ExitFromSlideState();
			GoToWalkState();
			return;
		}

		if (Mathf.Abs(Velocity.X) <= 0.01f)
		{
			ExitFromSlideState();
			GoToDuckState();
		}
	}

	private void WallState(float delta)
	{
		Velocity = new Vector2(Velocity.X, Velocity.Y + WallAcceleration * delta);

		if (_leftWallDetector.IsColliding())
		{
			_anim.FlipH = false;
			_direction = 1;
		}
		else if (_rightWallDetector.IsColliding())
		{
			_anim.FlipH = true;
			_direction = -1;
		}
		else
		{
			GoToFallState();
			return;
		}

		if (IsGrounded())
		{
			GoToIdleState();
			return;
		}

		if (Input.IsActionJustPressed("jump"))
		{
			Velocity = new Vector2(WallJumpVelocity * _direction, Velocity.Y);
			GoToJumpState();
		}
	}

	private void SwimmingState(float delta)
	{
		UpdateDirection();

		if (Mathf.Abs(_direction) > 0.01f)
			Velocity = new Vector2(Mathf.MoveToward(Velocity.X, WaterMaxSpeed * _direction, WaterAcceleration * delta), Velocity.Y);
		else
			Velocity = new Vector2(Mathf.MoveToward(Velocity.X, 0.0f, WaterAcceleration * delta), Velocity.Y);

		Velocity = new Vector2(Velocity.X, Velocity.Y + WaterAcceleration * delta);
		if (Velocity.Y > WaterMaxSpeed)
			Velocity = new Vector2(Velocity.X, WaterMaxSpeed);

		if (Input.IsActionJustPressed("jump"))
			Velocity = new Vector2(Velocity.X, WaterJumpForce);
	}

	private void HurtState(float delta)
	{
		ApplyGravity(delta);
	}

	private void MoveHorizontal(float delta)
	{
		UpdateDirection();

		if (Mathf.Abs(_direction) > 0.01f)
			Velocity = new Vector2(Mathf.MoveToward(Velocity.X, _direction * MaxSpeed, Acceleration * delta), Velocity.Y);
		else
			Velocity = new Vector2(Mathf.MoveToward(Velocity.X, 0.0f, Deceleration * delta), Velocity.Y);
	}

	private void ApplyGravity(float delta)
	{
		if (_status == PlayerState.Swimming)
			return;

		Velocity += GetGravity() * delta;
	}

	private bool IsGrounded()
	{
		if (!IsOnFloor())
			return false;

		if (_forceAirRemaining > 0.0f)
			return false;

		return Velocity.Y >= 0.0f;
	}

	private void UpdateDirection()
	{
		_direction = Input.GetAxis("left", "right");

		if (_direction < 0)
			_anim.FlipH = true;
		else if (_direction > 0)
			_anim.FlipH = false;
	}

	private bool CanJump()
	{
		return _jumpCount < MaxJumpCount;
	}

	private void SetSmallCollider()
	{
		if (_collisionShape.Shape is CapsuleShape2D capsule)
		{
			capsule.Radius = 5;
			capsule.Height = 10;
		}

		_collisionShape.Position = new Vector2(_collisionShape.Position.X, 3);

		if (_hitboxCollision.Shape is RectangleShape2D rect)
			rect.Size = new Vector2(rect.Size.X, 10);

		_hitboxCollision.Position = new Vector2(_hitboxCollision.Position.X, 3);
	}

	private void SetLargeCollider()
	{
		if (_collisionShape.Shape is CapsuleShape2D capsule)
		{
			capsule.Radius = 6;
			capsule.Height = 16;
		}

		_collisionShape.Position = new Vector2(_collisionShape.Position.X, 0);

		if (_hitboxCollision.Shape is RectangleShape2D rect)
			rect.Size = new Vector2(rect.Size.X, 15);

		_hitboxCollision.Position = new Vector2(_hitboxCollision.Position.X, 0.5f);
	}

	private void _on_hitbox_area_entered(Area2D area)
	{
		if (area.IsInGroup("Enemies"))
		{
			HitEnemy(area);
			return;
		}

		if (area.IsInGroup("LethalArea"))
		{
			HitLethalArea();
		}
	}

	private void _on_hitbox_body_entered(Node2D body)
	{
		if (body.IsInGroup("LethalArea"))
		{
			GoToHurtState();
			return;
		}

		if (body.IsInGroup("Water"))
		{
			GoToSwimmingState();
		}
	}

	private void _on_hitbox_body_exited(Node2D body)
	{
		if (body.IsInGroup("Water"))
		{
			_jumpCount = 0;
			GoToJumpState();
		}
	}

	private void HitEnemy(Area2D area)
	{
		if (Velocity.Y > 0)
		{
			Node enemy = area.GetParent();
			enemy?.Call("take_damage");
			GoToJumpState();
		}
		else
		{
			GoToHurtState();
		}
	}

	private void HitLethalArea()
	{
		GoToHurtState();
	}

	private void _on_reload_timer_timeout()
	{
		GetTree().ReloadCurrentScene();
	}

	public void SetCanMove(bool value)
	{
		_canMove = value;
	}
}
