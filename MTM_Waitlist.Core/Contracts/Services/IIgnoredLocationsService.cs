using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// Shared read of the "ignored Infor Visual inventory locations" list so every consumer
/// (Waitlist inventory, Setup lookups) filters location lists and summed totals the
/// same way. The list belongs to the whole plant and is edited in Settings; reading it is open to everyone and
/// changing it is gated on <c>permission.settings.ignored_locations_edit</c> (FR-024).
/// </summary>
public interface IIgnoredLocationsService
{
    /// <summary>Current ignored location codes (uppercased, distinct, sorted), defaulting when unset.</summary>
    Task<IReadOnlyList<string>> GetIgnoredLocationsAsync(CancellationToken cancellationToken = default);

    /// <summary>True when the given location code is currently in the ignored list (case-insensitive).</summary>
    Task<bool> IsLocationIgnoredAsync(string location, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the plant-wide list. This is the only writer of it, and it is reached only from the Settings
    /// editor, only through a command that has already refused an unauthorised caller.
    /// </summary>
    /// <param name="locations">The codes the whole plant should hide. They are normalised before they are stored.</param>
    /// <param name="cancellationToken">Cancels the store write.</param>
    /// <returns>
    /// True when the list was stored. False when the signed-in person does not hold the edit entitlement, which is
    /// a refusal rather than a fault and is answered rather than raised so the caller can state the reason.
    /// </returns>
    Task<bool> SaveIgnoredLocationsAsync(IReadOnlyList<string> locations, CancellationToken cancellationToken = default);
}
