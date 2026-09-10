namespace MTM_Waitlist.Mock.Models;

/// <summary>One result row of read shape 4, <c>inventory_locations</c>.</summary>
/// <remarks>
/// Distinct from the Waitlist module's own grid row so the fallback cannot be confused with the
/// caller's presentation model; the caller maps across. The caller's on-hand &gt;= 1 filter and
/// ignored-location filtering stay caller-side and unchanged (contract §3).
/// </remarks>
public sealed record VisualInventoryLocationRow
{
    /// <summary>The part number.</summary>
    public string PartNumber { get; init; } = string.Empty;

    /// <summary>The storing location code.</summary>
    public string Location { get; init; } = string.Empty;

    /// <summary>The quantity on hand at this location.</summary>
    public decimal OnHandQuantity { get; init; }
}
