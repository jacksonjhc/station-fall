using Godot;
using Stationfall.Core.Progression;
using Stationfall.Core.SaveData;
// Godot.FileAccess collides with System.IO.FileAccess (pulled in by ImplicitUsings).
// Alias keeps the call sites readable.
using FileAccess = Godot.FileAccess;

namespace Stationfall.Godot.Persistence;

// Godot-side I/O wrapper around SaveSerializer. Owns the path on disk, the
// FileAccess calls, and the wipe-on-mismatch logging policy. Core stays
// pure — it just serializes strings.
//
// File location is user:// — the per-user, per-game data directory Godot
// resolves at runtime (see Godot's data paths docs). On Windows that's
// %APPDATA%/Godot/app_userdata/<project name>/.
public static class SaveFile
{
    public const string Path = "user://save.json";

    // Returns the loaded MetaState, or a fresh one if the file is missing,
    // unreadable, or schema-mismatched. Logs the wipe so we notice if it
    // happens when we didn't expect it (e.g. a save survived a schema bump
    // we didn't intend).
    public static MetaState LoadOrCreate()
    {
        if (!FileAccess.FileExists(Path))
            return new MetaState();

        using var file = FileAccess.Open(Path, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PushWarning($"[save] could not open {Path} for read (err={FileAccess.GetOpenError()}); starting fresh");
            return new MetaState();
        }

        var json = file.GetAsText();
        if (SaveSerializer.TryDeserialize(json, out var loaded))
            return loaded;

        GD.PushWarning($"[save] {Path} failed to deserialize (schema mismatch or malformed); starting fresh");
        return new MetaState();
    }

    public static void Save(MetaState state)
    {
        var json = SaveSerializer.Serialize(state);
        using var file = FileAccess.Open(Path, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            GD.PushError($"[save] could not open {Path} for write (err={FileAccess.GetOpenError()})");
            return;
        }
        file.StoreString(json);
    }
}
