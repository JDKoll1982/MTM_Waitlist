using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;

namespace MTM_Waitlist.Mock.Services;

/// <summary>Shape 5, <c>disposition_input</c>: the work-order status inputs used to resolve disposition.</summary>
/// <remarks>
/// <see cref="VisualDispositionInputRow.WorkOrderStatus"/> is carried through uninterpreted. The caller's
/// status vocabulary remains the only authority over what a status code means (FR-018).
/// </remarks>
public sealed class VisualDispositionInputFallback
    : VisualReadFallback<VisualDispositionInputRequest, VisualDispositionInputRow>
{
    private static readonly VisualReadShape s_shape = ResolveShape("disposition_input");

    /// <summary>Creates the fallback.</summary>
    /// <param name="executor">Runs the live read against Infor Visual.</param>
    /// <param name="mySqlHelperServer">Reads the <c>mtm_mock</c> mirror.</param>
    public VisualDispositionInputFallback(IVisualQueryExecutor executor, IMySqlHelperServer? mySqlHelperServer = null)
        : base(executor, mySqlHelperServer)
    {
    }

    /// <inheritdoc />
    protected override VisualReadShape Shape => s_shape;

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?> BuildLiveParameters(VisualDispositionInputRequest request) =>
        new Dictionary<string, object?>
        {
            ["WorkOrder"] = request.WorkOrder,
            ["PartNumber"] = request.PartNumber,
        };

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?> BuildCacheParameters(VisualDispositionInputRequest request) =>
        new Dictionary<string, object?>
        {
            ["p_work_order"] = request.WorkOrder,
            ["p_part_number"] = request.PartNumber,
        };

    /// <inheritdoc />
    protected override VisualDispositionInputRow MapRow(IReadOnlyDictionary<string, object?> row) => new()
    {
        WorkOrderStatus = VisualRowReader.GetString(row, "WorkOrderStatus"),
        OpenWorkOrderQuantity = VisualRowReader.GetDecimal(row, "OpenWorkOrderQuantity"),
        FinishedGoodsQuantity = VisualRowReader.GetDecimal(row, "FinishedGoodsQuantity"),
        HasOutsideVendorOperation = VisualRowReader.GetBoolean(row, "HasOutsideVendorOperation"),
    };
}
