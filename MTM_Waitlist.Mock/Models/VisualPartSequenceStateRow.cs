namespace MTM_Waitlist.Mock.Models;

/// <summary>One result row of read shape 11, <c>part_sequence_state</c>.</summary>
/// <remarks>
/// At most one row per (part, sequence). No row at all means the part has no operation at that sequence, which
/// the caller reads as "no state recorded" and answers by asking the floor first — deliberately different from an
/// operation that exists with a null status.
/// </remarks>
public sealed record VisualPartSequenceStateRow
{
    /// <summary>The operation's status code, or empty when the operation records none.</summary>
    public string OperationStatus { get; init; } = string.Empty;

    /// <summary>Whether the operation is the last one the work order still has to run.</summary>
    public int IsLastOperation { get; init; }

    /// <summary>The resource the part goes to next, empty for the last operation.</summary>
    public string NextResource { get; init; } = string.Empty;
}
