namespace MTM_Waitlist.Module_Startup.Models;

public sealed class StartupCredentialCheckResult
{
    public bool IsAuthenticated { get; init; }

    public bool RequiresPasswordChange { get; init; }

    public long UserId { get; init; }

    public string CurrentRole { get; init; } = string.Empty;

    public static StartupCredentialCheckResult Failed()
    {
        return new StartupCredentialCheckResult
        {
            IsAuthenticated = false,
            RequiresPasswordChange = false,
            UserId = 0,
            CurrentRole = string.Empty
        };
    }

    public static StartupCredentialCheckResult Success(long userId, string currentRole, bool requiresPasswordChange)
    {
        return new StartupCredentialCheckResult
        {
            IsAuthenticated = true,
            RequiresPasswordChange = requiresPasswordChange,
            UserId = userId,
            CurrentRole = currentRole
        };
    }
}