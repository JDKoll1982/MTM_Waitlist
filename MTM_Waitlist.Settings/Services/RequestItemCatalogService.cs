using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Settings.Services;

/// <inheritdoc cref="IRequestItemCatalogService"/>
public sealed class RequestItemCatalogService : IRequestItemCatalogService
{
    /// <summary>The canonical order for the umbrella categories (CSV col 1 grouping).</summary>
    private static readonly RequestCategory[] CategoriesInOrder =
    {
        RequestCategory.Pickup,
        RequestCategory.Deliver,
        RequestCategory.Assist,
        RequestCategory.Other,
    };

    /// <inheritdoc />
    public IReadOnlyList<RequestItemDefinition> GetAllItems() => RequestItemCatalog.Items;

    /// <inheritdoc />
    public IReadOnlyList<RequestItemDefinition> GetByCategory(RequestCategory category) =>
        RequestItemCatalog.GetByCategory(category);

    /// <inheritdoc />
    public RequestItemDefinition? FindById(string? id) => RequestItemCatalog.FindById(id);

    /// <inheritdoc />
    public IReadOnlyList<RequestCategory> GetCategoriesInOrder() => CategoriesInOrder;
}
