namespace MTM_Waitlist.Mock.Models;

/// <summary>The input of read shape 7, <c>part_warehouse_quantities</c>.</summary>
/// <param name="PartNumber">The part to total. It is also the shape's returned identifying column.</param>
public sealed record VisualPartWarehouseQuantitiesRequest(string PartNumber);
