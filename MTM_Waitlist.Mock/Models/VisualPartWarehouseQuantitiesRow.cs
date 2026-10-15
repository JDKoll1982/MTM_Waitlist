namespace MTM_Waitlist.Mock.Models;

/// <summary>One result row of read shape 7, <c>part_warehouse_quantities</c>.</summary>
/// <remarks>
/// One row per part. <see cref="WarehouseQuantity"/> is the part-wide on-hand quantity summed over every
/// location, and <see cref="FinishedGoodsQuantity"/> is the same sum restricted to finished-goods
/// locations. Neither is <c>PART.QTY_ON_HAND</c>, which is the FR-090 defect this shape replaces.
/// </remarks>
public sealed record VisualPartWarehouseQuantitiesRow
{
    /// <summary>The part number.</summary>
    public string PartNumber { get; init; } = string.Empty;

    /// <summary>The part-wide on-hand quantity, summed over every location the part occupies.</summary>
    public decimal WarehouseQuantity { get; init; }

    /// <summary>The quantity on hand at finished-goods locations only.</summary>
    public decimal FinishedGoodsQuantity { get; init; }
}
