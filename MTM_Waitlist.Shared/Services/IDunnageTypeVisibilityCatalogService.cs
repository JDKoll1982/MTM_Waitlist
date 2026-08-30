using MTM_Waitlist.Module_Shared.Models;

namespace MTM_Waitlist.Module_Shared.Services;

public interface IDunnageTypeVisibilityCatalogService
{
    Task<DunnageTypeVisibilityCatalogResult> GetCatalogAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, bool>> GetVisibilityMapAsync(CancellationToken cancellationToken = default);

    Task<string?> SaveVisibleDunnageTypesAsync(IReadOnlyCollection<string> visibleDunnageTypeIds, CancellationToken cancellationToken = default);
}
