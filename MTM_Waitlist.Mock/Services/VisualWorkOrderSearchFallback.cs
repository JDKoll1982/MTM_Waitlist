using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;

namespace MTM_Waitlist.Mock.Services;

/// <summary>Shape 8, <c>work_order_search</c>: the open work orders a setup run can start from.</summary>
public sealed class VisualWorkOrderSearchFallback
    : VisualReadFallback<VisualWorkOrderSearchRequest, VisualWorkOrderSearchRow>
{
    private static readonly VisualReadShape s_shape = ResolveShape("work_order_search");

    /// <summary>Creates the fallback.</summary>
    /// <param name="executor">Runs the live read against Infor Visual.</param>
    /// <param name="mySqlHelperServer">Reads the <c>mtm_mock</c> mirror.</param>
    /// <param name="reachability">Supplies the settled reachability verdict the shared read algorithm honours.</param>
    public VisualWorkOrderSearchFallback(
        IVisualQueryExecutor executor,
        IMySqlHelperServer? mySqlHelperServer = null,
        IVisualReachabilityDetector? reachability = null)
        : base(executor, mySqlHelperServer, reachability)
    {
    }

    /// <inheritdoc />
    protected override VisualReadShape Shape => s_shape;

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?> BuildLiveParameters(VisualWorkOrderSearchRequest request) =>
        new Dictionary<string, object?>
        {
            ["WorkOrderText"] = request.WorkOrderText ?? string.Empty,
            ["PartFilter"] = request.PartFilter ?? string.Empty,
            ["CustomerFilter"] = request.CustomerFilter ?? string.Empty,
        };

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?> BuildCacheParameters(VisualWorkOrderSearchRequest request) =>
        new Dictionary<string, object?>
        {
            ["p_work_order_text"] = request.WorkOrderText ?? string.Empty,
            ["p_part_filter"] = request.PartFilter ?? string.Empty,
            ["p_customer_filter"] = request.CustomerFilter ?? string.Empty,
        };

    /// <inheritdoc />
    protected override VisualWorkOrderSearchRow MapRow(IReadOnlyDictionary<string, object?> row) => new()
    {
        WorkOrder = VisualRowReader.GetString(row, "WorkOrder"),
        PartNumber = VisualRowReader.GetString(row, "PartNumber"),
        PartDescription = VisualRowReader.GetString(row, "PartDescription"),
        CustomerName = VisualRowReader.GetString(row, "CustomerName"),
        WorkCenter = VisualRowReader.GetString(row, "WorkCenter"),
        Status = VisualRowReader.GetString(row, "Status"),
        WantDate = VisualRowReader.GetNullableDateTimeOffset(row, "WantDate"),
    };
}
