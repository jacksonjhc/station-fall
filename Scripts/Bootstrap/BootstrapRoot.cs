using Godot;
using Stationfall.Godot.Player;
using Stationfall.Godot.UI;

namespace Stationfall.Godot.Bootstrap;

// Root of the M1 sandbox scene. Wires the HUD to the player.
public partial class BootstrapRoot : Node2D
{
    [Export] public NodePath PlayerPath { get; set; } = "Player";
    [Export] public NodePath HealthBarPath { get; set; } = "HUD/HealthBar";

    public override void _Ready()
    {
        var player = GetNodeOrNull<PlayerController>(PlayerPath);
        var healthBar = GetNodeOrNull<HealthBar>(HealthBarPath);

        if (player != null && healthBar != null)
        {
            healthBar.SetHealth(player.Stats.Hp, player.Stats.MaxHp);
            player.HealthChanged += (hp, maxHp) => healthBar.SetHealth((int)hp, (int)maxHp);
        }

        if (player != null)
        {
            player.AdrenalineActivated += () => GD.Print("[adrenaline] ACTIVE");
            player.Died += () => GD.Print("[player] died");
        }
    }
}
