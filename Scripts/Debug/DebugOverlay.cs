using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stationfall.Core.Combat;
using Stationfall.Core.Items;
using Stationfall.Godot.Combat;
using Stationfall.Godot.Dungeon;
using Stationfall.Godot.Items;
using Stationfall.Godot.Player;

namespace Stationfall.Godot.Debug;

// Dev keys + a small text console.
//
// Quick toggles (F1–F5) live on _UnhandledInput so they're suppressed while
// the LineEdit owns focus. The console (`) lives on _Input so its toggle
// preempts the LineEdit consuming the backtick.
//
// Command surface is intentionally narrow — commands map to thin wrappers on
// DungeonRoot / EnemyRegistry rather than reaching into private state. New
// commands belong here; the action they take belongs on the relevant node.
public partial class DebugOverlay : CanvasLayer
{
    [Export] public NodePath PlayerPath { get; set; } = "../Player";
    [Export] public NodePath DungeonRootPath { get; set; } = "..";
    [Export] public NodePath StatusLabelPath { get; set; } = "Panel/Status";
    [Export] public NodePath ConsolePanelPath { get; set; } = "Console";
    [Export] public NodePath ConsoleHistoryPath { get; set; } = "Console/VBox/History";
    [Export] public NodePath ConsoleInputPath { get; set; } = "Console/VBox/Input";
    [Export] public int Seed { get; set; } = 0;

    private const int HistoryLineCap = 8;

    private PlayerController? _player;
    private DungeonRoot? _dungeon;
    private Label? _status;
    private Control? _consolePanel;
    // RichTextLabel rather than Label — Label reports its content size as its
    // minimum size, so a multi-line `help` response grows it past the
    // VBoxContainer's bounds and pushes the LineEdit off the bottom. The
    // RichTextLabel ignores content for sizing, scrolls vertically when
    // overflowed, and follows the tail when scroll_following is set.
    private RichTextLabel? _history;
    private LineEdit? _input;
    private EnemyRegistry? _registry;
    private readonly List<string> _historyLines = new();
    // Random jitter on debug-spawn positions. Two CharacterBody2Ds spawned at
    // the same world point depenetrate violently and can tunnel through walls
    // — jitter avoids the stack.
    private readonly System.Random _spawnJitter = new();

    public override void _Ready()
    {
        _player = GetNodeOrNull<PlayerController>(PlayerPath);
        _dungeon = GetNodeOrNull<DungeonRoot>(DungeonRootPath);
        _status = GetNodeOrNull<Label>(StatusLabelPath);
        _consolePanel = GetNodeOrNull<Control>(ConsolePanelPath);
        _history = GetNodeOrNull<RichTextLabel>(ConsoleHistoryPath);
        _input = GetNodeOrNull<LineEdit>(ConsoleInputPath);

        // Registry scans res://Assets/Data/Enemies/ on its own _Ready. Adding
        // it as a child means it ticks once after this _Ready returns.
        _registry = new EnemyRegistry { Name = "EnemyRegistry" };
        AddChild(_registry);

        if (_consolePanel != null) _consolePanel.Visible = false;
        if (_input != null)
        {
            // Default LineEdit behavior is release_focus() right after the
            // text_submitted signal — meaning the user has to click or
            // re-toggle the console for every command. Override so submitting
            // keeps the caret active.
            _input.KeepEditingOnTextSubmit = true;
            _input.TextSubmitted += OnConsoleSubmit;
        }

        Refresh();
    }

    public override void _Process(double delta) => Refresh();

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventKey key || !key.Pressed || key.Echo) return;
        if (key.Keycode == Key.Quoteleft)
        {
            ToggleConsole();
            GetViewport().SetInputAsHandled();
            return;
        }
        if (key.Keycode == Key.Escape && _consolePanel?.Visible == true)
        {
            ToggleConsole();
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey key || !key.Pressed || key.Echo) return;
        if (_player == null) return;

        switch (key.Keycode)
        {
            case Key.F1:
                _player.GodMode = !_player.GodMode;
                GD.Print($"[debug] god mode: {_player.GodMode}");
                break;
            case Key.F2:
                ApplyDebugDamage(1);
                GD.Print("[debug] deal 1 damage to player");
                break;
            case Key.F3:
                GD.Print($"[debug] seed: {Seed}");
                break;
            case Key.F4:
                _player.Heal(99);
                GD.Print("[debug] full heal");
                break;
            case Key.F5:
                GD.Print($"[debug] hp={_player.Stats.Hp}/{_player.Stats.MaxHp} state={_player.State} adrenaline={_player.SignatureState.BuffActive}");
                break;
        }
    }

    private void ToggleConsole()
    {
        if (_consolePanel == null || _input == null) return;
        bool nextVisible = !_consolePanel.Visible;
        _consolePanel.Visible = nextVisible;
        // Pause the world while the console is up. Debug node has
        // process_mode=Always (set in scene) so input + LineEdit keep ticking,
        // but PlayerController and EnemyController halt — otherwise WASD
        // bleeds through to player movement while typing.
        GetTree().Paused = nextVisible;
        if (nextVisible)
        {
            _input.Text = "";
            _input.GrabFocus();
        }
        else
        {
            _input.ReleaseFocus();
        }
    }

    private void OnConsoleSubmit(string text)
    {
        var trimmed = text?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            _input?.Clear();
            return;
        }
        AppendHistory("> " + trimmed);
        var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var cmd = parts[0].ToLowerInvariant();
        var args = parts.Skip(1).ToArray();
        try
        {
            var response = Execute(cmd, args);
            if (!string.IsNullOrEmpty(response)) AppendHistory(response);
        }
        catch (Exception ex)
        {
            AppendHistory("error: " + ex.Message);
        }
        _input?.Clear();
        // Multi-line responses (e.g. `help`) can grow the History label and
        // trigger a layout reflow that drops focus mid-frame even with
        // KeepEditingOnTextSubmit. Re-grab on the next idle to be sure.
        Callable.From(() => _input?.GrabFocus()).CallDeferred();
    }

    private string Execute(string cmd, string[] args) => cmd switch
    {
        "spawn" => CmdSpawn(args),
        "tp" => CmdTeleport(args),
        "clear" => CmdClear(),
        "killall" => CmdKillAll(),
        "hb" => CmdToggleHitboxViz(),
        "seed" => CmdSeed(args),
        "give" => CmdGive(args),
        "combo" => CmdCombo(args),
        "weapon" => CmdWeapon(args),
        "m7test" => CmdM7Test(),
        "help" or "?" => CmdHelp(),
        _ => $"unknown: {cmd} (try 'help')",
    };

    private string CmdGive(string[] args)
    {
        if (args.Length == 0)
            return "usage: give <key|credit|passive> [amount|passive_id]";
        switch (args[0].ToLowerInvariant())
        {
            case "key":
            {
                if (KeysService.Instance == null) return "keys service not ready";
                int amount = ParseAmountOrDefault(args, 1, 1);
                KeysService.Instance.Add(amount);
                return $"+{amount} key (now {KeysService.Instance.Count})";
            }
            case "credit":
            {
                if (CreditsService.Instance == null) return "credits service not ready";
                int amount = ParseAmountOrDefault(args, 1, 1);
                CreditsService.Instance.Add(amount);
                return $"+{amount} credit (now {CreditsService.Instance.Balance})";
            }
            case "passive":
                return GivePassive(args);
            default:
                return $"unknown item '{args[0]}'; try: key, credit, passive";
        }
    }

    private static int ParseAmountOrDefault(string[] args, int index, int fallback) =>
        args.Length > index && int.TryParse(args[index], out var n) ? n : fallback;

    // Accepts a passive id (PassiveCatalog.RefrainId etc.) or a short name
    // (refrain / pirouette / curtain_call / cc). Idempotent up to stack cap.
    private string GivePassive(string[] args)
    {
        if (PassivesService.Instance == null) return "passives service not ready";
        if (args.Length < 2)
            return "usage: give passive <refrain|pirouette|curtain_call|all>";
        var token = args[1].ToLowerInvariant();
        if (token == "all")
        {
            var summary = new List<string>();
            foreach (var def in PassiveCatalog.M7DemoOffering)
            {
                bool added = PassivesService.Instance.TryAdd(def);
                int count = PassivesService.Instance.GetStackCount(def.Id);
                summary.Add($"{def.DisplayName}{(added ? "" : " (cap)")} x{count}");
            }
            return "granted: " + string.Join(", ", summary);
        }
        var resolved = ResolvePassive(token);
        if (resolved == null) return $"no passive '{token}'; try: refrain, pirouette, curtain_call";
        bool ok = PassivesService.Instance.TryAdd(resolved);
        int stack = PassivesService.Instance.GetStackCount(resolved.Id);
        return ok
            ? $"+ {resolved.DisplayName} (stack {stack}/{resolved.StackCap})"
            : $"{resolved.DisplayName} at stack cap ({resolved.StackCap})";
    }

    private static ItemDefinition? ResolvePassive(string token) => token switch
    {
        "refrain" or "r" => PassiveCatalog.Refrain,
        "pirouette" or "p" => PassiveCatalog.Pirouette,
        "curtain_call" or "curtaincall" or "cc" or "c" => PassiveCatalog.CurtainCall,
        _ => PassiveCatalog.FindById(token),
    };

    // `combo`              — dump current combo state.
    // `combo buffer <n>`   — set AttackInputBufferFrames at runtime.
    // `combo grace <n>`    — set ComboContinuationGraceFrames at runtime.
    private string CmdCombo(string[] args)
    {
        if (_player == null) return "player not ready";
        if (args.Length == 0) return DumpCombo();

        if (args.Length < 2 || !int.TryParse(args[1], out var n))
            return "usage: combo | combo buffer <frames> | combo grace <frames>";
        n = Math.Max(0, n);
        switch (args[0].ToLowerInvariant())
        {
            case "buffer":
                _player.AttackInputBufferFrames = n;
                return $"buffer = {n} frames";
            case "grace":
                _player.ComboContinuationGraceFrames = n;
                return $"grace = {n} frames";
            default:
                return $"unknown: combo {args[0]}; try buffer|grace";
        }
    }

    private string DumpCombo()
    {
        if (_player == null) return "player not ready";
        var mods = _player.ComboMods;
        var passives = PassivesService.Instance?.Passives;
        string passiveSummary = "—";
        if (passives != null && passives.Stacks.Count > 0)
        {
            passiveSummary = string.Join(", ", passives.Stacks.Select(kv => $"{kv.Key}×{kv.Value}"));
        }
        return string.Join("\n",
            FormatComboLine(_player),
            $"length: {mods.FinalComboLength} (+{mods.ExtraComboSteps})",
            $"finisher shape: {mods.FinisherShape} | dmg×{mods.FinisherDamageMultiplier} | status: {(mods.FinisherStatus?.Kind.ToString() ?? "—")}",
            $"passives: {passiveSummary}",
            $"buffer: {_player.AttackInputBufferFrames} frames | grace: {_player.ComboContinuationGraceFrames} frames");
    }

    // Player-readable 1-indexed combo line. During grace this shows the
    // last-completed step (so a sustained "Combo: 2 / 3" while the player
    // walks around between presses confirms the chain is still live).
    private static string FormatComboLine(PlayerController p)
    {
        var mods = p.ComboMods;
        int len = mods.FinalComboLength;
        if (!p.HasComboLineToShow) return $"Combo: — / {len}";

        int displayStep = p.ComboIndex + 1;
        bool finisher = mods.IsFinisher(p.ComboIndex);
        bool radial = finisher && mods.FinisherShape == ComboFinisherShape.Surround360;
        string suffix =
            finisher && radial ? " FINISHER RADIAL" :
            finisher ? " FINISHER" :
            "";
        return $"Combo: {displayStep} / {len}{suffix}";
    }

    // Cadence A/B test: swap the player's held weapon between the default
    // Sword (4/3/8 · 4/3/8 · 8/4/14 = 56f chained) and SwordSlow (5/4/11 ·
    // 5/4/11 · 9/5/17 = 71f chained) to compare action-adventure feel
    // before either becomes the design-doc default.
    private string CmdWeapon(string[] args)
    {
        if (_player == null) return "player not ready";
        if (args.Length == 0)
        {
            var w = _player.Weapon;
            string steps = string.Join(" · ", w.ComboSteps.Select(
                s => $"{s.WindupFrames}/{s.ActiveFrames}/{s.RecoveryFrames}"));
            int total = w.ComboSteps.Sum(s => s.TotalFrames);
            return $"current: {w.Name} — {steps}  (total {total}f)\nuse: weapon sword | weapon sword_slow";
        }
        var token = args[0].ToLowerInvariant();
        WeaponDefinition? next = token switch
        {
            "sword" or "fast" or "default" => WeaponDefinition.Sword,
            "sword_slow" or "slow" => WeaponDefinition.SwordSlow,
            _ => null,
        };
        if (next == null) return $"unknown weapon '{token}'; try sword | sword_slow";
        _player.SwapWeapon(next);
        int totalFrames = next.ComboSteps.Sum(s => s.TotalFrames);
        return $"weapon → {next.Name} (combo total {totalFrames}f chained)";
    }

    // M7 manual test: give Pirouette and ring the player with three Twitching
    // Patients (front / back-left / back-right) so the radial finisher can be
    // verified against front/side/back enemies in one swing.
    private string CmdM7Test()
    {
        if (_player == null || _dungeon == null) return "player or dungeon not ready";
        if (_registry == null) return "registry not ready";
        if (PassivesService.Instance == null) return "passives service not ready";

        var def = _registry.Resolve("twitching_patient");
        if (def == null) return "no twitching_patient enemy resource — check Assets/Data/Enemies/";

        // Three angles around the player at radius slightly larger than the
        // patient's melee range so they don't immediately lunge before the
        // player can swing. 0° = right, 120°, 240° gives a balanced fan.
        const float ringRadius = 110f;
        float[] anglesRad = { 0f, 2.0944f, 4.1888f };
        int spawned = 0;
        foreach (var rad in anglesRad)
        {
            var offset = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * ringRadius;
            if (_dungeon.TrySpawnEnemy(def, _player.GlobalPosition + offset)) spawned++;
        }

        bool addedPirouette = PassivesService.Instance.TryAdd(PassiveCatalog.Pirouette);
        return $"M7 test: +Pirouette ({(addedPirouette ? "new" : "already had")}), spawned {spawned}/3 patients";
    }

    private string CmdSpawn(string[] args)
    {
        if (_registry == null) return "registry not ready";
        if (args.Length == 0)
            return "usage: spawn <id>; available: " + JoinIds(_registry.Ids);
        var def = _registry.Resolve(args[0]);
        if (def == null)
            return $"no enemy '{args[0]}'; available: " + JoinIds(_registry.Ids);
        if (_player == null || _dungeon == null) return "player or dungeon not ready";
        // Spawn 160–200px in front of the player, jittered 0–60px perpendicular
        // so the new enemy never lands inside the player's body but repeated
        // spawns don't stack (overlapping CharacterBody2Ds depenetrate hard
        // enough to tunnel through walls). 160px is well outside the patient's
        // 76px melee range and inside its 360px aggro radius.
        var fwd = _player.Facing;
        var perp = new Vector2(-fwd.Y, fwd.X);
        float forward = 160f + (float)_spawnJitter.NextDouble() * 40f;
        float sideways = (float)(_spawnJitter.NextDouble() - 0.5) * 60f;
        var spawn = _player.GlobalPosition + fwd * forward + perp * sideways;
        return _dungeon.TrySpawnEnemy(def, spawn) ? $"spawned {def.Id}" : "spawn failed (no active room?)";
    }

    private string CmdTeleport(string[] args)
    {
        if (_dungeon == null) return "dungeon not ready";
        var roomIds = _dungeon.AllRoomIds.ToList();
        if (args.Length == 0)
            return "usage: tp <roomId>; rooms: " + string.Join(", ", roomIds);
        if (!roomIds.Contains(args[0]))
            return $"no room '{args[0]}'; rooms: " + string.Join(", ", roomIds);
        _dungeon.TeleportTo(args[0]);
        return "tp → " + args[0];
    }

    private string CmdClear()
    {
        if (_dungeon == null) return "dungeon not ready";
        _dungeon.ClearActiveRoom();
        return "cleared active room";
    }

    private string CmdKillAll()
    {
        if (_dungeon == null) return "dungeon not ready";
        _dungeon.KillAllEnemies();
        return "killed all enemies";
    }

    private string CmdToggleHitboxViz()
    {
        bool next = !CombatAreaDebug.DefaultVisible;
        CombatAreaDebug.DefaultVisible = next;
        foreach (var node in GetTree().GetNodesInGroup(CombatAreaDebug.Group))
        {
            if (node is HitboxComponent hit) hit.SetDebugVisible(next);
            else if (node is HurtboxComponent hurt) hurt.SetDebugVisible(next);
        }
        return next ? "hitbox viz: ON" : "hitbox viz: off";
    }

    private string CmdSeed(string[] args)
    {
        if (args.Length > 0 && int.TryParse(args[0], out var s)) Seed = s;
        // No-op for now; M5's procgen entry point will read this seed when it lands.
        return $"seed = {Seed} (no-op until M5)";
    }

    private static string CmdHelp() => string.Join("\n",
        "spawn <id>    spawn enemy in active room",
        "tp <roomId>   teleport to room",
        "clear         clear active room",
        "killall       kill every enemy in dungeon",
        "hb            toggle hitbox/hurtbox viz",
        "seed [n]      show / set run seed",
        "give <type> [n]  key|credit [n] / passive <id|all>",
        "combo                  dump live combo state",
        "combo buffer <frames>  set in-swing input buffer",
        "combo grace  <frames>  set post-swing continuation grace",
        "weapon [sword|sword_slow]  swap between cadence A/B test weapons",
        "m7test        give Pirouette + spawn 3 patients in a ring");

    private static string JoinIds(IEnumerable<string> ids)
    {
        var list = ids.ToList();
        return list.Count == 0 ? "(none)" : string.Join(", ", list);
    }

    private void AppendHistory(string line)
    {
        _historyLines.Add(line);
        while (_historyLines.Count > HistoryLineCap) _historyLines.RemoveAt(0);
        if (_history != null) _history.Text = string.Join("\n", _historyLines);
    }

    private void ApplyDebugDamage(int amount)
    {
        if (_player == null) return;
        var fakeResult = new DamageResult(Amount: amount, ArmorAbsorbed: 0, ArmorBroken: false, Killed: _player.Stats.Hp - amount <= 0);
        // Bypass god mode for the debug-damage key by temporarily disabling it.
        bool wasGod = _player.GodMode;
        _player.GodMode = false;
        _player.TakeDamage(fakeResult);
        _player.GodMode = wasGod;
    }

    private void Refresh()
    {
        if (_status == null || _player == null) return;
        string passiveLine = BuildPassiveLine();
        _status.Text =
            $"hp {_player.Stats.Hp}/{_player.Stats.MaxHp}  " +
            $"state {_player.State}  " +
            $"god {(_player.GodMode ? "ON" : "off")}  " +
            $"adrenaline {(_player.SignatureState.BuffActive ? "ACTIVE" : (_player.SignatureState.Queued ? "queued" : "—"))}\n" +
            $"{FormatComboLine(_player)}  passives: {passiveLine}\n" +
            $"buffer {_player.AttackInputBufferFrames}f  grace {_player.ComboContinuationGraceFrames}f  " +
            "F1 god  F2 dmg  F3 seed  F4 heal  F5 dump  ` console";
    }

    private static string BuildPassiveLine()
    {
        var passives = PassivesService.Instance?.Passives;
        if (passives == null || passives.Stacks.Count == 0) return "—";
        var parts = new List<string>(passives.Stacks.Count);
        foreach (var (id, stacks) in passives.Stacks)
        {
            var def = PassiveCatalog.FindById(id);
            string name = def?.DisplayName ?? id;
            parts.Add(stacks > 1 ? $"{name}×{stacks}" : name);
        }
        return string.Join(", ", parts);
    }
}
