using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class QuizUI : Control
{
	private Label instructionLabel;
	private CheckBox optionA, optionB, optionC, optionD;
	private Button verifyButton;
	private Label scoreLabel;

	private List<QuizQuestion> pendingQuestions = new();
	private int currentIndex = 0;
	private int correctCount = 0;
	private int totalQuestions = 0;
	private string selectedKey = null;
	private CheckBox selectedCheckBox = null;

	[Export] public NodePath PlayerPath { get; set; }
	private Node playerNode;

	public Action OnQuizFinished { get; set; }

	public override void _Ready()
	{
		instructionLabel = GetNode<Label>("QuizPanel/MarginLabel/InstructionLabel");
		optionA = GetNode<CheckBox>("QuizPanel/MarginVBox/VBoxContainer/OptionA");
		optionB = GetNode<CheckBox>("QuizPanel/MarginVBox/VBoxContainer/OptionB");
		optionC = GetNode<CheckBox>("QuizPanel/MarginVBox/VBoxContainer/OptionC");
		optionD = GetNode<CheckBox>("QuizPanel/MarginVBox/VBoxContainer/OptionD");
		verifyButton = GetNode<Button>("QuizPanel/HBoxContainer/MarginButton/Next");
		scoreLabel = GetNode<Label>("QuizPanel/HBoxContainer/MarginScore/ScoreLabel");

		optionA.Toggled += pressed => OnOptionToggled("A", pressed);
		optionB.Toggled += pressed => OnOptionToggled("B", pressed);
		optionC.Toggled += pressed => OnOptionToggled("C", pressed);
		optionD.Toggled += pressed => OnOptionToggled("D", pressed);

		verifyButton.Pressed += OnVerifyPressed;

		playerNode = GetNodeOrNull(PlayerPath);

		Visible = false;
	}


	public void StartQuiz(Godot.Collections.Array<QuizQuestion> questions, Action onComplete)
	{
		OnQuizFinished = onComplete;

		pendingQuestions.Clear();
		foreach (var q in questions)
			pendingQuestions.Add(q);

		totalQuestions = pendingQuestions.Count;
		correctCount = 0;
		currentIndex = 0;

		UpdateScore();
		LoadCurrentQuestion();

		Visible = true;
		playerNode?.Call("SetCanMove", false);
	}


	public void StartQuiz()
	{

	}

	private void LoadCurrentQuestion()
	{
		if (currentIndex >= pendingQuestions.Count)
		{
			FinishQuiz();
			return;
		}

		var q = pendingQuestions[currentIndex];
		instructionLabel.Text = q.Instruction;

		var opts = q.GetOptionsDict();

		optionA.Text = opts.GetValueOrDefault("A", "");
		optionB.Text = opts.GetValueOrDefault("B", "");
		optionC.Text = opts.GetValueOrDefault("C", "");
		optionD.Text = opts.GetValueOrDefault("D", "");

		optionA.ButtonPressed = false;
		optionB.ButtonPressed = false;
		optionC.ButtonPressed = false;
		optionD.ButtonPressed = false;

		ResetOptionColors();

		verifyButton.Disabled = true;
		UpdateVerifyStyle();
		selectedKey = null;
		selectedCheckBox = null;
	}

	private void OnOptionToggled(string key, bool pressed)
	{
		if (!pressed) return;

		selectedKey = key;
		selectedCheckBox = GetNode<CheckBox>($"QuizPanel/MarginVBox/VBoxContainer/Option{key}");

		verifyButton.Disabled = false;
		UpdateVerifyStyle();
	}

	private void OnVerifyPressed()
	{
		var q = pendingQuestions[currentIndex];
		bool isCorrect = selectedKey == q.CorrectOption;

		ResetOptionColors();

		if (isCorrect)
		{
			if (selectedCheckBox != null)
				selectedCheckBox.AddThemeColorOverride("font_color", Colors.Green);

			correctCount++;
			pendingQuestions.RemoveAt(currentIndex);
		}
		else
		{
			if (selectedCheckBox != null)
			{
				selectedCheckBox.AddThemeColorOverride("font_color", Colors.Red);

				var timer = GetTree().CreateTimer(1.2);
				timer.Timeout += () =>
				{
					if (IsInstanceValid(selectedCheckBox))
						selectedCheckBox.RemoveThemeColorOverride("font_color");
				};
			}

			var wrong = pendingQuestions[currentIndex];
			pendingQuestions.RemoveAt(currentIndex);
			pendingQuestions.Add(wrong);
		}

		UpdateScore();

		currentIndex = currentIndex % pendingQuestions.Count;

		if (pendingQuestions.Count == 0)
		{
			FinishQuiz();
		}
		else
		{
			var delay = GetTree().CreateTimer(0.6);
			delay.Timeout += LoadCurrentQuestion;
		}
	}

	private void ResetOptionColors()
	{
		foreach (var cb in new[] { optionA, optionB, optionC, optionD })
		{
			cb.RemoveThemeColorOverride("font_color");
		}
	}

	private void UpdateVerifyStyle()
	{
		verifyButton.Modulate = verifyButton.Disabled
			? new Color(0.6f, 0.6f, 0.6f)
			: new Color(0.2f, 1.0f, 0.3f);
	}

	private void UpdateScore()
	{
		scoreLabel.Text = $"{correctCount} / {totalQuestions}";
	}

	private void FinishQuiz()
	{
		instructionLabel.Text = $"Concluído! Acertos: {correctCount} de {totalQuestions}";
		verifyButton.Disabled = true;

		var timer = GetTree().CreateTimer(2.0);
		timer.Timeout += () =>
		{
			Visible = false;
			playerNode?.Call("SetCanMove", true);
			OnQuizFinished?.Invoke();
		};
	}


	public void Reset()
	{
		pendingQuestions.Clear();
		currentIndex = 0;
		correctCount = 0;
		totalQuestions = 0;
		selectedKey = null;
		ResetOptionColors();
		verifyButton.Disabled = true;
	}
}