namespace MTM_Waitlist.Module_Shared.Models;

public sealed class WorkCenterCatalogResult
{
    public string ComputerName { get; init; } = string.Empty;

    public IReadOnlyList<string> HotWorkCenters { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> OtherWorkCenters { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> ActiveJobWorkCenters { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Per-work-center enriched detail (building, last updated, and latest active job)
    /// keyed case-insensitively by work-center name.
    /// </summary>
    public IReadOnlyDictionary<string, WorkCenterDetail> WorkCenterDetails { get; init; } =
        new Dictionary<string, WorkCenterDetail>(StringComparer.OrdinalIgnoreCase);
}
