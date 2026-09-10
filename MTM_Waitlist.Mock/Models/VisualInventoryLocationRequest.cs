namespace MTM_Waitlist.Mock.Models;

/// <summary>The input of read shape 4, <c>inventory_locations</c>.</summary>
/// <param name="PartNumber">The part whose inventory locations are read.</param>
public sealed record VisualInventoryLocationRequest(string PartNumber);
