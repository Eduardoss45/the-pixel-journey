using Godot;
using System;
using CodeEditor.Logic;

public partial class CodeEditorUI : Node
{
    [Export] private CodeEdit codeEdit = null!;
    [Export] private Button verifyButton = null!;
    [Export] private Label feedbackLabel = null!;

    public override void _Ready()
    {
        codeEdit = GetNodeOrNull<CodeEdit>("%CodeEdit") ?? GetNodeOrNull<CodeEdit>("CodeEdit");
        verifyButton = GetNodeOrNull<Button>("%VerifyButton") ?? GetNodeOrNull<Button>("Verify");
        feedbackLabel = GetNodeOrNull<Label>("%FeedbackLabel") ?? GetNodeOrNull<Label>("Feedback");

        if (codeEdit == null || verifyButton == null || feedbackLabel == null)
        {
            GD.PrintErr("Componentes UI essenciais não encontrados.");
            return;
        }

        SetupHighlighter();
        verifyButton.Pressed += OnVerifyPressed;

        // Opcional: limpa feedback inicial
        feedbackLabel.Text = "Digite seu código HTML e clique em Verificar!";
        feedbackLabel.Modulate = Colors.White;
    }

    private void SetupHighlighter()
    {
        var highlighter = new CodeHighlighter();

        // Regiões delimitadas (prioridade alta)
        highlighter.AddColorRegion("<!--", "-->", new Color(0.4f, 0.6f, 0.4f));     // comentários
        highlighter.AddColorRegion("\"", "\"", new Color(0.8f, 1f, 0.6f));          // strings duplas
        highlighter.AddColorRegion("'", "'", new Color(0.8f, 1f, 0.6f));            // strings simples

        // Palavras-chave (nomes de tags e atributos)
        highlighter.AddKeywordColor("!doctype", new Color(0.8f, 0.8f, 0.4f));       // amarelo ouro
        highlighter.AddKeywordColor("html", new Color(1f, 0.5f, 0.8f));             // rosa
        highlighter.AddKeywordColor("head", new Color(1f, 0.5f, 0.8f));
        highlighter.AddKeywordColor("body", new Color(1f, 0.5f, 0.8f));
        highlighter.AddKeywordColor("title", new Color(1f, 0.5f, 0.8f));
        highlighter.AddKeywordColor("meta", new Color(0.5f, 0.8f, 1f));             // azul claro
        highlighter.AddKeywordColor("img", new Color(0.5f, 0.8f, 1f));

        // Atributos comuns
        highlighter.AddKeywordColor("src", new Color(0.9f, 0.6f, 1f));              // roxo claro
        highlighter.AddKeywordColor("alt", new Color(0.9f, 0.6f, 1f));
        highlighter.AddKeywordColor("charset", new Color(0.9f, 0.6f, 1f));
        highlighter.AddKeywordColor("name", new Color(0.9f, 0.6f, 1f));
        highlighter.AddKeywordColor("content", new Color(0.9f, 0.6f, 1f));
        highlighter.AddKeywordColor("viewport", new Color(0.9f, 0.6f, 1f));

        // Cores especiais
        highlighter.SymbolColor = new Color(1f, 0.7f, 0.7f);   // símbolos = / > <
        highlighter.NumberColor = new Color(0.6f, 1f, 0.8f);    // números (ex: widths, etc.)

        codeEdit.SyntaxHighlighter = highlighter;

        // Configurações do editor
        codeEdit.GuttersDrawLineNumbers = true;
        codeEdit.GuttersZeroPadLineNumbers = true;
        codeEdit.IndentWrappedLines = true;
        codeEdit.AutoBraceCompletionEnabled = true;
        codeEdit.IndentAutomatic = true;
        codeEdit.CodeCompletionEnabled = true;
        codeEdit.ScrollSmooth = true;
        codeEdit.WrapMode = TextEdit.LineWrappingMode.Boundary;
        codeEdit.ContextMenuEnabled = true;
        codeEdit.LineFolding = true;
        codeEdit.GuttersDrawFoldGutter = true;
    }

    private void OnVerifyPressed()
    {
        if (codeEdit == null) return;

        var result = HtmlValidator.Validate(codeEdit.Text);

        feedbackLabel.Modulate = result.Success ? Colors.LimeGreen : Colors.IndianRed;
        feedbackLabel.Text = result.Message;

        if (!result.Success && result.Errors.Count > 0)
        {
            feedbackLabel.Text += "\n\nErros encontrados:\n" + string.Join("\n• ", result.Errors);
        }

        GD.Print($"Validação concluída → Sucesso: {result.Success} | Pontuação: {result.Score:P0}");

        // Opcional: animação simples ou som
        // var tween = CreateTween();
        // tween.TweenProperty(feedbackLabel, "scale", Vector2.One * 1.1f, 0.15).SetTrans(Tween.TransitionType.Bounce);
        // tween.TweenProperty(feedbackLabel, "scale", Vector2.One, 0.15);
    }
}