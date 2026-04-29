namespace Stationfall.Core.ProcGen;

public enum RoomType
{
    Entry,
    Combat,
    Item,
    Vendor,
    MidBoss,
    Boss,
    Secret,
    Narrative,
    Empty,
    // Off-pool guaranteed grant of a specific tool — the M6 Magnetic Grapple
    // pedestal (PLANNING.md § Slice tool roster: "Guaranteed via M6 pedestal
    // room (off-pool entirely)"). One per layout by default; Sector 2+ may
    // drop this to 0 once the run already carries the grapple.
    ToolPedestal,
}
