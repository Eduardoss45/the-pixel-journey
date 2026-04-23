using Godot;
using System;

public partial class Hud : CanvasLayer
{
	[Export] public NodePath LivesLabelPath { get; set; } = "ControlBox/HBoxNumberLifes/CountLifes";
	[Export] public NodePath PlayerPath { get; set; } = "../Player";
	[Export] public NodePath BookButtonPath { get; set; } = "Book";
	[Export] public NodePath MenuContainerPath { get; set; } = "../UILayer/MenuContainer";
	[Export] public NodePath InformationMenuPath { get; set; } = "../UILayer/MenuContainer/Menu";

	private const float FadeDurationSeconds = 0.45f;
	private const float DeathPauseSeconds = 0.15f;

	private Label livesLabel;
	private Player player;
	private TextureButton bookButton;
	private Control menuContainer;
	private InformationMenu informationMenu;
	private ColorRect fadeOverlay;
	private GameSession gameSession;
	private bool deathFlowRunning;

	public override void _Ready()
	{
		livesLabel = GetNodeOrNull<Label>(LivesLabelPath);
		player = ResolvePlayer();
		bookButton = GetNodeOrNull<TextureButton>(BookButtonPath);
		menuContainer = GetNodeOrNull<Control>(MenuContainerPath);
		informationMenu = GetNodeOrNull<InformationMenu>(InformationMenuPath);
		gameSession = GameSession.Instance ?? GetNodeOrNull<GameSession>("/root/GameSession");

		if (gameSession == null)
		{
			GD.PushError("GameSession nao encontrado.");
			return;
		}

		gameSession.LivesChanged += OnLivesChanged;
		gameSession.EnterPlayingState();
		OnLivesChanged(gameSession.Lives);

		if (player == null)
		{
			GD.PushError("Player nao encontrado para o HUD.");
			return;
		}

		player.DeathTriggered += OnPlayerDeathTriggered;
		if (bookButton != null)
			bookButton.Pressed += OnBookPressed;
		if (informationMenu != null)
			informationMenu.MenuClosed += OnInformationMenuClosed;

		if (menuContainer != null)
			menuContainer.Visible = false;
		informationMenu?.CloseMenu();

		EnsureFadeOverlay();
	}

	private Player ResolvePlayer()
	{
		if (PlayerPath != null && !PlayerPath.IsEmpty)
		{
			var byPath = GetNodeOrNull<Player>(PlayerPath);
			if (byPath != null)
				return byPath;
		}

		var players = GetTree().GetNodesInGroup("Player");
		if (players.Count > 0 && players[0] is Player groupedPlayer)
			return groupedPlayer;

		return GetTree().Root.FindChild("Player", true, false) as Player;
	}

	public override void _ExitTree()
	{
		if (gameSession != null)
			gameSession.LivesChanged -= OnLivesChanged;

		if (player != null)
			player.DeathTriggered -= OnPlayerDeathTriggered;
		if (bookButton != null)
			bookButton.Pressed -= OnBookPressed;
		if (informationMenu != null)
			informationMenu.MenuClosed -= OnInformationMenuClosed;
	}

	private void OnLivesChanged(int lives)
	{
		if (livesLabel != null)
			livesLabel.Text = lives.ToString();
	}

	private async void OnPlayerDeathTriggered()
	{
		if (deathFlowRunning || gameSession == null || !gameSession.CanProcessPlayerDeath())
			return;

		deathFlowRunning = true;
		SfxBus.Instance?.PlayLoose();
		player?.SetCanMove(false);
		CloseInformationMenu();

		if (!gameSession.ConsumeLifeAndSetState())
		{
			deathFlowRunning = false;
			return;
		}

		var deathCause = player?.LastDeathCause ?? Player.DeathCause.Damage;

		await PlayDeathVisualSequence();

		if (gameSession.State == GameSession.GameState.PlayerDead &&
			deathCause == Player.DeathCause.Damage)
		{
			gameSession.EnterPlayingState();
			var respawnPosition = gameSession.GetRespawnPositionOr(player?.GlobalPosition ?? Vector2.Zero);
			player?.RespawnAt(respawnPosition);
			await FadeBackIn();
			deathFlowRunning = false;
			return;
		}

		gameSession.ResetGlobalRuntimeState();

		if (gameSession.State == GameSession.GameState.PlayerDead)
		{
			gameSession.EnterPlayingState();
			GetTree().ReloadCurrentScene();
			return;
		}

		GetTree().ChangeSceneToFile(GameSession.GameOverScenePath);
	}

	private void OnBookPressed()
	{
		if (deathFlowRunning || gameSession == null || gameSession.State != GameSession.GameState.Playing)
			return;

		if (menuContainer == null || informationMenu == null)
			return;

		if (menuContainer.Visible)
		{
			CloseInformationMenu(true);
			player?.SetCanMove(true);
			return;
		}

		menuContainer.Visible = true;
		informationMenu.OpenMenu();
		SfxBus.Instance?.PlayOpen();
		player?.SetCanMove(false);
	}

	private void OnInformationMenuClosed()
	{
		CloseInformationMenu(true);
		if (!deathFlowRunning && gameSession != null && gameSession.State == GameSession.GameState.Playing)
			player?.SetCanMove(true);
	}

	private void CloseInformationMenu(bool playSfx = false)
	{
		if (playSfx && menuContainer != null && menuContainer.Visible)
			SfxBus.Instance?.PlayClose();

		informationMenu?.CloseMenu();
		if (menuContainer != null)
			menuContainer.Visible = false;
	}

	private async System.Threading.Tasks.Task PlayDeathVisualSequence()
	{
		if (fadeOverlay == null)
		{
			await ToSignal(GetTree().CreateTimer(FadeDurationSeconds + DeathPauseSeconds), SceneTreeTimer.SignalName.Timeout);
			return;
		}

		fadeOverlay.Modulate = new Color(1, 1, 1, 0);
		var tween = CreateTween();
		tween.TweenProperty(fadeOverlay, "modulate:a", 1.0f, FadeDurationSeconds);
		await ToSignal(tween, Tween.SignalName.Finished);
		await ToSignal(GetTree().CreateTimer(DeathPauseSeconds), SceneTreeTimer.SignalName.Timeout);
	}

	private async System.Threading.Tasks.Task FadeBackIn()
	{
		if (fadeOverlay == null)
			return;

		var tween = CreateTween();
		tween.TweenProperty(fadeOverlay, "modulate:a", 0.0f, FadeDurationSeconds);
		await ToSignal(tween, Tween.SignalName.Finished);
	}

	private void EnsureFadeOverlay()
	{
		fadeOverlay = GetNodeOrNull<ColorRect>("DeathFade");
		if (fadeOverlay != null)
		{
			fadeOverlay.Modulate = new Color(1, 1, 1, 0);
			return;
		}

		fadeOverlay = new ColorRect();
		fadeOverlay.Name = "DeathFade";
		fadeOverlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		fadeOverlay.Color = new Color(0, 0, 0, 1);
		fadeOverlay.Modulate = new Color(1, 1, 1, 0);
		fadeOverlay.MouseFilter = Control.MouseFilterEnum.Ignore;
		fadeOverlay.ZIndex = 100;
		AddChild(fadeOverlay);
	}
}
