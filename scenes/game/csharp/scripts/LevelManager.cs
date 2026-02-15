using Godot;

public partial class LevelManager : Node
{
    private System.Collections.Generic.Dictionary<string, LevelData> levels = new();


    public override void _Ready()
    {
        LoadLevels();
    }

    private void LoadLevels()
    {
        var file = FileAccess.Open("res://data/levels.json", FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PrintErr("Não encontrou levels.json!");
            return;
        }

        string jsonText = file.GetAsText();
        file.Close();

        var json = Json.ParseString(jsonText).AsGodotDictionary();
        if (json == null || !json.ContainsKey("levels"))
        {
            GD.PrintErr("JSON inválido ou sem chave 'levels'");
            return;
        }

        var levelsArray = json["levels"].AsGodotArray();
        foreach (var levelObj in levelsArray)
        {
            var dict = levelObj.AsGodotDictionary();
            var data = new LevelData
            {
                LevelId = dict["levelId"].AsString(),
                Instruction = dict["instruction"].AsString(),
                Type = dict.ContainsKey("type") ? dict["type"].AsString() : "variable",
                RequiredVariable = dict.ContainsKey("requiredVariable") ? dict["requiredVariable"].AsString() : "",
                ExpectedValue = dict.ContainsKey("expectedValue") ? dict["expectedValue"] : Variant.From(false),
                RequiredFunction = dict.ContainsKey("requiredFunction") ? dict["requiredFunction"].AsString() : "",
                ExpectedReturn = dict.ContainsKey("expectedReturn") ? dict["expectedReturn"] : new Variant(),
                ValidPatterns = dict.ContainsKey("validPatterns")
        ? dict["validPatterns"].AsGodotArray<int>()
        : new Godot.Collections.Array<int>(),
                Effect = dict.ContainsKey("effect") ? dict["effect"].AsString() : ""
            };


            levels[data.LevelId] = data;
            GD.Print($"Carregado nível: {data.LevelId}");
        }
    }

    public LevelData GetLevel(string levelId)
    {
        return levels.TryGetValue(levelId, out var level) ? level : null;
    }
}