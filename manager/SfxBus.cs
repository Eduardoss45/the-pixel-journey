using Godot;
using System.Collections.Generic;

public partial class SfxBus : Node
{
    public static SfxBus Instance { get; private set; }

    private const string LoosePath = "res://assets/effects/loose.ogg";
    private const string CollectPath = "res://assets/effects/collect.ogg";
    private const string EliminationPath = "res://assets/effects/elimination.ogg";
    private const string OpenPath = "res://assets/effects/open.ogg";
    private const string ClosePath = "res://assets/effects/close.ogg";

    private readonly Dictionary<string, AudioStream> _cache = new();

    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    public void PlayLoose() => PlayFromPath(LoosePath);
    public void PlayCollect() => PlayFromPath(CollectPath);
    public void PlayElimination() => PlayFromPath(EliminationPath);
    public void PlayOpen() => PlayFromPath(OpenPath);
    public void PlayClose() => PlayFromPath(ClosePath);

    private void PlayFromPath(string path)
    {
        var stream = LoadStream(path);
        if (stream == null)
            return;

        var player = new AudioStreamPlayer
        {
            Stream = stream,
            ProcessMode = ProcessModeEnum.Always
        };

        AddChild(player);
        player.Finished += player.QueueFree;
        player.Play();
    }

    private AudioStream LoadStream(string path)
    {
        if (_cache.TryGetValue(path, out var cached))
            return cached;

        var loaded = GD.Load<AudioStream>(path);
        if (loaded == null)
        {
            GD.PushWarning($"[SfxBus] Could not load stream '{path}'.");
            return null;
        }

        _cache[path] = loaded;
        return loaded;
    }
}
