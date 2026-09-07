using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// Reports whether the app can reach each external data source (Infor Visual SQL Server, and the
/// receiving-app MySQL DB). When a source is <c>Unreachable</c>, the app falls back to mock data for
/// that source. The <c>mtm_waitlist</c> DB is deliberately not part of this service because it is
/// hard-required for startup.
/// </summary>
public interface IConnectionHealthService
{
    /// <summary>Performs a fresh reachability check for a single source.</summary>
    Task<ConnectionHealthState> CheckAsync(ConnectionSource source, CancellationToken cancellationToken = default);

    /// <summary>Performs a fresh reachability check for all external sources.</summary>
    Task<IReadOnlyDictionary<ConnectionSource, ConnectionHealthState>> CheckAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns the last known snapshot without forcing a new check (or <c>Unknown</c>).</summary>
    ConnectionHealthState GetLastKnown(ConnectionSource source);
}
