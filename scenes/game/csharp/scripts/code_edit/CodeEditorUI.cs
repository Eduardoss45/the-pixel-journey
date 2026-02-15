using Godot;
using System;
using System.Linq;

public partial class CodeEditorUI : Control
{
    [Export] private CodeEdit codeEdit = null!;
    [Export] private Button verifyButton = null!;
    [Export] private Button cancelButton = null!;
    [Export] private Label instructionLabel = null!;

    private LevelData currentLevelData;
    private LevelManager levelManager;

    public CodeBlock codeBlockParent;

    public override void _Ready()
    {
        // Cache do LevelManager (autoload esperado em /root/LevelManager)
        levelManager = GetNodeOrNull<LevelManager>("/root/LevelManager");

        if (levelManager == null)
            GD.PrintErr("LevelManager não encontrado em /root/LevelManager.");

        codeEdit ??= GetNodeOrNull<CodeEdit>("CodeEdit");
        verifyButton ??= GetNodeOrNull<Button>("VerifyButton");
        cancelButton ??= GetNodeOrNull<Button>("CancelButton");
        instructionLabel ??= GetNodeOrNull<Label>("MarginContainer/InstructionLabel");

        if (codeEdit == null) GD.PrintErr("CodeEdit NÃO encontrado!");
        if (verifyButton == null) GD.PrintErr("VerifyButton NÃO encontrado!");
        if (cancelButton == null) GD.PrintErr("CancelButton NÃO encontrado!");
        if (instructionLabel == null) GD.PrintErr("InstructionLabel NÃO encontrado!");

        if (codeEdit == null || verifyButton == null || cancelButton == null || instructionLabel == null)
        {
            GD.PrintErr("Componentes essenciais da UI estão faltando. Verifique os nomes na cena.");
            return;
        }

        SetupMinimalEditor();

        verifyButton.Pressed += OnVerifyPressed;
        cancelButton.Pressed += OnCancelPressed;

        instructionLabel.Text = "Escreva seu código JavaScript conforme a instrução e clique em Verificar.";
        instructionLabel.Modulate = new Color(1, 1, 1);
    }

    public void Open(CodeBlock caller, string levelId)
    {
        codeBlockParent = caller;

        if (levelManager != null)
            currentLevelData = levelManager.GetLevel(levelId);

        if (currentLevelData == null)
        {
            GD.PrintErr($"Nível não encontrado para ID: {levelId}. Usando fallback.");

            currentLevelData = new LevelData
            {
                Instruction = "Lição não configurada. Escreva qualquer código para testar.",
                LevelId = levelId
            };
        }

        Visible = true;
        ProcessMode = ProcessModeEnum.Inherit;

        codeEdit.Clear();
        codeEdit.GrabFocus();

        instructionLabel.Modulate = new Color(1, 1, 1);
        instructionLabel.Text = currentLevelData.Instruction
            ?? $"Escreva seu código para a lição {levelId} e clique em Verificar.";

        GD.Print($"Editor aberto pelo CodeBlock: {caller?.Name} | Nível: {levelId}");
    }

    private void OnVerifyPressed()
    {
        if (codeEdit == null || currentLevelData == null)
            return;

        string playerCode = codeEdit.Text.Trim();

        if (string.IsNullOrWhiteSpace(playerCode))
        {
            instructionLabel.Modulate = new Color(0.9f, 0.3f, 0.3f);
            instructionLabel.Text = "Escreva algum código antes de verificar.";
            return;
        }

        var executionResult = SandboxExecutor.Execute(playerCode, currentLevelData);

        if (executionResult == null)
        {
            instructionLabel.Modulate = new Color(0.9f, 0.3f, 0.3f);
            instructionLabel.Text = "Erro ao executar o código.";
            return;
        }

        bool success = currentLevelData.Validate(executionResult.Variables);

        if (success)
        {
            instructionLabel.Modulate = new Color(0.4f, 0.8f, 0.4f);
            instructionLabel.Text = "Correto! " + executionResult.Message;

            TriggerGameSuccess();
            codeBlockParent?.CloseCodeEditor();
        }
        else
        {
            Variant firstValue = executionResult.Variables != null &&
                     executionResult.Variables.Count > 0
    ? executionResult.Variables.Values.First()
    : new Variant();

            instructionLabel.Modulate = new Color(0.9f, 0.3f, 0.3f);
            instructionLabel.Text =
                currentLevelData.GetErrorMessage(firstValue) +
                " " +
                executionResult.Message;
        }
    }

    private void OnCancelPressed()
    {
        codeBlockParent?.CloseCodeEditor();

        instructionLabel.Modulate = new Color(1, 1, 1);
        instructionLabel.Text = "Edição cancelada.";
    }

    private void TriggerGameSuccess()
    {
        GD.Print($"Nível {currentLevelData?.LevelId ?? "desconhecido"} concluído!");
        // Aqui você pode disparar animação, sinal, abrir porta etc.
    }

    private void SetupMinimalEditor()
    {
        codeEdit.GuttersDrawLineNumbers = true;
        codeEdit.IndentWrappedLines = true;
        codeEdit.AutoBraceCompletionEnabled = true;
        codeEdit.IndentAutomatic = true;
        codeEdit.IndentSize = 2;
        codeEdit.WrapMode = TextEdit.LineWrappingMode.Boundary;
        codeEdit.ScrollSmooth = true;
        codeEdit.ContextMenuEnabled = true;
        codeEdit.LineFolding = false;
        codeEdit.PlaceholderText = "// Escreva seu código aqui...";
        codeEdit.SyntaxHighlighter = null;
    }
}
