using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;

namespace MTM_Waitlist.Mock.Services;

/// <summary>Shape 7, <c>part_warehouse_quantities</c>: a part's part-wide on-hand total.</summary>
/// <remarks>
/// Added by feature 012 (FR-090) so the card's `Quantity in house`, `Quantity` and `Total Qty In House`
/// cells read the full sum for the part instead of the per-location figure the inventory-locations shape
/// returns, and instead of <c>PART.QTY_ON_HAND</c>, which is the defect FR-090 records.
/// </remarks>
public sealed class VisualPartWarehouseQuantitiesFallback
    : VisualReadFallback<VisualPartWarehouseQuantitiesRequest, VisualPartWarehouseQuantitiesRow>
{
    private static readonly VisualReadShape s_shape = ResolveShape("part_warehouse_quantities");

    /// <summary>Creates the fallback.</summary>
    /// <param name="executor">Runs the live read against Infor Visual.</param>
    /// <param name="mySqlHelperServer">Reads the <c>mtm_mock</c> mirror.</param>
    /// <param name="reachability">Supplies the settled reachability verdict the shared read algorithm honours.</param>
    public VisualPartWarehouseQuantitiesFallback(
        IVisualQueryExecutor executor,
        IMySqlHelperServer? mySqlHelperServer = null,
        IVisualReachabilityDetector? reachability = null)
        : base(executor, mySqlHelperServer, reachability)
    {
    }

    /// <inheritdoc />
    protected override VisualReadShape Shape => s_shape;

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?> BuildLiveParameters(VisualPartWarehouseQuantitiesRequest request) =>
        new Dictionary<string, object?>
        {
            ["PartNumber"] = request.PartNumber,
        };

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?> BuildCacheParameters(VisualPartWarehouseQuantitiesRequest request) =>
        new Dictionary<string, object?>
        {
            ["p_part_number"] = request.PartNumber,
        };

    /// <inheritdoc />
    protected override VisualPartWarehouseQuantitiesRow MapRow(IReadOnlyDictionary<string, object?> row) => new()
    {
        PartNumber = VisualRowReader.GetString(row, "PartNumber"),
        WarehouseQuantity = VisualRowReader.GetDecimal(row, "WarehouseQuantity"),
        FinishedGoodsQuantity = VisualRowReader.GetDecimal(row, "FinishedGoodsQuantity"),
    };
}
