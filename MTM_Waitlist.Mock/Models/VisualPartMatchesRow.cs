namespace MTM_Waitlist.Mock.Models;

/// <summary>One result row of read shape 9, <c>part_matches</c>.</summary>
/// <remarks>
/// One row per part the term matches. The description travels with the number because that is what the person
/// reads when picking the part they meant, exactly as the live read answers it.
/// </remarks>
public sealed record VisualPartMatchesRow
{
    /// <summary>The part number.</summary>
    public string PartNumber { get; init; } = string.Empty;

    /// <summary>The part's description, or empty when the plant holds none.</summary>
    public string Description { get; init; } = string.Empty;
}
