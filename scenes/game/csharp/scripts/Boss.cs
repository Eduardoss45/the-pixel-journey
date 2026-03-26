using Godot;
using System;

public partial class Boss : CharacterBody2D
{
	private enum BossState
	{
		Idle,
		Chase,
		AttackJump,
		Stunned,
		Dead
	}

	[Export] public float ChaseRange { get; set; } = 240.0f;
	[Export] public float LoseRange { get; set; } = 300.0f;
	[Export] public float Speed { get; set; } = 85.0f;
	[Export] public float JumpVelocity { get; set; } = -260.0f;
	[Export] public float JumpHeightThreshold { get; set; } = 18.0f;
	[Export] public float JumpHorizontalRange { get; set; } = 96.0f;
	[Export] public float StopDistance { get; set; } = 28.0f;
	[Export] public float ReactionDelaySeconds { get; set; } = 0.35f;
	[Export] public float AttackJumpRange { get; set; } = 96.0f;
	[Export] public float AttackJumpCooldownSeconds { get; set; } = 1.4f;
	[Export] public float AttackJumpVelocity { get; set; } = -260.0f;
	[Export] public float AttackJumpHorizontalSpeed { get; set; } = 110.0f;
	[Export] public float AttackJumpAirAcceleration { get; set; } = 500.0f;
	[Export] public float AttackJumpLeadTime { get; set; } = 0.25f;
	[Export] public float AttackJumpLeadMax { get; set; } = 32.0f;
	[Export] public float AttackJumpMinChaseSeconds { get; set; } = 0.5f;
	[Export] public float DeathJumpVelocity { get; set; } = -240.0f;
	[Export] public float ContactCooldownSeconds { get; set; } = 0.25f;
	[Export] public float ContactKnockbackX { get; set; } = 60.0f;
	[Export] public float ContactKnockbackY { get; set; } = 0.0f;
	[Export] public float StompVerticalAllowance { get; set; } = 6.0f;
	[Export] public float StompMinDownwardVelocity { get; set; } = 10.0f;

	[Export] public int InitialLives { get; set; } = 3;
	[Export] public string QuizSetId { get; set; } = "1";
	[Export(PropertyHint.Range, "0,99,1")] public int MaxQuestions { get; set; } = 1;
	[Export] public int[] StompQuestionIds { get; set; } = new int[] { 0, 0, 0 };

	private AnimatedSprite2D _anim = null!;
	private CollisionShape2D _bodyCollision = null!;
	private Area2D _hitbox = null!;
	private RayCast2D _wallDetector = null!;
	private RayCast2D _groundDetector = null!;

	private Control _quizContainer;
	private QuizUI _quizUI;
	private Player _playerThatHit;

	private BossState _status;
	private int _direction = 1;
	private int _lives;
	private bool _isProcessingHit;
	private bool _quizActive;
	private bool _isChasing;
	private float _reactionTimer;
	private bool _deathSequenceStarted;
	private float _deathStartY;
	private float _contactCooldown;
	private Vector2 _contactVelocity;
	private float _attackCooldownRemaining;
	private float _attackTargetX;
	private bool _attackLeftFloor;
	private float _chaseTime;
	private float _attackDesiredVx;

	public override void _Ready()
	{
		_anim = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_bodyCollision = GetNode<CollisionShape2D>("CollisionShape2D");
		_hitbox = GetNode<Area2D>("Hitbox");
		_wallDetector = GetNode<RayCast2D>("WallDetector");
		_groundDetector = GetNode<RayCast2D>("GroundDetector");

		_lives = Mathf.Max(1, InitialLives);
		EnsureQuestionSlots();

		_quizContainer = FindQuizContainer();
		if (_quizContainer != null)
		{
			_quizContainer.Visible = false;
			_quizContainer.ProcessMode = ProcessModeEnum.Disabled;
			_quizUI = _quizContainer.GetNodeOrNull<QuizUI>("Quiz");
		}
		else
		{
			GD.PrintErr("Boss: QuizContainer nao encontrado.");
		}

		GoToIdleState();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (GlobalPosition.Y > 178f)
		{
			ZIndex = -4;
		}

		if (_status == BossState.Dead)
		{
			if (!_deathSequenceStarted)
			{
				_deathSequenceStarted = true;
				_deathStartY = GlobalPosition.Y;
				Velocity = new Vector2(0.0f, DeathJumpVelocity);
				_anim.Play("death");
			}

			Velocity += GetGravity() * (float)delta;
			MoveAndSlide();

			if (GlobalPosition.Y >= _deathStartY + 220.0f)
			{
				QueueFree();
			}
			return;
		}

		if (_status == BossState.Stunned)
		{
			Velocity = new Vector2(0.0f, Velocity.Y);
			MoveAndSlide();
			return;
		}

		if (_attackCooldownRemaining > 0.0f)
		{
			_attackCooldownRemaining = Mathf.Max(0.0f, _attackCooldownRemaining - (float)delta);
		}

		if (_contactCooldown > 0.0f)
		{
			_contactCooldown = Mathf.Max(0.0f, _contactCooldown - (float)delta);
			Velocity = new Vector2(_contactVelocity.X, Velocity.Y);
			MoveAndSlide();
			return;
		}

		if (!IsOnFloor())
		{
			Velocity += GetGravity() * (float)delta;
		}

		var player = GetPlayer();
		UpdateChaseState(player, (float)delta);

		if (_status == BossState.AttackJump)
		{
			ProcessAttackJump((float)delta);
		}
		else if (_isChasing && player != null)
		{
			_chaseTime += (float)delta;
			if (CanStartAttackJump(player))
			{
				StartAttackJump(player);
			}
			else
			{
				GoToChaseState();
				ChasePlayer(player);
			}
		}
		else
		{
			_chaseTime = 0.0f;
			GoToIdleState();
			Velocity = new Vector2(0.0f, Velocity.Y);
		}

		MoveAndSlide();
	}

	private void GoToIdleState()
	{
		if (_status == BossState.Idle)
			return;

		_status = BossState.Idle;
		_anim.Play("idle");
	}

	private void GoToChaseState()
	{
		if (_status == BossState.Chase)
			return;

		_status = BossState.Chase;
		_anim.Play("walk");
	}

	private void GoToStunnedState()
	{
		_status = BossState.Stunned;
		_anim.Play("stunned");
		Velocity = Vector2.Zero;
	}

	private void ChasePlayer(Node2D player)
	{
		float deltaX = player.GlobalPosition.X - GlobalPosition.X;
		int desiredDirection = deltaX >= 0 ? 1 : -1;
		if (desiredDirection != _direction)
		{
			FlipDirection();
		}

		float horizontalDistance = Mathf.Abs(deltaX);
		if (horizontalDistance <= StopDistance)
		{
			Velocity = new Vector2(0.0f, Velocity.Y);
		}
		else
		{
			Velocity = new Vector2(Speed * _direction, Velocity.Y);
		}

		if (!IsOnFloor())
		{
			if (_anim.Animation != "jump")
				_anim.Play("jump");
		}
		else
		{
			if (_anim.Animation != "walk")
				_anim.Play("walk");
		}
	}

	private bool IsPlayerInRange(Node2D player)
	{
		if (player == null)
			return false;

		return GlobalPosition.DistanceTo(player.GlobalPosition) <= ChaseRange;
	}

	private bool IsPlayerInLoseRange(Node2D player)
	{
		if (player == null)
			return false;

		return GlobalPosition.DistanceTo(player.GlobalPosition) <= LoseRange;
	}

	private void UpdateChaseState(Node2D player, float delta)
	{
		if (player == null)
		{
			_isChasing = false;
			_reactionTimer = 0.0f;
			return;
		}

		if (_isChasing)
		{
			if (!IsPlayerInLoseRange(player))
			{
				_isChasing = false;
				_reactionTimer = 0.0f;
			}
			return;
		}

		if (!IsPlayerInRange(player))
		{
			_reactionTimer = 0.0f;
			return;
		}

		_reactionTimer += delta;
		if (_reactionTimer >= ReactionDelaySeconds)
		{
			_isChasing = true;
			_reactionTimer = 0.0f;
		}
	}

	private bool CanStartAttackJump(Node2D player)
	{
		if (player == null || !IsOnFloor())
			return false;

		if (_attackCooldownRemaining > 0.0f)
			return false;

		if (_chaseTime < AttackJumpMinChaseSeconds)
			return false;

		float horizontalDistance = Mathf.Abs(player.GlobalPosition.X - GlobalPosition.X);
		return horizontalDistance <= AttackJumpRange;
	}

	private void StartAttackJump(Node2D player)
	{
		_status = BossState.AttackJump;
		_attackTargetX = GetAttackTargetX(player);
		_attackCooldownRemaining = AttackJumpCooldownSeconds;
		_attackLeftFloor = false;

		_attackDesiredVx = ComputeAttackDesiredVx();

		float direction = _attackDesiredVx >= 0.0f ? 1.0f : -1.0f;
		if (direction != _direction)
			FlipDirection();

		Velocity = new Vector2(_attackDesiredVx, AttackJumpVelocity);
		_anim.Play("jump");
	}

	private float GetAttackTargetX(Node2D player)
	{
		float targetX = player.GlobalPosition.X;
		if (player is Player p)
		{
			float lead = p.Velocity.X * AttackJumpLeadTime;
			lead = Mathf.Clamp(lead, -AttackJumpLeadMax, AttackJumpLeadMax);
			targetX += lead;
		}

		return targetX;
	}

	private void ProcessAttackJump(float delta)
	{
		if (!IsOnFloor())
		{
			Velocity = new Vector2(
				Mathf.MoveToward(Velocity.X, _attackDesiredVx, AttackJumpAirAcceleration * delta),
				Velocity.Y
			);
		}

		if (!IsOnFloor())
		{
			_attackLeftFloor = true;
		}
		else if (_attackLeftFloor)
		{
			_status = BossState.Chase;
			_attackLeftFloor = false;
		}
	}

	private float ComputeAttackDesiredVx()
	{
		float gravity = GetGravity().Y;
		float time = gravity > 0.0f ? (2.0f * -AttackJumpVelocity) / gravity : 0.0f;

		float desiredVx = 0.0f;
		if (time > 0.0f)
			desiredVx = (_attackTargetX - GlobalPosition.X) / time;
		else
			desiredVx = AttackJumpHorizontalSpeed * (_attackTargetX >= GlobalPosition.X ? 1.0f : -1.0f);

		return Mathf.Clamp(desiredVx, -AttackJumpHorizontalSpeed, AttackJumpHorizontalSpeed);
	}

	private Player GetPlayer()
	{
		return GetTree().GetFirstNodeInGroup("Player") as Player;
	}

	private void FlipDirection()
	{
		Scale = new Vector2(Scale.X * -1.0f, Scale.Y);
		_direction *= -1;
	}

	private void _on_hitbox_body_entered(Node2D body)
	{
		if (_status == BossState.Dead || _quizActive || !body.IsInGroup("Player"))
		{
			return;
		}

		if (body is not Player player)
		{
			return;
		}

		bool isStomp = player.Velocity.Y > StompMinDownwardVelocity &&
			player.GlobalPosition.Y < GlobalPosition.Y + StompVerticalAllowance;

		if (isStomp)
		{
			player.Velocity = new Vector2(player.Velocity.X, Player.JumpVelocity);
			HandleStomp(player);
			return;
		}

		ApplyContactSeparation(player);
	}

	private void HandleStomp(Player player)
	{
		if (_status == BossState.Dead || _quizActive)
			return;

		_playerThatHit = player;
		_lives = Mathf.Max(0, _lives - 1);

		GoToStunnedState();
		OpenQuizForStomp(GetStompIndex());
	}

	private int GetStompIndex()
	{
		int index = InitialLives - _lives - 1;
		index = Mathf.Clamp(index, 0, StompQuestionIds.Length - 1);
		return index;
	}

	private void OpenQuizForStomp(int stompIndex)
	{
		if (_quizUI == null)
		{
			ResumeAfterQuiz();
			return;
		}

		if (QuizManager.Instance != null && !QuizManager.Instance.TryAcquireQuiz(this))
		{
			ResumeAfterQuiz();
			return;
		}

		int questionId = 0;
		if (stompIndex >= 0 && stompIndex < StompQuestionIds.Length)
			questionId = StompQuestionIds[stompIndex];

		int[] questionIds = questionId > 0 ? new[] { questionId } : Array.Empty<int>();

		var questions = QuizManager.Instance?.GetQuestions(QuizSetId, questionIds, MaxQuestions)
			?? new Godot.Collections.Array<QuizQuestion>();

		if (questions.Count == 0)
		{
			GD.PrintErr($"Boss: sem perguntas para o stomp {stompIndex + 1}.");
			QuizManager.Instance?.ReleaseQuiz(this);
			ResumeAfterQuiz();
			return;
		}

		_quizContainer.Visible = true;
		_quizContainer.ProcessMode = ProcessModeEnum.Always;

		var tree = GetTree();
		if (tree != null)
			tree.Paused = true;

		_quizActive = true;
		_quizUI.StartQuiz(questions, () =>
		{
			CloseQuiz();
			ResumeAfterQuiz();
		});

		_playerThatHit?.SetCanMove(false);
	}

	private void CloseQuiz()
	{
		if (_quizContainer == null)
			return;

		_quizContainer.Visible = false;
		_quizContainer.ProcessMode = ProcessModeEnum.Disabled;

		_quizUI?.Reset();
		QuizManager.Instance?.ReleaseQuiz(this);

		if (_playerThatHit != null)
		{
			_playerThatHit.SetCanMove(true);
			_playerThatHit = null;
		}

		var tree = GetTree();
		if (tree != null)
			tree.Paused = false;

		_quizActive = false;
	}

	private void ResumeAfterQuiz()
	{
		if (_lives <= 0)
		{
			GoToDeadState();
			return;
		}

		GoToChaseState();
	}

	private void GoToDeadState()
	{
		_status = BossState.Dead;
		Velocity = Vector2.Zero;
		_anim.Play("death");

		_hitbox.SetDeferred(Node.PropertyName.ProcessMode, (int)ProcessModeEnum.Disabled);
		if (_bodyCollision != null)
			_bodyCollision.SetDeferred("disabled", true);
	}

	private void TriggerPlayerDeath(Player player)
	{
		if (_isProcessingHit)
			return;

		_isProcessingHit = true;
		player?.EmitSignal(Player.SignalName.DeathTriggered);
		_isProcessingHit = false;
	}

	private void ApplyContactSeparation(Player player)
	{
		if (ContactCooldownSeconds <= 0.0f)
			return;

		_contactCooldown = ContactCooldownSeconds;

		float directionAway = player.GlobalPosition.X >= GlobalPosition.X ? -1.0f : 1.0f;
		float knockbackY = player.GlobalPosition.Y > GlobalPosition.Y ? 0.0f : ContactKnockbackY;
		_contactVelocity = new Vector2(ContactKnockbackX * directionAway, knockbackY);

		Velocity = _contactVelocity;
	}

	private Control FindQuizContainer()
	{
		var candidates = GetTree().GetNodesInGroup("QuizContainerGroup");
		if (candidates.Count > 0)
			return candidates[0] as Control;

		return GetTree().Root.FindChild("QuizContainer", true, false) as Control;
	}

	private void EnsureQuestionSlots()
	{
		if (StompQuestionIds == null || StompQuestionIds.Length == 0)
		{
			StompQuestionIds = new int[] { 0, 0, 0 };
			return;
		}

		if (StompQuestionIds.Length == 3)
			return;

		var resized = new int[3];
		for (int i = 0; i < Mathf.Min(3, StompQuestionIds.Length); i++)
			resized[i] = StompQuestionIds[i];

		StompQuestionIds = resized;
	}
}
