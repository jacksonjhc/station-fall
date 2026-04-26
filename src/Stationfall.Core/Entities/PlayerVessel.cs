namespace Stationfall.Core.Entities;

public class PlayerVessel
{
    public string VesselType { get; }
    public Stats BaseStats { get; }

    public PlayerVessel(string vesselType, Stats baseStats)
    {
        VesselType = vesselType;
        BaseStats = baseStats;
    }

    public static PlayerVessel Clone() =>
        new("Clone", new Stats(MaxHp: 100, Hp: 100, Speed: 5, Attack: 10, Defense: 5));

    public static PlayerVessel Android() =>
        new("Android", new Stats(MaxHp: 80, Hp: 80, Speed: 7, Attack: 8, Defense: 8));
}
