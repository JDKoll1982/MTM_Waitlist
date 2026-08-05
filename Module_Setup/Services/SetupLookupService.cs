using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Setup.Models;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Module_Setup.Services;

public sealed class SetupLookupService : IInforVisualLookupService, ISubordinatePartService
{
    private readonly SqlHelperServer _sqlHelperServer;

    public SetupLookupService(SqlHelperServer sqlHelperServer)
    {
        _sqlHelperServer = sqlHelperServer;
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

    private static async Task<SetupLookupResult> LookupWorkOrderFromBackendAsync(string normalizedWorkOrder, CancellationToken cancellationToken)
    {
        StartupDebugLog.Info("SetupLookup", "LookupWorkOrderFromBackendAsync loading SQL script LookupWorkOrder.");
        _ = await MTM_Waitlist.Module_Setup.Services.SetupSqlScriptStore.LoadAsync("LookupWorkOrder", cancellationToken).ConfigureAwait(false);
        return await LookupWorkOrderFromMockAsync(normalizedWorkOrder, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IReadOnlyList<SetupSequenceResult>> GetSequencesFromMockAsync(string normalizedWorkOrder, string partNumber, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await Task.FromResult(SetupDataCatalog.GetSequences(normalizedWorkOrder, partNumber)).ConfigureAwait(false);
    }

    private static async Task<IReadOnlyList<SetupSequenceResult>> GetSequencesFromBackendAsync(string normalizedWorkOrder, string partNumber, CancellationToken cancellationToken)
    {
        StartupDebugLog.Info("SetupLookup", "GetSequencesFromBackendAsync loading SQL script GetSequences.");
        _ = await MTM_Waitlist.Module_Setup.Services.SetupSqlScriptStore.LoadAsync("GetSequences", cancellationToken).ConfigureAwait(false);
        return await GetSequencesFromMockAsync(normalizedWorkOrder, partNumber, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IReadOnlyList<SetupSubordinatePart>> GetSubordinatePartsFromMockAsync(string normalizedWorkOrder, string partNumber, string sequenceNumber, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await Task.FromResult(SetupDataCatalog.GetSubordinateParts(normalizedWorkOrder, partNumber, sequenceNumber)).ConfigureAwait(false);
    }

    private static async Task<IReadOnlyList<SetupSubordinatePart>> GetSubordinatePartsFromBackendAsync(string normalizedWorkOrder, string partNumber, string sequenceNumber, CancellationToken cancellationToken)
    {
        StartupDebugLog.Info("SetupLookup", "GetSubordinatePartsFromBackendAsync loading SQL script GetSubordinateParts.");
        _ = await MTM_Waitlist.Module_Setup.Services.SetupSqlScriptStore.LoadAsync("GetSubordinateParts", cancellationToken).ConfigureAwait(false);
        return await GetSubordinatePartsFromMockAsync(normalizedWorkOrder, partNumber, sequenceNumber, cancellationToken).ConfigureAwait(false);
    }
}