using Godot;
using System;
using CodeEditor.Logic; // presumindo que HtmlValidator está aqui

public partial class CodeEditorUI : Node
{
    [Export] private CodeEdit codeEdit = null!;
    [Export] private Button verifyButton = null!;
    [Export] private Label feedbackLabel = null!;

    [Export] private FontFile editorFont;
    [Export] private int fontSize = 15;

    public override void _Ready()
    {
        codeEdit ??= GetNodeOrNull<CodeEdit>("%CodeEdit") ?? GetNodeOrNull<CodeEdit>("CodeEdit");
        verifyButton ??= GetNodeOrNull<Button>("%VerifyButton") ?? GetNodeOrNull<Button>("Verify");
        feedbackLabel ??= GetNodeOrNull<Label>("%FeedbackLabel") ?? GetNodeOrNull<Label>("Feedback");

        if (codeEdit == null || verifyButton == null || feedbackLabel == null)
        {
            GD.PrintErr("Componentes UI essenciais não encontrados.");
            return;
        }

        ApplyVSCodeLikeTheme();
        SetupHtmlHighlighterCloserToVSCode();
        SetupEditorPreferences();

        verifyButton.Pressed += OnVerifyPressed;

        feedbackLabel.Text = "Digite seu código HTML e clique em Verificar!";
        feedbackLabel.Modulate = Colors.White;
    }

    private void ApplyVSCodeLikeTheme()
    {
        // Cores próximas do VS Code Dark+ (valores hex aproximados 2024–2026)
        codeEdit.AddThemeColorOverride("background_color",    new Color(0.118f, 0.118f, 0.118f)); // #1e1e1e
        codeEdit.AddThemeColorOverride("font_color",          new Color(0.831f, 0.831f, 0.831f)); // #d4d4d4
        codeEdit.AddThemeColorOverride("font_readonly_color", new Color(0.6f, 0.6f, 0.6f));
        codeEdit.AddThemeColorOverride("caret_color",         new Color(0.682f, 0.686f, 0.678f)); // #aeafad
        codeEdit.AddThemeColorOverride("selection_color",     new Color(0.149f, 0.310f, 0.471f, 0.8f)); // #264f78 com alpha
        codeEdit.AddThemeColorOverride("current_line_color",  new Color(0.157f, 0.157f, 0.157f)); // #282828
        codeEdit.AddThemeColorOverride("line_number_color",   new Color(0.522f, 0.522f, 0.522f)); // #858585
        codeEdit.AddThemeColorOverride("gutter_background",   new Color(0.145f, 0.145f, 0.145f)); // #252526

        // Se a fonte for moderna (JetBrains Mono, Fira Code, etc.)
        if (editorFont != null)
        {
            codeEdit.AddThemeFontOverride("font", editorFont);
            codeEdit.AddThemeFontSizeOverride("font_size", fontSize);
        }
    }

    private void SetupHtmlHighlighterCloserToVSCode()
    {
        var hl = new CodeHighlighter();

        // Comentários HTML
        hl.AddColorRegion("<!--", "-->", new Color("#6a9955"));

        // Strings
        hl.AddColorRegion("\"", "\"", new Color("#ce9178"), false);
        hl.AddColorRegion("'", "'", new Color("#ce9178"), false);

        // Tags HTML
        string[] tags = { "html", "head", "body", "title", "meta", "link", "script", "div", "span", "p", "h1","h2","h3", "img", "a", "ul", "li", "form", "input", "button", "style" };
        foreach (var tag in tags)
            hl.AddKeywordColor(tag, new Color("#569cd6"));

        // Atributos
        string[] attrs = { "id", "class", "style", "src", "alt", "href", "type", "name", "value", "placeholder", "charset", "content", "rel", "viewport" };
        foreach (var attr in attrs)
            hl.AddKeywordColor(attr, new Color("#9cdcfe"));

        // !DOCTYPE e especiais
        hl.AddKeywordColor("!doctype", new Color("#c586c0"));

        hl.SymbolColor = new Color("#d4d4d4");
        hl.NumberColor = new Color("#b5cea8");

        codeEdit.SyntaxHighlighter = hl;
    }

    private void SetupEditorPreferences()
    {
        codeEdit.GuttersDrawLineNumbers = true;
        codeEdit.GuttersZeroPadLineNumbers = false;
        codeEdit.IndentWrappedLines = true;
        codeEdit.AutoBraceCompletionEnabled = true;
        codeEdit.IndentAutomatic = true;           // Isso existe → auto-indenta ao pressionar Enter
        codeEdit.IndentSize = 2;                   // 2 espaços — padrão VS Code para HTML/JS
        codeEdit.WrapMode = TextEdit.LineWrappingMode.Boundary;
        codeEdit.ScrollSmooth = true;
        codeEdit.ContextMenuEnabled = true;
        codeEdit.LineFolding = true;
        codeEdit.GuttersDrawFoldGutter = true;
    }

    private void OnVerifyPressed()
    {
        if (codeEdit == null) return;

        var result = HtmlValidator.Validate(codeEdit.Text);

        feedbackLabel.Modulate = result.Success ? new Color("#6a9955") : new Color("#f44747");
        feedbackLabel.Text = result.Message;

        if (!result.Success && result.Errors.Count > 0)
        {
            feedbackLabel.Text += "\n\nErros encontrados:\n" + string.Join("\n• ", result.Errors);
        }

        GD.Print($"Validação → Sucesso: {result.Success} | Score: {result.Score:P0}");
    }
}