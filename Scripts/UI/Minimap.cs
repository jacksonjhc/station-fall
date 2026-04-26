using Godot;
using Stationfall.Core.ProcGen;

namespace Stationfall.Godot.UI;

public partial class Minimap : Control
{
    [Export] public int CellSize { get; set; } = 18;
    [Export] public int CellSpacing { get; set; } = 4;
    [Export] public Color VisitedColor { get; set; } = new Color(0.85f, 0.85f, 0.85f);
    [Export] public Color AdjacentOutlineColor { get; set; } = new Color(0.45f, 0.45f, 0.45f);
    [Export] public Color ActiveOutlineColor { get; set; } = new Color(1f, 0.9f, 0.4f);
    [Export] public Color CorridorColor { get; set; } = new Color(0.5f, 0.5f, 0.5f);

    private DungeonLayout? _layout;
    private System.Collections.Generic.IReadOnlyDictionary<string, GridPosition>? _positions;
    private readonly System.Collections.Generic.HashSet<string> _visited = new();
    private string _activeRoomId = "";

    public void SetLayout(DungeonLayout layout)
    {
        _layout = layout;
        _positions = LayoutPositions.ComputeGridPositions(layout);
        QueueRedraw();
    }

    public void OnRoomVisited(string roomId)
    {
        _visited.Add(roomId);
        QueueRedraw();
    }

    public void SetActiveRoom(string roomId)
    {
        _activeRoomId = roomId;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_layout == null || _positions == null) return;

        // Center the grid around (0,0).
        int minX = int.MaxValue, minY = int.MaxValue;
        foreach (var pos in _positions.Values)
        {
            if (pos.X < minX) minX = pos.X;
            if (pos.Y < minY) minY = pos.Y;
        }

        // Pre-compute room visibility: visited OR adjacent-to-visited.
        var visibleIds = new System.Collections.Generic.HashSet<string>(_visited);
        foreach (var visitedId in _visited)
        {
            if (!_layout.TryGetRoom(visitedId, out var visitedRoom)) continue;
            foreach (var (_, door) in visitedRoom.Doors)
                visibleIds.Add(door.TargetRoomId);
        }

        // Pass 1: corridors (between visited rooms).
        foreach (var room in _layout.Rooms)
        {
            if (!_visited.Contains(room.Id)) continue;
            foreach (var (direction, door) in room.Doors)
            {
                if (!_visited.Contains(door.TargetRoomId)) continue;
                // Draw each pair only once.
                if (string.CompareOrdinal(room.Id, door.TargetRoomId) > 0) continue;
                DrawCorridor(room.Id, door.TargetRoomId, minX, minY);
            }
        }

        // Pass 2: cells.
        foreach (var room in _layout.Rooms)
        {
            if (!visibleIds.Contains(room.Id)) continue;
            DrawCell(room.Id, minX, minY);
        }
    }

    private void DrawCell(string roomId, int minX, int minY)
    {
        if (_positions == null) return;
        var pos = _positions[roomId];
        var rect = CellRect(pos, minX, minY);

        bool visited = _visited.Contains(roomId);
        bool active = roomId == _activeRoomId;

        if (visited) DrawRect(rect, VisitedColor, filled: true);
        var outline = active ? ActiveOutlineColor : (visited ? VisitedColor : AdjacentOutlineColor);
        DrawRect(rect, outline, filled: false, width: 2f);
    }

    private void DrawCorridor(string a, string b, int minX, int minY)
    {
        if (_positions == null) return;
        var ra = CellRect(_positions[a], minX, minY);
        var rb = CellRect(_positions[b], minX, minY);
        var line = new Vector2[]
        {
            new(ra.Position.X + ra.Size.X / 2, ra.Position.Y + ra.Size.Y / 2),
            new(rb.Position.X + rb.Size.X / 2, rb.Position.Y + rb.Size.Y / 2),
        };
        DrawLine(line[0], line[1], CorridorColor, 2f);
    }

    private Rect2 CellRect(GridPosition pos, int minX, int minY)
    {
        int x = (pos.X - minX) * (CellSize + CellSpacing);
        int y = (pos.Y - minY) * (CellSize + CellSpacing);
        return new Rect2(new Vector2(x, y), new Vector2(CellSize, CellSize));
    }
}
