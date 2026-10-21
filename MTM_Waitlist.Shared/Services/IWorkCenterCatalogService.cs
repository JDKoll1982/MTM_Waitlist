using MTM_Waitlist.Module_Shared.Models;

namespace MTM_Waitlist.Module_Shared.Services;

public interface IWorkCenterCatalogService
{
    string GetCurrentComputerName();

    Task<IReadOnlyList<ComputerOption>> GetAvailableComputersAsync(CancellationToken cancellationToken = default);

    Task<WorkCenterCatalogResult> GetCatalogAsync(string workstationName, CancellationToken cancellationToken = default);

    /// <summary>
    /// One computer's active Local Work Centers — the hot work centers assigned to that machine — by name, in the
    /// catalog's own display order. This is the one read a screen needs when it is narrowing a list to the work
    /// centers this computer serves, and it deliberately does not carry the catalog's other reads.
    /// </summary>
    Task<IReadOnlyList<string>> GetHotWorkCenterNamesAsync(string workstationName, CancellationToken cancellationToken = default);

    Task<string?> SaveHotWorkCentersAsync(string workstationName, IReadOnlyCollection<string> hotWorkCenters, CancellationToken cancellationToken = default);
}
