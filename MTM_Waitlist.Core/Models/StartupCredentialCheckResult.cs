namespace MTM_Waitlist.Module_Core.Models;

public sealed record StartupCredentialCheckResult
{
    public bool IsAuthenticated { get; init; }

    public bool RequiresPasswordChange { get; init; }

    public long UserId { get; init; }

    public string CurrentRole { get; init; } = string.Empty;

    /// <summary>The role code from <c>auth_roles_catalog.role_code</c>, which is the role's identity.</summary>
    public string CurrentRoleCode { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string EmployeeIdentifier { get; init; } = string.Empty;

    /// <summary>
    /// Whether the credential presented was a temporary one: the forced-change flag is set, or the stored hash
    /// is empty or the legacy marker <c>0000</c>. The five-attempt limit applies to exactly these accounts.
    /// </summary>
    public bool HoldsTemporaryCredential { get; init; }

    /// <summary>
    /// Wrong attempts recorded against that credential after this attempt: one more than before on a failure, and
    /// zero once it has been cleared. It is a count of attempts, never a record of who tried (FR-043).
    /// </summary>
    public int TemporaryCredentialFailedAttempts { get; init; }

    /// <summary>
    /// Whether the limit had already been reached before this attempt, so the attempt was refused without the
    /// value being compared. A fresh reset is the only way back (FR-042).
    /// </summary>
    public bool TemporaryCredentialAttemptLimitReached { get; init; }

    public static StartupCredentialCheckResult Failed()
    {
        return new StartupCredentialCheckResult
        {
            IsAuthenticated = false,
            RequiresPasswordChange = false,
            UserId = 0,
            CurrentRole = string.Empty,
            CurrentRoleCode = string.Empty,
            DisplayName = string.Empty,
            EmployeeIdentifier = string.Empty
        };
    }

    public static StartupCredentialCheckResult Success(long userId, string currentRole, bool requiresPasswordChange, string displayName = "", string employeeIdentifier = "", string currentRoleCode = "")
    {
        return new StartupCredentialCheckResult
        {
            IsAuthenticated = true,
            RequiresPasswordChange = requiresPasswordChange,
            UserId = userId,
            CurrentRole = currentRole,
            CurrentRoleCode = currentRoleCode,
            DisplayName = displayName,
            EmployeeIdentifier = employeeIdentifier
        };
    }
}