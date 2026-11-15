using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;

namespace MTM_Waitlist.Mock.Services;

/// <summary>Shape 12, <c>die_search</c>: the dies a die number or a part number matches.</summary>
/// <remarks>
/// Added so the generic die request keeps answering while Infor Visual is unreachable. The mirror holds one row
/// per (term, die) association — the die itself, the dies <c>PART_ALTERNATE</c> records against a part, and the
/// dies a part's work orders required — so a part number still finds every die the plant ties to it.
/// </remarks>
public sealed class VisualDieSearchFallback
    : VisualReadFallback<VisualDieSearchRequest, VisualDieSearchRow>
{
    private static readonly VisualReadShape s_shape = ResolveShape("die_search");

    /// <summary>Creates the fallback.</summary>
    /// <param name="executor">Runs the live read against Infor Visual.</param>
    /// <param name="mySqlHelperServer">Reads the <c>mtm_mock</c> mirror.</param>
    /// <param name="reachability">Supplies the settled reachability verdict the shared read algorithm honours.</param>
    public VisualDieSearchFallback(
        IVisualQueryExecutor executor,
        IMySqlHelperServer? mySqlHelperServer = null,
        IVisualReachabilityDetector? reachability = null)
        : base(executor, mySqlHelperServer, reachability)
    {
    }

    /// <inheritdoc />
    protected override VisualReadShape Shape => s_shape;

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?> BuildLiveParameters(VisualDieSearchRequest request) =>
        new Dictionary<string, object?>
        {
            ["Term"] = request.Term ?? string.Empty,
        };

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?> BuildCacheParameters(VisualDieSearchRequest request) =>
        new Dictionary<string, object?>
        {
            ["p_term"] = request.Term ?? string.Empty,
        };

    /// <inheritdoc />
    protected override VisualDieSearchRow MapRow(IReadOnlyDictionary<string, object?> row) => new()
    {
        DieNumber = VisualRowReader.GetString(row, "DieNumber"),
        Description = VisualRowReader.GetString(row, "Description"),
        Association = VisualRowReader.GetString(row, "Association"),
        Location = VisualRowReader.GetString(row, "Location"),
    };
}
