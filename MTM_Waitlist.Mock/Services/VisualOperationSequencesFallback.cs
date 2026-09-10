using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;

namespace MTM_Waitlist.Mock.Services;

/// <summary>Shape 2, <c>operation_sequences</c>: the operation sequences of a part on a work order.</summary>
public sealed class VisualOperationSequencesFallback
    : VisualReadFallback<VisualOperationSequenceRequest, VisualOperationSequenceRow>
{
    private static readonly VisualReadShape s_shape = ResolveShape("operation_sequences");

    /// <summary>Creates the fallback.</summary>
    /// <param name="executor">Runs the live read against Infor Visual.</param>
    /// <param name="mySqlHelperServer">Reads the <c>mtm_mock</c> mirror.</param>
    public VisualOperationSequencesFallback(IVisualQueryExecutor executor, IMySqlHelperServer? mySqlHelperServer = null)
        : base(executor, mySqlHelperServer)
    {
    }

    /// <inheritdoc />
    protected override VisualReadShape Shape => s_shape;

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?> BuildLiveParameters(VisualOperationSequenceRequest request) =>
        new Dictionary<string, object?>
        {
            ["NormalizedWorkOrder"] = request.NormalizedWorkOrder,
            ["PartNumber"] = request.PartNumber,
        };

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?> BuildCacheParameters(VisualOperationSequenceRequest request) =>
        new Dictionary<string, object?>
        {
            ["p_normalized_work_order"] = request.NormalizedWorkOrder,
            ["p_part_number"] = request.PartNumber,
        };

    /// <inheritdoc />
    protected override VisualOperationSequenceRow MapRow(IReadOnlyDictionary<string, object?> row) => new()
    {
        SequenceNumber = VisualRowReader.GetString(row, "SequenceNumber"),
        Description = VisualRowReader.GetString(row, "Description"),
    };
}
