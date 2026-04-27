using Godot;

namespace Stationfall.Godot.Audio;

// Centralized SFX dispatch. Sits at root of Dungeon.tscn; combat sites call
// Sfx.Instance.PlayXxx(). Source files come from Kenney CC0 audio packs (see
// CLAUDE.md § Asset Bootstrap), referenced by res:// path so missing assets
// degrade silently rather than crash.
//
// Round-robin variant selection (Cycle) avoids back-to-back duplicate samples
// during fast combo trades. Random would occasionally repeat; cycling is
// deterministic and feels more "alive."
//
// One AudioStreamPlayer per logical channel (hit, dodge, death). Two
// rapid-fire hits will truncate the first — fine for ~80ms impact samples,
// noticeable for the ~600ms death bell. If chaining deaths becomes an issue
// later, swap each channel for a tiny round-robin pool.
public partial class Sfx : Node
{
    public const string Group = "sfx";
    public static Sfx? Instance { get; private set; }

    private const string ImpactPunchMediumFmt = "res://Assets/Kenney/kenney_impact-sounds/Audio/impactPunch_medium_{0:D3}.ogg";
    private const string ImpactPunchHeavyFmt = "res://Assets/Kenney/kenney_impact-sounds/Audio/impactPunch_heavy_{0:D3}.ogg";
    private const string ImpactPlateHeavyFmt = "res://Assets/Kenney/kenney_impact-sounds/Audio/impactPlate_heavy_{0:D3}.ogg";
    private const string ImpactBellHeavyFmt = "res://Assets/Kenney/kenney_impact-sounds/Audio/impactBell_heavy_{0:D3}.ogg";
    private const string DodgePath = "res://Assets/Kenney/kenney_sci-fi-sounds/Audio/forceField_004.ogg";
    private const string PlayerDeathPath = "res://Assets/Kenney/kenney_sci-fi-sounds/Audio/explosionCrunch_004.ogg";
    private const string DoorUnlockPath = "res://Assets/Kenney/kenney_sci-fi-sounds/Audio/doorOpen_001.ogg";
    private const int VariantCount = 5;

    private AudioStream?[] _hitLight = new AudioStream?[VariantCount];
    private AudioStream?[] _hitHeavy = new AudioStream?[VariantCount];
    private AudioStream?[] _damageTaken = new AudioStream?[VariantCount];
    private AudioStream?[] _enemyDeath = new AudioStream?[VariantCount];
    private AudioStream? _dodge;
    private AudioStream? _playerDeath;
    private AudioStream? _doorUnlock;

    private int _hitLightIdx;
    private int _hitHeavyIdx;
    private int _damageTakenIdx;
    private int _enemyDeathIdx;

    private AudioStreamPlayer? _hitChannel;
    private AudioStreamPlayer? _dodgeChannel;
    private AudioStreamPlayer? _deathChannel;

    public override void _Ready()
    {
        Instance = this;
        AddToGroup(Group);

        LoadVariants(_hitLight, ImpactPunchMediumFmt);
        LoadVariants(_hitHeavy, ImpactPunchHeavyFmt);
        LoadVariants(_damageTaken, ImpactPlateHeavyFmt);
        LoadVariants(_enemyDeath, ImpactBellHeavyFmt);
        _dodge = ResourceLoader.Load<AudioStream>(DodgePath);
        _playerDeath = ResourceLoader.Load<AudioStream>(PlayerDeathPath);
        _doorUnlock = ResourceLoader.Load<AudioStream>(DoorUnlockPath);

        // Per-event volume offsets: hits balanced quietest because they fire
        // most often during a combo; death loudest because it's a one-shot
        // story beat. Tunable in playtest.
        _hitChannel = new AudioStreamPlayer { Name = "HitChannel", VolumeDb = -4f };
        _dodgeChannel = new AudioStreamPlayer { Name = "DodgeChannel", VolumeDb = -6f };
        _deathChannel = new AudioStreamPlayer { Name = "DeathChannel", VolumeDb = 0f };
        AddChild(_hitChannel);
        AddChild(_dodgeChannel);
        AddChild(_deathChannel);
    }

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
    }

    public void PlayHitLanded(bool isHeavy)
    {
        var pool = isHeavy ? _hitHeavy : _hitLight;
        ref int idx = ref (isHeavy ? ref _hitHeavyIdx : ref _hitLightIdx);
        Play(_hitChannel, NextVariant(pool, ref idx));
    }

    public void PlayDamageTaken() => Play(_hitChannel, NextVariant(_damageTaken, ref _damageTakenIdx));
    public void PlayDodge() => Play(_dodgeChannel, _dodge);
    public void PlayPlayerDeath() => Play(_deathChannel, _playerDeath);
    public void PlayEnemyDeath() => Play(_deathChannel, NextVariant(_enemyDeath, ref _enemyDeathIdx));
    public void PlayDoorUnlock() => Play(_dodgeChannel, _doorUnlock);

    private static void LoadVariants(AudioStream?[] target, string pathFormat)
    {
        for (int i = 0; i < target.Length; i++)
        {
            var path = string.Format(pathFormat, i);
            target[i] = ResourceLoader.Load<AudioStream>(path);
            if (target[i] == null) GD.PushWarning($"Sfx: missing {path}");
        }
    }

    private static AudioStream? NextVariant(AudioStream?[] pool, ref int idx)
    {
        if (pool.Length == 0) return null;
        var pick = pool[idx];
        idx = (idx + 1) % pool.Length;
        // If the indexed slot is null (asset missing), scan forward once for
        // any loaded variant. Avoids silence when only some variants imported.
        if (pick == null)
        {
            foreach (var s in pool) if (s != null) return s;
        }
        return pick;
    }

    private static void Play(AudioStreamPlayer? channel, AudioStream? stream)
    {
        if (channel == null || stream == null) return;
        channel.Stream = stream;
        channel.Play();
    }
}
