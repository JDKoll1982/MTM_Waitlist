namespace MTM_Waitlist.Mock.Models;

/// <summary>One result row of read shape 10, <c>part_sequences</c>.</summary>
/// <remarks>
/// <para>
/// One row per (sequence, relation) the part is attributed at, which is the shape <c>GetPartSequences.sql</c>
/// returns: the application merges the rows a number is named by into one option, and it can only do that if a
/// row still says which relation named it.
/// </para>
/// <para>
/// <see cref="IsLastOperation"/> and <see cref="NextResource"/> carry the plant's own typing rule — the last
/// operation a work order still has to run produces the finished good, and every other operation is work in
/// progress — and <see cref="CancelledOnly"/> marks a sequence none of whose operations is still live.
/// </para>
/// </remarks>
public sealed record VisualPartSequencesRow
{
    /// <summary>The sequence number, as text, exactly as the live read projects it.</summary>
    public string Sequence { get; init; } = string.Empty;

    /// <summary>The shop resource of the operation, which is what a person reads at that sequence.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>The operation status code the caller classifies against.</summary>
    public string OperationStatus { get; init; } = string.Empty;

    /// <summary>Whether this is the last operation the work order still has to run.</summary>
    public int IsLastOperation { get; init; }

    /// <summary>The resource the part goes to after this operation, empty for the last one.</summary>
    public string NextResource { get; init; } = string.Empty;

    /// <summary>Whether every operation behind this number is cancelled, so Visual does not offer it.</summary>
    public int CancelledOnly { get; init; }

    /// <summary>Which relation named the sequence: <c>OwnPart</c> or <c>Requirement</c>.</summary>
    public string SequenceSource { get; init; } = string.Empty;
}
