namespace MTM_Waitlist.Mock.Models;

/// <summary>The input of read shape 1, <c>work_order_lookup</c>.</summary>
/// <param name="NormalizedWorkOrder">The normalized work-order number to look up.</param>
public sealed record VisualWorkOrderLookupRequest(string NormalizedWorkOrder);
