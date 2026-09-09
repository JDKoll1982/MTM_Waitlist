namespace MTM_Waitlist.Module_Core.Models;

/// <summary>
/// Options for the external MTM Receiving Application MySQL database
/// (<c>mtm_receiving_application</c>). Bound from the <c>ReceivingDatabaseOptions</c>
/// configuration section; the matching environment variable
/// (<c>MTM_RECEIVING_APPLICATION_DB_CONNECTION_STRING</c>) takes precedence at
/// resolution time (see <c>MySqlHelperServer.ResolveConnectionString</c>).
/// The receiving database is a separate deployment owned by the MTM Receiving
/// Application and typically lives on a different MySQL host than <c>mtm_waitlist</c>,
/// so it must not silently fall back to the waitlist connection string.
/// </summary>
public sealed class ReceivingDatabaseOptions
{
    public string ConnectionStringEnvironmentVariable { get; set; } = "MTM_RECEIVING_APPLICATION_DB_CONNECTION_STRING";

    public string ConnectionString { get; set; } = string.Empty;
}
