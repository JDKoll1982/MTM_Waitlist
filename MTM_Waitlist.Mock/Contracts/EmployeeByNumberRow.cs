using MTM_Waitlist.Mock.Models;

namespace MTM_Waitlist.Mock.Contracts;

/// <summary>
/// The agreed row shape of read shape 6, <c>employee_by_number</c>
/// (`contracts/employee-read-shape-contract.md` §2; FR-032).
/// </summary>
/// <remarks>
/// <para>
/// <b>Live and mirrored rows have this shape and no other.</b> The live read returns
/// <c>EmployeeNumber, FirstName, LastName</c> and <c>sp_visual_employee_by_number_get</c> projects the same three
/// names in the same order, so a caller of the fallback cannot tell which source answered — which is precisely
/// what FR-032 requires of every read shape.
/// </para>
/// <para>
/// <b>An unknown number returns no row at all.</b> That is what lets the set-up path say it cannot be completed
/// rather than guessing a name (FR-013).
/// </para>
/// </remarks>
public sealed record EmployeeByNumberRow
{
    /// <summary>The employee number the row answers for.</summary>
    public string EmployeeNumber { get; init; } = string.Empty;

    /// <summary>The person's first name.</summary>
    public string FirstName { get; init; } = string.Empty;

    /// <summary>The person's last name.</summary>
    public string LastName { get; init; } = string.Empty;

    /// <summary>
    /// The agreed row, read from the shape's own mapped row, so the two cannot drift apart.
    /// </summary>
    /// <param name="row">The row the read shape's fallback mapped.</param>
    /// <returns>The row in its agreed shape.</returns>
    public static EmployeeByNumberRow From(VisualEmployeeByNumberRow row)
    {
        ArgumentNullException.ThrowIfNull(row);

        return new EmployeeByNumberRow
        {
            EmployeeNumber = row.EmployeeNumber,
            FirstName = row.FirstName,
            LastName = row.LastName,
        };
    }
}
