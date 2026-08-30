namespace MTM_Waitlist.Module_Core.Models;

public sealed class StartupSessionSnapshot
{
    public bool IsUserMatched { get; init; }

    public bool IsComputerRegistered { get; init; }

    public bool IsComputerRegistrationAuthoritative { get; init; } = true;

    public string CurrentRole { get; init; } = string.Empty;

    public bool HasDatabaseSession { get; init; }

    public DateTimeOffset? DatabaseSessionExpiresUtc { get; init; }
}
