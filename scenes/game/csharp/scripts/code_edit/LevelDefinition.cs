
using Godot;
using System.Collections.Generic;
[GlobalClass]
public partial class LevelDefinition : Resource
{
    [Export] public string LevelId = "level_01";
    [Export] public string Instruction = "Declare uma variável chamada 'codigo' e atribua o valor 4321.";
    [Export] public string RequiredVariable = "codigo";
    [Export] public Variant ExpectedValue = 4321;

    [Export] public string RequiredFunction = "";


    [Export] public Variant ExpectedReturn = new Variant();

    public string GetInstructionText() => Instruction;

    public bool Validate(Dictionary<string, Variant> extractedVars)
    {
        if (!string.IsNullOrEmpty(RequiredVariable))
        {
            if (!extractedVars.ContainsKey(RequiredVariable))
                return false;

            return extractedVars[RequiredVariable].Equals(ExpectedValue);
        }

        if (!string.IsNullOrEmpty(RequiredFunction))
        {

            if (ExpectedReturn.VariantType == Variant.Type.Nil)
            {

                if (extractedVars.ContainsKey(RequiredFunction))
                {
                    return extractedVars[RequiredFunction].VariantType != Variant.Type.Nil;
                }
                return false;
            }
            else
            {

                if (extractedVars.ContainsKey(RequiredFunction))
                {
                    return extractedVars[RequiredFunction].Equals(ExpectedReturn);
                }
            }
        }

        return false;
    }
}