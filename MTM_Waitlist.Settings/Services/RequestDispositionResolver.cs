using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;

namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// End-to-end FG / WIP / Outside Service resolution for a finished-product part on a work center's
/// active setup job. Reads Infor Visual disposition input through the shape-5 fallback - live first,
/// and the <c>mtm_mock</c> mirror only when the source is unreachable - plus the MTM WIP Application
/// floor snapshot, and feeds the pure <see cref="RequestDispositionClassifier"/>.
///
/// The floor lookup is optional and a failed Visual read is caught here, so the resolver degrades to
/// "classify from whatever snapshot we have" - never a crash. The non-trivial composition lives in
/// <see cref="RequestDispositionMapper"/> (pure + unit tested); this class is intentionally thin.
/// </summary>
public sealed class RequestDispositionResolver
{
    private readonly IVisualReadFallback<VisualDispositionInputRequest, VisualDispositionInputRow> _dispositionInputFallback;
    private readonly WipFloorInventoryService _wipFloorInventoryService;

    /// <summary>Creates the resolver.</summary>
    /// <param name="dispositionInputFallback">Shape 5, disposition input.</param>
    /// <param name="wipFloorInventoryService">The floor/WIP half, which stays always live (FR-018).</param>
    public RequestDispositionResolver(
        IVisualReadFallback<VisualDispositionInputRequest, VisualDispositionInputRow> dispositionInputFallback,
        WipFloorInventoryService wipFloorInventoryService)
    {
        ArgumentNullException.ThrowIfNull(dispositionInputFallback);
        ArgumentNullException.ThrowIfNull(wipFloorInventoryService);

        _dispositionInputFallback = dispositionInputFallback;
        _wipFloorInventoryService = wipFloorInventoryService;
    }

    /// <summary>
    /// Resolves the classifier input (and then the disposition) for a part on a work order, using the
    /// Infor snapshot primarily and the floor snapshot as fallback/cross-check.
    /// </summary>
    public async Task<RequestDispositionClassifier.DispositionInput> GetDispositionInputAsync(
        string? workOrder,
        string? partNumber,
        CancellationToken cancellationToken = default)
    {
        var normalizedPart = (partNumber ?? string.Empty).Trim();

        InforDispositionRow? inforRow = null;
        if (!string.IsNullOrWhiteSpace(normalizedPart))
        {
            try
            {
                var rows = await _dispositionInputFallback
                    .ReadAsync(new VisualDispositionInputRequest(workOrder ?? string.Empty, normalizedPart), cancellationToken)
                    .ConfigureAwait(false);

                inforRow = ToInforRow(rows.FirstOrDefault());
            }
            catch (Exception ex)
            {
                // A failed Visual read must not take down disposition resolution - the floor snapshot is
                // still usable. Note the cache is never substituted for a genuine read failure (FR-024).
                StartupDebugLog.Error("RequestDisposition", ex, $"Disposition input read failed for part '{normalizedPart}'.");
            }
        }

        var floorSnapshot = await _wipFloorInventoryService.GetFloorSnapshotAsync(normalizedPart, cancellationToken).ConfigureAwait(false);

        return RequestDispositionMapper.BuildDispositionInput(inforRow, floorSnapshot);
    }

    /// <summary>Resolves and classifies a part into a <see cref="RequestDisposition"/>.</summary>
    public async Task<RequestDisposition> GetDispositionAsync(
        string? workOrder,
        string? partNumber,
        CancellationToken cancellationToken = default)
    {
        var input = await GetDispositionInputAsync(workOrder, partNumber, cancellationToken).ConfigureAwait(false);
        return RequestDispositionClassifier.Classify(input);
    }

    /// <summary>
    /// Bridges the shape's row onto the existing mapper input. The status code is carried through
    /// uninterpreted: <c>RequestDispositionStatusCodes</c> remains the only authority over what a
    /// status means (FR-018).
    /// </summary>
    private static InforDispositionRow? ToInforRow(VisualDispositionInputRow? row)
    {
        if (row is null)
        {
            return null;
        }

        return new InforDispositionRow
        {
            WorkOrderStatus = string.IsNullOrWhiteSpace(row.WorkOrderStatus) ? null : row.WorkOrderStatus,
            OpenWorkOrderQuantity = row.OpenWorkOrderQuantity,
            FinishedGoodsQuantity = row.FinishedGoodsQuantity,
            HasOutsideVendorOperation = row.HasOutsideVendorOperation,
        };
    }
}
