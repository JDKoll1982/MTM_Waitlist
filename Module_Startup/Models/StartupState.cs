namespace MTM_Waitlist.Module_Startup.Models;

public sealed class StartupState
{
    public const string SessionTokenSourceNone = "None";

    public const string SessionTokenSourceLocal = "Local";

    public const string SessionTokenSourceDatabase = "Database";

    public bool IsBusy { get; set; } = true;

    public string StatusText { get; set; } = "Preparing startup checks...";

    public DateTimeOffset LastUpdatedUtc { get; set; } = DateTimeOffset.UtcNow;

    public string Username { get; set; } = string.Empty;

    public bool ConfigurationLoaded { get; set; }

    public string ConfigurationFolder { get; set; } = string.Empty;

    public string ConfigurationFile { get; set; } = string.Empty;

    public string HostnameNormalized { get; set; } = string.Empty;

    public string MacAddressNormalized { get; set; } = string.Empty;

    public string CurrentRole { get; set; } = string.Empty;

    public bool IsUserMatched { get; set; }

    public bool IsComputerRegistered { get; set; }

    public bool IsComputerRegistrationAuthoritative { get; set; }

    public bool IsSessionValid { get; set; }

    public string SessionTokenSource { get; set; } = SessionTokenSourceNone;

    public DateTimeOffset ServerTimeUtc { get; set; }

    public bool RequireNewUserAction { get; set; }

    public string LoginHint { get; set; } = string.Empty;

    public bool IsDeveloper => string.Equals(CurrentRole, "Developer", StringComparison.OrdinalIgnoreCase);
}
