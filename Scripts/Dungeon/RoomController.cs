using Godot;

namespace Stationfall.Godot.Dungeon;

public enum RoomLifecycle
{
    Unexplored,
    Entered,
    Active,
    Cleared,
}

public partial class RoomController : Node2D
{
    [Signal] public delegate void EnteredEventHandler();
    [Signal] public delegate void ClearedEventHandler();
    [Signal] public delegate void StateChangedEventHandler(int state);

    [Export] public string RoomId { get; set; } = "";
    [Export] public bool RequiresClear { get; set; } = false;

    public RoomLifecycle State { get; private set; } = RoomLifecycle.Unexplored;

    public override void _Ready()
    {
        // Group membership lets the debug console enumerate rooms cheaply.
        AddToGroup("rooms");
    }

    public void OnPlayerEntered()
    {
        if (State != RoomLifecycle.Unexplored && State != RoomLifecycle.Entered) return;

        ChangeState(RoomLifecycle.Entered);
        EmitSignal(SignalName.Entered);

        if (RequiresClear && !IsCleared())
            ChangeState(RoomLifecycle.Active);
        else
            // Route through Clear() so the Cleared signal fires — door nodes rely on it
            // to refresh their locks. Going straight to Cleared via ChangeState skipped that.
            Clear();
    }

    public void Clear()
    {
        if (State == RoomLifecycle.Cleared) return;
        ChangeState(RoomLifecycle.Cleared);
        EmitSignal(SignalName.Cleared);
    }

    public bool IsCleared() => State == RoomLifecycle.Cleared;

    private void ChangeState(RoomLifecycle next)
    {
        if (State == next) return;
        State = next;
        EmitSignal(SignalName.StateChanged, (int)next);
    }
}
