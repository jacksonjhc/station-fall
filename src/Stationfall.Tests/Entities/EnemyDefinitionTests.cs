using Stationfall.Core.Ai;
using Stationfall.Core.Entities;
using Xunit;

namespace Stationfall.Tests.Entities;

public class EnemyDefinitionTests
{
    [Fact]
    public void Record_RoundTripsBrainConfig_IntoTickPipeline()
    {
        // Verifies the Godot loader's contract: an EnemyDefinition built from
        // .tres data hands its Brain config straight to EnemyAiBrain.Tick.
        var def = new EnemyDefinition(
            Id: "twitching_patient",
            DisplayName: "Twitching Patient",
            Brain: TwitchingPatientConfig.Default);

        var sensor = new SensorData(DistanceToPlayerPx: 100f, HasLineOfSight: true, SelfHpRatio: 1f);
        var s = EnemyAiBrain.Tick(EnemyAiSnapshot.Initial, sensor, nowSeconds: 0, deltaSeconds: 1f / 60f, def.Brain);
        Assert.Equal(AiState.Chase, s.Phase);
    }

    [Fact]
    public void MassClass_DefaultsToLight_AndRoundTrips()
    {
        var defaulted = new EnemyDefinition(
            Id: "twitching_patient",
            DisplayName: "Twitching Patient",
            Brain: TwitchingPatientConfig.Default);
        Assert.Equal(MassClass.Light, defaulted.MassClass);

        var heavy = defaulted with { MassClass = MassClass.Heavy };
        Assert.Equal(MassClass.Heavy, heavy.MassClass);
    }
}
