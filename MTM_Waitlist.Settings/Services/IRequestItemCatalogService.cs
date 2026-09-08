using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// Loader over the canonical 18-row Category/Item catalog (mirror of
/// <c>WeekendProject/Documents/Request-Config-Template.csv</c>). A single injection point for the
/// New Request picker (Phase 3) and the uniform card renderer (Phase 4) so both consume the same
/// Category → Item definition list instead of duplicating ad-hoc lookups.
/// </summary>
public interface IRequestItemCatalogService
{
    /// <summary>All 18 canonical items, ordered by Category then Order.</summary>
    IReadOnlyList<RequestItemDefinition> GetAllItems();

    /// <summary>Items under one umbrella category, ordered by Order.</summary>
    IReadOnlyList<RequestItemDefinition> GetByCategory(RequestCategory category);

    /// <summary>Looks up an item by its stable normalized id (e.g. "pickup-coil"), or null.</summary>
    RequestItemDefinition? FindById(string? id);

    /// <summary>The four umbrella categories in canonical display order (Pickup → Other).</summary>
    IReadOnlyList<RequestCategory> GetCategoriesInOrder();
}
