namespace MTM_Waitlist.Mock.Models;

/// <summary>The input of read shape 3, <c>subordinate_parts</c>.</summary>
/// <param name="NormalizedWorkOrder">The normalized work-order number.</param>
/// <param name="PartNumber">The operation's part — the parent of the subordinate parts returned.</param>
/// <param name="SequenceNumber">
/// The operation sequence. The live script takes this as text while the mirror's procedure takes an
/// integer, so the fallback converts it on the cache path.
/// </param>
public sealed record VisualSubordinatePartRequest(string NormalizedWorkOrder, string PartNumber, string SequenceNumber);
