using Godot;
using System.Collections.Generic;
using Stationfall.Godot.Enemies;

namespace Stationfall.Godot.Debug;

// Debug-only lookup of EnemyResource by Id, populated by scanning
// res://Assets/Data/Enemies/ at _Ready. Production spawning runs through the
// per-room EnemySpawner authored in scenes; this exists so the debug console
// can spawn arbitrary enemies by short name without scene plumbing.
//
// Editor / dev builds only — DirAccess on res:// against unpacked content
// won't work in exported PCK builds, but the debug overlay never ships to
// players anyway.
public partial class EnemyRegistry : Node
{
    private const string EnemiesDir = "res://Assets/Data/Enemies/";

    private readonly Dictionary<string, EnemyResource> _byId = new(System.StringComparer.OrdinalIgnoreCase);

    public IEnumerable<string> Ids => _byId.Keys;

    public override void _Ready()
    {
        var dir = DirAccess.Open(EnemiesDir);
        if (dir == null)
        {
            GD.PushWarning($"EnemyRegistry: could not open {EnemiesDir}");
            return;
        }

        dir.ListDirBegin();
        for (var name = dir.GetNext(); !string.IsNullOrEmpty(name); name = dir.GetNext())
        {
            if (dir.CurrentIsDir()) continue;
            // Godot may surface .remap entries instead of raw .tres in some
            // import states — strip the suffix to get the real resource path.
            var resourceName = name.EndsWith(".remap") ? name[..^".remap".Length] : name;
            if (!resourceName.EndsWith(".tres")) continue;

            var path = EnemiesDir + resourceName;
            if (ResourceLoader.Load(path) is EnemyResource res && !string.IsNullOrEmpty(res.Id))
            {
                _byId[res.Id] = res;
            }
        }
        dir.ListDirEnd();

        GD.Print($"[debug] EnemyRegistry loaded {_byId.Count} enemy definitions: {string.Join(", ", _byId.Keys)}");
    }

    public EnemyResource? Get(string id) => _byId.TryGetValue(id, out var r) ? r : null;

    // Console-friendly resolution: exact id wins; otherwise a unique substring
    // match. So `spawn patient` resolves to `twitching_patient`. Returns null
    // for ambiguous or unknown queries — callers list options to disambiguate.
    public EnemyResource? Resolve(string query)
    {
        if (string.IsNullOrEmpty(query)) return null;
        if (_byId.TryGetValue(query, out var exact)) return exact;
        EnemyResource? hit = null;
        int hits = 0;
        foreach (var (id, res) in _byId)
        {
            if (id.Contains(query, System.StringComparison.OrdinalIgnoreCase))
            {
                hit = res;
                hits++;
            }
        }
        return hits == 1 ? hit : null;
    }
}
