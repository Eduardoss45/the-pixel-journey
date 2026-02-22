using Godot;
using System;
using System.Collections.Generic;

public partial class QuizUI : Control
{
	private Label instructionLabel;
	private CheckBox optionA, optionB, optionC, optionD;
	private Button verifyButton;
	private Label progressLabel;

	private List<QuizQuestion> pendingQuestions = new();
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
		progressLabel = GetNode<Label>("QuizPanel/HBoxContainer/MarginScore/ScoreLabel");

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

		Visible = true;
		playerNode?.Call("SetCanMove", false);

		if (pendingQuestions.Count == 0)
		{
			instructionLabel.Text = "Nenhuma pergunta configurada para este quiz.";
			verifyButton.Disabled = true;
			UpdateVerifyStyle();
			UpdateProgress();
			return;
		}

		LoadCurrentQuestion();
	}

	private void LoadCurrentQuestion()
	{
		if (pendingQuestions.Count == 0)
		{
			FinishQuiz();
			return;
		}

		var q = pendingQuestions[0];
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
		UpdateProgress();
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
		if (pendingQuestions.Count == 0)
			return;

		var q = pendingQuestions[0];
		bool isCorrect = selectedKey == q.CorrectOption;

		ResetOptionColors();

		if (isCorrect)
		{
			if (selectedCheckBox != null)
				selectedCheckBox.AddThemeColorOverride("font_color", Colors.Green);

			correctCount++;
			pendingQuestions.RemoveAt(0);
		}
		else
		{
			if (selectedCheckBox != null)
				selectedCheckBox.AddThemeColorOverride("font_color", Colors.Red);

			pendingQuestions.RemoveAt(0);
			pendingQuestions.Add(q);

			var timer = GetTree().CreateTimer(1.2);
			timer.Timeout += () =>
			{
				if (IsInstanceValid(selectedCheckBox))
					selectedCheckBox.RemoveThemeColorOverride("font_color");
			};
		}

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
			cb.RemoveThemeColorOverride("font_color");
	}

	private void UpdateVerifyStyle()
	{
		verifyButton.Modulate = verifyButton.Disabled
			? new Color(0.6f, 0.6f, 0.6f)
			: new Color(0.2f, 1.0f, 0.3f);
	}

	private void UpdateProgress()
	{
		if (totalQuestions <= 0)
		{
			progressLabel.Text = "Etapas: 0/0";
			return;
		}

		int visibleStep = Mathf.Clamp(correctCount + 1, 1, totalQuestions);
		progressLabel.Text = $"Etapas: {visibleStep}/{totalQuestions}";
	}

	private void FinishQuiz()
	{
		instructionLabel.Text = $"Concluido! Acertos: {correctCount} de {totalQuestions}";
		verifyButton.Disabled = true;
		UpdateVerifyStyle();
		progressLabel.Text = $"Etapas: {totalQuestions}/{totalQuestions}";

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
		correctCount = 0;
		totalQuestions = 0;
		selectedKey = null;
		selectedCheckBox = null;
		ResetOptionColors();
		verifyButton.Disabled = true;
		UpdateVerifyStyle();
		UpdateProgress();
	}
}
