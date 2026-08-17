namespace MTM_Waitlist.Module_Shared.Models;

public sealed class WorkCenterCatalogResult
{
    public string WorkstationName { get; init; } = string.Empty;

    public IReadOnlyList<string> HotWorkCenters { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> OtherWorkCenters { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> ActiveJobWorkCenters { get; init; } = Array.Empty<string>();
}
