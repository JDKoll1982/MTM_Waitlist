using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Waitlist.Services;

/// <inheritdoc cref="IWaitlistInventoryService"/>
public sealed class WaitlistInventoryService : IWaitlistInventoryService
{
    private const string QueueName = "Waitlist.InforVisualInventoryLocations";

    private readonly SqlHelperServer _sqlHelperServer;
    private readonly IIgnoredLocationsService _ignoredLocationsService;

    public WaitlistInventoryService(SqlHelperServer sqlHelperServer, IIgnoredLocationsService ignoredLocationsService)
    {
        _sqlHelperServer = sqlHelperServer;
        _ignoredLocationsService = ignoredLocationsService;
    }

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
            rows = await _sqlHelperServer.ExecuteReadOnlyQueueAsync(
                QueueName,
                normalizedPart,
                () => GetInventoryLocationsFromMockAsync(normalizedPart, cancellationToken),
                () => GetInventoryLocationsFromBackendAsync(normalizedPart, cancellationToken)).ConfigureAwait(false);
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

    private static async Task<IReadOnlyList<InventoryLocationRow>> GetInventoryLocationsFromMockAsync(string partNumber, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await Task.FromResult(SampleInventoryLocationCatalog.GetRows(partNumber)).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<InventoryLocationRow>> GetInventoryLocationsFromBackendAsync(string partNumber, CancellationToken cancellationToken)
    {
        // Executes the Infor Visual queue script GetInventoryLocations.sql. This requires an
        // Infor Visual SQL executor accessible to the Waitlist module (mirrors Setup's
        // InforVisualSqlQueryService); until that shared executor is wired, no live rows are
        // returned and callers fall back to an empty grid.
        StartupDebugLog.Info("WaitlistInventory", $"GetInventoryLocationsFromBackendAsync not executed for part '{partNumber}' (Infor Visual executor pending).");
        await Task.CompletedTask.ConfigureAwait(false);
        return Array.Empty<InventoryLocationRow>();
    }
}
