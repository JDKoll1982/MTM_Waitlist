namespace MTM_Waitlist.Mock.Models;

/// <summary>The input of read shape 5, <c>disposition_input</c>.</summary>
/// <param name="WorkOrder">The raw work-order number.</param>
/// <param name="PartNumber">The part number.</param>
public sealed record VisualDispositionInputRequest(string WorkOrder, string PartNumber);
