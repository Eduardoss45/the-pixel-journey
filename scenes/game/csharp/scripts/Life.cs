using Godot;

public partial class Life : Area2D
{
    [Export] public int Amount { get; set; } = 1;
    [Export] public bool OnlyOnce { get; set; } = true;
    [Export] public float BobAmplitude { get; set; } = 3.0f;
    [Export] public float BobSpeed { get; set; } = 2.0f;

    private bool _collected;
    private CollisionShape2D _collisionShape;
    private GameSession _gameSession;
    private Node2D _visual;
    private float _baseVisualY;
    private float _bobTime;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        _collisionShape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        _visual = GetNodeOrNull<Node2D>("Sprite2D") ?? this;
        _baseVisualY = _visual.Position.Y;

        _gameSession = GameSession.Instance ?? GetNodeOrNull<GameSession>("/root/GameSession");
        if (_gameSession != null)
            _gameSession.RuntimeReset += OnRuntimeReset;
    }

    public override void _Process(double delta)
    {
        if (_collected)
            return;

        _bobTime += (float)delta * BobSpeed;
        float offset = Mathf.Sin(_bobTime) * BobAmplitude;
        if (_visual != null)
            _visual.Position = new Vector2(_visual.Position.X, _baseVisualY + offset);
    }

    public override void _ExitTree()
    {
        if (_gameSession != null)
            _gameSession.RuntimeReset -= OnRuntimeReset;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (_collected && OnlyOnce)
            return;

        if (body is not Player)
            return;

        var gameSession = GameSession.Instance ?? GetNodeOrNull<GameSession>("/root/GameSession");
        gameSession?.AddLives(Amount);
        _collected = true;
        SetCollectedState(true);
    }

    private void OnRuntimeReset()
    {
        _collected = false;
        SetCollectedState(false);
    }

    private void SetCollectedState(bool collected)
    {
        Visible = !collected;
        Monitoring = !collected;
        Monitorable = !collected;
        if (_collisionShape != null)
            _collisionShape.Disabled = collected;
        if (_visual != null)
            _visual.Visible = !collected;
    }
}
