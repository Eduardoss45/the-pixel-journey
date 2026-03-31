using Godot;

public partial class FireBall : Area2D
{
	[Export] public bool DealDamage { get; set; } = false;

	private AnimatedSprite2D _anim = null!;
	private static bool _isProcessingHit;

	private const float Speed = 180.0f;
	private int _direction = 1;
	private Vector2 _velocity = Vector2.Zero;

	public override void _Ready()
	{
		_anim = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_velocity = new Vector2(Speed * _direction, 0.0f);
	}

	public override void _Process(double delta)
	{
		Position += _velocity * (float)delta;
	}

	public void SetDirection(int skeletonDirection)
	{
		_direction = skeletonDirection;
		_velocity = new Vector2(Speed * _direction, 0.0f);
		_anim.FlipH = _direction < 0;
	}

	public void set_direction(int skeletonDirection)
	{
		SetDirection(skeletonDirection);
	}

	public void SetAngleDegrees(int direction, float angleDegrees)
	{
		_direction = direction;
		float radians = Mathf.DegToRad(angleDegrees);
		float x = Mathf.Cos(radians) * _direction;
		float y = -Mathf.Sin(radians);
		_velocity = new Vector2(x, y) * Speed;
		_anim.FlipH = _direction < 0;
	}

	private void _on_self_destruct_timer_timeout()
	{
		QueueFree();
	}

	private void _on_area_entered(Area2D _area)
	{
		QueueFree();
	}

	private void _on_body_entered(Node2D body)
	{
		if (body.IsInGroup("Player"))
		{
			if (DealDamage)
			{
				TriggerPlayerDeath(body);
			}
			return;
		}

		QueueFree();
	}

	private void TriggerPlayerDeath(Node2D body)
	{
		if (_isProcessingHit)
		{
			return;
		}

		_isProcessingHit = true;

		Player player = body as Player;
		if (player == null)
		{
			player = GetTree().GetFirstNodeInGroup("Player") as Player;
		}

		player?.TryApplyDamage();
		QueueFree();
		_isProcessingHit = false;
	}
}
