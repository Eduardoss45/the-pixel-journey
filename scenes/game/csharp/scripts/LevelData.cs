using Godot;
using System;
using System.Collections.Generic;

public class LevelData
{
    public string LevelId { get; set; }
    public string Instruction { get; set; }
    public string Type { get; set; } = "variable";

    public string RequiredVariable { get; set; }
    public Variant ExpectedValue { get; set; }

    public string RequiredFunction { get; set; }
    public Variant ExpectedReturn { get; set; }

    public Godot.Collections.Array<int> ValidPatterns { get; set; } =
        new Godot.Collections.Array<int>();

    public string Effect { get; set; }

    public bool Validate(Dictionary<string, Variant> extractedVars)
    {
        if (Type == "variable" && !string.IsNullOrEmpty(RequiredVariable))
        {
            if (!extractedVars.ContainsKey(RequiredVariable))
                return false;

            return CompareVariants(
                extractedVars[RequiredVariable],
                ExpectedValue
            );
        }

        if (Type == "function" && !string.IsNullOrEmpty(RequiredFunction))
        {
            if (!extractedVars.ContainsKey(RequiredFunction))
                return false;

            var returned = extractedVars[RequiredFunction];

            if (ExpectedReturn.VariantType != Variant.Type.Nil)
                return CompareVariants(returned, ExpectedReturn);

            if (ValidPatterns.Count > 0)
            {
                if (returned.VariantType == Variant.Type.Int ||
                    returned.VariantType == Variant.Type.Float)
                {
                    int value = (int)returned.AsDouble();
                    return ValidPatterns.Contains(value);
                }

                return false;
            }

            return true;
        }

        return false;
    }

    private bool CompareVariants(Variant a, Variant b)
    {
        if (IsNumeric(a) && IsNumeric(b))
        {
            double da = a.AsDouble();
            double db = b.AsDouble();

            return Math.Abs(da - db) < 0.0001;
        }

        return a.Equals(b);
    }

    private bool IsNumeric(Variant v)
    {
        return v.VariantType == Variant.Type.Int ||
               v.VariantType == Variant.Type.Float;
    }

    public string GetErrorMessage(Variant attemptedReturn)
    {
        if (Type == "function" && ValidPatterns.Count > 0)
        {
            return $"Padrão {attemptedReturn} inválido. " +
                   $"Padrões aceitos: {string.Join(", ", ValidPatterns)}";
        }

        return "Resultado não atende aos requisitos da lição.";
    }
}
