using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// Pure picker rules over the canonical <see cref="RequestItemCatalog"/> that the New Request
/// Category→Item picker (Phase 3) and the uniform card renderer (Phase 4) consume. Encodes the
/// CSV columns that are not already on <see cref="RequestItemDefinition"/>:
///   • auto-populated Item visibility — a job-part item appears only if the requesting job has that part
///   • the finished-parts and WIP pickups appear on any active job, because the job's own product is the work
///     they name; the NCM and outside-service pickups appear on the signal each one names (FR-094)
///   • always-available manual equipment/consumable Items (Riser Table / Hopper / Scrap / Other)
///   • Deliver destination is always the requesting work center (no user entry)
/// Deterministic and DB-free so the rules are unit-testable before any UI wiring.
/// </summary>
public static class RequestItemPickerRules
{
    /// <summary>
    /// The active-job part an auto-populated Item depends on for visibility, or <see cref="RequestJobPartKind.None"/>
    /// for manual / always-available Items. Grounded in the visibility rules recorded in
    /// specs/004-unified-card-item-picker/contracts/request-picker-flow.md §4.
    /// <para>
    /// The four previously-hidden pickups answer here on the work the requesting job actually has — finished
    /// goods to return, non-conforming stock, work in progress, subcontracted work — rather than on a fixed list
    /// of Item codes (FR-094). A hidden Item and a missing row stay different states: the four are still
    /// catalogued, and what keeps one out is a job that has none of the work it names — for the finished-parts
    /// and WIP pickups that means no active job at all, since the job's own product is the work they name.
    /// </para>
    /// </summary>
    public static RequestJobPartKind RequiredJobPart(RequestItemDefinition item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.Id switch
        {
            // The merged Pickup Item covers a coil or a flatstock — either material lights it up (D21).
            "pickup-coil" => RequestJobPartKind.CoilOrFlatstock,
            "deliver-coil" or "assist-coil-turn" or "deliver-wrong-coil" => RequestJobPartKind.Coil,
            "deliver-flatstock" or "deliver-wrong-flatstock" => RequestJobPartKind.Flatstock,
            "pickup-die" or "deliver-die" => RequestJobPartKind.Die,
            // Both component Items are gated the same way, so the Deliver family mirrors the Pickup choice
            // rather than offering a component on a job that has none.
            "pickup-component" or "deliver-component" => RequestJobPartKind.Component,
            "pickup-dunnage" or "deliver-dunnage" => RequestJobPartKind.Dunnage,
            // The Scrap Item is offered only for a real scrap decision — not for 'No Scrap' and not for the
            // 'Scrap Type Required' placeholder, which means no decision was made (FR-031, contract §5).
            "pickup-scrap" => RequestJobPartKind.Scrap,
            "assist-table-place" or "assist-table-remove" => RequestJobPartKind.AnySubordinate,
            // The finished-parts and WIP pickups gate on the job itself; the two that name a signal keep it.
            "pickup-fg" => RequestJobPartKind.FinishedGoodsToReturn,
            "pickup-ncm" => RequestJobPartKind.NonconformingStockToReturn,
            "pickup-wip" => RequestJobPartKind.WorkInProgressToReturn,
            "pickup-outside-service" => RequestJobPartKind.OutsideServiceWorkToReturn,
            _ => RequestJobPartKind.None,
        };
    }

    /// <summary>Whether the Item's availability depends on the active job having a specific part.</summary>
    public static bool IsJobPartItem(RequestItemDefinition item) => RequiredJobPart(item) is not (
        RequestJobPartKind.None or RequestJobPartKind.RequiresActiveJob);

    /// <summary>
    /// Whether the Item is always-available manual equipment/consumable (Riser Table / Hopper) —
    /// zero-payload flag selections that never depend on a job part and need no user part pick.
    /// </summary>
    public static bool IsManualEquipment(RequestItemDefinition item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.Id is "pickup-riser-table" or "deliver-riser-table" or "pickup-hopper" or "deliver-hopper";
    }

    /// <summary>
    /// Destination rule: every Deliver Item is delivered to the requesting work center (no user entry).
    /// </summary>
    public static bool IsDeliverDestinationWorkCenter(RequestItemDefinition item)
        => item.Category == RequestCategory.Deliver;

    /// <summary>
    /// Conditional-visibility rule: an Item is shown only when the requesting job has what it needs.
    /// Manual / always-available Items are always visible.
    /// </summary>
    public static bool IsVisible(RequestItemDefinition item, RequestJobPartAvailability availability)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(availability);

        return RequiredJobPart(item) switch
        {
            RequestJobPartKind.None => true,
            RequestJobPartKind.Coil => availability.HasCoil,
            RequestJobPartKind.CoilOrFlatstock => availability.HasCoil || availability.HasFlatstock,
            RequestJobPartKind.Flatstock => availability.HasFlatstock,
            RequestJobPartKind.Die => availability.HasDie,
            RequestJobPartKind.Component => availability.HasComponent,
            RequestJobPartKind.Dunnage => availability.HasDunnage,
            RequestJobPartKind.Scrap => availability.HasScrapDecision,
            RequestJobPartKind.AnySubordinate => availability.HasAnySubordinate,
            RequestJobPartKind.RequiresActiveJob => availability.HasActiveJob,
            // The finished-parts and WIP pickups are the job's own product, so an active job is the work they
            // name and both are offered on it. The derivation that separates the two leans on finished-goods
            // location names that are not confirmed for this installation, and it needs the plant system to
            // answer at all, so gating the choice on it hid a pickup the shop plainly had to raise. What the
            // derivation does still decide is the `Part Type` the card shows, which stays hidden when it names
            // nothing (FR-091, FR-094).
            RequestJobPartKind.FinishedGoodsToReturn or RequestJobPartKind.WorkInProgressToReturn => availability.HasActiveJob,
            // Non-conforming stock and subcontracted work are read from signals that are confirmed — the
            // location the stock occupies and the operation's vendor fields — so those two keep their own gate.
            RequestJobPartKind.NonconformingStockToReturn => availability.HasNonconformingStockToReturn,
            RequestJobPartKind.OutsideServiceWorkToReturn => availability.HasOutsideServiceWorkToReturn,
            _ => true,
        };
    }
}
