using System;
using Godot;

public partial class Boss : CharacterBody2D
{
    private static readonly PackedScene FireBallScene = GD.Load<PackedScene>(
        "res://scenes/game/csharp/entities/fire_ball.tscn"
    );

    private enum BossState
    {
        Idle,
        Chase,
        AttackJump,
        Stunned,
        Dead,
    }

    private enum ComboState
    {
        None,
        SpinningPunch,
        Punch,
        FireWindup,
        JumpAttack,
    }

    [Export]
    public float ChaseRange { get; set; } = 240.0f;

    [Export]
    public float LoseRange { get; set; } = 300.0f;

    [Export]
    public float Speed { get; set; } = 105.0f;

    [Export]
    public float StopDistance { get; set; } = 15.0f;

    [Export]
    public float ReactionDelaySeconds { get; set; } = 0.35f;

    [Export]
    public float AttackJumpRange { get; set; } = 150.0f;

    [Export]
    public float AttackJumpCooldownSeconds { get; set; } = 5.0f;

    [Export]
    public float AttackJumpVelocity { get; set; } = -360.0f;

    [Export]
    public float AttackJumpHorizontalSpeed { get; set; } = 310.0f;

    [Export]
    public float AttackJumpAirAcceleration { get; set; } = 2000.0f;

    [Export]
    public float AttackJumpLeadTime { get; set; } = 0.05f;

    [Export]
    public float AttackJumpLeadMax { get; set; } = 16.0f;

    [Export]
    public float AttackJumpMinChaseSeconds { get; set; } = 0.5f;

    [Export]
    public float DeathJumpVelocity { get; set; } = -240.0f;

    [Export]
    public float ContactCooldownSeconds { get; set; } = 0.025f;

    [Export]
    public float ContactKnockbackX { get; set; } = 10.0f;

    [Export]
    public float ContactKnockbackY { get; set; } = 5.0f;

    [Export]
    public float StompVerticalAllowance { get; set; } = 2.0f;

    [Export]
    public float StompMinDownwardVelocity { get; set; } = 10.0f;

    [Export]
    public float StompHorizontalAllowance { get; set; } = 10.0f;

    [Export]
    public bool BlockStompOnWall { get; set; } = true;

    [Export]
    public float StompCooldownSeconds { get; set; } = 0.4f;

    [Export]
    public float StompKnockbackX { get; set; } = 220.0f;

    [Export]
    public float StompKnockbackY { get; set; } = -40.0f;

    [Export]
    public float StompSeparationSeconds { get; set; } = 0.25f;

    [Export]
    public float WallPunishRange { get; set; } = 120.0f;

    [Export]
    public float WallPunishRetreatSeconds { get; set; } = 0.25f;

    [Export]
    public float WallPunishRetreatSpeed { get; set; } = 90.0f;

    [Export]
    public float WallPunishJumpVelocity { get; set; } = -420.0f;

    [Export]
    public float WallPunishCooldownSeconds { get; set; } = 2.0f;

    [Export]
    public float EarthquakeRange { get; set; } = 140.0f;

    [Export]
    public float EarthquakeStunSeconds { get; set; } = 0.6f;

    [Export]
    public float EarthquakeShakeSeconds { get; set; } = 0.35f;

    [Export]
    public float EarthquakeShakeStrength { get; set; } = 4.0f;

    [Export]
    public float FireballRange { get; set; } = 480.0f;

    [Export]
    public float FireballVerticalTolerance { get; set; } = 28.0f;

    [Export]
    public float FireballCooldownSeconds { get; set; } = 3.5f;

    [Export]
    public float FireballReactionSeconds { get; set; } = 0.35f;

    [Export]
    public float FireballWindupSeconds { get; set; } = 0.25f;

    [Export]
    public float ComboRange { get; set; } = 72.0f;

    [Export]
    public float ComboDashSpeed { get; set; } = 260.0f;

    [Export]
    public float ComboCooldownSeconds { get; set; } = 0.75f;

    [Export]
    public float PostQuizImmunitySeconds { get; set; } = 1.5f;

    [Export]
    public bool StartsActive = true;

    [Export]
    public int InitialLives { get; set; } = 3;

    [Export]
    public string QuizSetId { get; set; } = "1";

    [Export(PropertyHint.Range, "0,99,1")]
    public int MaxQuestions { get; set; } = 1;

    [Export]
    public int[] StompQuestionIds { get; set; } = new int[] { 0, 0, 0 };

    private AnimatedSprite2D _anim = null!;
    private CollisionShape2D _bodyCollision = null!;
    private Area2D _hitbox = null!;
    private Node2D _fireballStartPosition = null!;

    private Control _quizContainer;
    private QuizUI _quizUI;
    private Player _playerThatHit;

    private BossState _status;
    private int _direction = 1;
    private int _lives;
    private int _pendingStompIndex;
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
    private float _fireballCooldownRemaining;
    private float _fireballReactionRemaining;
    private Node2D _fireballTargetPlayer;
    private bool _fireballPlaying;
    private float _fireballWindupRemaining;
    private ComboState _comboState;
    private float _comboCooldownRemaining;
    private Player _comboTargetPlayer;
    private int _comboDirection = 1;
    private float _postQuizImmunityRemaining;
    private Player _attackTargetPlayer;
    private bool _attackExceptionApplied;
    private float _wallPunishCooldownRemaining;
    private float _wallPunishWindupRemaining;
    private bool _wallPunishActive;
    private int _wallPunishDirection;
    private float _stompCooldownRemaining;
    private Player _stompSeparatedPlayer;
    private bool _stompSeparationActive;

    private bool _isActive;

    public override void _Ready()
    {
        _anim = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        _bodyCollision = GetNode<CollisionShape2D>("CollisionShape2D");
        _hitbox = GetNode<Area2D>("Hitbox");
        _fireballStartPosition =
            GetNodeOrNull<Node2D>("FireBallStartPosition")
            ?? GetNodeOrNull<Node2D>("BoneStartPosition");

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

        _isActive = StartsActive;
        var gameSession = GameSession.Instance ?? GetNodeOrNull<GameSession>("/root/GameSession");
        gameSession?.SetBossActive(_isActive);
        GoToIdleState();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_isActive)
        {
            Velocity = Vector2.Zero;
            if (_status != BossState.Dead && _anim.Animation != "idle")
                _anim.Play("idle");
            MoveAndSlide();
            return;
        }

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

        if (_attackCooldownRemaining > 0.0f)
        {
            _attackCooldownRemaining = Mathf.Max(0.0f, _attackCooldownRemaining - (float)delta);
        }

        if (_postQuizImmunityRemaining > 0.0f)
        {
            _postQuizImmunityRemaining = Mathf.Max(0.0f, _postQuizImmunityRemaining - (float)delta);
            GoToStunnedState();
            Velocity = new Vector2(0.0f, Velocity.Y);
            MoveAndSlide();

            if (_postQuizImmunityRemaining <= 0.0f)
            {
                ResetAbilityCooldowns();
                GoToIdleState();
            }
            return;
        }

        if (_status == BossState.Stunned)
        {
            RestoreAttackCollisionException();
            Velocity = new Vector2(0.0f, Velocity.Y);
            MoveAndSlide();
            return;
        }

        if (_comboCooldownRemaining > 0.0f)
        {
            _comboCooldownRemaining = Mathf.Max(0.0f, _comboCooldownRemaining - (float)delta);
        }

        if (_stompCooldownRemaining > 0.0f)
        {
            _stompCooldownRemaining = Mathf.Max(0.0f, _stompCooldownRemaining - (float)delta);
        }

        if (_wallPunishCooldownRemaining > 0.0f)
        {
            _wallPunishCooldownRemaining = Mathf.Max(0.0f, _wallPunishCooldownRemaining - (float)delta);
        }

        if (_fireballCooldownRemaining > 0.0f)
        {
            _fireballCooldownRemaining = Mathf.Max(0.0f, _fireballCooldownRemaining - (float)delta);
        }

        if (_fireballReactionRemaining > 0.0f)
        {
            _fireballReactionRemaining = Mathf.Max(0.0f, _fireballReactionRemaining - (float)delta);
        }

        if (_contactCooldown > 0.0f)
        {
            _contactCooldown = Mathf.Max(0.0f, _contactCooldown - (float)delta);
            Velocity = new Vector2(_contactVelocity.X, Velocity.Y);
            MoveAndSlide();
            return;
        }

        if (_wallPunishWindupRemaining > 0.0f)
        {
            _wallPunishWindupRemaining = Mathf.Max(0.0f, _wallPunishWindupRemaining - (float)delta);
            int retreatDirection = -_wallPunishDirection;
            Velocity = new Vector2(WallPunishRetreatSpeed * retreatDirection, Velocity.Y);
            if (IsOnFloor() && _anim.Animation != "walk")
                _anim.Play("walk");
            MoveAndSlide();

            if (_wallPunishWindupRemaining <= 0.0f)
            {
                StartWallPunishJump();
            }
            return;
        }

        if (_fireballWindupRemaining > 0.0f)
        {
            _fireballWindupRemaining = Mathf.Max(0.0f, _fireballWindupRemaining - (float)delta);
            Velocity = new Vector2(0.0f, Velocity.Y);
            MoveAndSlide();

            if (_fireballWindupRemaining <= 0.0f)
            {
                ThrowFireball();
                if (_comboState == ComboState.FireWindup && _comboTargetPlayer != null)
                {
                    _comboState = ComboState.JumpAttack;
                    StartAttackJump(_comboTargetPlayer);
                }
            }
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
        else if (_comboState != ComboState.None)
        {
            ProcessCombo();
        }
        else if (_isChasing && player != null)
        {
            _chaseTime += (float)delta;
            if (TryStartWallPunish(player))
            {
                return;
            }
            if (TryStartCombo(player))
            {
                ProcessCombo();
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

    public void SetActive(bool value)
    {
        _isActive = value;
        var gameSession = GameSession.Instance ?? GetNodeOrNull<GameSession>("/root/GameSession");
        gameSession?.SetBossActive(_isActive);
        if (!_isActive)
        {
            _isChasing = false;
            _reactionTimer = 0.0f;
            _comboState = ComboState.None;
            _attackCooldownRemaining = 0.0f;
            _fireballCooldownRemaining = 0.0f;
            _fireballReactionRemaining = 0.0f;
            _fireballWindupRemaining = 0.0f;
            _contactCooldown = 0.0f;
            Velocity = Vector2.Zero;
            GoToIdleState();
        }
    }

    private void GoToIdleState()
    {
        if (_status == BossState.Idle)
            return;

        _status = BossState.Idle;
        SetCollisionEnabled(true);
        _anim.Play("idle");
    }

    private void GoToChaseState()
    {
        if (_status == BossState.Chase)
            return;

        _status = BossState.Chase;
        SetCollisionEnabled(true);
        _anim.Play("walk");
    }

    private void GoToStunnedState()
    {
        _status = BossState.Stunned;
        _anim.Play("stunned");
        SetCollisionEnabled(false);
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
            if (IsOnFloor() && _anim.Animation != "idle")
                _anim.Play("idle");
            return;
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
        if (_comboState != ComboState.None)
            return false;

        if (player == null || !IsOnFloor())
            return false;

        if (_attackCooldownRemaining > 0.0f)
            return false;

        if (_chaseTime < AttackJumpMinChaseSeconds)
            return false;

        float horizontalDistance = Mathf.Abs(player.GlobalPosition.X - GlobalPosition.X);
        return horizontalDistance <= AttackJumpRange;
    }

    private void TryStartFireball(Node2D player)
    {
        if (_comboState != ComboState.None)
            return;

        if (!IsOnFloor() || _status == BossState.AttackJump)
            return;

        if (_fireballCooldownRemaining > 0.0f)
            return;

        if (!ShouldThrowFireball(player))
        {
            _fireballTargetPlayer = null;
            _fireballReactionRemaining = 0.0f;
            return;
        }

        if (_fireballTargetPlayer == null)
        {
            _fireballTargetPlayer = player;
            _fireballReactionRemaining = FireballReactionSeconds;
            return;
        }

        if (_fireballReactionRemaining <= 0.0f && ShouldThrowFireball(_fireballTargetPlayer))
        {
            StartFireballWindup();
        }
    }

    private bool ShouldThrowFireball(Node2D player)
    {
        if (player == null)
            return false;

        float horizontalDistance = Mathf.Abs(player.GlobalPosition.X - GlobalPosition.X);
        float verticalDistance = Mathf.Abs(player.GlobalPosition.Y - GlobalPosition.Y);
        if (horizontalDistance > FireballRange || verticalDistance > FireballVerticalTolerance)
            return false;

        int targetDirection = player.GlobalPosition.X >= GlobalPosition.X ? 1 : -1;
        if (targetDirection != _direction)
            FlipDirection();

        return true;
    }

    private void StartFireballWindup()
    {
        if (_fireballWindupRemaining > 0.0f)
            return;

        _fireballWindupRemaining = FireballWindupSeconds;
        _fireballPlaying = true;
        _anim.Play("fire");
    }

    private void ThrowFireball()
    {
        SpawnFireball(0.0f);
        SpawnFireball(15.0f);
        SpawnFireball(30.0f);
        _fireballCooldownRemaining = FireballCooldownSeconds;
        _fireballTargetPlayer = null;
        _fireballReactionRemaining = 0.0f;
    }

    private void SpawnFireball(float angleDegrees)
    {
        FireBall newBall = FireBallScene.Instantiate<FireBall>();
        GetParent().AddChild(newBall);
        newBall.GlobalPosition =
            _fireballStartPosition != null ? _fireballStartPosition.GlobalPosition : GlobalPosition;

        if (Mathf.IsZeroApprox(angleDegrees))
            newBall.SetDirection(_direction);
        else
            newBall.SetAngleDegrees(_direction, angleDegrees);
    }

    private void _on_animated_sprite_2d_animation_finished()
    {
        if (_anim.Animation == "fire")
        {
            _fireballPlaying = false;
            if (_comboState == ComboState.FireWindup || _comboState == ComboState.JumpAttack)
                return;
            if (_status == BossState.Chase)
            {
                if (IsOnFloor() && Mathf.Abs(Velocity.X) <= 0.01f)
                    _anim.Play("idle");
                else if (IsOnFloor())
                    _anim.Play("walk");
            }
            return;
        }

        if (_anim.Animation == "punch" && _comboState == ComboState.Punch)
        {
            _comboState = ComboState.SpinningPunch;
            _anim.Play("spinning_punch");
            return;
        }

        if (_anim.Animation == "spinning_punch" && _comboState == ComboState.SpinningPunch)
        {
            _comboState = ComboState.None;
            _comboCooldownRemaining = ComboCooldownSeconds;
            _comboTargetPlayer = null;
            _fireballCooldownRemaining = FireballCooldownSeconds;
            _attackCooldownRemaining = AttackJumpCooldownSeconds;
            return;
        }
    }

    private void StartAttackJump(Node2D player)
    {
        _status = BossState.AttackJump;
        _attackTargetX = GetAttackTargetX(player);
        _attackCooldownRemaining = AttackJumpCooldownSeconds;
        _attackLeftFloor = false;
        _attackTargetPlayer = player as Player;
        _attackExceptionApplied = false;

        _attackDesiredVx = ComputeDesiredVx(_attackTargetX, AttackJumpVelocity, AttackJumpHorizontalSpeed);

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
            ApplyAttackCollisionException();

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
            RestoreAttackCollisionException();
            _status = BossState.Chase;
            _attackLeftFloor = false;
            if (_wallPunishActive)
            {
                TriggerEarthquake(_attackTargetPlayer ?? GetPlayer());
                _wallPunishActive = false;
                _wallPunishCooldownRemaining = WallPunishCooldownSeconds;
            }
            if (_comboState == ComboState.JumpAttack)
            {
                _comboState = ComboState.Punch;
                if (_comboTargetPlayer != null)
                    _comboDirection =
                        _comboTargetPlayer.GlobalPosition.X >= GlobalPosition.X ? 1 : -1;
                if (_comboDirection != _direction)
                    FlipDirection();
                _anim.Play("punch");
            }
        }
    }

    private bool TryStartCombo(Player player)
    {
        if (_comboState != ComboState.None)
            return false;

        if (_comboCooldownRemaining > 0.0f)
            return false;

        if (!IsOnFloor() || _status == BossState.AttackJump)
            return false;

        float horizontalDistance = Mathf.Abs(player.GlobalPosition.X - GlobalPosition.X);
        if (horizontalDistance > ComboRange)
            return false;

        _comboState = ComboState.FireWindup;
        _comboTargetPlayer = player;
        _comboDirection = player.GlobalPosition.X >= GlobalPosition.X ? 1 : -1;
        if (_comboDirection != _direction)
            FlipDirection();

        _fireballTargetPlayer = null;
        _fireballReactionRemaining = 0.0f;
        _fireballCooldownRemaining = 0.0f;
        _attackCooldownRemaining = 0.0f;
        StartFireballWindup();
        return true;
    }

    private void ProcessCombo()
    {
        if (_comboState == ComboState.SpinningPunch || _comboState == ComboState.Punch)
        {
            if (_comboDirection != _direction)
                FlipDirection();

            Velocity = new Vector2(ComboDashSpeed * _comboDirection, Velocity.Y);
        }
        else
        {
            Velocity = new Vector2(0.0f, Velocity.Y);
        }
    }

    private float ComputeAttackDesiredVx()
    {
        return ComputeDesiredVx(_attackTargetX, AttackJumpVelocity, AttackJumpHorizontalSpeed);
    }

    private float ComputeDesiredVx(float targetX, float jumpVelocity, float maxHorizSpeed)
    {
        float gravity = GetGravity().Y;
        float time = gravity > 0.0f ? (2.0f * -jumpVelocity) / gravity : 0.0f;

        float desiredVx = 0.0f;
        if (time > 0.0f)
            desiredVx = (targetX - GlobalPosition.X) / time;
        else
            desiredVx = maxHorizSpeed * (targetX >= GlobalPosition.X ? 1.0f : -1.0f);

        return Mathf.Clamp(desiredVx, -maxHorizSpeed, maxHorizSpeed);
    }

    private bool TryStartWallPunish(Player player)
    {
        if (player == null)
            return false;
        if (!player.IsOnWall())
            return false;
        if (!IsOnFloor())
            return false;
        if (_comboState != ComboState.None || _status == BossState.AttackJump)
            return false;
        if (_wallPunishCooldownRemaining > 0.0f)
            return false;

        float horizontalDistance = Mathf.Abs(player.GlobalPosition.X - GlobalPosition.X);
        if (horizontalDistance > WallPunishRange)
            return false;

        _wallPunishDirection = player.GlobalPosition.X >= GlobalPosition.X ? 1 : -1;
        if (_wallPunishDirection != _direction)
            FlipDirection();

        _attackTargetX = player.GlobalPosition.X;
        _attackTargetPlayer = player;
        _wallPunishActive = true;
        _wallPunishWindupRemaining = WallPunishRetreatSeconds;
        _attackLeftFloor = false;
        _status = BossState.Chase;
        return true;
    }

    private void StartWallPunishJump()
    {
        _status = BossState.AttackJump;
        _attackDesiredVx = ComputeDesiredVx(
            _attackTargetX,
            WallPunishJumpVelocity,
            AttackJumpHorizontalSpeed
        );

        float direction = _attackDesiredVx >= 0.0f ? 1.0f : -1.0f;
        if (direction != _direction)
            FlipDirection();

        Velocity = new Vector2(_attackDesiredVx, WallPunishJumpVelocity);
        _anim.Play("jump");
    }

    private void TriggerEarthquake(Player player)
    {
        if (player != null)
        {
            float distance = GlobalPosition.DistanceTo(player.GlobalPosition);
            if (distance <= EarthquakeRange)
            {
                player.ApplyStun(EarthquakeStunSeconds);
                player.QueueStunAnimation(EarthquakeShakeSeconds);
            }
        }

        var camera = GetViewport().GetCamera2D() as Camera;
        camera?.StartShake(EarthquakeShakeSeconds, EarthquakeShakeStrength);
    }

    private void ApplyAttackCollisionException()
    {
        if (_attackExceptionApplied)
            return;

        if (_attackTargetPlayer != null)
        {
            AddCollisionExceptionWith(_attackTargetPlayer);
            _attackExceptionApplied = true;
        }
    }

    private void RestoreAttackCollisionException()
    {
        if (_attackExceptionApplied && _attackTargetPlayer != null)
        {
            RemoveCollisionExceptionWith(_attackTargetPlayer);
            _attackExceptionApplied = false;
            _attackTargetPlayer = null;
        }
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
        if (_postQuizImmunityRemaining > 0.0f)
        {
            return;
        }

        if (_status == BossState.Dead || _quizActive || !body.IsInGroup("Player"))
        {
            return;
        }

        if (body is not Player player)
        {
            return;
        }

        bool isStomp =
            player.Velocity.Y > StompMinDownwardVelocity
            && player.GlobalPosition.Y < GlobalPosition.Y + StompVerticalAllowance;

        if (_stompCooldownRemaining > 0.0f)
            isStomp = false;

        if (isStomp && BlockStompOnWall && player.IsOnWall())
            isStomp = false;

        if (isStomp)
        {
            float horizontalDelta = Mathf.Abs(player.GlobalPosition.X - GlobalPosition.X);
            if (horizontalDelta > StompHorizontalAllowance)
                isStomp = false;
        }

        if (isStomp)
        {
            ApplyStompSeparation(player);
            _stompCooldownRemaining = StompCooldownSeconds;
            HandleStomp(player);
            return;
        }

        ApplyContactSeparation(player);
    }

    private void HandleStomp(Player player)
    {
        if (
            _status == BossState.Dead
            || _quizActive
            || _status == BossState.Stunned
            || _postQuizImmunityRemaining > 0.0f
        )
            return;

        _playerThatHit = player;
        _pendingStompIndex = GetStompIndex();

        GoToStunnedState();
        OpenQuizForStomp(_pendingStompIndex);
    }

    private async void ApplyStompSeparation(Player player)
    {
        float directionAway = player.GlobalPosition.X >= GlobalPosition.X ? 1.0f : -1.0f;
        player.Velocity = new Vector2(directionAway * StompKnockbackX, StompKnockbackY);

        if (StompSeparationSeconds <= 0.0f)
            return;

        if (_stompSeparationActive && _stompSeparatedPlayer != null)
            RemoveCollisionExceptionWith(_stompSeparatedPlayer);

        _stompSeparatedPlayer = player;
        _stompSeparationActive = true;
        AddCollisionExceptionWith(player);

        await ToSignal(GetTree().CreateTimer(StompSeparationSeconds), SceneTreeTimer.SignalName.Timeout);

        if (_stompSeparatedPlayer == player)
        {
            RemoveCollisionExceptionWith(player);
            _stompSeparatedPlayer = null;
            _stompSeparationActive = false;
        }
    }

    public void TakeDamage()
    {
        if (
            _status == BossState.Dead
            || _quizActive
            || _status == BossState.Stunned
            || _postQuizImmunityRemaining > 0.0f
        )
            return;

        Player player = GetPlayer();
        if (player == null)
            return;

        HandleStomp(player);
    }

    public void take_damage()
    {
        TakeDamage();
    }

    private int GetStompIndex()
    {
        int index = InitialLives - _lives;
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

        var questions =
            QuizManager.Instance?.GetQuestions(QuizSetId, questionIds, MaxQuestions)
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
        SfxBus.Instance?.PlayOpen();

        var tree = GetTree();
        if (tree != null)
            tree.Paused = true;

        _quizActive = true;
        _quizUI.StartQuizWithResult(
            questions,
            wasCorrect =>
            {
                CloseQuiz(wasCorrect);
                if (wasCorrect)
                {
                    _lives = Mathf.Max(0, _lives - 1);
                }
                else
                {
                    GameSession.Instance?.ConsumeLifeSilently();
                }
                ResumeAfterQuiz();
            }
        );

        _playerThatHit?.SetCanMove(false);
    }

    private void CloseQuiz(bool applyImmunity)
    {
        if (_quizContainer == null)
            return;
        if (_quizContainer.Visible)
            SfxBus.Instance?.PlayClose();

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
        if (applyImmunity)
        {
            _postQuizImmunityRemaining = PostQuizImmunitySeconds;
            GoToStunnedState();
        }
        else
        {
            _postQuizImmunityRemaining = 0.0f;
            RestoreAttackCollisionException();
            ResetAbilityCooldowns();
            GoToChaseState();
            SetCollisionEnabled(true);
            _isChasing = true;
            _reactionTimer = 0.0f;
            _chaseTime = 0.0f;
        }
    }

    private void ResetAbilityCooldowns()
    {
        _attackCooldownRemaining = 0.0f;
        _fireballCooldownRemaining = 0.0f;
        _fireballReactionRemaining = 0.0f;
        _fireballWindupRemaining = 0.0f;
        _fireballTargetPlayer = null;
        _fireballPlaying = false;

        _comboState = ComboState.None;
        _comboCooldownRemaining = 0.0f;
        _comboTargetPlayer = null;

        _wallPunishCooldownRemaining = 0.0f;
        _wallPunishWindupRemaining = 0.0f;
        _wallPunishActive = false;
        _stompCooldownRemaining = 0.0f;
        if (_stompSeparationActive && _stompSeparatedPlayer != null)
            RemoveCollisionExceptionWith(_stompSeparatedPlayer);
        _stompSeparationActive = false;
        _stompSeparatedPlayer = null;

        _chaseTime = 0.0f;
        _reactionTimer = 0.0f;
        _isChasing = false;
        _attackLeftFloor = false;
        _attackDesiredVx = 0.0f;
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
        SfxBus.Instance?.PlayElimination();

        GameSession.Instance?.MarkBossDefeated();

        SetCollisionEnabled(false);
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

    private void SetCollisionEnabled(bool enabled)
    {
        if (_hitbox != null)
            _hitbox.SetDeferred(Node.PropertyName.ProcessMode, enabled ? (int)ProcessModeEnum.Inherit : (int)ProcessModeEnum.Disabled);
        if (_bodyCollision != null)
            _bodyCollision.SetDeferred("disabled", !enabled);
    }
}
