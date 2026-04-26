using Stationfall.Core.Progression;
using Stationfall.Core.SaveData;
using Xunit;

namespace Stationfall.Tests.SaveData;

public class SaveSerializerTests
{
    [Fact]
    public void RoundTrip_EmptyMetaState_PreservesEmptyFlags()
    {
        var original = new MetaState();
        var json = SaveSerializer.Serialize(original);

        Assert.True(SaveSerializer.TryDeserialize(json, out var loaded));
        Assert.Empty(loaded.NarrativeFlags);
    }

    [Fact]
    public void RoundTrip_PopulatedMetaState_PreservesFlags()
    {
        var original = new MetaState();
        original.AddFlag("firstDeath");
        original.AddFlag("readMedlogTerminal");

        var json = SaveSerializer.Serialize(original);

        Assert.True(SaveSerializer.TryDeserialize(json, out var loaded));
        Assert.True(loaded.HasFlag("firstDeath"));
        Assert.True(loaded.HasFlag("readMedlogTerminal"));
        Assert.Equal(2, loaded.NarrativeFlags.Count);
    }

    [Fact]
    public void Serialize_IncludesCurrentSchemaVersion()
    {
        var json = SaveSerializer.Serialize(new MetaState());
        Assert.Contains($"\"SchemaVersion\": {SaveSchema.Current}", json);
    }

    [Fact]
    public void TryDeserialize_OldSchemaVersion_TakesWipePath()
    {
        // Hand-crafted old-version envelope. Should be rejected without throwing
        // and the out param should be a fresh empty MetaState.
        var oldJson = $"{{\"SchemaVersion\": {SaveSchema.Current - 1}, \"Payload\": {{\"NarrativeFlags\": [\"shouldNotLoad\"]}}}}";

        Assert.False(SaveSerializer.TryDeserialize(oldJson, out var loaded));
        Assert.Empty(loaded.NarrativeFlags);
    }

    [Fact]
    public void TryDeserialize_FutureSchemaVersion_TakesWipePath()
    {
        var futureJson = $"{{\"SchemaVersion\": {SaveSchema.Current + 1}, \"Payload\": {{\"NarrativeFlags\": [\"shouldNotLoad\"]}}}}";

        Assert.False(SaveSerializer.TryDeserialize(futureJson, out var loaded));
        Assert.Empty(loaded.NarrativeFlags);
    }

    [Fact]
    public void TryDeserialize_GarbageJson_TakesWipePath()
    {
        Assert.False(SaveSerializer.TryDeserialize("not json at all {{{", out var loaded));
        Assert.NotNull(loaded);
        Assert.Empty(loaded.NarrativeFlags);
    }

    [Fact]
    public void TryDeserialize_EmptyString_TakesWipePath()
    {
        Assert.False(SaveSerializer.TryDeserialize("", out var loaded));
        Assert.NotNull(loaded);
        Assert.Empty(loaded.NarrativeFlags);
    }

    [Fact]
    public void TryDeserialize_UnknownExtraFields_AreIgnored()
    {
        // Forward-compat: a save written by a future build that added optional
        // fields should still load on an older client as long as the schema
        // version matches. (When schema bumps, we wipe — but unknown fields at
        // the same schema version are tolerable.)
        var json = $"{{\"SchemaVersion\": {SaveSchema.Current}, \"Payload\": {{\"NarrativeFlags\": [\"a\"], \"FutureField\": 42}}, \"AnotherFutureField\": \"x\"}}";

        Assert.True(SaveSerializer.TryDeserialize(json, out var loaded));
        Assert.True(loaded.HasFlag("a"));
    }

    [Fact]
    public void TryDeserialize_NullPayload_TakesWipePath()
    {
        var json = $"{{\"SchemaVersion\": {SaveSchema.Current}, \"Payload\": null}}";
        Assert.False(SaveSerializer.TryDeserialize(json, out var loaded));
        Assert.Empty(loaded.NarrativeFlags);
    }
}
