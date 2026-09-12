using MTM_Waitlist.Mock.Service.Models;

namespace MTM_Waitlist.Mock.Service.Contracts;

/// <summary>
/// Checks whether one store's MySQL database can be connected to from this machine.
/// </summary>
/// <remarks>
/// The probe opens a connection and closes it. It issues no statement text at all, which keeps the
/// stored-procedure-first rule intact with no inline-SQL audit exemption — the same reasoning as the Infor
/// Visual probe.
/// </remarks>
public interface IMySqlStoreConnectivityProbe
{
    /// <summary>Probes one store.</summary>
    /// <param name="store">The store to probe.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<StoreProbeResult> ProbeAsync(BackupStore store, CancellationToken cancellationToken = default);
}
