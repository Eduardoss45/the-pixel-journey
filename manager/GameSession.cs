using Godot;

public partial class GameSession : Node
{
    public enum GameState
    {
        Playing,
        PlayerDead,
        GameOver
    }

    [Signal] public delegate void LivesChangedEventHandler(int lives);
    [Signal] public delegate void StateChangedEventHandler(int state);
    [Signal] public delegate void RuntimeResetEventHandler();

    public const int InitialLives = 5;
    public const string GameOverScenePath = "res://scenes/game/csharp/entities/game_over.tscn";

    public static GameSession Instance { get; private set; }

    public int Lives { get; private set; } = InitialLives;
    public GameState State { get; private set; } = GameState.Playing;
    public Vector2 RespawnPosition { get; private set; } = Vector2.Zero;
    public bool HasRespawnPosition { get; private set; }
    public bool BossDefeated { get; private set; }
    public bool BossActive { get; private set; }

    public override void _Ready()
    {
        Instance = this;
    }

    public void StartNewRun()
    {
        Lives = InitialLives;
        SetState(GameState.Playing);
        EmitSignal(SignalName.LivesChanged, Lives);
        ResetRespawnPosition();
        BossDefeated = false;
        BossActive = false;
    }

    public void EnterPlayingState()
    {
        SetState(GameState.Playing);
    }

    public bool CanProcessPlayerDeath()
    {
        return State == GameState.Playing;
    }

    public bool ConsumeLifeAndSetState()
    {
        if (!CanProcessPlayerDeath())
            return false;

        Lives = Mathf.Max(0, Lives - 1);
        EmitSignal(SignalName.LivesChanged, Lives);

        SetState(Lives > 0 ? GameState.PlayerDead : GameState.GameOver);
        return true;
    }

    public bool ConsumeLifeSilently()
    {
        if (Lives <= 0)
            return false;

        Lives = Mathf.Max(0, Lives - 1);
        EmitSignal(SignalName.LivesChanged, Lives);
        return Lives > 0;
    }

    public void AddLives(int amount)
    {
        if (amount <= 0)
            return;

        Lives += amount;
        EmitSignal(SignalName.LivesChanged, Lives);
    }

    public void ResetGlobalRuntimeState()
    {
        var tree = GetTree();
        if (tree != null)
            tree.Paused = false;

        ObjectManager.Instance?.ResetRuntimeState();
        QuizManager.Instance?.ResetRuntimeState();
        EmitSignal(SignalName.RuntimeReset);
    }

    public void SetRespawnPosition(Vector2 position)
    {
        RespawnPosition = position;
        HasRespawnPosition = true;
    }

    public Vector2 GetRespawnPositionOr(Vector2 fallback)
    {
        return HasRespawnPosition ? RespawnPosition : fallback;
    }

    public void ResetRespawnPosition()
    {
        HasRespawnPosition = false;
        RespawnPosition = Vector2.Zero;
    }

    public void MarkBossDefeated()
    {
        BossDefeated = true;
        BossActive = false;
    }

    public void SetBossActive(bool value)
    {
        BossActive = value;
        if (!value && !BossDefeated)
        {
            // keep defeated state if already set
            BossDefeated = false;
        }
    }

    private void SetState(GameState newState)
    {
        if (State == newState)
            return;

        State = newState;
        EmitSignal(SignalName.StateChanged, (int)State);
    }
}
