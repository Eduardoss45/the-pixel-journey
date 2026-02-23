using Godot;

public partial class GameOver : Control
{
	[Export] public NodePath RestartButtonPath { get; set; } = "Button";
	[Export] public string RestartScenePath { get; set; } = "res://scenes/game/csharp/scenes/tropic.tscn";

	private Button restartButton;

	public override void _Ready()
	{
		restartButton = GetNodeOrNull<Button>(RestartButtonPath);
		if (restartButton == null)
		{
			GD.PushError("Botao de restart nao encontrado na cena de Game Over.");
			return;
		}

		restartButton.Pressed += OnRestartPressed;
	}

	public override void _ExitTree()
	{
		if (restartButton != null)
			restartButton.Pressed -= OnRestartPressed;
	}

	private void OnRestartPressed()
	{
		var gameSession = GameSession.Instance ?? GetNodeOrNull<GameSession>("/root/GameSession");
		gameSession?.ResetGlobalRuntimeState();
		gameSession?.StartNewRun();

		GetTree().ChangeSceneToFile(RestartScenePath);
	}
}
