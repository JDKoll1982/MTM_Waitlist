namespace MTM_Waitlist.Mock.Models;

/// <summary>The input of read shape 5, <c>disposition_input</c>.</summary>
/// <param name="WorkOrder">The raw work-order number.</param>
/// <param name="PartNumber">The part number.</param>
/// <param name="WorkCenter">
/// The work centre the job is set up on. It is the anchor the routing is read from: the operation at this work
/// centre is the job's own, so the read can name the operation that follows it.
/// </param>
public sealed record VisualDispositionInputRequest(string WorkOrder, string PartNumber, string WorkCenter);
