using Godot;
using System;

public partial class Camera : Camera2D
{
	private Node2D target;
	private float _shakeRemaining;
	private float _shakeStrength;
	private readonly RandomNumberGenerator _rng = new RandomNumberGenerator();

	public override void _Ready()
	{
		GetTarget();
		_rng.Randomize();
	}

	public override void _Process(double delta)
	{
		if (target != null)
		{
			if (_shakeRemaining > 0.0f)
			{
				_shakeRemaining = Mathf.Max(0.0f, _shakeRemaining - (float)delta);
				Vector2 offset = new Vector2(
					_rng.RandfRange(-_shakeStrength, _shakeStrength),
					_rng.RandfRange(-_shakeStrength, _shakeStrength)
				);
				Position = target.Position + offset;
			}
			else
			{
				Position = target.Position;
			}
		}
	}

	public void StartShake(float durationSeconds, float strength)
	{
		if (durationSeconds <= 0.0f || strength <= 0.0f)
			return;

		_shakeRemaining = Mathf.Max(_shakeRemaining, durationSeconds);
		_shakeStrength = Mathf.Max(_shakeStrength, strength);
	}

	private void GetTarget()
	{
		var nodes = GetTree().GetNodesInGroup("Player");

		if (nodes.Count == 0)
		{
			GD.PushError("Player not found");
			return;
		}

		target = nodes[0] as Node2D;

		if (target == null)
		{
			GD.PushError("Player node is not a Node2D");
		}
	}
}
