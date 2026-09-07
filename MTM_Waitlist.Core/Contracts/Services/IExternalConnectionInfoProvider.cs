using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// Supplies the resolved connection strings for the external (non-<c>mtm_waitlist</c>) data sources so
/// the connection-health service can probe them. Implemented at the app composition root where config +
/// environment-variable resolution already lives. Returns <c>null</c> when a source is not configured.
/// </summary>
public interface IExternalConnectionInfoProvider
{
    /// <summary>Returns the ADO.NET connection string for a source, or null if it is not configured.</summary>
    string? ResolveConnectionString(ConnectionSource source);

    /// <summary>True if the source is a SQL Server target (Infor Visual); false for MySQL (Receiving).</summary>
    bool IsSqlServer(ConnectionSource source);
}
