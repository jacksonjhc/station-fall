using Godot;
using Stationfall.Core.Combat;
using Stationfall.Godot.Player;

namespace Stationfall.Godot.Debug;

// Minimal M1 dev keys. Expanded set lands in M3.5 per the roadmap.
public partial class DebugOverlay : CanvasLayer
{
    [Export] public NodePath PlayerPath { get; set; } = "../Player";
    [Export] public NodePath StatusLabelPath { get; set; } = "Panel/Status";
    [Export] public int Seed { get; set; } = 0;

    private PlayerController? _player;
    private Label? _status;

    public override void _Ready()
    {
        _player = GetNodeOrNull<PlayerController>(PlayerPath);
        _status = GetNodeOrNull<Label>(StatusLabelPath);
        Refresh();
    }

    public override void _Process(double delta) => Refresh();

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
        _status.Text =
            $"hp {_player.Stats.Hp}/{_player.Stats.MaxHp}  " +
            $"state {_player.State}  " +
            $"god {(_player.GodMode ? "ON" : "off")}  " +
            $"adrenaline {(_player.SignatureState.BuffActive ? "ACTIVE" : (_player.SignatureState.Queued ? "queued" : "—"))}\n" +
            "F1 god  F2 dmg  F3 seed  F4 heal  F5 dump";
    }
}
