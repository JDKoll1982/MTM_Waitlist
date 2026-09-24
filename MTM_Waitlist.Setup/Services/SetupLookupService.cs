using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Setup.Models;
using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;

namespace MTM_Waitlist.Module_Setup.Services;

/// <summary>
/// Reads the three Setup Infor Visual shapes (work order, operation sequences, subordinate parts).
/// </summary>
/// <remarks>
/// Each read goes through the shape's fallback, so it is attempted live and transparently served from
/// the <c>mtm_mock</c> mirror only when Infor Visual is unreachable (FR-002). The previous
/// mock/backend branching — and the sample-data catalogue behind it — is gone: internal behaviour no
/// longer depends on a demo toggle (FR-001, FR-014).
/// <para>
/// <b>Setup does not consult the ignored-locations set.</b> That set exists to keep plant inventory codes
/// (<c>WC</c>, <c>NCM</c>, <c>SHIP</c>) out of the waitlist's <i>inventory location lists</i>, where they are
/// noise. A job's requirement data is a different thing: a coil's location in Infor Visual <i>is</i> the plant or
/// work-centre code it was issued to. Filtering the subordinate parts on it removed whole parts from the job —
/// the coil on WO-074171 vanished from the Setup review, from the saved record and so from the waitlist's coil
/// availability, while Infor Visual carried it all along. Every subordinate part the read returns is kept, and
/// its location is shown as it comes (FR-019).
/// </para>
/// </remarks>
public sealed class SetupLookupService : IInforVisualLookupService, ISubordinatePartService
{
    private const decimal LowStockThreshold = 10m;

    private readonly IVisualReadFallback<VisualWorkOrderLookupRequest, VisualWorkOrderLookupRow> _workOrderLookupFallback;
    private readonly IVisualReadFallback<VisualOperationSequenceRequest, VisualOperationSequenceRow> _operationSequencesFallback;
    private readonly IVisualReadFallback<VisualSubordinatePartRequest, VisualSubordinatePartRow> _subordinatePartsFallback;

    /// <summary>Creates the lookup service.</summary>
    /// <param name="workOrderLookupFallback">Shape 1, work-order parts.</param>
    /// <param name="operationSequencesFallback">Shape 2, operation sequences.</param>
    /// <param name="subordinatePartsFallback">Shape 3, subordinate parts.</param>
    public SetupLookupService(
        IVisualReadFallback<VisualWorkOrderLookupRequest, VisualWorkOrderLookupRow> workOrderLookupFallback,
        IVisualReadFallback<VisualOperationSequenceRequest, VisualOperationSequenceRow> operationSequencesFallback,
        IVisualReadFallback<VisualSubordinatePartRequest, VisualSubordinatePartRow> subordinatePartsFallback)
    {
        ArgumentNullException.ThrowIfNull(workOrderLookupFallback);
        ArgumentNullException.ThrowIfNull(operationSequencesFallback);
        ArgumentNullException.ThrowIfNull(subordinatePartsFallback);

        _workOrderLookupFallback = workOrderLookupFallback;
        _operationSequencesFallback = operationSequencesFallback;
        _subordinatePartsFallback = subordinatePartsFallback;
    }

    /// <inheritdoc />
    public async Task<SetupLookupResult> LookupWorkOrderAsync(string normalizedWorkOrder, CancellationToken cancellationToken = default)
    {
        StartupDebugLog.Info("SetupLookup", $"LookupWorkOrderAsync started. NormalizedWorkOrder='{normalizedWorkOrder}'.");
        try
        {
            var rows = await _workOrderLookupFallback
                .ReadAsync(new VisualWorkOrderLookupRequest(normalizedWorkOrder), cancellationToken)
                .ConfigureAwait(false);

            return new SetupLookupResult
            {
                Success = true,
                Parts = rows
                    .Select(row => new SetupPartResult
                    {
                        PartNumber = row.PartNumber,
                        Description = row.Description,
                        WorkCenter = row.WorkCenter,
                    })
                    .Where(item => !string.IsNullOrWhiteSpace(item.PartNumber))
                    .ToArray(),
            };
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

    /// <inheritdoc />
    public async Task<IReadOnlyList<SetupSequenceResult>> GetSequencesAsync(string normalizedWorkOrder, string partNumber, CancellationToken cancellationToken = default)
    {
        StartupDebugLog.Info("SetupLookup", $"GetSequencesAsync started. WO='{normalizedWorkOrder}', Part='{partNumber}'.");
        try
        {
            var rows = await _operationSequencesFallback
                .ReadAsync(new VisualOperationSequenceRequest(normalizedWorkOrder, partNumber), cancellationToken)
                .ConfigureAwait(false);

            return rows
                .Select(row => new SetupSequenceResult
                {
                    SequenceNumber = row.SequenceNumber,
                    Description = row.Description,
                })
                .Where(item => !string.IsNullOrWhiteSpace(item.SequenceNumber))
                .ToArray();
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("SetupLookup", ex, $"GetSequencesAsync failed. WO='{normalizedWorkOrder}', Part='{partNumber}'.");
            return Array.Empty<SetupSequenceResult>();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SetupSubordinatePart>> GetSubordinatePartsAsync(string normalizedWorkOrder, string partNumber, string sequenceNumber, CancellationToken cancellationToken = default)
    {
        StartupDebugLog.Info("SetupLookup", $"GetSubordinatePartsAsync started. WO='{normalizedWorkOrder}', Part='{partNumber}', Sequence='{sequenceNumber}'.");
        try
        {
            var rows = await _subordinatePartsFallback
                .ReadAsync(new VisualSubordinatePartRequest(normalizedWorkOrder, partNumber, sequenceNumber), cancellationToken)
                .ConfigureAwait(false);

            var parts = rows
                .Select(row =>
                {
                    var onHandQuantity = row.OnHandQuantity;
                    return new SetupSubordinatePart
                    {
                        Category = row.Category,
                        PartNumber = row.PartNumber,
                        Description = row.Description,
                        Location = row.Location,
                        User8 = row.User8,
                        OnHandQuantity = onHandQuantity,
                        IsLowStock = onHandQuantity > 0 && onHandQuantity < LowStockThreshold,
                    };
                })
                .Where(item => !string.IsNullOrWhiteSpace(item.PartNumber))
                .ToArray();

            return parts;
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("SetupLookup", ex, $"GetSubordinatePartsAsync failed. WO='{normalizedWorkOrder}', Part='{partNumber}', Sequence='{sequenceNumber}'.");
            return Array.Empty<SetupSubordinatePart>();
        }
    }
}
