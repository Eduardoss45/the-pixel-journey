using Godot;

public partial class Skeleton : CharacterBody2D
{
	private enum SkeletonState
	{
		Walk,
		Attack,
		Hurt,
		Dead
	}

	private const float Speed = 100.0f;
	private const float PlayerDetectionRange = 72.0f;
	private const float PlayerVerticalTolerance = 28.0f;
	private const float StompVerticalAllowance = 2.0f;
	private const float StompMinDownwardVelocity = 30.0f;
	private const float ThrowCooldownSeconds = 2.0f;
	private const float ReactionTimeSeconds = 0.35f;
	private static readonly PackedScene SpinningBoneScene =
		GD.Load<PackedScene>("res://scenes/game/csharp/entities/spinning_bone.tscn");

	private AnimatedSprite2D _anim = null!;
	private CollisionShape2D _bodyCollision = null!;
	private Area2D _hitbox = null!;
	private RayCast2D _wallDetector = null!;
	private RayCast2D _groundDetector = null!;
	private RayCast2D _playerDetector = null!;
	private Node2D _boneStartPosition = null!;

	private SkeletonState _status;
	private int _direction = 1;
	private bool _canThrow = true;
	private float _throwCooldownRemaining = 0.0f;
	private float _reactionTimerRemaining = 0.0f;
	private Node2D _reactionTargetPlayer;

	public override void _Ready()
	{
		_anim = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_bodyCollision = GetNode<CollisionShape2D>("CollisionShape2D");
		_hitbox = GetNode<Area2D>("Hitbox");
		_wallDetector = GetNode<RayCast2D>("WallDetector");
		_groundDetector = GetNode<RayCast2D>("GroundDetector");
		_playerDetector = GetNode<RayCast2D>("PlayerDetector");
		_boneStartPosition = GetNode<Node2D>("BoneStartPosition");

		GoToWalkState();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_throwCooldownRemaining > 0.0f)
		{
			_throwCooldownRemaining = Mathf.Max(0.0f, _throwCooldownRemaining - (float)delta);
		}

		if (_reactionTimerRemaining > 0.0f)
		{
			_reactionTimerRemaining = Mathf.Max(0.0f, _reactionTimerRemaining - (float)delta);
		}

		if (!IsOnFloor())
		{
			Velocity += GetGravity() * (float)delta;
		}

		switch (_status)
		{
			case SkeletonState.Walk:
				WalkState();
				break;
			case SkeletonState.Attack:
				AttackState();
				break;
			case SkeletonState.Hurt:
				HurtState();
				break;
			case SkeletonState.Dead:
				break;
		}

		MoveAndSlide();
	}

	private void GoToWalkState()
	{
		_status = SkeletonState.Walk;
		_anim.Play("walk");
		_reactionTargetPlayer = null;
	}

	private void GoToAttackState()
	{
		_status = SkeletonState.Attack;
		_anim.Play("attack");
		Velocity = Vector2.Zero;
		_canThrow = true;
	}

	private void GoToHurtState()
	{
		_status = SkeletonState.Hurt;
		_anim.Play("hurt");
		_hitbox.ProcessMode = ProcessModeEnum.Disabled;
		Velocity = Vector2.Zero;
	}

	private void WalkState()
	{
		Node2D player = GetPlayer();

		if (_anim.Frame == 3 || _anim.Frame == 4)
		{
			Velocity = new Vector2(Speed * _direction, Velocity.Y);
		}
		else
		{
			Velocity = new Vector2(0.0f, Velocity.Y);
		}

		if (_wallDetector.IsColliding() || !_groundDetector.IsColliding())
		{
			Scale = new Vector2(Scale.X * -1.0f, Scale.Y);
			_direction *= -1;
		}

		if (_throwCooldownRemaining <= 0.0f && ShouldAttackPlayer(player))
		{
			if (_reactionTargetPlayer == null)
			{
				_reactionTargetPlayer = player;
				_reactionTimerRemaining = ReactionTimeSeconds;
			}
		}
		else
		{
			_reactionTargetPlayer = null;
			_reactionTimerRemaining = 0.0f;
		}

		if (_reactionTargetPlayer != null &&
			_reactionTimerRemaining <= 0.0f &&
			ShouldAttackPlayer(_reactionTargetPlayer))
		{
			GoToAttackState();
		}
	}

	private void AttackState()
	{
		if (_anim.Frame == 2 && _canThrow)
		{
			ThrowBone();
			_canThrow = false;
		}
	}

	private void HurtState()
	{
	}

	public void TakeDamage()
	{
		GoToHurtState();
	}

	public void take_damage()
	{
		TakeDamage();
	}

	private void ThrowBone()
	{
		SpinningBone newBone = SpinningBoneScene.Instantiate<SpinningBone>();
		GetParent().AddChild(newBone);
		newBone.GlobalPosition = _boneStartPosition.GlobalPosition;
		newBone.SetDirection(_direction);
		_throwCooldownRemaining = ThrowCooldownSeconds;
	}

	private bool IsPlayerDetected()
	{
		if (!_playerDetector.IsColliding())
		{
			return false;
		}

		return _playerDetector.GetCollider() is Node collider && collider.IsInGroup("Player");
	}

	private Node2D GetPlayer()
	{
		return GetTree().GetFirstNodeInGroup("Player") as Node2D;
	}

	private bool ShouldAttackPlayer(Node2D player)
	{
		if (player == null)
		{
			return false;
		}

		float horizontalDistance = Mathf.Abs(player.GlobalPosition.X - GlobalPosition.X);
		float verticalDistance = Mathf.Abs(player.GlobalPosition.Y - GlobalPosition.Y);
		if (horizontalDistance > PlayerDetectionRange || verticalDistance > PlayerVerticalTolerance)
		{
			return false;
		}

		int targetDirection = player.GlobalPosition.X >= GlobalPosition.X ? 1 : -1;
		if (targetDirection != _direction)
		{
			FlipDirection();
		}

		return IsPlayerDetected() || IsPlayerInFront(player);
	}

	private bool IsPlayerInFront(Node2D player)
	{
		float deltaX = player.GlobalPosition.X - GlobalPosition.X;
		return (_direction > 0 && deltaX > 0) || (_direction < 0 && deltaX < 0);
	}

	private void FlipDirection()
	{
		Scale = new Vector2(Scale.X * -1.0f, Scale.Y);
		_direction *= -1;
	}

	private void _on_hitbox_body_entered(Node2D body)
	{
		if (_status == SkeletonState.Hurt || !body.IsInGroup("Player"))
		{
			return;
		}

		if (body is not Player player)
		{
			return;
		}

		bool isStomp = player.Velocity.Y > StompMinDownwardVelocity &&
			player.GlobalPosition.Y < GlobalPosition.Y + StompVerticalAllowance;

		if (!isStomp)
		{
			return;
		}

		player.Velocity = new Vector2(player.Velocity.X, Player.JumpVelocity);
		TakeDamage();
	}

	private void _on_animated_sprite_2d_animation_finished()
	{
		if (_anim.Animation == "attack")
		{
			GoToWalkState();
			return;
		}

		if (_anim.Animation == "hurt")
		{
			_status = SkeletonState.Dead;
			Velocity = Vector2.Zero;
			_bodyCollision.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
			_wallDetector.Enabled = false;
			_groundDetector.Enabled = false;
			_playerDetector.Enabled = false;
			SetPhysicsProcess(false);
		}
	}
}
