namespace MTM_Waitlist.Mock.Models;

/// <summary>One result row of read shape 2, <c>operation_sequences</c>.</summary>
/// <remarks>
/// <c>SequenceNumber</c> is carried as text so both sources agree exactly: the live column is numeric
/// while the mirror's <c>sequence_number</c> is an integer, and both are rendered the same way (FR-004).
/// </remarks>
public sealed record VisualOperationSequenceRow
{
    /// <summary>The operation sequence number, rendered as the live read renders it.</summary>
    public string SequenceNumber { get; init; } = string.Empty;

    /// <summary>The operation description.</summary>
    public string Description { get; init; } = string.Empty;
}
