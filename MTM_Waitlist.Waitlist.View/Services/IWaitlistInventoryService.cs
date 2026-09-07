using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Waitlist.Services;

/// <summary>
/// Returns the per-location on-hand inventory rows for the Waitlist detail location grid,
/// already filtered to on-hand &gt;= 1 and omitting ignored locations (file 05). Routes to
/// sample data when <c>Feature.InforVisualMockData</c> is ON, else the Infor Visual queue
/// script GetInventoryLocations.
/// </summary>
public interface IWaitlistInventoryService
{
    Task<IReadOnlyList<InventoryLocationRow>> GetInventoryLocationRowsAsync(string partNumber, CancellationToken cancellationToken = default);
}
