namespace MTM_Waitlist.Module_Settings.ViewModels;

/// <summary>
/// The five facts a roster row shows, each of which the table can be ordered by (FR-086, T045).
/// </summary>
/// <remarks>
/// One column of the table is one member of this, and the view model's ordering is a switch over it. Keeping the
/// keys a type rather than the column header's text is what stops a reworded header from silently re-ordering
/// nothing: a column whose tag names no member here is not sortable, and the page ignores it.
/// </remarks>
public enum RosterSortColumn
{
    /// <summary>Fact 1: the person's name.</summary>
    Name = 0,

    /// <summary>Fact 2: the sign-in name.</summary>
    SignInName = 1,

    /// <summary>Fact 3: the employee number.</summary>
    EmployeeNumber = 2,

    /// <summary>Fact 4: the role.</summary>
    Role = 3,

    /// <summary>Fact 5: whether the account is active.</summary>
    Status = 4,
}
