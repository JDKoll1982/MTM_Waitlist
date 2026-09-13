namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// Canonical Category → Item catalog for the Type/Category/Item model.
/// Type/Category refactor (2026-09-07). Mirror of the authoritative implementation spec recorded in
/// specs/004-unified-card-item-picker/contracts/request-picker-flow.md §4 and §6
/// (23 rows: Pickup 11 / Deliver 8 / Assist 3 / Other 1).
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
        Item(RequestCategory.Pickup, 1, "pickup-coil", "coil-or-flatstock", "{part_number}", "pickup-coil"),
        Item(RequestCategory.Pickup, 2, "pickup-die", "die", "{die_number:die_location=Home Location}", "pickup-die"),
        Item(RequestCategory.Pickup, 3, "pickup-component", "component", "{component}", "pickup-component"),
        Item(RequestCategory.Pickup, 4, "pickup-fg", "finished-goods-fg", "{part_number}", "pickup-fg"),
        Item(RequestCategory.Pickup, 5, "pickup-ncm", "non-conforming-ncm", "{part_number}", "pickup-ncm"),
        Item(RequestCategory.Pickup, 6, "pickup-wip", "work-in-process-wip", "{part_number}", "pickup-wip"),
        Item(RequestCategory.Pickup, 7, "pickup-outside-service", "outside-service", "{part_number}", "pickup-outside-service"),
        Item(RequestCategory.Pickup, 8, "pickup-riser-table", "riser-table", "Riser Table", "pickup-riser-table"),
        Item(RequestCategory.Pickup, 9, "pickup-dunnage", "dunnage", "{dunnage_part}", "pickup-dunnage"),
        Item(RequestCategory.Pickup, 10, "pickup-scrap", "scrap-offal-removal", "{scrap_type}", "pickup-scrap"),
        Item(RequestCategory.Pickup, 11, "pickup-hopper", "hopper-pickup", "Hopper", "pickup-hopper"),

        // --- Deliver (8) ---
        Item(RequestCategory.Deliver, 1, "deliver-coil", "coil", "{part_number}", "deliver-coil"),
        Item(RequestCategory.Deliver, 2, "deliver-riser-table", "riser-table", "Riser Table", "deliver-riser-table"),
        Item(RequestCategory.Deliver, 3, "deliver-hopper", "hopper", "Hopper", "deliver-hopper"),
        Item(RequestCategory.Deliver, 4, "deliver-flatstock", "flatstock", "{part_number}", "deliver-flatstock"),
        Item(RequestCategory.Deliver, 5, "deliver-die", "die", "{die_number} / {die_location}", "deliver-die"),
        Item(RequestCategory.Deliver, 6, "deliver-dunnage", "dunnage", "{dunnage_part}", "deliver-dunnage"),
        Item(RequestCategory.Deliver, 7, "deliver-wrong-coil", "wrong-coil", "{part_number}", "deliver-wrong-coil"),
        Item(RequestCategory.Deliver, 8, "deliver-wrong-flatstock", "wrong-flatstock", "{part_number}", "deliver-wrong-flatstock"),

        // --- Assist (3) ---
        Item(RequestCategory.Assist, 1, "assist-coil-turn", "coil", "{part_number}", "assist-coil-turn"),
        Item(RequestCategory.Assist, 2, "assist-table-place", "table-parts", "{part_number}", "assist-table-place"),
        Item(RequestCategory.Assist, 3, "assist-table-remove", "table-parts-remove", "{part_number}", "assist-table-remove"),

        // --- Other (1) ---
        Item(RequestCategory.Other, 1, "other", "other", "{answer}", "other")
    };

    /// <summary>All items belonging to a given umbrella category, ordered by Order.</summary>
    public static IReadOnlyList<RequestItemDefinition> GetByCategory(RequestCategory category) =>
        Items.Where(i => i.Category == category).OrderBy(i => i.Order).ToArray();

    /// <summary>Finds an item by its stable normalized id (e.g. "pickup-coil"), or null.</summary>
    public static RequestItemDefinition? FindById(string? id) =>
        Items.FirstOrDefault(i => string.Equals(i.Id, id, StringComparison.OrdinalIgnoreCase));

    /// <summary>Total catalog count (23).</summary>
    public static int TotalCount => Items.Count;

    /// <summary>
    /// The resource key for an Item's display name. One convention, one place, so the resolver, the
    /// picker tile and the tests cannot drift apart (FR-022).
    /// </summary>
    public static string DisplayNameResourceKeyFor(string itemId) => $"RequestItem.{itemId}.DisplayName";

    /// <summary>
    /// The Item's umbrella phrase for card Line 1: the Category's own word, or an Item-specific phrase
    /// where the Item defines one (contract §2). The two wrong-material phrases are pinned verbatim.
    /// </summary>
    private static string UmbrellaVerbFor(RequestCategory category, string id) => id switch
    {
        "deliver-wrong-coil" => "Wrong Coil Bring:",
        "deliver-wrong-flatstock" => "Wrong Flatstock Bring:",
        _ => category switch
        {
            RequestCategory.Pickup => "Pickup",
            RequestCategory.Deliver => "Deliver",
            RequestCategory.Assist => "Assist",
            _ => "Other"
        }
    };

    private static RequestItemDefinition Item(
        RequestCategory category, int order, string id, string normalizedName,
        string cardLine2Template, string producedValue) =>
        new()
        {
            Category = category,
            Order = order,
            Id = id,
            DisplayNameResourceKey = DisplayNameResourceKeyFor(id),
            NormalizedName = normalizedName,
            UmbrellaVerb = UmbrellaVerbFor(category, id),
            CardLine2Template = cardLine2Template,
            ProducedValue = producedValue
        };
}
