using Godot;
using System;

public partial class UnstablePlatform : AnimatableBody2D, IGameMechanism
{
	[Export] public string MechanismId { get; set; } = "";
	[Export] public bool StartActive = false;
	[Export] public bool StartWithPhysics = false;
	[Export] public float ShakeDuration = 0.35f;
	[Export] public float ShakeAmplitude = 2.0f;
	[Export] public float ShakeFrequency = 28.0f;
	[Export] public float LiftHeight = 4.0f;
	[Export] public float LiftDuration = 0.12f;
	[Export] public float FallAcceleration = 900.0f;
	[Export] public float MaxFallSpeed = 900.0f;

	private enum UnstableState
	{
		Idle,
		Shaking,
		Rising,
		Falling
	}

	private UnstableState state = UnstableState.Idle;
	private Vector2 startPosition;
	private CollisionShape2D collisionShape;
	private float shakeElapsed;
	private float riseElapsed;
	private float verticalVelocity;

	public override void _Ready()
	{
		startPosition = Position;
		SyncToPhysics = true;

		collisionShape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");

		MechanismId = MechanismId.Trim();
		ObjectManager.Instance?.Register(this);

		SetPhysicsEnabled(StartWithPhysics || StartActive);

		if (StartActive)
			ActivateMovement();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (state == UnstableState.Idle)
			return;

		float dt = (float)delta;
		switch (state)
		{
			case UnstableState.Shaking:
				shakeElapsed += dt;
				float phase = shakeElapsed * ShakeFrequency;
				float xOffset = Mathf.Sin(phase * Mathf.Tau) * ShakeAmplitude;
				float yOffset = Mathf.Sin(phase * Mathf.Tau * 0.5f) * (ShakeAmplitude * 0.25f);
				Position = new Vector2(startPosition.X + xOffset, startPosition.Y + yOffset);

				if (shakeElapsed >= ShakeDuration)
				{
					state = UnstableState.Rising;
					riseElapsed = 0f;
				}
				break;

			case UnstableState.Rising:
				riseElapsed += dt;
				float t = Mathf.Clamp(riseElapsed / Mathf.Max(LiftDuration, 0.01f), 0f, 1f);
				float eased = 1f - Mathf.Pow(1f - t, 2f);
				Position = new Vector2(startPosition.X, Mathf.Lerp(startPosition.Y, startPosition.Y - LiftHeight, eased));

				if (t >= 1f)
				{
					state = UnstableState.Falling;
					verticalVelocity = 0f;
				}
				break;

			case UnstableState.Falling:
				verticalVelocity = Mathf.Min(MaxFallSpeed, verticalVelocity + FallAcceleration * dt);
				Position += new Vector2(0f, verticalVelocity * dt);
				break;
		}
	}

	public void ActivateMovement()
	{
		if (state != UnstableState.Idle)
			return;

		shakeElapsed = 0f;
		riseElapsed = 0f;
		verticalVelocity = 0f;
		state = UnstableState.Shaking;
		SetPhysicsEnabled(true);
	}

	public void Stop()
	{
		state = UnstableState.Idle;
		Position = startPosition;
		SetPhysicsEnabled(StartWithPhysics);
	}

	public void ReturnToStart()
	{
		Position = startPosition;
		state = UnstableState.Idle;
	}

	private void SetPhysicsEnabled(bool enabled)
	{
		if (collisionShape == null)
			return;

		collisionShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, !enabled);
	}

	public void ApplyEffect(string effectId, Variant? value = null)
	{
		switch (effectId)
		{
			case "activate":
				ActivateMovement();
				break;

			case "stop":
				Stop();
				break;
		}
	}
}
