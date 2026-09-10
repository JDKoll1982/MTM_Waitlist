using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;

namespace MTM_Waitlist.Mock.Services;

/// <summary>Shape 4, <c>inventory_locations</c>: the inventory locations that hold a part.</summary>
/// <remarks>
/// The caller keeps its own on-hand &gt;= 1 filter and ignored-location filtering; those rules are
/// unaffected by which source answered (contract §3).
/// </remarks>
public sealed class VisualInventoryLocationsFallback
    : VisualReadFallback<VisualInventoryLocationRequest, VisualInventoryLocationRow>
{
    private static readonly VisualReadShape s_shape = ResolveShape("inventory_locations");

    /// <summary>Creates the fallback.</summary>
    /// <param name="executor">Runs the live read against Infor Visual.</param>
    /// <param name="mySqlHelperServer">Reads the <c>mtm_mock</c> mirror.</param>
    public VisualInventoryLocationsFallback(IVisualQueryExecutor executor, IMySqlHelperServer? mySqlHelperServer = null)
        : base(executor, mySqlHelperServer)
    {
    }

    /// <inheritdoc />
    protected override VisualReadShape Shape => s_shape;

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?> BuildLiveParameters(VisualInventoryLocationRequest request) =>
        new Dictionary<string, object?>
        {
            ["PartNumber"] = request.PartNumber,
        };

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?> BuildCacheParameters(VisualInventoryLocationRequest request) =>
        new Dictionary<string, object?>
        {
            ["p_part_number"] = request.PartNumber,
        };

    /// <inheritdoc />
    protected override VisualInventoryLocationRow MapRow(IReadOnlyDictionary<string, object?> row) => new()
    {
        PartNumber = VisualRowReader.GetString(row, "PartNumber"),
        Location = VisualRowReader.GetString(row, "Location"),
        OnHandQuantity = VisualRowReader.GetDecimal(row, "OnHandQuantity"),
    };
}
