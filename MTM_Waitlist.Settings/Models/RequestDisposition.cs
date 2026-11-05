namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// Material disposition of a part/order on the shop floor, used to drive the Finished-Product
/// Item leaves (FG / NCM / WIP) in the Type/Category/Item model.
/// Type/Category refactor (2026-09-07): the Finished Product grouping node branches into these
/// dispositions. FG / WIP are derived from the Infor Visual job state; NCM is derived from the location the stock
/// occupies and is never a production type (FR-093). There is deliberately no outside-service member: outside
/// service is the name of a shop resource, printed on a WIP tag's next-resource line, so a job routed there is
/// work in process (IT, 2026-11-04 — see <c>RequestDispositionClassifier</c>).
/// Dispositions are recorded by specs/004-unified-card-item-picker/contracts/request-picker-flow.md §6
/// (pickup-fg / pickup-ncm / pickup-wip / pickup-outside-service).
/// </summary>
public enum RequestDisposition
{
    /// <summary>Unknown / not yet determined (no matching rule fired).</summary>
    Unknown,

    /// <summary>Finished Goods — stock on hand at a finished-goods / shipping location (PART.QTY_ON_HAND / PART_LOCATION.QTY &gt; 0).</summary>
    FinishedGoods,

    /// <summary>
    /// Work In Process — the work order still has operations to run, so the plant's tag for it is a WIP tag. A job
    /// whose next operation is the subcontracted one belongs here too: its tag names "Outside Service" as the next
    /// resource, it does not become a fourth type.
    /// </summary>
    WorkInProcess,

    /// <summary>
    /// Non-Conforming Material — stock sitting at a non-conforming (NCM) location. This is a state of the stock's
    /// <b>location</b>, not a production type, so it is never derived from the routing and never from a part's
    /// recorded item type (FR-093). The location is the whole signal: the WIP application marks NCM areas in
    /// <c>md_locations</c> (<c>Location LIKE 'NCM%'</c>) while the part's own <c>ItemType</c> still reads
    /// <c>WIP</c>, and on the Infor side the same idea lives in <c>PART_LOCATION.STATUS</c> against the
    /// <c>LOCATION</c> the stock occupies.
    /// </summary>
    Ncm
}
