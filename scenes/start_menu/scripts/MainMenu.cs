using System;
using Godot;

public partial class MainMenu : Control
{
    [Export]
    private TextureRect Background;

    [Export]
    private TextureButton BtnIniciar;

    [Export]
    private TextureButton BtnSair;

    public override void _Ready()
    {
        Background = GetNode<TextureRect>("Background");
        BtnIniciar = GetNode<TextureButton>("ContainerCentral/ContainerHorizontal/Btn_iniciar");
        BtnSair = GetNode<TextureButton>("ContainerInferiorEsquerdo/Margem/Btn_sair");

        if (Background == null)
            GD.PrintErr("Background não encontrado!");
        if (BtnIniciar == null || BtnSair == null)
            GD.PrintErr("Botões não encontrados!");

        var bgTexture = GD.Load<Texture2D>("res://scenes/start_menu/assets/background/background.png");
        Background.Texture = bgTexture;

        BtnIniciar.Pressed += OnBtnIniciarPressed;
        BtnSair.Pressed += OnBtnSairPressed;
    }

    private void OnBtnIniciarPressed()
    {
        GD.Print("Iniciar pressionado!");
        // * Aqui você pode carregar a próxima cena do jogo
    }

    private void OnBtnSairPressed()
    {
        GD.Print("Sair pressionado!");
        GetTree().Quit();
    }
}
