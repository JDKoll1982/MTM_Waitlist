using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// Pure picker rules over the canonical <see cref="RequestItemCatalog"/> that the New Request
/// Category→Item picker (Phase 3) and the uniform card renderer (Phase 4) consume. Encodes the
/// CSV columns that are not already on <see cref="RequestItemDefinition"/>:
///   • auto-populated Item visibility — a job-part item appears only if the requesting job has that part
///   • always-available manual equipment/consumable Items (Riser Table / Hopper / Scrap / Other)
///   • Deliver destination is always the requesting work center (no user entry)
/// Deterministic and DB-free so the rules are unit-testable before any UI wiring.
/// </summary>
public static class RequestItemPickerRules
{
    /// <summary>
    /// Catalogued Items that are <b>never offered</b>: they exist in the catalog so the twenty-four-row
    /// identity assertion stays true, and are hidden by this rule rather than by deletion — a hidden Item and a
    /// missing row are different states (FR-028).
    /// </summary>
    private static readonly string[] s_outOfScopeItemIds =
    [
        "pickup-fg",
        "pickup-ncm",
        "pickup-wip",
        "pickup-outside-service"
    ];

    /// <summary>Whether the Item is catalogued but deliberately never offered (FR-028).</summary>
    public static bool IsOutOfScope(RequestItemDefinition item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return s_outOfScopeItemIds.Contains(item.Id, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The active-job part an auto-populated Item depends on for visibility, or <see cref="RequestJobPartKind.None"/>
    /// for manual / always-available Items. Grounded in the visibility rules recorded in
    /// specs/004-unified-card-item-picker/contracts/request-picker-flow.md §4.
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
            // The four out-of-scope Items fall here rather than on their own arm: they are hidden by
            // IsOutOfScope regardless of what the job has (FR-028).
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
    /// Manual / always-available Items are always visible; a catalogued-but-out-of-scope Item never is.
    /// </summary>
    public static bool IsVisible(RequestItemDefinition item, RequestJobPartAvailability availability)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(availability);

        if (IsOutOfScope(item))
        {
            return false;
        }

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
            _ => true,
        };
    }
}
