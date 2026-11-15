namespace MTM_Waitlist.Mock.Models;

/// <summary>One result row of read shape 12, <c>die_search</c>.</summary>
/// <remarks>
/// One row per die, under the strongest association that named it — the term's own die number, then the dies
/// <c>PART_ALTERNATE</c> records against the term's part, then the dies its work orders required. A die is not
/// inventoried, so <see cref="Location"/> is the part's primary location on <c>PART_SITE</c>, falling back to its
/// stock rows, which is exactly what the live read answers.
/// </remarks>
public sealed record VisualDieSearchRow
{
    /// <summary>The die's part number.</summary>
    public string DieNumber { get; init; } = string.Empty;

    /// <summary>The die's description, or empty when the plant holds none.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>The association that named the die: <c>Die</c>, <c>Explicit</c> or <c>History</c>.</summary>
    public string Association { get; init; } = string.Empty;

    /// <summary>The die's home location, or empty when the plant records none.</summary>
    public string Location { get; init; } = string.Empty;
}
