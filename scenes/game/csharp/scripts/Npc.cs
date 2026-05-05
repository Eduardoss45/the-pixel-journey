using System;
using Godot;

public partial class Npc : CharacterBody2D
{
    [Export]
    public NodePath VisualPath { get; set; } = "AnimationSprite2D";

    [Export]
    public NodePath InteractionAreaPath { get; set; } = "Area2D";

    [Export]
    public float FaceDeadzone { get; set; } = 2.0f;

    [Export]
    public bool AutoStartDialogue { get; set; } = true;

    [Export]
    public float AutoStartDelaySeconds { get; set; } = 2.0f;

    [Export]
    public bool ClickToStartDialogue { get; set; } = true;

    [Export]
    public string TimelinePath { get; set; } = "res://timelines/byte_intro.dtl";

    [Export]
    public string DialogicStyle { get; set; } = "res://styles/dialogs.tres";

    private Node2D _visual;
    private Player _player;
    private Node _dialogic;
    private bool _isDialogRunning;
    private Area2D _interactionArea;
    private bool _canSkip = false;

    public override void _Ready()
    {
        _dialogic = GetNodeOrNull<Node>("/root/Dialogic");
        RegisterDialogicHooks();
        _player = GetTree().GetFirstNodeInGroup("Player") as Player;

        if (VisualPath != null && !VisualPath.IsEmpty)
            _visual = GetNodeOrNull<Node2D>(VisualPath);
        if (_visual == null)
            _visual =
                GetNodeOrNull<Node2D>("AnimationSprite2D")
                ?? GetNodeOrNull<Node2D>("AnimatedSprite2D")
                ?? GetNodeOrNull<Node2D>("Sprite2D");

        if (InteractionAreaPath != null && !InteractionAreaPath.IsEmpty)
            _interactionArea = GetNodeOrNull<Area2D>(InteractionAreaPath);
        if (_interactionArea == null)
            _interactionArea = GetNodeOrNull<Area2D>("Area2D");

        if (_interactionArea != null)
        {
            _interactionArea.InputEvent += OnInteractionInput;
            _interactionArea.Monitoring = true;
            _interactionArea.Monitorable = true;
            _interactionArea.InputPickable = true;
        }

        if (AutoStartDialogue && AutoStartDelaySeconds > 0.0f)
            _ = StartDialogueAfterDelay();
    }

    public override void _Input(InputEvent @event)
    {
        if (_isDialogRunning && _canSkip && @event.IsActionPressed("ui_cancel"))
        {
            SkipDialogue();
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!ClickToStartDialogue || _isDialogRunning)
            return;

        if (
            @event is not InputEventMouseButton mouse
            || !mouse.Pressed
            || mouse.ButtonIndex != MouseButton.Left
        )
            return;

        if (IsMouseClickOverNpc(mouse.GlobalPosition))
            StartDialogue();
    }

    public override void _Process(double delta)
    {
        UpdateFacing();
    }

    public override void _ExitTree()
    {
        UnregisterDialogicHooks();
        if (_interactionArea != null)
            _interactionArea.InputEvent -= OnInteractionInput;
    }

    private async System.Threading.Tasks.Task EnableSkipAfterDelay()
    {
        await ToSignal(GetTree().CreateTimer(1.0), SceneTreeTimer.SignalName.Timeout);
        _canSkip = true;
    }

    private void OnDialogStarted()
    {
        _isDialogRunning = true;
        _canSkip = false;
        _ = EnableSkipAfterDelay();
    }

    private void OnDialogEnded()
    {
        _isDialogRunning = false;
        _canSkip = false;
    }

    private void SkipDialogue()
    {
        if (_dialogic == null || !_isDialogRunning)
            return;

        _dialogic.Call("end_timeline");
    }

    private void OnInteractionInput(Node viewport, InputEvent @event, long shapeIdx)
    {
        if (!ClickToStartDialogue)
            return;

        if (
            @event is InputEventMouseButton mouse
            && mouse.Pressed
            && mouse.ButtonIndex == MouseButton.Left
        )
            StartDialogue();
    }

    private bool IsMouseClickOverNpc(Vector2 globalMousePosition)
    {
        var spaceState = GetWorld2D().DirectSpaceState;
        var query = new PhysicsPointQueryParameters2D
        {
            Position = globalMousePosition,
            CollideWithAreas = true,
            CollideWithBodies = false,
        };

        var hits = spaceState.IntersectPoint(query);
        foreach (var hit in hits)
        {
            var colliderVariant = hit["collider"];
            if (colliderVariant.VariantType != Variant.Type.Object)
                continue;

            var colliderObject = colliderVariant.AsGodotObject();
            if (colliderObject == _interactionArea || colliderObject == this)
                return true;
        }

        return false;
    }

    private async System.Threading.Tasks.Task StartDialogueAfterDelay()
    {
        await ToSignal(
            GetTree().CreateTimer(AutoStartDelaySeconds),
            SceneTreeTimer.SignalName.Timeout
        );
        StartDialogue();
    }

    private void StartDialogue()
    {
        if (_isDialogRunning)
            return;

        if (_dialogic == null || string.IsNullOrWhiteSpace(TimelinePath))
            return;

        if (!string.IsNullOrWhiteSpace(DialogicStyle))
        {
            var stylesVariant = _dialogic.Get("Styles");
            var styles =
                stylesVariant.VariantType == Variant.Type.Object
                    ? stylesVariant.AsGodotObject() as Node
                    : null;
            styles?.Call("change_style", DialogicStyle, true);
        }

        Variant timelineArg = TimelinePath;

        if (TimelinePath.Contains("://") && ResourceLoader.Exists(TimelinePath))
        {
            var timelineRes = ResourceLoader.Load(TimelinePath);
            if (timelineRes != null)
                timelineArg = timelineRes;
        }

        _dialogic.Call("start", timelineArg);
    }

    private void RegisterDialogicHooks()
    {
        if (_dialogic == null)
            return;

        var onStarted = new Callable(this, nameof(OnDialogStarted));
        var onEnded = new Callable(this, nameof(OnDialogEnded));

        if (!_dialogic.IsConnected("timeline_started", onStarted))
            _dialogic.Connect("timeline_started", onStarted);
        if (!_dialogic.IsConnected("timeline_ended", onEnded))
            _dialogic.Connect("timeline_ended", onEnded);
    }

    private void UnregisterDialogicHooks()
    {
        if (_dialogic == null)
            return;

        var onStarted = new Callable(this, nameof(OnDialogStarted));
        var onEnded = new Callable(this, nameof(OnDialogEnded));

        if (_dialogic.IsConnected("timeline_started", onStarted))
            _dialogic.Disconnect("timeline_started", onStarted);
        if (_dialogic.IsConnected("timeline_ended", onEnded))
            _dialogic.Disconnect("timeline_ended", onEnded);
    }

    private void UpdateFacing()
    {
        if (_player == null)
            _player = GetTree().GetFirstNodeInGroup("Player") as Player;

        if (_player == null || _visual == null)
            return;

        float deltaX = _player.GlobalPosition.X - GlobalPosition.X;
        if (Mathf.Abs(deltaX) <= FaceDeadzone)
            return;

        bool faceRight = deltaX > 0.0f;

        if (_visual is Sprite2D sprite)
        {
            sprite.FlipH = !faceRight;
            return;
        }

        if (_visual is AnimatedSprite2D anim)
        {
            anim.FlipH = !faceRight;
            return;
        }

        Vector2 scale = _visual.Scale;
        scale.X = Mathf.Abs(scale.X) * (faceRight ? 1.0f : -1.0f);
        _visual.Scale = scale;
    }
}
