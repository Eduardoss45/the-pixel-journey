using Godot;
using System.Collections.Generic;
using System.Text;

public partial class InformationMenu : Control
{
	[Signal] public delegate void MenuClosedEventHandler();

	[Export] public string InfoFilePath { get; set; } = "res://data/information_topics.json";
	[Export] public string LevelsFilePath { get; set; } = "res://data/levels.json";
	[Export] public string QuestionsFilePath { get; set; } = "res://data/questions.json";

	[Export] public NodePath OptionsContainerPath { get; set; } = "MarginContainer/VBoxContainer/HBoxContainer/ScrollConainer/VBoxContainer";
	[Export] public NodePath OptionTemplatePath { get; set; } = "MarginContainer/VBoxContainer/HBoxContainer/ScrollConainer/VBoxContainer/Option";
	[Export] public NodePath ContentLabelPath { get; set; } = "MarginContainer/VBoxContainer/HBoxContainer/RichTextLabel";
	[Export] public NodePath CloseButtonPath { get; set; } = "MarginContainer/VBoxContainer/MarginContainer/QuitButton";

	private VBoxContainer optionsContainer;
	private Button optionTemplate;
	private RichTextLabel contentLabel;
	private Button closeButton;

	private readonly List<InfoTopic> topics = new();

	public override void _Ready()
	{
		optionsContainer = GetNodeOrNull<VBoxContainer>(OptionsContainerPath);
		optionTemplate = GetNodeOrNull<Button>(OptionTemplatePath);
		contentLabel = GetNodeOrNull<RichTextLabel>(ContentLabelPath);
		closeButton = GetNodeOrNull<Button>(CloseButtonPath);

		if (optionsContainer == null || optionTemplate == null || contentLabel == null || closeButton == null)
		{
			GD.PushError("InformationMenu: hierarquia de nos incompleta.");
			return;
		}

		closeButton.Pressed += OnClosePressed;
		contentLabel.ScrollActive = true;

		LoadTopics();
		BuildTopicButtons();
		SelectTopic(0);
	}

	public override void _ExitTree()
	{
		if (closeButton != null)
			closeButton.Pressed -= OnClosePressed;
	}

	public void OpenMenu()
	{
		Visible = true;
		if (topics.Count > 0)
			SelectTopic(0);
	}

	public void CloseMenu()
	{
		Visible = false;
	}

	private void OnClosePressed()
	{
		CloseMenu();
		EmitSignal(SignalName.MenuClosed);
	}

	private void LoadTopics()
	{
		topics.Clear();

		if (TryLoadInfoFile())
			return;

		var generated = BuildTopicsFromExistingData();
		if (generated.Count == 0)
		{
			generated.Add(new InfoTopic
			{
				Title = "Introducao",
				Content = "Use os desafios para praticar variaveis, condicoes e funcoes. Tente escrever, testar e ajustar seu codigo ate passar."
			});
		}

		topics.AddRange(generated);
		TrySaveInfoFile(topics);
	}

	private bool TryLoadInfoFile()
	{
		if (!FileAccess.FileExists(InfoFilePath))
			return false;

		using var file = FileAccess.Open(InfoFilePath, FileAccess.ModeFlags.Read);
		if (file == null)
			return false;

		var parsed = Json.ParseString(file.GetAsText());
		if (parsed.VariantType != Variant.Type.Array)
			return false;

		var array = parsed.AsGodotArray();
		foreach (var item in array)
		{
			if (item.VariantType != Variant.Type.Dictionary)
				continue;

			var dict = item.AsGodotDictionary();
			var title = dict.ContainsKey("title") ? dict["title"].AsString() : "";
			var content = dict.ContainsKey("content") ? dict["content"].AsString() : "";

			if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
				continue;

			topics.Add(new InfoTopic { Title = title, Content = content });
		}

		return topics.Count > 0;
	}

	private List<InfoTopic> BuildTopicsFromExistingData()
	{
		var result = new List<InfoTopic>();


		if (FileAccess.FileExists(LevelsFilePath))
		{
			using var levelsFile = FileAccess.Open(LevelsFilePath, FileAccess.ModeFlags.Read);
			var parsedLevels = Json.ParseString(levelsFile.GetAsText());

			if (parsedLevels.VariantType == Variant.Type.Dictionary)
			{
				var levelsRoot = parsedLevels.AsGodotDictionary();
				if (levelsRoot.ContainsKey("levels"))
				{
					var levelsArray = levelsRoot["levels"].AsGodotArray();
					foreach (var entry in levelsArray)
					{
						if (entry.VariantType != Variant.Type.Dictionary)
							continue;

						var level = entry.AsGodotDictionary();
						var levelId = level.ContainsKey("levelId") ? level["levelId"].AsString() : "topico";
						var instruction = level.ContainsKey("instruction") ? level["instruction"].AsString() : "Resolva a atividade proposta.";
						var type = level.ContainsKey("type") ? level["type"].AsString() : "geral";
						var effect = level.ContainsKey("effect") ? level["effect"].AsString() : "efeito do jogo";

						var title = $"Topico {result.Count + 1}: {ToLabel(levelId)}";
						var content = BuildLessonText(instruction, type, effect);
						result.Add(new InfoTopic { Title = title, Content = content });
					}
				}
			}
		}


		if (FileAccess.FileExists(QuestionsFilePath))
		{
			using var questionsFile = FileAccess.Open(QuestionsFilePath, FileAccess.ModeFlags.Read);
			var parsedQuestions = Json.ParseString(questionsFile.GetAsText());
			if (parsedQuestions.VariantType == Variant.Type.Array)
			{
				var questions = parsedQuestions.AsGodotArray();
				if (questions.Count > 0)
				{
					var sb = new StringBuilder();
					sb.AppendLine("Dicas para os quizzes:");
					sb.AppendLine("- Leia a pergunta ate o fim antes de escolher.");
					sb.AppendLine("- Elimine opcoes claramente erradas.");
					sb.AppendLine("- Compare palavras-chave: let, const, null, true/false.");
					sb.AppendLine();
					sb.AppendLine("Referencia: https://developer.mozilla.org/en-US/docs/Web/JavaScript");

					result.Add(new InfoTopic
					{
						Title = $"Topico {result.Count + 1}: Dicas de Quiz",
						Content = sb.ToString().TrimEnd()
					});
				}
			}
		}

		return result;
	}

	private static string BuildLessonText(string instruction, string type, string effect)
	{
		var sb = new StringBuilder();
		sb.AppendLine($"Objetivo da licao: {instruction}");
		sb.AppendLine();
		sb.AppendLine($"Tipo de desafio: {type}");
		sb.AppendLine($"Impacto no jogo: {effect}");
		sb.AppendLine();
		sb.AppendLine("Como resolver:");
		sb.AppendLine("- Identifique o nome exato da variavel ou funcao pedida.");
		sb.AppendLine("- Escreva a solucao em passos pequenos.");
		sb.AppendLine("- Confira maiusculas/minusculas e valor esperado.");
		sb.AppendLine("- Teste e ajuste ate o efeito aparecer no cenario.");
		sb.AppendLine();
		sb.AppendLine("Referencia sugerida:");
		sb.AppendLine("- MDN JavaScript Guide: https://developer.mozilla.org/en-US/docs/Web/JavaScript/Guide");
		return sb.ToString().TrimEnd();
	}

	private void TrySaveInfoFile(List<InfoTopic> generatedTopics)
	{
		var array = new Godot.Collections.Array<Godot.Collections.Dictionary>();
		foreach (var topic in generatedTopics)
		{
			array.Add(new Godot.Collections.Dictionary
			{
				{ "title", topic.Title },
				{ "content", topic.Content }
			});
		}

		using var file = FileAccess.Open(InfoFilePath, FileAccess.ModeFlags.Write);
		file?.StoreString(Json.Stringify(array, "\t"));
	}

	private void BuildTopicButtons()
	{
		for (int i = optionsContainer.GetChildCount() - 1; i >= 0; i--)
		{
			var child = optionsContainer.GetChild(i);
			if (child != optionTemplate)
				child.QueueFree();
		}

		optionTemplate.Visible = false;

		for (int index = 0; index < topics.Count; index++)
		{
			var button = CreateTopicButton(index);
			optionsContainer.AddChild(button);
		}
	}

	private Button CreateTopicButton(int index)
	{
		var button = new Button();
		var topic = topics[index];

		button.Name = $"TopicButton{index}";
		button.Text = topic.Title;
		button.CustomMinimumSize = optionTemplate.CustomMinimumSize;
		button.SizeFlagsHorizontal = optionTemplate.SizeFlagsHorizontal;
		button.SizeFlagsVertical = optionTemplate.SizeFlagsVertical;
		button.Theme = optionTemplate.Theme;
		var buttonFont = optionTemplate.GetThemeFont("font");
		if (buttonFont != null)
			button.AddThemeFontOverride("font", buttonFont);
		button.AddThemeFontSizeOverride("font_size", optionTemplate.GetThemeFontSize("font_size"));
		var normalStyle = optionTemplate.GetThemeStylebox("normal");
		if (normalStyle != null)
			button.AddThemeStyleboxOverride("normal", normalStyle);
		var focusStyle = optionTemplate.GetThemeStylebox("focus");
		if (focusStyle != null)
			button.AddThemeStyleboxOverride("focus", focusStyle);
		button.Pressed += () => SelectTopic(index);
		return button;
	}

	private void SelectTopic(int index)
	{
		if (index < 0 || index >= topics.Count || contentLabel == null)
			return;

		contentLabel.Text = topics[index].Content;
		contentLabel.ScrollToLine(0);
	}

	private static string ToLabel(string raw)
	{
		if (string.IsNullOrWhiteSpace(raw))
			return "Topico";

		return raw.Replace("_", " ");
	}

	private sealed class InfoTopic
	{
		public string Title { get; set; }
		public string Content { get; set; }
	}
}
