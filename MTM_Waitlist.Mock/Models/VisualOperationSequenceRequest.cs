namespace MTM_Waitlist.Mock.Models;

/// <summary>The input of read shape 2, <c>operation_sequences</c>.</summary>
/// <param name="NormalizedWorkOrder">The normalized work-order number.</param>
/// <param name="PartNumber">The part whose operation sequences are read.</param>
public sealed record VisualOperationSequenceRequest(string NormalizedWorkOrder, string PartNumber);
