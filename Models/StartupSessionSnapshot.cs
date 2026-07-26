namespace MTM_Waitlist.Models;

public sealed class StartupSessionSnapshot
{
    public bool IsUserMatched { get; init; }

    public bool IsWorkstationRegistered { get; init; }

    public bool IsWorkstationRegistrationAuthoritative { get; init; } = true;

    public string CurrentRole { get; init; } = string.Empty;

    public bool HasDatabaseSession { get; init; }

    public DateTimeOffset? DatabaseSessionExpiresUtc { get; init; }
}
