using Godot;
using System.Collections.Generic;

public partial class QuizManager : Node
{
    public static QuizManager Instance { get; private set; }

    [Export] public string QuestionsFilePath { get; set; } = "res://data/questions.json";

    private readonly Dictionary<string, QuizQuestion> _questionsById = new();
    private readonly Dictionary<string, List<QuizQuestion>> _questionsBySet = new();
    private readonly List<QuizQuestion> _allQuestions = new();
    private QuizBlock _activeQuizBlock;

    public override void _Ready()
    {
        Instance = this;
        LoadQuestions();
    }

    public bool TryAcquireQuiz(QuizBlock block)
    {
        if (_activeQuizBlock != null && IsInstanceValid(_activeQuizBlock))
            return _activeQuizBlock == block;

        _activeQuizBlock = block;
        return true;
    }

    public void ReleaseQuiz(QuizBlock block)
    {
        if (_activeQuizBlock == block)
            _activeQuizBlock = null;
    }

    public Godot.Collections.Array<QuizQuestion> GetQuestions(
        string quizSetId,
        int[] questionIds,
        int maxQuestions
    )
    {
        var selected = new List<QuizQuestion>();

        if (questionIds != null && questionIds.Length > 0)
        {
            foreach (int id in questionIds)
            {
                string key = id.ToString();
                if (_questionsById.TryGetValue(key, out var question))
                    selected.Add(question);
                else
                    GD.PrintErr($"QuizManager: id de pergunta nao encontrado: {id}");
            }
        }
        else if (!string.IsNullOrEmpty(quizSetId) && _questionsBySet.TryGetValue(quizSetId, out var setQuestions))
        {
            foreach (var q in setQuestions)
                selected.Add(q);
        }
        else
        {
            foreach (var q in _allQuestions)
                selected.Add(q);
        }

        if (maxQuestions > 0 && selected.Count > maxQuestions)
            selected = selected.GetRange(0, maxQuestions);

        var result = new Godot.Collections.Array<QuizQuestion>();
        foreach (var q in selected)
            result.Add(q);
        return result;
    }

    private void LoadQuestions()
    {
        _questionsById.Clear();
        _questionsBySet.Clear();
        _allQuestions.Clear();

        if (!FileAccess.FileExists(QuestionsFilePath))
        {
            GD.PrintErr($"QuizManager: arquivo nao encontrado: {QuestionsFilePath}");
            return;
        }

        using var file = FileAccess.Open(QuestionsFilePath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PrintErr($"QuizManager: erro ao abrir arquivo: {QuestionsFilePath}");
            return;
        }

        var parsed = Json.ParseString(file.GetAsText());
        if (parsed.VariantType != Variant.Type.Array)
        {
            GD.PrintErr("QuizManager: JSON invalido, esperado array de perguntas.");
            return;
        }

        var questionArray = parsed.AsGodotArray();
        foreach (var item in questionArray)
        {
            if (item.VariantType != Variant.Type.Dictionary)
                continue;

            var dict = item.AsGodotDictionary();
            var question = BuildQuestion(dict);
            if (string.IsNullOrEmpty(question.QuestionId))
            {
                GD.PrintErr("QuizManager: pergunta sem id valido no JSON.");
                continue;
            }

            _questionsById[question.QuestionId] = question;
            _allQuestions.Add(question);

            if (!string.IsNullOrEmpty(question.QuizSetId))
            {
                if (!_questionsBySet.ContainsKey(question.QuizSetId))
                    _questionsBySet[question.QuizSetId] = new List<QuizQuestion>();

                _questionsBySet[question.QuizSetId].Add(question);
            }
        }

        GD.Print($"QuizManager: {_questionsById.Count} perguntas carregadas.");
    }

    private static QuizQuestion BuildQuestion(Godot.Collections.Dictionary dict)
    {
        var question = new QuizQuestion();

        if (dict.ContainsKey("id"))
            question.QuestionId = ReadQuestionId(dict["id"]);

        if (dict.ContainsKey("quizSetId"))
            question.QuizSetId = dict["quizSetId"].AsString();
        else if (dict.ContainsKey("setId"))
            question.QuizSetId = dict["setId"].AsString();

        if (dict.ContainsKey("instruction"))
            question.Instruction = dict["instruction"].AsString();

        if (dict.ContainsKey("correctOption"))
            question.CorrectOption = dict["correctOption"].AsString();

        if (dict.ContainsKey("optionKeys"))
            question.OptionKeys = ReadStringArray(dict["optionKeys"]);

        if (dict.ContainsKey("optionValues"))
            question.OptionValues = ReadStringArray(dict["optionValues"]);

        return question;
    }

    private static string ReadQuestionId(Variant value)
    {
        return value.VariantType switch
        {
            Variant.Type.Int => ((int)value.AsDouble()).ToString(),
            Variant.Type.Float => ((int)value.AsDouble()).ToString(),
            _ => value.AsString(),
        };
    }

    private static string[] ReadStringArray(Variant value)
    {
        if (value.VariantType == Variant.Type.PackedStringArray)
            return value.AsStringArray();

        if (value.VariantType == Variant.Type.Array)
        {
            var array = value.AsGodotArray();
            var result = new string[array.Count];
            for (int i = 0; i < array.Count; i++)
                result[i] = array[i].AsString();

            return result;
        }

        return System.Array.Empty<string>();
    }
}
