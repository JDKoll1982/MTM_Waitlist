namespace MTM_Waitlist.Mock.Models;

/// <summary>One result row of read shape 6, <c>employee_by_number</c>.</summary>
/// <remarks>
/// The projection is identical live and cached (FR-032): the live read returns
/// <c>EmployeeNumber, FirstName, LastName</c>, and <c>sp_visual_employee_by_number_get</c> projects the
/// same names in the same order. An employee number the mirror has never held returns no row at all,
/// which is what lets the set-up path refuse rather than guess a name.
/// </remarks>
public sealed record VisualEmployeeByNumberRow
{
    /// <summary>The employee number the row answers for.</summary>
    public string EmployeeNumber { get; init; } = string.Empty;

    /// <summary>The person's first name.</summary>
    public string FirstName { get; init; } = string.Empty;

    /// <summary>The person's last name.</summary>
    public string LastName { get; init; } = string.Empty;
}
