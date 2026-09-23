namespace MTM_Waitlist.Module_Core.Models;

public sealed class StartupState
{
    public const string SessionTokenSourceNone = "None";

    public const string SessionTokenSourceLocal = "Local";

    public const string SessionTokenSourceDatabase = "Database";

    public bool IsBusy { get; set; } = true;

    public string StatusText { get; set; } = "Preparing startup checks...";

    public DateTimeOffset LastUpdatedUtc { get; set; } = DateTimeOffset.UtcNow;

    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// The signed-in person's <c>core_users_profiles.id</c>, so a gate or an audit row can name the person
    /// rather than their text. Zero means no account has been resolved yet.
    /// </summary>
    public long UserId { get; set; }

    public bool ConfigurationLoaded { get; set; }

    public string ConfigurationFolder { get; set; } = string.Empty;

    public string ConfigurationFile { get; set; } = string.Empty;

    public string HostnameNormalized { get; set; } = string.Empty;

    public string MacAddressNormalized { get; set; } = string.Empty;

    /// <summary>The signed-in person's role name. Presentation only: no access decision reads it (FR-054).</summary>
    public string CurrentRole { get; set; } = string.Empty;

    /// <summary>
    /// The signed-in person's role code, from <c>auth_roles_catalog.role_code</c>. This is the role's identity,
    /// so every access decision and every rank comparison reads this and never <see cref="CurrentRole"/>.
    /// </summary>
    public string CurrentRoleCode { get; set; } = string.Empty;

    public string EmployeeNumber { get; set; } = string.Empty;

    public string EmployeeName { get; set; } = string.Empty;

    public bool IsUserMatched { get; set; }

    public bool IsComputerRegistered { get; set; }

    public bool IsComputerRegistrationAuthoritative { get; set; }

    public bool IsSessionValid { get; set; }

    public string SessionTokenSource { get; set; } = SessionTokenSourceNone;

    public DateTimeOffset ServerTimeUtc { get; set; }

    public bool RequireNewUserAction { get; set; }

    /// <summary>
    /// Set when the resolved account still holds its temporary default password, so the login surface opens
    /// straight onto "set a new password" and never shows the sign-in form.
    /// </summary>
    public bool RequirePasswordChange { get; set; }

    /// <summary>The account the startup-resolved password change applies to, from the store.</summary>
    public long PasswordChangeUserId { get; set; }

    public string LoginHint { get; set; } = string.Empty;

    /// <summary>
    /// Whether the signed-in person holds the developer role. Answered from the role code, because a display
    /// name is presentation that may change and must never decide access (FR-054).
    /// </summary>
    public bool IsDeveloper => string.Equals(CurrentRoleCode, "developer", StringComparison.OrdinalIgnoreCase);

    public bool IsEmployeeIdentified =>
        !string.IsNullOrWhiteSpace(EmployeeNumber)
        || !string.IsNullOrWhiteSpace(EmployeeName);
}
