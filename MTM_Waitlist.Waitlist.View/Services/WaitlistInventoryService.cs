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
    private readonly InforVisualSqlQueryService _inforVisualSqlQueryService;

    public WaitlistInventoryService(
        SqlHelperServer sqlHelperServer,
        IIgnoredLocationsService ignoredLocationsService,
        InforVisualSqlQueryService inforVisualSqlQueryService)
    {
        _sqlHelperServer = sqlHelperServer;
        _ignoredLocationsService = ignoredLocationsService;
        _inforVisualSqlQueryService = inforVisualSqlQueryService;
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
        // Executes the checked-in Infor Visual queue script GetInventoryLocations.sql via the shared
        // Core executor when Feature.InforVisualMockData is OFF. The executor never throws: a missing
        // connection or script yields an empty set (zero rows handled), so callers fall back cleanly.
        StartupDebugLog.Info("WaitlistInventory", $"GetInventoryLocationsFromBackendAsync executing SQL script GetInventoryLocations for part '{partNumber}'.");
        var rows = await _inforVisualSqlQueryService.ExecuteQueueAsync(
            "GetInventoryLocations",
            new Dictionary<string, object?>
            {
                ["PartNumber"] = partNumber,
            },
            cancellationToken).ConfigureAwait(false);

        return rows
            .Where(row => row is not null)
            .Select(MapToInventoryLocationRow)
            .ToArray();
    }

    /// <summary>Maps a raw Infor Visual result row (PartNumber/Location/OnHandQuantity) to a grid row.</summary>
    internal static InventoryLocationRow MapToInventoryLocationRow(IReadOnlyDictionary<string, object?> row)
    {
        return new InventoryLocationRow
        {
            PartNumber = GetString(row, "PartNumber"),
            Location = GetString(row, "Location"),
            OnHandQuantity = GetDecimal(row, "OnHandQuantity"),
        };
    }

    private static string GetString(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null)
        {
            return string.Empty;
        }

        return Convert.ToString(value)?.Trim() ?? string.Empty;
    }

    private static decimal GetDecimal(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null)
        {
            return 0m;
        }

        return value switch
        {
            decimal decimalValue => decimalValue,
            double doubleValue => Convert.ToDecimal(doubleValue),
            float floatValue => Convert.ToDecimal(floatValue),
            int intValue => intValue,
            long longValue => longValue,
            _ => decimal.TryParse(Convert.ToString(value), out var parsed) ? parsed : 0m,
        };
    }
}
