using System;
using Godot;
using System.Threading.Tasks;

public partial class MainMenu : Control
{
    [Export]
    private TextureRect Background;

    [Export]
    private Button BtnIniciar;

    [Export]
    private Button BtnSair;

    [Export] private AudioStreamPlayer2D MenuMusic;

    [Export] private VideoStreamPlayer Cutscene;

    public override void _Ready()
    {
        BtnIniciar = GetNode<Button>("ContainerCentral/Btn_iniciar");
        BtnSair = GetNode<Button>("ContainerInferior/Margem/Btn_sair");
        MenuMusic ??= GetNodeOrNull<AudioStreamPlayer2D>("AudioStreamPlayer2D");
        Cutscene ??= GetNodeOrNull<VideoStreamPlayer>("VideoStreamPlayer");

        if (Cutscene != null)
        {
            Cutscene.Expand = true;
            Cutscene.SetAnchorsPreset(LayoutPreset.FullRect);
            Cutscene.OffsetLeft = 0;
            Cutscene.OffsetTop = 0;
            Cutscene.OffsetRight = 0;
            Cutscene.OffsetBottom = 0;
            Cutscene.CustomMinimumSize = Vector2.Zero;
            Cutscene.Size = GetViewportRect().Size;
            Cutscene.Visible = false;
            Cutscene.Paused = true;
            Cutscene.ZIndex = 100;
            Cutscene.MouseFilter = MouseFilterEnum.Ignore;
        }

        BtnIniciar.Pressed += OnBtnIniciarPressed;
        BtnSair.Pressed += OnBtnSairPressed;
    }

    private async void OnBtnIniciarPressed()
    {
        BtnIniciar.Disabled = true;
        BtnSair.Disabled = true;


        MenuMusic?.Stop();


        await PlayCutscene();


        GameSession.Instance?.StartNewRun();


        GetTree().ChangeSceneToFile("res://scenes/game/csharp/scenes/tropic.tscn");
    }

    private async Task PlayCutscene()
    {
        if (Cutscene == null || Cutscene.Stream == null)
            return;

        Cutscene.Size = GetViewportRect().Size;
        Cutscene.Visible = true;
        Cutscene.Paused = false;
        Cutscene.StreamPosition = 0.0;
        Cutscene.Play();

        await ToSignal(Cutscene, VideoStreamPlayer.SignalName.Finished);

        Cutscene.Stop();
        Cutscene.Visible = false;
    }

    private void OnBtnSairPressed()
    {
        GetTree().Quit();
    }
}
