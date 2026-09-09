using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Waitlist.Models;

/// <summary>
/// One umbrella Category stage of the Phase 3 Category→Item picker: the category's canonical Items
/// in CSV order, each carrying its submittable legacy leaf. Only categories that resolve at least one
/// submittable item are surfaced by <c>NewRequestCanonicalPicker</c>.
/// </summary>
public sealed class NewRequestCanonicalCategory
{
    /// <summary>Umbrella category (Pickup/Deliver/Assist/Other).</summary>
    public RequestCategory Category { get; init; }

    /// <summary>Display name for the category tile/header, e.g. "Pickup".</summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>Canonical Items under this category, ordered by CSV Order.</summary>
    public IReadOnlyList<NewRequestCanonicalItem> Items { get; init; } = Array.Empty<NewRequestCanonicalItem>();
}
