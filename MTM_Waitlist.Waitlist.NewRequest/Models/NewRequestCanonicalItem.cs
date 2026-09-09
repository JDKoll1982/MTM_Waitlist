using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Waitlist.Models;

/// <summary>
/// A selectable canonical Item card in the Phase 3 Category→Item picker. Carries the canonical
/// identity (Category + CSV ItemId) AND the legacy DB leaf (RequestType + optional Subtype) that the
/// wizard must submit so the existing downstream steps and persistence are unchanged. Items are
/// derived by grouping the DB request-type tree's leaves into canonical Category/Item groups
/// (see <c>MTM_Waitlist.Module_Waitlist.Services.NewRequestCanonicalPicker</c>).
/// </summary>
public sealed class NewRequestCanonicalItem
{
    /// <summary>Umbrella category this item hangs under (Pickup/Deliver/Assist/Other).</summary>
    public RequestCategory Category { get; init; }

    /// <summary>Canonical CSV item id (Request-Config-Template.csv col 3), e.g. "pickup-coil".</summary>
    public string ItemId { get; init; } = string.Empty;

    /// <summary>Canonical display name (CSV col 4); falls back to the primary leaf name.</summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>Umbrella verb shown as the card Line 1 / tile subtitle (CSV col 6).</summary>
    public string UmbrellaVerb { get; init; } = string.Empty;

    /// <summary>CSV display order within the category (CSV col 2).</summary>
    public int Order { get; init; }

    /// <summary>Legacy leaf's request type to submit, e.g. "Pickup" / "Die Handling".</summary>
    public string RequestType { get; init; } = string.Empty;

    /// <summary>Legacy leaf's subtype to submit, or null when the submission target is the type leaf itself.</summary>
    public string? Subtype { get; init; }

    public bool RequiresTextInput { get; init; }

    public string PromptText { get; init; } = string.Empty;

    public int MinLength { get; init; }

    public int MaxLength { get; init; } = 200;

    /// <summary>True when the item merges more than one legacy leaf (e.g. pickup-coil ← Coil Pickup + Pickup Coil + …).</summary>
    public bool MergesLegacyLeaves { get; init; }

    /// <summary>Short human line under the item name on the tile (legacy leaf name(s)).</summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>Resolved tile image path (override from settings or default asset); set by the view model.</summary>
    public string ImagePath { get; set; } = string.Empty;
}
