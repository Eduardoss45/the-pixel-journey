using Godot;
using System;

public partial class QuizBlock : AnimatableBody2D
{
	[Export] public string[] TargetMechanismIds;
	[Export] public string QuizSetId { get; set; } = "js_basics_01";
	[Export] public float BounceHeight = 10f;
	[Export] public float BounceDuration = 0.6f;

	[Export] public Godot.Collections.Array<QuizQuestion> Questions { get; set; } = new();

	private Vector2 _startPosition;
	private bool _isBouncing = false;
	private Player _playerThatHit = null;

	private Control quizContainer;
	private QuizUI quizUI;

	public override void _Ready()
	{
		_startPosition = Position;

		var hitDetector = GetNode<Area2D>("Area2D");
		hitDetector.BodyEntered += OnBodyEntered;

		quizContainer = FindQuizContainer();

		if (quizContainer != null)
		{
			quizContainer.Visible = false;
			quizContainer.ProcessMode = ProcessModeEnum.Disabled;
			quizUI = quizContainer.GetNodeOrNull<QuizUI>("Quiz");
		}
		else
		{
			GD.PrintErr("QuizContainer não encontrado!");
		}

		SyncToPhysics = true;
	}

	private void OnBodyEntered(Node body)
	{
		if (body is not Player player) return;
		if (_isBouncing) return;

		_playerThatHit = player;
		TriggerBlock();
	}

	private void TriggerBlock()
	{
		_isBouncing = true;
		AnimateBounce();
		OpenQuiz();
	}

	private void AnimateBounce()
	{
		var tween = CreateTween();
		tween.SetTrans(Tween.TransitionType.Sine);
		tween.SetEase(Tween.EaseType.Out);

		tween.TweenProperty(this, "position:y", _startPosition.Y - BounceHeight, BounceDuration * 0.4f)
			 .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);

		tween.TweenProperty(this, "position:y", _startPosition.Y + BounceHeight * 0.15f, BounceDuration * 0.3f)
			 .SetTrans(Tween.TransitionType.Bounce).SetEase(Tween.EaseType.Out);

		tween.TweenProperty(this, "position", _startPosition, BounceDuration * 0.3f)
			 .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);

		tween.TweenCallback(Callable.From(() => _isBouncing = false));
	}

	private void OpenQuiz()
	{
		if (quizUI == null) return;

		quizContainer.Visible = true;
		quizContainer.ProcessMode = ProcessModeEnum.Inherit;

		quizUI.StartQuiz(Questions, () =>
		{
			CloseQuiz();
		});

		if (_playerThatHit != null)
			_playerThatHit.SetCanMove(false);
	}

	public void CloseQuiz()
	{
		if (quizContainer == null) return;

		quizContainer.Visible = false;
		quizContainer.ProcessMode = ProcessModeEnum.Disabled;

		if (_playerThatHit != null)
		{
			_playerThatHit.SetCanMove(true);
			_playerThatHit = null;
		}

		quizUI?.Reset();
	}

	private Control FindQuizContainer()
	{
		var candidates = GetTree().GetNodesInGroup("QuizContainerGroup");
		if (candidates.Count > 0)
			return candidates[0] as Control;

		return GetTree().Root.GetNodeOrNull<Control>("Tropic/UILayer/QuizContainer");
	}
}