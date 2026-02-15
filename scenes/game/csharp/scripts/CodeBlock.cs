using Godot;
using System;

public partial class CodeBlock : AnimatableBody2D
{
	[Export] public string[] TargetMechanismIds;
	[Export] public string LevelId { get; set; } = "functions_move_01";
	[Export] public float BounceHeight = 10f;
	[Export] public float BounceDuration = 0.6f;

	private Vector2 _startPosition;
	private bool _isBouncing = false;

	private Player _playerThatHit = null;
	private Control codeEditorContainer;

	public override void _Ready()
	{
		_startPosition = Position;

		var hitDetector = GetNode<Area2D>("Area2D");
		hitDetector.BodyEntered += OnBodyEntered;

		codeEditorContainer = FindCodeEditorContainer();

		if (codeEditorContainer == null)
		{
			GD.PrintErr("CodeEditorContainer NÃO ENCONTRADO em lugar nenhum! Verifique grupos ou nome do nó.");
		}
		else
		{
			codeEditorContainer.Visible = false;
			codeEditorContainer.ProcessMode = ProcessModeEnum.Disabled;
			GD.Print("CodeEditorContainer encontrado com sucesso via: " +
					 (codeEditorContainer.IsInGroup("CodeEditorUI") ? "grupo" : "outra forma"));
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
		OpenCodeEditor();
	}

	private void AnimateBounce()
	{
		var tween = CreateTween();
		tween.SetTrans(Tween.TransitionType.Sine);
		tween.SetEase(Tween.EaseType.Out);

		tween.TweenProperty(this, "position:y", _startPosition.Y - BounceHeight, BounceDuration * 0.4f)
			 .SetTrans(Tween.TransitionType.Cubic)
			 .SetEase(Tween.EaseType.Out);

		tween.TweenProperty(this, "position:y", _startPosition.Y + BounceHeight * 0.15f, BounceDuration * 0.3f)
			 .SetTrans(Tween.TransitionType.Bounce)
			 .SetEase(Tween.EaseType.Out);

		tween.TweenProperty(this, "position", _startPosition, BounceDuration * 0.3f)
			 .SetTrans(Tween.TransitionType.Sine)
			 .SetEase(Tween.EaseType.InOut);

		tween.TweenCallback(Callable.From(() => _isBouncing = false));
	}

	private void OpenCodeEditor()
	{
		if (codeEditorContainer == null)
		{
			GD.PrintErr("Tentou abrir CodeEditor mas container é null!");
			return;
		}

		codeEditorContainer.Visible = true;
		codeEditorContainer.ProcessMode = ProcessModeEnum.Inherit;

		var editorUI = codeEditorContainer.GetNodeOrNull<CodeEditorUI>("Code");
		if (editorUI != null)
		{
			editorUI.Open(this, LevelId);
			GD.Print($"CodeBlock {Name} abriu o editor e passou referência para CodeEditorUI");
		}
		else
		{
			GD.PrintErr("Nó 'Code' (CodeEditorUI) não encontrado dentro de CodeEditorContainer!");
		}

		if (_playerThatHit != null)
		{
			_playerThatHit.SetCanMove(false);
			GD.Print("Code editor aberto → Player parado via referência direta");
		}
		else
		{
			GD.PrintErr("Nenhum player registrado na colisão! Movimento não bloqueado.");
		}
	}

	public void CloseCodeEditor()
	{
		if (codeEditorContainer == null) return;

		codeEditorContainer.Visible = false;
		codeEditorContainer.ProcessMode = ProcessModeEnum.Disabled;

		if (_playerThatHit != null)
		{
			_playerThatHit.SetCanMove(true);
			GD.Print($"Code editor fechado → Player liberado (CodeBlock: {Name})");
			_playerThatHit = null;
		}
		else
		{
			GD.Print("CloseCodeEditor chamado, mas player já estava liberado ou não registrado.");
		}


		var editorUI = codeEditorContainer.GetNodeOrNull<CodeEditorUI>("Code");
		if (editorUI != null)
		{
			editorUI.codeBlockParent = null;
		}
	}

	private Control FindCodeEditorContainer()
	{

		var candidates = GetTree().GetNodesInGroup("CodeEditorUI");
		if (candidates.Count > 0)
		{
			if (candidates.Count > 1)
				GD.Print("Aviso: Mais de um nó no grupo 'CodeEditorUI'. Usando o primeiro.");
			return candidates[0] as Control;
		}


		var ui = GetTree().Root.FindChild("CodeEditorContainer", true, false) as Control;
		if (ui != null) return ui;


		return GetNodeOrNull<Control>("../../UILayer/CodeEditorContainer");
	}
}