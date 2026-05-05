using System;
using System.Threading.Tasks;
using Godot;

public partial class MainMenu : Control
{
    private const string DefaultWelcomeCutscenePath = "res://assets/cutscenes/welcome.mp4";
    private const string DefaultCreditsCutscenePath = "res://assets/cutscenes/credits.mp4";

    [Export]
    private TextureRect Background;

    [Export]
    private Button BtnIniciar;

    [Export]
    private Button BtnCreditos;

    [Export]
    private Button BtnSair;

    [Export]
    private AudioStreamPlayer2D MenuMusic;

    [Export]
    private VideoStreamPlayer Cutscene;

    [Export]
    private Label LblSkip;

    [Export(PropertyHint.File, "*.mp4")]
    public string WelcomeCutscenePath { get; set; } = DefaultWelcomeCutscenePath;

    [Export(PropertyHint.File, "*.mp4")]
    public string CreditsCutscenePath { get; set; } = DefaultCreditsCutscenePath;

    private VideoStream _welcomeCutsceneStream;
    private VideoStream _creditsCutsceneStream;

    private bool _isPlayingCutscene = false;
    private bool _canSkip = false;
    private TaskCompletionSource _cutsceneTcs;

    public override void _Ready()
    {
        BtnIniciar = GetNode<Button>("ContainerCentral/Btn_iniciar");
        BtnCreditos = GetNodeOrNull<Button>("ContainerCentral/Btn_creditos");
        BtnSair = GetNode<Button>("ContainerInferior/Margem/Btn_sair");
        MenuMusic ??= GetNodeOrNull<AudioStreamPlayer2D>("AudioStreamPlayer2D");
        Cutscene ??= GetNodeOrNull<VideoStreamPlayer>("VideoStreamPlayer");

        _welcomeCutsceneStream = LoadCutsceneOrNull(WelcomeCutscenePath);
        _creditsCutsceneStream = LoadCutsceneOrNull(CreditsCutscenePath);

        if (Cutscene != null)
        {
            Cutscene.Expand = true;
            Cutscene.SetAnchorsPreset(LayoutPreset.FullRect);
            Cutscene.OffsetLeft = 0;
            Cutscene.OffsetTop = 0;
            Cutscene.OffsetRight = 0;
            Cutscene.OffsetBottom = 0;
            Cutscene.CustomMinimumSize = Vector2.Zero;
            Cutscene.Visible = false;
            Cutscene.Paused = true;
            Cutscene.ZIndex = 100;
            Cutscene.MouseFilter = MouseFilterEnum.Ignore;
            if (_welcomeCutsceneStream != null)
                Cutscene.Stream = _welcomeCutsceneStream;
        }

        BtnIniciar.Pressed += OnBtnIniciarPressed;
        if (BtnCreditos != null)
            BtnCreditos.Pressed += OnBtnCreditosPressed;
        BtnSair.Pressed += OnBtnSairPressed;

        LblSkip ??= GetNodeOrNull<Label>("LblSkip");

        if (LblSkip != null)
        {
            LblSkip.Visible = false;
            LblSkip.ZIndex = 200;
            LblSkip.TopLevel = true;
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (_isPlayingCutscene && _canSkip && @event.IsActionPressed("ui_cancel"))
        {
            SkipCutscene();
        }
    }

    private async void OnBtnIniciarPressed()
    {
        SetButtonsEnabled(false);
        MenuMusic?.Stop();

        await PlayCutscene(_welcomeCutsceneStream);

        GameSession.Instance?.StartNewRun();
        GetTree().ChangeSceneToFile("res://scenes/game/csharp/scenes/tropic.tscn");
    }

    private async void OnBtnCreditosPressed()
    {
        SetButtonsEnabled(false);
        MenuMusic?.Stop();

        await PlayCutscene(_creditsCutsceneStream);

        SetButtonsEnabled(true);
        if (MenuMusic != null && !MenuMusic.Playing)
            MenuMusic.Play();
    }

    private void OnCutsceneFinished()
    {
        _cutsceneTcs?.TrySetResult();
    }

    private void SkipCutscene()
    {
        if (!_isPlayingCutscene || !_canSkip)
            return;

        _cutsceneTcs?.TrySetResult();
    }

    private async Task EnableSkipAfterDelay()
    {
        await ToSignal(GetTree().CreateTimer(1.0), "timeout");

        _canSkip = true;

        if (LblSkip != null)
            LblSkip.Visible = true;
    }

    private void CleanupCutscene()
    {
        _isPlayingCutscene = false;
        _canSkip = false;

        if (Cutscene != null)
        {
            Cutscene.Stop();
            Cutscene.Visible = false;
            Cutscene.Finished -= OnCutsceneFinished;
        }

        if (LblSkip != null)
            LblSkip.Visible = false;
    }

    private async Task PlayCutscene(VideoStream stream)
    {
        if (Cutscene == null || stream == null)
            return;

        _isPlayingCutscene = true;
        _canSkip = false;
        _cutsceneTcs = new TaskCompletionSource();

        Cutscene.Visible = true;
        Cutscene.Paused = false;
        Cutscene.Stream = stream;
        Cutscene.StreamPosition = 0.0;
        Cutscene.Play();

        if (LblSkip != null)
            LblSkip.Visible = false;

        _ = EnableSkipAfterDelay();

        Cutscene.Finished += OnCutsceneFinished;

        await _cutsceneTcs.Task;

        CleanupCutscene();
    }

    private void OnBtnSairPressed()
    {
        GetTree().Quit();
    }

    private void SetButtonsEnabled(bool enabled)
    {
        if (BtnIniciar != null)
            BtnIniciar.Disabled = !enabled;
        if (BtnCreditos != null)
            BtnCreditos.Disabled = !enabled;
        if (BtnSair != null)
            BtnSair.Disabled = !enabled;
    }

    private static VideoStream LoadCutsceneOrNull(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        if (!FileAccess.FileExists(path))
        {
            GD.PushWarning($"[Cutscene] Video file not found: '{path}'.");
            return null;
        }

        if (
            path.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)
            && ClassDB.ClassExists("FFmpegVideoStream")
        )
        {
            var ffmpegVariant = ClassDB.Instantiate("FFmpegVideoStream");
            var ffmpegObject =
                ffmpegVariant.VariantType == Variant.Type.Object
                    ? ffmpegVariant.AsGodotObject()
                    : null;
            if (ffmpegObject is VideoStream ffmpegStream)
            {
                ffmpegStream.Set("file", path);
                return ffmpegStream;
            }

            GD.PushWarning(
                "[Cutscene] FFmpegVideoStream class exists but could not be instantiated as VideoStream."
            );
        }

        try
        {
            var resource = ResourceLoader.Load(path);
            if (resource is VideoStream stream)
                return stream;

            GD.PushWarning($"[Cutscene] Failed to load VideoStream from '{path}'.");
            return null;
        }
        catch (Exception ex)
        {
            GD.PushWarning($"[Cutscene] Exception while loading '{path}': {ex.Message}");
            return null;
        }
    }
}
