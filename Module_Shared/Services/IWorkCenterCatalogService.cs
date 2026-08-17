using MTM_Waitlist.Module_Shared.Models;

namespace MTM_Waitlist.Module_Shared.Services;

public interface IWorkCenterCatalogService
{
    string GetCurrentWorkstationName();

    Task<IReadOnlyList<string>> GetAvailableWorkstationsAsync(CancellationToken cancellationToken = default);

    Task<WorkCenterCatalogResult> GetCatalogAsync(string workstationName, CancellationToken cancellationToken = default);

    Task<string?> SaveHotWorkCentersAsync(string workstationName, IReadOnlyCollection<string> hotWorkCenters, CancellationToken cancellationToken = default);
}
