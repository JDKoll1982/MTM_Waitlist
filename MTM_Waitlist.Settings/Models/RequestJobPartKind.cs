namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// Which active-job part (if any) an auto-populated canonical Item depends on for
/// visibility in the New Request picker / card render. The per-Item visibility rules are recorded in
/// specs/004-unified-card-item-picker/contracts/request-picker-flow.md §4.
/// </summary>
public enum RequestJobPartKind
{
    /// <summary>No job part required — always available (manual equipment/consumable or free text).</summary>
    None,

    /// <summary>Visible only when the requesting job has a coil (MMC / Coil subordinate).</summary>
    Coil,

    /// <summary>
    /// Visible when the requesting job has a coil <b>or</b> flatstock. This is the merged Pickup Item
    /// (<c>pickup-coil</c>), whose one request covers either material (FR-002, D21): a flatstock-only job that
    /// withheld it would silently lose a supported Item, which is what SC-004 measures.
    /// </summary>
    CoilOrFlatstock,

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

    /// <summary>
    /// Visible only when the requesting job has a <b>real scrap decision</b> — a scrap value that is set, is
    /// not <c>No Scrap</c> and is not the <c>Scrap Type Required</c> placeholder (FR-031, contract §5).
    /// Evaluated through the canonical predicate in <c>MTM_Waitlist.Core</c> so Setup and New Request
    /// cannot diverge.
    /// </summary>
    Scrap,

    /// <summary>Visible only when the requesting job has a work order/sequence (FG / WIP / Outside, NCM defect).</summary>
    RequiresActiveJob,

    /// <summary>
    /// The requesting job's finished parts — the work the <c>pickup-fg</c> Item names. The picker offers that
    /// Item on any active job, because the job's own product is the work it names; this flag records what the
    /// derivation found, which the card's <c>Part Type</c> cell reads, and does not hide the Item (FR-094).
    /// </summary>
    FinishedGoodsToReturn,

    /// <summary>
    /// Visible only when the requesting job has <b>non-conforming stock</b> — material sitting at an NCM
    /// location. This is the work the <c>pickup-ncm</c> Item names, and it is a signal of the location the stock
    /// occupies, never of the part's recorded item type (FR-093, FR-094).
    /// </summary>
    NonconformingStockToReturn,

    /// <summary>
    /// The requesting job's work in progress — the work the <c>pickup-wip</c> Item names. Like the finished-parts
    /// pickup, the Item is offered on any active job and this flag records what the derivation found (FR-094).
    /// </summary>
    WorkInProgressToReturn,

    /// <summary>
    /// Visible only when the requesting job has <b>subcontracted work</b> — an operation an outside vendor
    /// performs. This is the work the <c>pickup-outside-service</c> Item names (FR-094).
    /// </summary>
    OutsideServiceWorkToReturn,
}
