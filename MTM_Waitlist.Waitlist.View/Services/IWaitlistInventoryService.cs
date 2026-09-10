using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Waitlist.Services;

/// <summary>
/// Returns the per-location on-hand inventory rows for the Waitlist detail location grid,
/// already filtered to on-hand &gt;= 1 and omitting ignored locations (file 05). The read is
/// attempted live against Infor Visual and transparently served from the mirror only while
/// that source is unreachable (FR-002, FR-004).
/// </summary>
public interface IWaitlistInventoryService
{
    Task<IReadOnlyList<InventoryLocationRow>> GetInventoryLocationRowsAsync(string partNumber, CancellationToken cancellationToken = default);
}
