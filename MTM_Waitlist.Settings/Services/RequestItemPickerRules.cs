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
    /// The active-job part an auto-populated Item depends on for visibility, or <see cref="RequestJobPartKind.None"/>
    /// for manual / always-available Items. Grounded in Request-Config-Template.csv Source + Notes columns.
    /// </summary>
    public static RequestJobPartKind RequiredJobPart(RequestItemDefinition item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.Id switch
        {
            "pickup-coil" or "deliver-coil" or "assist-coil-turn" or "deliver-wrong-coil" => RequestJobPartKind.Coil,
            "pickup-flatstock" or "deliver-flatstock" or "deliver-wrong-flatstock" => RequestJobPartKind.Flatstock,
            "pickup-die" or "deliver-die" => RequestJobPartKind.Die,
            "pickup-component" => RequestJobPartKind.Component,
            "pickup-dunnage" or "deliver-dunnage" => RequestJobPartKind.Dunnage,
            "assist-table-place" or "assist-table-remove" => RequestJobPartKind.AnySubordinate,
            // FG / WIP / Outside / NCM are resolved against a live work order + sequence (Phase 6.2).
            "pickup-fg" or "pickup-wip" or "pickup-outside-service" or "pickup-ncm" => RequestJobPartKind.RequiresActiveJob,
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
    /// Whether the Item is a user-entry path (needs the operator to enter/select a value). Mirrors
    /// <see cref="RequestItemDefinition.NeedsUserEntry"/>; kept here so picker call sites have one rule surface.
    /// </summary>
    public static bool IsUserEntryItem(RequestItemDefinition item) => item.NeedsUserEntry;

    /// <summary>
    /// Destination rule: every Deliver Item is delivered to the requesting work center (no user entry).
    /// </summary>
    public static bool IsDeliverDestinationWorkCenter(RequestItemDefinition item)
        => item.Category == RequestCategory.Deliver;

    /// <summary>
    /// Conditional-visibility rule: an Item is shown only when the requesting job has what it needs.
    /// Manual / user-entry / free-text Items are always visible.
    /// </summary>
    public static bool IsVisible(RequestItemDefinition item, RequestJobPartAvailability availability)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(availability);
        return RequiredJobPart(item) switch
        {
            RequestJobPartKind.None => true,
            RequestJobPartKind.Coil => availability.HasCoil,
            RequestJobPartKind.Flatstock => availability.HasFlatstock,
            RequestJobPartKind.Die => availability.HasDie,
            RequestJobPartKind.Component => availability.HasComponent,
            RequestJobPartKind.Dunnage => availability.HasDunnage,
            RequestJobPartKind.AnySubordinate => availability.HasAnySubordinate,
            RequestJobPartKind.RequiresActiveJob => availability.HasActiveJob,
            _ => true,
        };
    }
}
