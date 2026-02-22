using Godot;

public partial class QuizQuestion : GodotObject
{
    public string QuestionId { get; set; } = "1";
    public string QuizSetId { get; set; } = "";
    public string Instruction { get; set; } = "Pergunta aqui...";
    public string[] OptionKeys { get; set; } = new[] { "A", "B", "C", "D" };
    public string[] OptionValues { get; set; } = new[] { "Opcao A", "Opcao B", "Opcao C", "Opcao D" };
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
}
