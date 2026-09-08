namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// Canonical request Item (the thing being handled) in the Type/Category/Item model.
/// Type/Category refactor (2026-09-07): Item hangs under a Category umbrella and is shown as the
/// card Line 2 identifier. Mirror of one row of Request-Config-Template.csv (18 rows total).
/// </summary>
public sealed class RequestItemDefinition
{
    /// <summary>Umbrella category this item belongs to (Pickup/Deliver/Assist/Other).</summary>
    public RequestCategory Category { get; init; }

    /// <summary>Display order within the category (CSV col 2).</summary>
    public int Order { get; init; }

    /// <summary>Stable normalized id (CSV col 3), e.g. "pickup-coil", "other".</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Human-friendly display name (CSV col 4).</summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>Normalized item name (CSV col 5).</summary>
    public string NormalizedName { get; init; } = string.Empty;

    /// <summary>The umbrella verb shown as card Line 1 (CSV col 6).</summary>
    public string UmbrellaVerb { get; init; } = string.Empty;

    /// <summary>Item value type for the Line 2 identifier / payload (CSV col 10).</summary>
    public RequestItemValueType ValueType { get; init; } = RequestItemValueType.String;

    /// <summary>Whether the operator must enter/select this item (CSV col 12).</summary>
    public bool NeedsUserEntry { get; init; }

    /// <summary>The value persisted when this request is submitted (CSV col 18).</summary>
    public string ProducedValue { get; init; } = string.Empty;
}
