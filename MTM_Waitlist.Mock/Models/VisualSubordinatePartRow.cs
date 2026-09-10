namespace MTM_Waitlist.Mock.Models;

/// <summary>One result row of read shape 3, <c>subordinate_parts</c>.</summary>
/// <remarks>
/// <see cref="PartNumber"/> is the SUBORDINATE part. The operation's own part is the read's input and is
/// persisted as <c>parent_part_number</c> in the mirror, so the names never collide (data-model.md §3.3).
/// </remarks>
public sealed record VisualSubordinatePartRow
{
    /// <summary>The subordinate-part category, for example <c>Coil</c> or <c>Die</c>.</summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>The subordinate part number.</summary>
    public string PartNumber { get; init; } = string.Empty;

    /// <summary>The subordinate part description.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>The storing location code.</summary>
    public string Location { get; init; } = string.Empty;

    /// <summary>The Visual <c>USER_8</c> classification value.</summary>
    public string User8 { get; init; } = string.Empty;

    /// <summary>The quantity on hand for this part at this location.</summary>
    public decimal OnHandQuantity { get; init; }
}
