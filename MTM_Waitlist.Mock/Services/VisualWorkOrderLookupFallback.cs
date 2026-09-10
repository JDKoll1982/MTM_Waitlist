using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;

namespace MTM_Waitlist.Mock.Services;

/// <summary>Shape 1, <c>work_order_lookup</c>: the parts built by a work order.</summary>
public sealed class VisualWorkOrderLookupFallback
    : VisualReadFallback<VisualWorkOrderLookupRequest, VisualWorkOrderLookupRow>
{
    private static readonly VisualReadShape s_shape = ResolveShape("work_order_lookup");

    /// <summary>Creates the fallback.</summary>
    /// <param name="executor">Runs the live read against Infor Visual.</param>
    /// <param name="mySqlHelperServer">Reads the <c>mtm_mock</c> mirror.</param>
    public VisualWorkOrderLookupFallback(IVisualQueryExecutor executor, IMySqlHelperServer? mySqlHelperServer = null)
        : base(executor, mySqlHelperServer)
    {
    }

    /// <inheritdoc />
    protected override VisualReadShape Shape => s_shape;

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?> BuildLiveParameters(VisualWorkOrderLookupRequest request) =>
        new Dictionary<string, object?>
        {
            ["NormalizedWorkOrder"] = request.NormalizedWorkOrder,
        };

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?> BuildCacheParameters(VisualWorkOrderLookupRequest request) =>
        new Dictionary<string, object?>
        {
            ["p_normalized_work_order"] = request.NormalizedWorkOrder,
        };

    /// <inheritdoc />
    protected override VisualWorkOrderLookupRow MapRow(IReadOnlyDictionary<string, object?> row) => new()
    {
        PartNumber = VisualRowReader.GetString(row, "PartNumber"),
        Description = VisualRowReader.GetString(row, "Description"),
        WorkCenter = VisualRowReader.GetString(row, "WorkCenter"),
    };
}
