using Godot;
using Stationfall.Core.ProcGen;

namespace Stationfall.Godot.Dungeon;

// Resolves a RoomDescriptor.TemplateName to a PackedScene and instantiates it.
// Replaces DungeonRoot.ResolveScene's hard-coded switch — the registry is now
// data-driven, so generator-emitted template names plug in without code changes.
//
// Lifecycle, door configuration, and persistence stay on DungeonRoot.
public sealed class DungeonInstantiator
{
    private readonly System.Collections.Generic.Dictionary<string, PackedScene> _registry = new();

    public DungeonInstantiator(System.Collections.Generic.IEnumerable<(string Name, PackedScene? Scene)> entries)
    {
        foreach (var (name, scene) in entries)
        {
            if (scene == null) continue;
            if (string.IsNullOrEmpty(name)) continue;
            _registry[name] = scene;
        }
    }

    public bool TryResolve(string templateName, out PackedScene scene)
    {
        if (_registry.TryGetValue(templateName, out var found))
        {
            scene = found;
            return true;
        }
        scene = null!;
        return false;
    }

    public RoomController? Build(RoomDescriptor descriptor)
    {
        if (!TryResolve(descriptor.TemplateName, out var scene))
        {
            GD.PushError($"DungeonInstantiator: no scene registered for template '{descriptor.TemplateName}' (room '{descriptor.Id}')");
            return null;
        }
        var instance = scene.Instantiate<RoomController>();
        instance.RoomId = descriptor.Id;
        return instance;
    }

    public System.Collections.Generic.IReadOnlyCollection<string> KnownTemplateNames => _registry.Keys;
}
