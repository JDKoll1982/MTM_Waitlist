using MTM_Waitlist.Module_Core.Helpers;

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
        Item(RequestCategory.Pickup, 2, "pickup-die", "die", "{die}", "pickup-die", cardLine1Template: "{umbrella} {job_part_number}"),
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
        Item(RequestCategory.Deliver, 5, "deliver-die", "die", "{die}", "deliver-die", cardLine1Template: "{umbrella} {job_part_number}"),
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
    /// where the Item defines one (contract §2). The two wrong-material phrases are pinned verbatim and
    /// resolved through the resource mechanism (FR-022), with the pinned text itself as the fallback so
    /// Line 1 can never become a bare resource key.
    /// </summary>
    private static string UmbrellaVerbFor(RequestCategory category, string id) => id switch
    {
        "deliver-wrong-coil" => PinnedLine1ForKey(id, "Wrong Coil Bring:"),
        "deliver-wrong-flatstock" => PinnedLine1ForKey(id, "Wrong Flatstock Bring:"),
        // The die Items name the thing being moved rather than the bare Category word, and their Line 1 template
        // appends the requesting job's part number so the handler knows which part the die is for (FR-005).
        "pickup-die" => PinnedLine1ForKey(id, "Pickup Die:"),
        "deliver-die" => PinnedLine1ForKey(id, "Deliver Die:"),
        _ => category switch
        {
            RequestCategory.Pickup => "Pickup",
            RequestCategory.Deliver => "Deliver",
            RequestCategory.Assist => "Assist",
            _ => "Other"
        }
    };

    /// <summary>The resource key for an Item's own first-line phrase, following the same one-convention rule the
    /// display name uses (FR-022).</summary>
    public static string Line1ResourceKeyFor(string itemId) => $"RequestItem.{itemId}.Line1";

    /// <summary>
    /// The name a person reads for a Category, resolved through the resource mechanism with a readable fallback
    /// so a missing entry never shows a bare resource key (FR-022). The screens that group by Category — the
    /// picture screen, for one — resolve it here rather than carrying their own copy of the convention.
    /// </summary>
    public static string ResolveCategoryName(RequestCategory category)
    {
        var key = $"RequestCategory.{category}.Name";
        var localized = key.GetLocalized();

        return string.IsNullOrWhiteSpace(localized) || string.Equals(localized, key, StringComparison.Ordinal)
            ? category.ToString()
            : localized;
    }

    /// <summary>
    /// The name a person reads for an Item, resolved from the Item's own resource key (FR-022), falling back to
    /// the catalog's normalized name rather than to a bare key.
    /// </summary>
    public static string ResolveDisplayName(RequestItemDefinition item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var key = string.IsNullOrWhiteSpace(item.DisplayNameResourceKey)
            ? DisplayNameResourceKeyFor(item.Id)
            : item.DisplayNameResourceKey;
        var localized = key.GetLocalized();

        return string.IsNullOrWhiteSpace(localized) || string.Equals(localized, key, StringComparison.Ordinal)
            ? item.NormalizedName
            : localized;
    }

    /// <summary>
    /// Resolves an Item's pinned first line through the resource mechanism, falling back to the pinned text
    /// itself so a missing entry shows the approved wording rather than a resource key.
    /// </summary>
    private static string PinnedLine1ForKey(string itemId, string pinnedText)
    {
        var key = Line1ResourceKeyFor(itemId);
        var localized = key.GetLocalized();
        return string.IsNullOrWhiteSpace(localized) || string.Equals(localized, key, StringComparison.Ordinal)
            ? pinnedText
            : localized;
    }

    private static RequestItemDefinition Item(
        RequestCategory category, int order, string id, string normalizedName,
        string cardLine2Template, string producedValue, string cardLine1Template = "") =>
        new()
        {
            Category = category,
            Order = order,
            Id = id,
            DisplayNameResourceKey = DisplayNameResourceKeyFor(id),
            NormalizedName = normalizedName,
            UmbrellaVerb = UmbrellaVerbFor(category, id),
            CardLine1Template = cardLine1Template,
            CardLine2Template = cardLine2Template,
            ProducedValue = producedValue
        };
}
