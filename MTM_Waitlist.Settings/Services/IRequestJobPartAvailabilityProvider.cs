using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// Supplies the availability snapshot the picker filters on (FR-002, contract §3).
/// <para>
/// The <b>mapping</b> from the active setup job onto <see cref="RequestJobPartAvailability"/> is owned by the
/// composition root, because that is the only place permitted to see both
/// <c>IActiveJobItemResolverService</c> (<c>MTM_Waitlist.Setup</c>) and this snapshot
/// (<c>MTM_Waitlist.Settings</c>).
/// </para>
/// <para>
/// The snapshot is <b>not</b> computed at registration time — registration happens at startup, when no work
/// centre is known. It is resolved when the Item step is <i>entered</i>, which is what makes the check run
/// before the Item list is built rather than after an Item has been offered.
/// </para>
/// </summary>
public interface IRequestJobPartAvailabilityProvider
{
    /// <summary>
    /// The snapshot for a work centre's active setup job. A work centre with no active job yields
    /// <see cref="RequestJobPartAvailability.None"/> rather than null, so the caller never has to special-case
    /// "no job" and the job-independent Items stay offerable.
    /// </summary>
    Task<RequestJobPartAvailability> GetAvailabilityAsync(string workCenter, CancellationToken cancellationToken = default);
}
