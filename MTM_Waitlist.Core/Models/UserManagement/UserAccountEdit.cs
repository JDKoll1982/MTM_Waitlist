namespace MTM_Waitlist.Module_Core.Models.UserManagement;

/// <summary>
/// The fields a create or a correction writes: the sign-in name, the two name parts, the employee number, the
/// role code and the active state (FR-001, FR-004, FR-005).
/// </summary>
/// <remarks>
/// The display name is deliberately absent: it is derived from the two name parts rather than typed, so one
/// person cannot be shown under two spellings. The create form offers no active control, and a create always
/// lands active (FR-007); <see cref="IsActive"/> is what a correction may change.
/// </remarks>
public sealed record UserAccountEdit(
    string Username,
    string FirstName,
    string LastName,
    string EmployeeIdentifier,
    string RoleCode,
    bool IsActive = true);
