using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;

namespace MTM_Waitlist.Module_Waitlist.Services;

/// <inheritdoc cref="IWaitlistInventoryService"/>
/// <remarks>
/// The read is attempted live against Infor Visual and transparently served from the <c>mtm_mock</c>
/// mirror only when the source is unreachable (FR-002, FR-004). The caller-side filtering — on-hand
/// &gt;= 1 and the ignored-location set — is unchanged and still applies to whichever source answered.
/// </remarks>
public sealed class WaitlistInventoryService : IWaitlistInventoryService
{
    private readonly IIgnoredLocationsService _ignoredLocationsService;
    private readonly IVisualReadFallback<VisualInventoryLocationRequest, VisualInventoryLocationRow> _inventoryLocationsFallback;

    /// <summary>Creates the service.</summary>
    /// <param name="ignoredLocationsService">Shared ignored-locations set.</param>
    /// <param name="inventoryLocationsFallback">Shape 4, inventory locations.</param>
    public WaitlistInventoryService(
        IIgnoredLocationsService ignoredLocationsService,
        IVisualReadFallback<VisualInventoryLocationRequest, VisualInventoryLocationRow> inventoryLocationsFallback)
    {
        ArgumentNullException.ThrowIfNull(ignoredLocationsService);
        ArgumentNullException.ThrowIfNull(inventoryLocationsFallback);

        _ignoredLocationsService = ignoredLocationsService;
        _inventoryLocationsFallback = inventoryLocationsFallback;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<InventoryLocationRow>> GetInventoryLocationRowsAsync(string partNumber, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedPart = (partNumber ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedPart))
        {
            return Array.Empty<InventoryLocationRow>();
        }

        StartupDebugLog.Info("WaitlistInventory", $"GetInventoryLocationRowsAsync started. Part='{normalizedPart}'.");

        IReadOnlyList<InventoryLocationRow> rows;
        try
        {
            var read = await _inventoryLocationsFallback
                .ReadAsync(new VisualInventoryLocationRequest(normalizedPart), cancellationToken)
                .ConfigureAwait(false);

            rows = read.Select(MapToInventoryLocationRow).ToArray();
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("WaitlistInventory", ex, $"GetInventoryLocationRowsAsync failed. Part='{normalizedPart}'.");
            return Array.Empty<InventoryLocationRow>();
        }

        var ignored = await _ignoredLocationsService.GetIgnoredLocationsAsync(cancellationToken).ConfigureAwait(false);
        var filtered = InventoryLocationFiltering.Apply(rows, ignored);
        StartupDebugLog.Info("WaitlistInventory", $"GetInventoryLocationRowsAsync completed. Part='{normalizedPart}', Raw={rows.Count}, Displayed={filtered.Count}.");
        return filtered;
    }

    /// <summary>Maps a shape-4 result row (PartNumber/Location/OnHandQuantity) to a grid row.</summary>
    internal static InventoryLocationRow MapToInventoryLocationRow(VisualInventoryLocationRow row)
    {
        ArgumentNullException.ThrowIfNull(row);

        return new InventoryLocationRow
        {
            PartNumber = row.PartNumber,
            Location = row.Location,
            OnHandQuantity = row.OnHandQuantity,
        };
    }
}
