namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// Canonical Category → Item catalog for the Type/Category/Item model.
/// Type/Category refactor (2026-09-07). Mirror of the authoritative implementation spec
/// WeekendProject/Documents/Request-Config-Template.csv (23 rows: Pickup 11 / Deliver 8 / Assist 3 / Other 1).
/// Expanded 2026-09-08 with legacy-only concepts confirmed by the user: Wrong Coil / Wrong Flatstock
/// (Deliver-correct + Pickup-wrong replacements), Scrap offal removal, Pickup Hopper (do not return),
/// and Table Remove Parts (Assist). Keeps stable GUID relationships intact via the existing
/// RequestType/RequestSubtype inventories.
/// </summary>
public static class RequestItemCatalog
{
    /// <summary>All 23 canonical items, ordered by Category then Order.</summary>
    public static readonly IReadOnlyList<RequestItemDefinition> Items = new[]
    {
        // --- Pickup (11) ---
        Item(RequestCategory.Pickup, 1, "pickup-coil", "Coil or Flatstock", "coil-or-flatstock", RequestItemValueType.String, false, "pickup-coil"),
        Item(RequestCategory.Pickup, 2, "pickup-die", "Die", "die", RequestItemValueType.Enum, true, "pickup-die"),
        Item(RequestCategory.Pickup, 3, "pickup-component", "Component", "component", RequestItemValueType.Enum, true, "pickup-component"),
        Item(RequestCategory.Pickup, 4, "pickup-fg", "Finished Goods (FG)", "finished-goods-fg", RequestItemValueType.String, false, "pickup-fg"),
        Item(RequestCategory.Pickup, 5, "pickup-ncm", "Non-Conforming (NCM)", "non-conforming-ncm", RequestItemValueType.String, true, "pickup-ncm"),
        Item(RequestCategory.Pickup, 6, "pickup-wip", "Work In Process (WIP)", "work-in-process-wip", RequestItemValueType.String, false, "pickup-wip"),
        Item(RequestCategory.Pickup, 7, "pickup-outside-service", "Outside Service", "outside-service", RequestItemValueType.String, false, "pickup-outside-service"),
        Item(RequestCategory.Pickup, 8, "pickup-riser-table", "Riser Table", "riser-table", RequestItemValueType.String, false, "pickup-riser-table"),
        Item(RequestCategory.Pickup, 9, "pickup-dunnage", "Dunnage", "dunnage", RequestItemValueType.Enum, true, "pickup-dunnage"),
        Item(RequestCategory.Pickup, 10, "pickup-scrap", "Scrap / Offal removal", "scrap-offal-removal", RequestItemValueType.Text, true, "pickup-scrap"),
        Item(RequestCategory.Pickup, 11, "pickup-hopper", "Pickup Hopper (do not return)", "hopper-pickup", RequestItemValueType.String, false, "pickup-hopper"),

        // --- Deliver (8) ---
        Item(RequestCategory.Deliver, 1, "deliver-coil", "Coil", "coil", RequestItemValueType.String, false, "deliver-coil"),
        Item(RequestCategory.Deliver, 2, "deliver-riser-table", "Riser Table", "riser-table", RequestItemValueType.String, false, "deliver-riser-table"),
        Item(RequestCategory.Deliver, 3, "deliver-hopper", "Hopper (scrap)", "hopper", RequestItemValueType.String, false, "deliver-hopper"),
        Item(RequestCategory.Deliver, 4, "deliver-flatstock", "Flatstock", "flatstock", RequestItemValueType.String, false, "deliver-flatstock"),
        Item(RequestCategory.Deliver, 5, "deliver-die", "Die", "die", RequestItemValueType.String, false, "deliver-die"),
        Item(RequestCategory.Deliver, 6, "deliver-dunnage", "Dunnage", "dunnage", RequestItemValueType.Enum, true, "deliver-dunnage"),
        Item(RequestCategory.Deliver, 7, "deliver-wrong-coil", "Wrong Coil", "wrong-coil", RequestItemValueType.String, true, "deliver-wrong-coil"),
        Item(RequestCategory.Deliver, 8, "deliver-wrong-flatstock", "Wrong Flatstock", "wrong-flatstock", RequestItemValueType.String, true, "deliver-wrong-flatstock"),

        // --- Assist (3) ---
        Item(RequestCategory.Assist, 1, "assist-coil-turn", "Coil", "coil", RequestItemValueType.String, false, "assist-coil-turn"),
        Item(RequestCategory.Assist, 2, "assist-table-place", "Place Parts on Table", "table-parts", RequestItemValueType.String, false, "assist-table-place"),
        Item(RequestCategory.Assist, 3, "assist-table-remove", "Remove Parts from Table", "table-parts-remove", RequestItemValueType.String, false, "assist-table-remove"),

        // --- Other (1) ---
        Item(RequestCategory.Other, 1, "other", "Other", "other", RequestItemValueType.Text, true, "other")
    };

    /// <summary>All items belonging to a given umbrella category, ordered by Order.</summary>
    public static IReadOnlyList<RequestItemDefinition> GetByCategory(RequestCategory category) =>
        Items.Where(i => i.Category == category).OrderBy(i => i.Order).ToArray();

    /// <summary>Finds an item by its stable normalized id (e.g. "pickup-coil"), or null.</summary>
    public static RequestItemDefinition? FindById(string? id) =>
        Items.FirstOrDefault(i => string.Equals(i.Id, id, StringComparison.OrdinalIgnoreCase));

    /// <summary>Total catalog count (23).</summary>
    public static int TotalCount => Items.Count;

    private static RequestItemDefinition Item(
        RequestCategory category, int order, string id, string displayName, string normalizedName,
        RequestItemValueType valueType, bool needsUserEntry, string producedValue) =>
        new()
        {
            Category = category,
            Order = order,
            Id = id,
            DisplayName = displayName,
            NormalizedName = normalizedName,
            UmbrellaVerb = UmbrellaVerbFor(category),
            ValueType = valueType,
            NeedsUserEntry = needsUserEntry,
            ProducedValue = producedValue
        };

    /// <summary>Maps a category to its umbrella verb shown as card Line 1.</summary>
    private static string UmbrellaVerbFor(RequestCategory category) => category switch
    {
        RequestCategory.Pickup => "Pickup",
        RequestCategory.Deliver => "Deliver",
        RequestCategory.Assist => "Assist",
        _ => "Other"
    };
}
