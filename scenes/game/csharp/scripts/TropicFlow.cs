using System;
using Godot;

public partial class TropicFlow : Node2D
{
    private const string DefaultGameMusicPath = "res://assets/music/music-game.ogg";
    private const string DefaultBossMusicPath = "res://assets/music/music-boss.ogg";
    private const string DefaultCreditsCutscenePath = "res://assets/cutscenes/credits.mp4";
    private const string DefaultMainMenuScenePath = "res://scenes/game/csharp/scenes/main_menu.tscn";
    private const float EndDelaySeconds = 5.0f;
    private const float PreCreditsFadeSeconds = 1.0f;
    private const float NormalVolumeDb = 0.0f;

    [Export] public NodePath MusicPlayerPath { get; set; } = "AudioStreamPlayer2D";
    [Export(PropertyHint.File, "*.ogg")] public string GameMusicPath { get; set; } = DefaultGameMusicPath;
    [Export(PropertyHint.File, "*.ogg")] public string BossMusicPath { get; set; } = DefaultBossMusicPath;
    [Export(PropertyHint.File, "*.mp4")] public string CreditsCutscenePath { get; set; } = DefaultCreditsCutscenePath;
    [Export(PropertyHint.File, "*.tscn")] public string MainMenuScenePath { get; set; } = DefaultMainMenuScenePath;

    private AudioStreamPlayer2D _musicPlayer;
    private AudioStream _gameMusic;
    private AudioStream _bossMusic;
    private bool _endingStarted;
    private CanvasLayer _fadeLayer;
    private ColorRect _fadeRect;
    private AudioStream _desiredMusic;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _musicPlayer = GetNodeOrNull<AudioStreamPlayer2D>(MusicPlayerPath);
        if (_musicPlayer != null)
        {
            _musicPlayer.ProcessMode = ProcessModeEnum.Always;
            _musicPlayer.Finished += OnMusicFinished;
            // BGM should be global and not attenuate with world distance.
            _musicPlayer.Attenuation = 0.0f;
            _musicPlayer.MaxDistance = 1000000.0f;
            _musicPlayer.PanningStrength = 0.0f;
        }
        _gameMusic = LoadMusicOrNull(GameMusicPath);
        _bossMusic = LoadMusicOrNull(BossMusicPath);
        ForceLoopEnabled(_gameMusic);
        ForceLoopEnabled(_bossMusic);
        EnsureFadeOverlay();

        var session = GameSession.Instance ?? GetNodeOrNull<GameSession>("/root/GameSession");
        if (session != null)
        {
            session.BossActiveChanged += OnBossActiveChanged;
            session.BossDefeatedSignal += OnBossDefeated;
            _ = ApplyMusicByBossStateAsync(session.BossActive);
        }
        else
        {
            _ = ApplyMusicByBossStateAsync(false);
        }
    }

    public override void _ExitTree()
    {
        var session = GameSession.Instance ?? GetNodeOrNull<GameSession>("/root/GameSession");
        if (session == null)
            return;

        session.BossActiveChanged -= OnBossActiveChanged;
        session.BossDefeatedSignal -= OnBossDefeated;

        if (_musicPlayer != null)
            _musicPlayer.Finished -= OnMusicFinished;
    }

    public override void _Process(double delta)
    {
        if (_endingStarted || _musicPlayer == null || _desiredMusic == null)
            return;

        if (_musicPlayer.Stream != _desiredMusic)
        {
            _musicPlayer.Stream = _desiredMusic;
            _musicPlayer.VolumeDb = NormalVolumeDb;
            _musicPlayer.Play();
            return;
        }

        // Defensive watchdog: if playback ended without looping or "Playing" got stuck,
        // restart the desired track to avoid permanent silence.
        if (_musicPlayer.VolumeDb < NormalVolumeDb - 1.0f)
            _musicPlayer.VolumeDb = NormalVolumeDb;

        if (!_musicPlayer.Playing || ShouldRestartCurrentTrack(_desiredMusic))
        {
            _musicPlayer.VolumeDb = NormalVolumeDb;
            _musicPlayer.Play();
        }
    }

    private void OnMusicFinished()
    {
        if (_endingStarted || _musicPlayer == null || _desiredMusic == null)
            return;

        if (_musicPlayer.Stream != _desiredMusic)
            return;

        _musicPlayer.VolumeDb = NormalVolumeDb;
        _musicPlayer.Play();
    }

    private void OnBossActiveChanged(bool active)
    {
        if (_endingStarted)
            return;

        _ = ApplyMusicByBossStateAsync(active);
    }

    private async void OnBossDefeated()
    {
        if (_endingStarted)
            return;

        _endingStarted = true;
        await WaitUntilGameUnlocked();
        await ToSignal(GetTree().CreateTimer(EndDelaySeconds, false), SceneTreeTimer.SignalName.Timeout);
        await FadeMusicOutAsync();
        await FadeScreenToBlackAsync();
        await PlayCreditsAndReturnToMenu();
    }

    private async System.Threading.Tasks.Task ApplyMusicByBossStateAsync(bool bossActive)
    {
        if (_musicPlayer == null)
            return;

        var target = bossActive ? _bossMusic : _gameMusic;
        if (target == null)
            return;

        _desiredMusic = target;

        if (_musicPlayer.Stream == target)
        {
            if (_musicPlayer.VolumeDb < NormalVolumeDb - 1.0f)
                _musicPlayer.VolumeDb = NormalVolumeDb;

            if (!_musicPlayer.Playing || ShouldRestartCurrentTrack(target))
            {
                _musicPlayer.VolumeDb = NormalVolumeDb;
                _musicPlayer.Play();
            }
            return;
        }

        _musicPlayer.Stop();
        _musicPlayer.Stream = target;
        _musicPlayer.VolumeDb = NormalVolumeDb;
        _musicPlayer.Play();
        await System.Threading.Tasks.Task.CompletedTask;
    }

    private async System.Threading.Tasks.Task PlayCreditsAndReturnToMenu()
    {
        var stream = LoadCutsceneOrNull(CreditsCutscenePath);
        if (stream == null)
        {
            GetTree().ChangeSceneToFile(MainMenuScenePath);
            return;
        }

        var overlay = new CanvasLayer
        {
            Layer = 100,
            ProcessMode = ProcessModeEnum.Always
        };
        AddChild(overlay);

        var player = new VideoStreamPlayer
        {
            ProcessMode = ProcessModeEnum.Always,
            Expand = true,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ZIndex = 100
        };

        overlay.AddChild(player);
        player.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        player.OffsetLeft = 0;
        player.OffsetTop = 0;
        player.OffsetRight = 0;
        player.OffsetBottom = 0;
        player.Stream = stream;
        player.StreamPosition = 0.0;
        player.Paused = false;
        LockPlayerMovementForCutscene();
        player.Play();

        await ToSignal(player, VideoStreamPlayer.SignalName.Finished);

        GetTree().ChangeSceneToFile(MainMenuScenePath);
    }

    private void LockPlayerMovementForCutscene()
    {
        var tree = GetTree();
        if (tree == null)
            return;

        var players = tree.GetNodesInGroup("Player");
        foreach (var node in players)
        {
            if (node is Player player)
                player.SetCanMove(false);
        }
    }

    private void EnsureFadeOverlay()
    {
        if (_fadeLayer != null && IsInstanceValid(_fadeLayer))
            return;

        _fadeLayer = new CanvasLayer
        {
            Layer = 95,
            ProcessMode = ProcessModeEnum.Always
        };
        AddChild(_fadeLayer);

        _fadeRect = new ColorRect
        {
            Color = new Color(0, 0, 0, 1),
            Modulate = new Color(1, 1, 1, 0),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _fadeLayer.AddChild(_fadeRect);
        _fadeRect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _fadeRect.OffsetLeft = 0;
        _fadeRect.OffsetTop = 0;
        _fadeRect.OffsetRight = 0;
        _fadeRect.OffsetBottom = 0;
    }

    private async System.Threading.Tasks.Task WaitUntilGameUnlocked()
    {
        var tree = GetTree();
        if (tree == null)
            return;

        while (tree.Paused)
            await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }

    private async System.Threading.Tasks.Task FadeScreenToBlackAsync()
    {
        if (_fadeRect == null || !IsInstanceValid(_fadeRect))
            return;

        var tween = CreateTween();
        tween.TweenProperty(_fadeRect, "modulate:a", 1.0f, PreCreditsFadeSeconds);
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    private System.Threading.Tasks.Task FadeMusicOutAsync()
    {
        if (_musicPlayer == null)
            return System.Threading.Tasks.Task.CompletedTask;

        _musicPlayer.VolumeDb = NormalVolumeDb;
        _musicPlayer.Stop();
        return System.Threading.Tasks.Task.CompletedTask;
    }

    private bool ShouldRestartCurrentTrack(AudioStream expected)
    {
        if (_musicPlayer == null || expected == null)
            return false;

        double length = expected.GetLength();
        if (length <= 0.0)
            return false;

        double pos = _musicPlayer.GetPlaybackPosition();
        return pos >= Math.Max(0.0, length - 0.05);
    }

    private static AudioStream LoadMusicOrNull(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        return GD.Load<AudioStream>(path);
    }

    private static void ForceLoopEnabled(AudioStream stream)
    {
        switch (stream)
        {
            case AudioStreamOggVorbis ogg:
                ogg.Loop = true;
                break;
            case AudioStreamMP3 mp3:
                mp3.Loop = true;
                break;
            case AudioStreamWav wav:
                if (wav.LoopMode == AudioStreamWav.LoopModeEnum.Disabled)
                    wav.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
                break;
        }
    }

    private static VideoStream LoadCutsceneOrNull(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        if (!FileAccess.FileExists(path))
            return null;

        if (path.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) && ClassDB.ClassExists("FFmpegVideoStream"))
        {
            var ffmpegVariant = ClassDB.Instantiate("FFmpegVideoStream");
            var ffmpegObject = ffmpegVariant.VariantType == Variant.Type.Object
                ? ffmpegVariant.AsGodotObject()
                : null;
            if (ffmpegObject is VideoStream ffmpegStream)
            {
                ffmpegStream.Set("file", path);
                return ffmpegStream;
            }
        }

        return ResourceLoader.Load<VideoStream>(path);
    }
}
