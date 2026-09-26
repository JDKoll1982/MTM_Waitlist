namespace MTM_Waitlist.Module_Core.Models;

/// <summary>
/// The <c>mtm_waitlist</c> store's connection settings: the one store every internal read and write goes to.
/// </summary>
/// <remarks>
/// Replaces the retired startup module's own database options, keeping the connection string it carried. The type
/// lives in Core rather than in the rebuilt startup module because <c>MTM_Waitlist.Shared</c> reads the same
/// connection string and cannot reference <c>MTM_Waitlist.Startup</c>.
/// </remarks>
public sealed class WaitlistDatabaseOptions
{
    public string ConnectionStringEnvironmentVariable { get; set; } = "MTM_WAITLIST_STARTUP_DB_CONNECTION_STRING";

    public string ConnectionString { get; set; } = string.Empty;

    public int ConnectionTimeoutSeconds { get; set; } = 10;

    public int MaxRetryCount { get; set; } = 2;

    public int RetryBaseDelayMilliseconds { get; set; } = 500;
}
