using Stationfall.Core.Progression;

namespace Stationfall.Core.SaveData;

// Versioning gate for the on-disk save format. Bump Current whenever
// MetaState changes shape in a way an older deserializer can't read.
// Mismatch on load → wipe path (SaveSerializer.TryDeserialize returns false,
// caller starts from a fresh MetaState).
public static class SaveSchema
{
    public const int Current = 1;
}

// On-disk envelope. Carries the schema version alongside the payload so the
// deserializer can reject incompatible saves without first parsing payload.
public record SaveEnvelope(int SchemaVersion, MetaState Payload);
