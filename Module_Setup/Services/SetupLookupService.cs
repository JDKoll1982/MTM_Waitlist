using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Setup.Models;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Module_Setup.Services;

public sealed class SetupLookupService : IInforVisualLookupService, ISubordinatePartService
{
    private readonly SqlHelperServer _sqlHelperServer;
    private readonly InforVisualSqlQueryService _inforVisualSqlQueryService;

    public SetupLookupService(SqlHelperServer sqlHelperServer, InforVisualSqlQueryService inforVisualSqlQueryService)
    {
        _sqlHelperServer = sqlHelperServer;
        _inforVisualSqlQueryService = inforVisualSqlQueryService;
    }

    public async Task<SetupLookupResult> LookupWorkOrderAsync(string normalizedWorkOrder, CancellationToken cancellationToken = default)
    {
        StartupDebugLog.Info("SetupLookup", $"LookupWorkOrderAsync started. NormalizedWorkOrder='{normalizedWorkOrder}'.");
        try
        {
            return await _sqlHelperServer.ExecuteReadOnlyQueueAsync(
                "Setup.InforVisualLookup",
                normalizedWorkOrder,
                () => LookupWorkOrderFromMockAsync(normalizedWorkOrder, cancellationToken),
                () => LookupWorkOrderFromBackendAsync(normalizedWorkOrder, cancellationToken)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("SetupLookup", ex, $"LookupWorkOrderAsync failed for '{normalizedWorkOrder}'.");
            return new SetupLookupResult
            {
                Success = false,
                Message = "Setup_Error.LookupUnavailable".GetLocalized(),
                Parts = Array.Empty<SetupPartResult>()
            };
        }
    }

    public async Task<IReadOnlyList<SetupSequenceResult>> GetSequencesAsync(string normalizedWorkOrder, string partNumber, CancellationToken cancellationToken = default)
    {
        StartupDebugLog.Info("SetupLookup", $"GetSequencesAsync started. WO='{normalizedWorkOrder}', Part='{partNumber}'.");
        try
        {
            return await _sqlHelperServer.ExecuteReadOnlyQueueAsync(
                "Setup.InforVisualSequences",
                normalizedWorkOrder,
                () => GetSequencesFromMockAsync(normalizedWorkOrder, partNumber, cancellationToken),
                () => GetSequencesFromBackendAsync(normalizedWorkOrder, partNumber, cancellationToken)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("SetupLookup", ex, $"GetSequencesAsync failed. WO='{normalizedWorkOrder}', Part='{partNumber}'.");
            return Array.Empty<SetupSequenceResult>();
        }
    }

    public async Task<IReadOnlyList<SetupSubordinatePart>> GetSubordinatePartsAsync(string normalizedWorkOrder, string partNumber, string sequenceNumber, CancellationToken cancellationToken = default)
    {
        StartupDebugLog.Info("SetupLookup", $"GetSubordinatePartsAsync started. WO='{normalizedWorkOrder}', Part='{partNumber}', Sequence='{sequenceNumber}'.");
        try
        {
            return await _sqlHelperServer.ExecuteReadOnlyQueueAsync(
                "Setup.InforVisualSubordinateParts",
                normalizedWorkOrder,
                () => GetSubordinatePartsFromMockAsync(normalizedWorkOrder, partNumber, sequenceNumber, cancellationToken),
                () => GetSubordinatePartsFromBackendAsync(normalizedWorkOrder, partNumber, sequenceNumber, cancellationToken)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("SetupLookup", ex, $"GetSubordinatePartsAsync failed. WO='{normalizedWorkOrder}', Part='{partNumber}', Sequence='{sequenceNumber}'.");
            return Array.Empty<SetupSubordinatePart>();
        }
    }

    private static async Task<SetupLookupResult> LookupWorkOrderFromMockAsync(string normalizedWorkOrder, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await Task.FromResult(new SetupLookupResult
        {
            Parts = SetupDataCatalog.GetParts(normalizedWorkOrder),
            Success = true
        }).ConfigureAwait(false);
    }

    private async Task<SetupLookupResult> LookupWorkOrderFromBackendAsync(string normalizedWorkOrder, CancellationToken cancellationToken)
    {
        StartupDebugLog.Info("SetupLookup", "LookupWorkOrderFromBackendAsync executing SQL script LookupWorkOrder.");
        var rows = await _inforVisualSqlQueryService.ExecuteQueueAsync(
            "LookupWorkOrder",
            new Dictionary<string, object?>
            {
                ["NormalizedWorkOrder"] = normalizedWorkOrder,
            },
            cancellationToken).ConfigureAwait(false);

        var parts = rows
            .Select(row => new SetupPartResult
            {
                PartNumber = GetString(row, "PartNumber"),
                Description = GetString(row, "Description"),
                WorkCenter = GetString(row, "WorkCenter"),
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.PartNumber))
            .ToArray();

        return new SetupLookupResult
        {
            Success = true,
            Parts = parts,
        };
    }

    private static async Task<IReadOnlyList<SetupSequenceResult>> GetSequencesFromMockAsync(string normalizedWorkOrder, string partNumber, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await Task.FromResult(SetupDataCatalog.GetSequences(normalizedWorkOrder, partNumber)).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<SetupSequenceResult>> GetSequencesFromBackendAsync(string normalizedWorkOrder, string partNumber, CancellationToken cancellationToken)
    {
        StartupDebugLog.Info("SetupLookup", "GetSequencesFromBackendAsync executing SQL script GetSequences.");
        var rows = await _inforVisualSqlQueryService.ExecuteQueueAsync(
            "GetSequences",
            new Dictionary<string, object?>
            {
                ["NormalizedWorkOrder"] = normalizedWorkOrder,
                ["PartNumber"] = partNumber,
            },
            cancellationToken).ConfigureAwait(false);

        return rows
            .Select(row => new SetupSequenceResult
            {
                SequenceNumber = GetString(row, "SequenceNumber"),
                Description = GetString(row, "Description"),
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.SequenceNumber))
            .ToArray();
    }

    private static async Task<IReadOnlyList<SetupSubordinatePart>> GetSubordinatePartsFromMockAsync(string normalizedWorkOrder, string partNumber, string sequenceNumber, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await Task.FromResult(SetupDataCatalog.GetSubordinateParts(normalizedWorkOrder, partNumber, sequenceNumber)).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<SetupSubordinatePart>> GetSubordinatePartsFromBackendAsync(string normalizedWorkOrder, string partNumber, string sequenceNumber, CancellationToken cancellationToken)
    {
        StartupDebugLog.Info("SetupLookup", "GetSubordinatePartsFromBackendAsync executing SQL script GetSubordinateParts.");
        var rows = await _inforVisualSqlQueryService.ExecuteQueueAsync(
            "GetSubordinateParts",
            new Dictionary<string, object?>
            {
                ["NormalizedWorkOrder"] = normalizedWorkOrder,
                ["PartNumber"] = partNumber,
                ["SequenceNumber"] = sequenceNumber,
            },
            cancellationToken).ConfigureAwait(false);

        return rows
            .Select(row =>
            {
                var onHandQuantity = GetDecimal(row, "OnHandQuantity");
                return new SetupSubordinatePart
                {
                    Category = GetString(row, "Category"),
                    PartNumber = GetString(row, "PartNumber"),
                    Description = GetString(row, "Description"),
                    Location = GetString(row, "Location"),
                    OnHandQuantity = onHandQuantity,
                    IsLowStock = onHandQuantity > 0 && onHandQuantity < 10,
                };
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.PartNumber))
            .ToArray();
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