using System.Text.Json;
using Stationfall.Core.Progression;

namespace Stationfall.Core.SaveData;

// Pure C# JSON read/write for MetaState. No I/O — callers (Godot SaveFile,
// tests) own bytes. No exceptions on bad input — TryDeserialize returns
// false on any malformed/old/forward-incompatible data so the caller can
// take the "wipe to fresh MetaState" path the roadmap calls for.
public static class SaveSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
    };

    public static string Serialize(MetaState state)
    {
        var envelope = new SaveEnvelope(SaveSchema.Current, state);
        return JsonSerializer.Serialize(envelope, Options);
    }

    public static bool TryDeserialize(string json, out MetaState state)
    {
        state = new MetaState();
        if (string.IsNullOrWhiteSpace(json)) return false;

        SaveEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<SaveEnvelope>(json, Options);
        }
        catch (JsonException)
        {
            return false;
        }

        if (envelope is null) return false;
        if (envelope.SchemaVersion != SaveSchema.Current) return false;
        if (envelope.Payload is null) return false;

        state = envelope.Payload;
        return true;
    }
}
