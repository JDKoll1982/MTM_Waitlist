using MTM_Waitlist.Mock.Service.Models;

namespace MTM_Waitlist.Mock.Service.Contracts;

/// <summary>
/// Reports what this host can currently do, so work that cannot succeed is disabled rather than attempted.
/// </summary>
/// <remarks>
/// <para>
/// The service is installable on any machine. Refreshing the mirror needs Infor Visual; backing up or
/// restoring a store needs that store's MySQL database. Rather than letting each engine discover this by
/// failing, they ask this gate and skip the work, and the status surface reports why.
/// </para>
/// <para>
/// Implementations cache their verdict for a short interval, so a loop that checks once per cycle — or a
/// status request — does not open a connection per call, while a host that <i>gains</i> access starts doing
/// the work again without a restart.
/// </para>
/// </remarks>
public interface IServiceCapabilityGate
{
    /// <summary>The most recently measured state, without probing. Never <see langword="null"/>.</summary>
    ServiceCapabilitySnapshot Current { get; }

    /// <summary>
    /// Returns the host's capabilities, measuring them when the cached verdict has expired.
    /// </summary>
    /// <param name="reprobe">Forces a fresh measurement instead of using the cached verdict.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ServiceCapabilitySnapshot> GetCapabilitiesAsync(
        bool reprobe = false,
        CancellationToken cancellationToken = default);
}
