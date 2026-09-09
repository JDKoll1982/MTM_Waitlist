using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// Assembles the ordered, availability-filtered Category→Item option set that the New Request
/// picker (Phase 3) and the uniform card renderer (Phase 4) bind to. Composes the canonical
/// <see cref="RequestItemCatalog"/> (Category + Order) with <see cref="RequestItemPickerRules"/>
/// (conditional visibility / Deliver-destination) so the wizard shows exactly the Items the
/// requesting work center's active job supports, in CSV order.
/// </summary>
public interface INewRequestPickerService
{
    /// <summary>Umbrella categories in canonical display order (Pickup → Deliver → Assist → Other).</summary>
    IReadOnlyList<RequestCategory> GetCategoriesInOrder();

    /// <summary>All canonical Items under a category, ordered by Order.</summary>
    IReadOnlyList<RequestItemDefinition> GetItems(RequestCategory category);

    /// <summary>The category's Items the requesting job supports, in Order (JobType-page list).</summary>
    IReadOnlyList<RequestItemDefinition> GetVisibleItems(RequestCategory category, RequestJobPartAvailability availability);

    /// <summary>Every visible Item across all categories, grouped in canonical order (drives the board/other UI).</summary>
    IReadOnlyList<RequestItemDefinition> GetAllVisible(RequestJobPartAvailability availability);
}

/// <inheritdoc cref="INewRequestPickerService"/>
public sealed class NewRequestPickerService : INewRequestPickerService
{
    private readonly IRequestItemCatalogService _catalog;

    public NewRequestPickerService(IRequestItemCatalogService catalog)
    {
        _catalog = catalog;
    }

    public IReadOnlyList<RequestCategory> GetCategoriesInOrder() => _catalog.GetCategoriesInOrder();

    public IReadOnlyList<RequestItemDefinition> GetItems(RequestCategory category) => _catalog.GetByCategory(category);

    public IReadOnlyList<RequestItemDefinition> GetVisibleItems(RequestCategory category, RequestJobPartAvailability availability)
        => _catalog
            .GetByCategory(category)
            .Where(item => RequestItemPickerRules.IsVisible(item, availability))
            .ToArray();

    public IReadOnlyList<RequestItemDefinition> GetAllVisible(RequestJobPartAvailability availability)
    {
        var result = new List<RequestItemDefinition>();
        foreach (var category in _catalog.GetCategoriesInOrder())
        {
            result.AddRange(GetVisibleItems(category, availability));
        }

        return result;
    }
}
