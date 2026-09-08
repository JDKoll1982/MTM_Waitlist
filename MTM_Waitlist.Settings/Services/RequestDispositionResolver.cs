using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// End-to-end FG / WIP / Outside Service resolution for a finished-product part on a work center's
/// active setup job. Runs the live Infor Visual <c>GetDispositionInput.sql</c> queue script plus the
/// MTM WIP Application floor snapshot and feeds the pure <see cref="RequestDispositionClassifier"/>.
///
/// Both executors never throw (they return empty results), and the floor lookup is optional, so a
/// missing connection/script degrades to "classify from whatever snapshot we have" - never a crash.
/// The non-trivial composition lives in <see cref="RequestDispositionMapper"/> (pure + unit tested);
/// this class is intentionally thin.
/// </summary>
public sealed class RequestDispositionResolver
{
    private const string DispositionInputScriptName = "GetDispositionInput";

    private readonly InforVisualSqlQueryService _inforVisualSqlQueryService;
    private readonly WipFloorInventoryService _wipFloorInventoryService;

    public RequestDispositionResolver(
        InforVisualSqlQueryService inforVisualSqlQueryService,
        WipFloorInventoryService wipFloorInventoryService)
    {
        _inforVisualSqlQueryService = inforVisualSqlQueryService;
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
            var rows = await _inforVisualSqlQueryService.ExecuteQueueAsync(
                DispositionInputScriptName,
                new Dictionary<string, object?>
                {
                    ["WorkOrder"] = workOrder ?? string.Empty,
                    ["PartNumber"] = normalizedPart,
                },
                cancellationToken).ConfigureAwait(false);

            inforRow = RequestDispositionMapper.MapInforRow(rows.FirstOrDefault());
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
}
