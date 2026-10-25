using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// Pure picker rules over the canonical <see cref="RequestItemCatalog"/> that the New Request
/// Category→Item picker (Phase 3) and the uniform card renderer (Phase 4) consume. Encodes the
/// CSV columns that are not already on <see cref="RequestItemDefinition"/>:
///   • auto-populated Item visibility — a job-part item appears only if the requesting job has that part
///   • the finished-parts, WIP and outside-service pickups are one choice — the job's own product — so exactly
///     one of the three is offered, selected by the derived product type; the NCM pickup is offered on the
///     signal it names (FR-094)
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
    /// catalogued, and what keeps one out is a job that does not name it — for the three product pickups that
    /// means another of the three was derived for the job, or that there is no active job at all.
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
    /// The one product pickup a job offers: outside service when the job's product is subcontracted, WIP while
    /// work remains, and finished goods otherwise — including when nothing was derived, where the generic pickup
    /// is the only honest answer because the pickup itself is real work (FR-089).
    /// <para>
    /// Answering with the Item's own id is what keeps the three mutually exclusive by construction: the Item step
    /// is a single question about the job's product, so it can never offer two answers to it.
    /// </para>
    /// </summary>
    public static string ProductPickupItemId(RequestJobPartAvailability availability)
    {
        ArgumentNullException.ThrowIfNull(availability);

        if (availability.HasOutsideServiceWorkToReturn)
        {
            return "pickup-outside-service";
        }

        return availability.HasWorkInProgressToReturn ? "pickup-wip" : "pickup-fg";
    }

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
            // Exactly one of the three product pickups is ever offered, and the derivation decides which; a job
            // whose product type nothing named still gets the generic one, because the pickup it names is real
            // work even when the type is not yet known. What stays hidden in that case is the card's
            // `Part Type` cell, not the choice (FR-089, FR-091, FR-094).
            RequestJobPartKind.FinishedGoodsToReturn =>
                availability.HasActiveJob && ProductPickupItemId(availability) == "pickup-fg",
            RequestJobPartKind.WorkInProgressToReturn =>
                availability.HasActiveJob && ProductPickupItemId(availability) == "pickup-wip",
            // Non-conforming stock and subcontracted work are read from signals that are confirmed — the
            // location the stock occupies and the operation's vendor fields — so those two keep their own gate.
            RequestJobPartKind.NonconformingStockToReturn => availability.HasNonconformingStockToReturn,
            RequestJobPartKind.OutsideServiceWorkToReturn =>
                ProductPickupItemId(availability) == "pickup-outside-service",
            _ => true,
        };
    }
}
