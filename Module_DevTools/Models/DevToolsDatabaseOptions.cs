namespace MTM_Waitlist.Module_DevTools.Models;

public sealed class DevToolsDatabaseOptions
{
    public string ConnectionStringEnvironmentVariable { get; set; } = "MTM_WAITLIST_DEVTOOLS_DB_CONNECTION_STRING";

    public string ConnectionString { get; set; } = string.Empty;

    public int ConnectionTimeoutSeconds { get; set; } = 10;
}
