namespace Stationfall.Core.Combat;

// Runtime kind of an applied status. Mirrors StatusTag (the metadata axis),
// but lives in Combat/ because applied statuses are gameplay state — separate
// from the authoring-time tagging system. M7 ships only Slow; Bleed / Poison
// / Stun land alongside W3 / W6 work.
public enum StatusKind
{
    Slowed,
}
