namespace MTM_Waitlist.Mock.Service.Models;

/// <summary>
/// Connection settings for the read-only external Infor Visual source.
/// </summary>
/// <remarks>
/// <b>There is deliberately no password property.</b> The Visual password continues to come from
/// the existing environment-variable / connection-string path, exactly as the rest of the
/// application obtains it — this type must never become a place a secret is persisted in plaintext
/// (FR-026, research.md R6).
/// </remarks>
public sealed record VisualSourceSettings
{
    /// <summary>SQL Server host or instance name, for example <c>VISUAL</c>.</summary>
    public string Server { get; init; } = "VISUAL";

    /// <summary>Visual database name, for example <c>MTMFG</c>.</summary>
    public string Database { get; init; } = "MTMFG";

    /// <summary>Read-only login name. The password is supplied out of band.</summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>Connection timeout for a Visual probe or read.</summary>
    public int ConnectionTimeoutSeconds { get; init; } = 10;
}
