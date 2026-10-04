namespace MTM_Waitlist.Mock.Models;

/// <summary>The input of read shape 6, <c>employee_by_number</c>.</summary>
/// <param name="EmployeeNumber">The employee number to look up. Matched verbatim, never reinterpreted.</param>
public sealed record VisualEmployeeByNumberRequest(string EmployeeNumber);
