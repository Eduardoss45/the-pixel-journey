using Godot;

public partial class SpinningBone : Area2D
{
	private AnimatedSprite2D _anim = null!;
	private static bool _isProcessingHit;

	private const float Speed = 60.0f;
	private int _direction = 1;

	public override void _Ready()
	{
		_anim = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
	}

	public override void _Process(double delta)
	{
		Position += new Vector2(Speed * (float)delta * _direction, 0.0f);
	}

	public void SetDirection(int skeletonDirection)
	{
		_direction = skeletonDirection;
		_anim.FlipH = _direction < 0;
	}

	public void set_direction(int skeletonDirection)
	{
		SetDirection(skeletonDirection);
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
			TriggerPlayerDeath(body);
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
