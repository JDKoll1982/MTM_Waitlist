namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// Material disposition of a part/order on the shop floor, used to drive the Finished-Product
/// Item leaves (FG / NCM / WIP / Outside Service) in the Type/Category/Item model.
/// Type/Category refactor (2026-09-07): the Finished Product grouping node branches into these
/// dispositions. FG / WIP / Outside Service are derived from the Infor Visual job state (file 14 Phase 6);
/// NCM is derived from the location the stock occupies and is never a production type (FR-093).
/// Dispositions are recorded by specs/004-unified-card-item-picker/contracts/request-picker-flow.md §6
/// (pickup-fg / pickup-ncm / pickup-wip / pickup-outside-service).
/// </summary>
public enum RequestDisposition
{
    /// <summary>Unknown / not yet determined (no matching rule fired).</summary>
    Unknown,

    /// <summary>Finished Goods — stock on hand at a finished-goods / shipping location (PART.QTY_ON_HAND / PART_LOCATION.QTY &gt; 0).</summary>
    FinishedGoods,

    /// <summary>Work In Process — open quantity still on the work order / MT_WIP_INVENTORY.</summary>
    WorkInProcess,

    /// <summary>Outside Service — an operation is subcontracted to an outside vendor (OPERATION has VENDOR_ID / SERVICE_ID / SERVICE_PART_ID).</summary>
    OutsideService,

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
