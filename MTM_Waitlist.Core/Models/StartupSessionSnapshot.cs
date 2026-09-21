namespace MTM_Waitlist.Module_Core.Models;

public sealed class StartupSessionSnapshot
{
    public bool IsUserMatched { get; init; }

    public bool IsComputerRegistered { get; init; }

    public bool IsComputerRegistrationAuthoritative { get; init; } = true;

    /// <summary>The matched account's <c>core_users_profiles.id</c>, or zero when no account matched.</summary>
    public long UserId { get; init; }

    /// <summary>The matched account's role name. Presentation only.</summary>
    public string CurrentRole { get; init; } = string.Empty;

    /// <summary>The matched account's role code, from <c>auth_roles_catalog.role_code</c>.</summary>
    public string CurrentRoleCode { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string EmployeeIdentifier { get; init; } = string.Empty;

    public bool HasDatabaseSession { get; init; }

    public DateTimeOffset? DatabaseSessionExpiresUtc { get; init; }
}
