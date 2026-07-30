namespace MTM_Waitlist.Module_Startup.Models;

public sealed class StartupDatabaseOptions
{
    public string ConnectionStringEnvironmentVariable { get; set; } = "MTM_WAITLIST_STARTUP_DB_CONNECTION_STRING";

    public string ConnectionString { get; set; } = string.Empty;

    public int ConnectionTimeoutSeconds { get; set; } = 10;

    public int MaxRetryCount { get; set; } = 2;

    public int RetryBaseDelayMilliseconds { get; set; } = 500;
}
