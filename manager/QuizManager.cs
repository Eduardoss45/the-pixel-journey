using Godot;
using System.Collections.Generic;

public partial class QuizQuestion : GodotObject
{
    public string QuestionId { get; set; } = "q1";
    public string Instruction { get; set; } = "Pergunta aqui...";

    public string[] OptionKeys { get; set; } = new[] { "A", "B", "C", "D" };
    public string[] OptionValues { get; set; } = new[] { "Opção A", "Opção B", "Opção C", "Opção D" };

    public string CorrectOption { get; set; } = "B";

    public Godot.Collections.Dictionary<string, string> GetOptionsDict()
    {
        var dict = new Godot.Collections.Dictionary<string, string>();
        int count = Mathf.Min(OptionKeys.Length, OptionValues.Length);
        for (int i = 0; i < count; i++)
        {
            dict[OptionKeys[i]] = OptionValues[i];
        }
        return dict;
    }

    public string GetErrorMessage(string selected)
    {
        return $"Errado! A resposta correta é {CorrectOption}.";
    }
}