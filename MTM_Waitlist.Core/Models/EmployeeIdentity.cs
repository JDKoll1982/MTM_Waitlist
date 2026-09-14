namespace MTM_Waitlist.Module_Core.Models;

/// <summary>
/// One person as the account records hold them: the employee identifier, the stored display name and whether the
/// account is active (FR-046).
/// </summary>
/// <remarks>
/// The identifier is the key. A request is attributed to a person by comparing the identifier the signed-in
/// session carries with the identifier the account stores — never by comparing a name, because a name is not what
/// an account is found by and two people may hold similar names.
/// </remarks>
public sealed class EmployeeIdentity
{
    public string EmployeeNumber { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public bool IsActive { get; init; }
}
