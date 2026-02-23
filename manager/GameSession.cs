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

    public const int InitialLives = 5;
    public const string GameOverScenePath = "res://scenes/game/csharp/entities/game_over.tscn";

    public static GameSession Instance { get; private set; }

    public int Lives { get; private set; } = InitialLives;
    public GameState State { get; private set; } = GameState.Playing;

    public override void _Ready()
    {
        Instance = this;
    }

    public void StartNewRun()
    {
        Lives = InitialLives;
        SetState(GameState.Playing);
        EmitSignal(SignalName.LivesChanged, Lives);
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

    public void ResetGlobalRuntimeState()
    {
        var tree = GetTree();
        if (tree != null)
            tree.Paused = false;

        ObjectManager.Instance?.ResetRuntimeState();
        QuizManager.Instance?.ResetRuntimeState();
    }

    private void SetState(GameState newState)
    {
        if (State == newState)
            return;

        State = newState;
        EmitSignal(SignalName.StateChanged, (int)State);
    }
}
