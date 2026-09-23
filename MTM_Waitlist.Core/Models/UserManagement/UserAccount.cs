namespace MTM_Waitlist.Module_Core.Models.UserManagement;

/// <summary>
/// One person as their own page shows them: the identity fields, their role, their active state and the
/// wrong-attempt count on any temporary credential.
/// </summary>
/// <remarks>
/// Reading is never refused: an account the reader cannot change is still readable, so this type carries no
/// permission of its own and the page states the refusal where the reader tries to act (FR-027, FR-101).
/// </remarks>
public sealed record UserAccount(
    long UserId,
    string PublicId,
    string UsernameNormalized,
    string FirstName,
    string LastName,
    string DisplayName,
    string EmployeeIdentifier,
    string RoleCode,
    string RoleName,
    int RoleRank,
    bool IsActive,
    int TemporaryCredentialFailedAttempts);
