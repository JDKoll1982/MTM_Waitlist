using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;

namespace MTM_Waitlist.Mock.Services;

/// <summary>Shape 11, <c>part_sequence_state</c>: one part's operation state at one sequence.</summary>
/// <remarks>
/// Added so the generic work centre request's quantity step keeps deciding who owns the quantity while Infor
/// Visual is unreachable. The mirror holds at most one row per (part, sequence); no row means the part has no
/// operation there, which the caller reads as "no state recorded" and answers from the floor first.
/// </remarks>
public sealed class VisualPartSequenceStateFallback
    : VisualReadFallback<VisualPartSequenceStateRequest, VisualPartSequenceStateRow>
{
    private static readonly VisualReadShape s_shape = ResolveShape("part_sequence_state");

    /// <summary>Creates the fallback.</summary>
    /// <param name="executor">Runs the live read against Infor Visual.</param>
    /// <param name="mySqlHelperServer">Reads the <c>mtm_mock</c> mirror.</param>
    /// <param name="reachability">Supplies the settled reachability verdict the shared read algorithm honours.</param>
    public VisualPartSequenceStateFallback(
        IVisualQueryExecutor executor,
        IMySqlHelperServer? mySqlHelperServer = null,
        IVisualReachabilityDetector? reachability = null)
        : base(executor, mySqlHelperServer, reachability)
    {
    }

    /// <inheritdoc />
    protected override VisualReadShape Shape => s_shape;

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?> BuildLiveParameters(VisualPartSequenceStateRequest request) =>
        new Dictionary<string, object?>
        {
            ["PartNumber"] = request.PartNumber ?? string.Empty,
            ["Sequence"] = request.Sequence ?? string.Empty,
        };

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?> BuildCacheParameters(VisualPartSequenceStateRequest request) =>
        new Dictionary<string, object?>
        {
            ["p_part_number"] = request.PartNumber ?? string.Empty,
            ["p_sequence"] = request.Sequence ?? string.Empty,
        };

    /// <inheritdoc />
    protected override VisualPartSequenceStateRow MapRow(IReadOnlyDictionary<string, object?> row) => new()
    {
        OperationStatus = VisualRowReader.GetString(row, "OperationStatus"),
        IsLastOperation = VisualRowReader.GetBoolean(row, "IsLastOperation") ? 1 : 0,
        NextResource = VisualRowReader.GetString(row, "NextResource"),
    };
}
