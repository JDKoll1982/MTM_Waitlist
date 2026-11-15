using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;

namespace MTM_Waitlist.Mock.Services;

/// <summary>Shape 9, <c>part_matches</c>: the parts a typed term matches, with the description of each.</summary>
/// <remarks>
/// Added so the generic work centre request's part page keeps answering while Infor Visual is unreachable. The
/// term is stored on each mirror row as the part's own number, and the read procedure filters on the output
/// columns, exactly as <c>work_order_search</c> stores the three things an operator searches by.
/// </remarks>
public sealed class VisualPartMatchesFallback
    : VisualReadFallback<VisualPartMatchesRequest, VisualPartMatchesRow>
{
    private static readonly VisualReadShape s_shape = ResolveShape("part_matches");

    /// <summary>Creates the fallback.</summary>
    /// <param name="executor">Runs the live read against Infor Visual.</param>
    /// <param name="mySqlHelperServer">Reads the <c>mtm_mock</c> mirror.</param>
    /// <param name="reachability">Supplies the settled reachability verdict the shared read algorithm honours.</param>
    public VisualPartMatchesFallback(
        IVisualQueryExecutor executor,
        IMySqlHelperServer? mySqlHelperServer = null,
        IVisualReachabilityDetector? reachability = null)
        : base(executor, mySqlHelperServer, reachability)
    {
    }

    /// <inheritdoc />
    protected override VisualReadShape Shape => s_shape;

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?> BuildLiveParameters(VisualPartMatchesRequest request) =>
        new Dictionary<string, object?>
        {
            ["Term"] = request.Term ?? string.Empty,
        };

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?> BuildCacheParameters(VisualPartMatchesRequest request) =>
        new Dictionary<string, object?>
        {
            ["p_term"] = request.Term ?? string.Empty,
        };

    /// <inheritdoc />
    protected override VisualPartMatchesRow MapRow(IReadOnlyDictionary<string, object?> row) => new()
    {
        PartNumber = VisualRowReader.GetString(row, "PartNumber"),
        Description = VisualRowReader.GetString(row, "Description"),
    };
}
