namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// Which active-job part (if any) an auto-populated canonical Item depends on for
/// visibility in the New Request picker / card render. Derived from
/// <c>Request-Config-Template.csv</c> (Item Source + Notes visibility columns).
/// </summary>
public enum RequestJobPartKind
{
    /// <summary>No job part required — always available (manual equipment/consumable or free text).</summary>
    None,

    /// <summary>Visible only when the requesting job has a coil (MMC / Coil subordinate).</summary>
    Coil,

    /// <summary>Visible only when the requesting job has flatstock (MMF / Flatstock subordinate).</summary>
    Flatstock,

    /// <summary>Visible only when the requesting job has a die (FGT / Die subordinate).</summary>
    Die,

    /// <summary>Visible only when the requesting job has a component (non-coil/flatstock subordinate).</summary>
    Component,

    /// <summary>Visible only when the requesting job has an assigned dunnage part.</summary>
    Dunnage,

    /// <summary>Visible only when the requesting job has any subordinate part (used by Assist table place/remove).</summary>
    AnySubordinate,

    /// <summary>Visible only when the requesting job has a work order/sequence (FG / WIP / Outside, NCM defect).</summary>
    RequiresActiveJob,
}

/// <summary>
/// What parts the requesting work center's active setup job actually has. Drives the
/// conditional-visibility rule for auto-populated canonical Items. Pure value snapshot so
/// picker rules stay testable and DB-free; a caller (Phase 3) maps an
/// <c>IActiveJobItemResolverService</c> snapshot onto these flags.
/// </summary>
public sealed record RequestJobPartAvailability(
    bool HasActiveJob,
    bool HasCoil,
    bool HasFlatstock,
    bool HasDie,
    bool HasComponent,
    bool HasDunnage)
{
    /// <summary>An empty job (nothing present) — used for offline "no active job" rendering.</summary>
    public static RequestJobPartAvailability None { get; } = new(false, false, false, false, false, false);

    /// <summary>A job with every part present — renders all auto-populated items.</summary>
    public static RequestJobPartAvailability All { get; } = new(true, true, true, true, true, true);

    public bool HasAnySubordinate => HasCoil || HasFlatstock || HasDie || HasComponent || HasDunnage;
}
