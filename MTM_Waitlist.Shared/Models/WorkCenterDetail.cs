namespace MTM_Waitlist.Module_Shared.Models;

/// <summary>
/// Enriched per-work-center detail used to render the work center selection cards.
/// Populated by <c>WorkCenterCatalogService.GetCatalogAsync</c> by merging
/// <c>setup_workstations_catalog</c> metadata (building, updated_utc) with the latest
/// active setup job for the work center.
/// </summary>
public sealed record WorkCenterDetail
{
    public string Building { get; init; } = string.Empty;

    public DateTime? LastUpdatedUtc { get; init; }

    public bool HasActiveJob { get; init; }

    public string CurrentWorkOrder { get; init; } = string.Empty;

    public string CurrentPartNumber { get; init; } = string.Empty;

    public string CurrentSequenceNumber { get; init; } = string.Empty;
}
