using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;

namespace MTM_Waitlist.Mock.Services;

/// <summary>Shape 10, <c>part_sequences</c>: every sequence the plant attributes to one part.</summary>
/// <remarks>
/// Added so the generic work centre request's sequence step keeps answering while Infor Visual is unreachable.
/// The mirror holds the sequences of open work orders, which is the domain the read offers; each row keeps the
/// relation that named it, because the caller merges the rows a number is named by into one option.
/// </remarks>
public sealed class VisualPartSequencesFallback
    : VisualReadFallback<VisualPartSequencesRequest, VisualPartSequencesRow>
{
    private static readonly VisualReadShape s_shape = ResolveShape("part_sequences");

    /// <summary>Creates the fallback.</summary>
    /// <param name="executor">Runs the live read against Infor Visual.</param>
    /// <param name="mySqlHelperServer">Reads the <c>mtm_mock</c> mirror.</param>
    /// <param name="reachability">Supplies the settled reachability verdict the shared read algorithm honours.</param>
    public VisualPartSequencesFallback(
        IVisualQueryExecutor executor,
        IMySqlHelperServer? mySqlHelperServer = null,
        IVisualReachabilityDetector? reachability = null)
        : base(executor, mySqlHelperServer, reachability)
    {
    }

    /// <inheritdoc />
    protected override VisualReadShape Shape => s_shape;

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?> BuildLiveParameters(VisualPartSequencesRequest request) =>
        new Dictionary<string, object?>
        {
            ["PartNumber"] = request.PartNumber ?? string.Empty,
        };

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?> BuildCacheParameters(VisualPartSequencesRequest request) =>
        new Dictionary<string, object?>
        {
            ["p_part_number"] = request.PartNumber ?? string.Empty,
        };

    /// <inheritdoc />
    protected override VisualPartSequencesRow MapRow(IReadOnlyDictionary<string, object?> row) => new()
    {
        Sequence = VisualRowReader.GetString(row, "Sequence"),
        Description = VisualRowReader.GetString(row, "Description"),
        OperationStatus = VisualRowReader.GetString(row, "OperationStatus"),
        IsLastOperation = VisualRowReader.GetBoolean(row, "IsLastOperation") ? 1 : 0,
        NextResource = VisualRowReader.GetString(row, "NextResource"),
        CancelledOnly = VisualRowReader.GetBoolean(row, "CancelledOnly") ? 1 : 0,
        SequenceSource = VisualRowReader.GetString(row, "SequenceSource"),
    };
}
