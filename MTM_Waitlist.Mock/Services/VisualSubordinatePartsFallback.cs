using System.Globalization;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;

namespace MTM_Waitlist.Mock.Services;

/// <summary>Shape 3, <c>subordinate_parts</c>: the subordinate parts of one operation.</summary>
/// <remarks>
/// This shape's cache procedure takes the operation's part as <c>p_part_number</c> while the live script
/// takes it as <c>ParentPartNumber</c>, and its sequence number is text live but an integer in the
/// mirror, so the two parameter sets are genuinely different.
/// </remarks>
public sealed class VisualSubordinatePartsFallback
    : VisualReadFallback<VisualSubordinatePartRequest, VisualSubordinatePartRow>
{
    private static readonly VisualReadShape s_shape = ResolveShape("subordinate_parts");

    /// <summary>Creates the fallback.</summary>
    /// <param name="executor">Runs the live read against Infor Visual.</param>
    /// <param name="mySqlHelperServer">Reads the <c>mtm_mock</c> mirror.</param>
    public VisualSubordinatePartsFallback(IVisualQueryExecutor executor, IMySqlHelperServer? mySqlHelperServer = null)
        : base(executor, mySqlHelperServer)
    {
    }

    /// <inheritdoc />
    protected override VisualReadShape Shape => s_shape;

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?> BuildLiveParameters(VisualSubordinatePartRequest request) =>
        new Dictionary<string, object?>
        {
            ["NormalizedWorkOrder"] = request.NormalizedWorkOrder,
            ["ParentPartNumber"] = request.PartNumber,
            ["SequenceNumber"] = request.SequenceNumber,
        };

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?> BuildCacheParameters(VisualSubordinatePartRequest request) =>
        new Dictionary<string, object?>
        {
            ["p_normalized_work_order"] = request.NormalizedWorkOrder,
            ["p_part_number"] = request.PartNumber,
            ["p_sequence_number"] = ParseSequenceNumber(request.SequenceNumber),
        };

    /// <inheritdoc />
    protected override VisualSubordinatePartRow MapRow(IReadOnlyDictionary<string, object?> row) => new()
    {
        Category = VisualRowReader.GetString(row, "Category"),
        PartNumber = VisualRowReader.GetString(row, "PartNumber"),
        Description = VisualRowReader.GetString(row, "Description"),
        Location = VisualRowReader.GetString(row, "Location"),
        User8 = VisualRowReader.GetString(row, "User8"),
        OnHandQuantity = VisualRowReader.GetDecimal(row, "OnHandQuantity"),
    };

    /// <summary>
    /// Converts the sequence number to the integer the mirror stores. An unparseable value yields zero,
    /// which matches no mirror row — the same "nothing found" outcome the live read produces for a
    /// non-numeric sequence.
    /// </summary>
    private static int ParseSequenceNumber(string sequenceNumber) =>
        int.TryParse(sequenceNumber, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;
}
