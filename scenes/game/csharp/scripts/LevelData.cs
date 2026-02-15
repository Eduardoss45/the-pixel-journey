using Godot;
using System.Collections.Generic;

public class LevelData
{
    public string LevelId { get; set; }
    public string Instruction { get; set; }
    public string Type { get; set; } = "variable";  // "variable" ou "function"
    public string RequiredVariable { get; set; }
    public Variant ExpectedValue { get; set; }
    public string RequiredFunction { get; set; }
    public Variant ExpectedReturn { get; set; }
    public Godot.Collections.Array<int> ValidPatterns { get; set; } = new();
    public string Effect { get; set; }

    // Novo método Validate (migra do LevelDefinition)
    public bool Validate(Dictionary<string, Variant> extractedVars)
    {
        if (Type == "variable" && !string.IsNullOrEmpty(RequiredVariable))
        {
            if (!extractedVars.ContainsKey(RequiredVariable))
                return false;
            return extractedVars[RequiredVariable].Equals(ExpectedValue);
        }

        if (Type == "function" && !string.IsNullOrEmpty(RequiredFunction))
        {
            if (!extractedVars.ContainsKey(RequiredFunction))
                return false;

            var returned = extractedVars[RequiredFunction];

            // Se espera um valor específico
            if (ExpectedReturn.VariantType != Variant.Type.Nil)
                return returned.Equals(ExpectedReturn);

            // Se tem lista de padrões válidos
            if (ValidPatterns.Count > 0 && returned.VariantType == Variant.Type.Int)
                return ValidPatterns.Contains(returned.AsInt32());

            // Se só precisa existir
            return true;
        }

        return false;
    }

    // Opcional: método para erro personalizado
    public string GetErrorMessage(Variant attemptedReturn)
    {
        if (Type == "function" && ValidPatterns.Count > 0 && attemptedReturn.VariantType == Variant.Type.Int)
            return $"Padrão {attemptedReturn.AsInt32()} inválido. Padrões aceitos: {string.Join(", ", ValidPatterns)}";
        return "Resultado não atende aos requisitos da lição.";
    }
}