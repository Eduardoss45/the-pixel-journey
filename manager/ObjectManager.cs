using Godot;
using System.Collections.Generic;

public partial class ObjectManager : Node
{
    public static ObjectManager Instance { get; private set; }

    private Dictionary<string, IGameMechanism> mechanisms = new();

    public override void _Ready()
    {
        Instance = this;
    }

    public void Register(IGameMechanism mechanism)
    {
        if (string.IsNullOrEmpty(mechanism.MechanismId))
        {
            GD.PrintErr("Mecanismo sem ID.");
            return;
        }

        mechanisms[mechanism.MechanismId] = mechanism;
    }

    public void ApplyEffect(string mechanismId, string effectId, Variant? value = null)
    {
        if (!mechanisms.ContainsKey(mechanismId))
        {
            GD.PrintErr($"MechanismId '{mechanismId}' não encontrado.");
            return;
        }

        mechanisms[mechanismId].ApplyEffect(effectId, value);
    }
}
