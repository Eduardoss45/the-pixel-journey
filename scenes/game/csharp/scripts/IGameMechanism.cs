using Godot;

public interface IGameMechanism
{
    string MechanismId { get; }
    void ApplyEffect(string effectId, Variant? value = null);
}
