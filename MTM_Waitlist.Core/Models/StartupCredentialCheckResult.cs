namespace MTM_Waitlist.Module_Core.Models;

public sealed class StartupCredentialCheckResult
{
    public bool IsAuthenticated { get; init; }

    public bool RequiresPasswordChange { get; init; }

    public long UserId { get; init; }

    public string CurrentRole { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string EmployeeIdentifier { get; init; } = string.Empty;

    public static StartupCredentialCheckResult Failed()
    {
        return new StartupCredentialCheckResult
        {
            IsAuthenticated = false,
            RequiresPasswordChange = false,
            UserId = 0,
            CurrentRole = string.Empty,
            DisplayName = string.Empty,
            EmployeeIdentifier = string.Empty
        };
    }

    public static StartupCredentialCheckResult Success(long userId, string currentRole, bool requiresPasswordChange, string displayName = "", string employeeIdentifier = "")
    {
        return new StartupCredentialCheckResult
        {
            IsAuthenticated = true,
            RequiresPasswordChange = requiresPasswordChange,
            UserId = userId,
            CurrentRole = currentRole,
            DisplayName = displayName,
            EmployeeIdentifier = employeeIdentifier
        };
    }
}