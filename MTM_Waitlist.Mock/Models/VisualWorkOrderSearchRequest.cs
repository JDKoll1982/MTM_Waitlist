namespace MTM_Waitlist.Mock.Models;

/// <summary>The input of read shape 8, <c>work_order_search</c>.</summary>
/// <remarks>
/// Each fragment may be empty, which means "no constraint on this column". The three are always sent,
/// because both reads treat an empty fragment as an unconstrained search rather than as a missing value.
/// </remarks>
/// <param name="WorkOrderText">What the operator typed for the work order, matched fuzzily.</param>
/// <param name="PartFilter">A part-number fragment.</param>
/// <param name="CustomerFilter">A customer-name fragment (a name, never an id).</param>
public sealed record VisualWorkOrderSearchRequest(string WorkOrderText, string PartFilter, string CustomerFilter);
