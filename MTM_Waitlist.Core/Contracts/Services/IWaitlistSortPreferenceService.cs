namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// The order a particular viewer has chosen for the waitlist list, remembered for them and applied when they
/// next use the application (FR-011, SC-009).
/// </summary>
/// <remarks>
/// This is a display preference, not operational data: it is per person, it is never written to the database,
/// and it lives in the existing local-settings mechanism (§D7). The value is one of the five
/// <see cref="WaitlistSortOrder"/> keys, and it defaults to most urgent when the viewer has never chosen
/// (FR-010).
/// </remarks>
public interface IWaitlistSortPreferenceService
{
    /// <summary>
    /// The viewer's remembered order, or <see cref="WaitlistSortOrder.MostUrgent"/> when they have never chosen
    /// one. A remembered value that no key names is reported as the default rather than being handed on as-is.
    /// </summary>
    Task<string> GetSortOrderAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Remembers <paramref name="sortOrder"/> for the viewer. A value that is not one of the five keys is
    /// remembered as the default, so nothing unusable is ever stored.
    /// </summary>
    Task SetSortOrderAsync(string? sortOrder, CancellationToken cancellationToken = default);
}
