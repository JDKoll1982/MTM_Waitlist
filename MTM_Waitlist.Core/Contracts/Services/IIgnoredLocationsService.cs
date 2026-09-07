using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// Shared read of the "ignored Infor Visual inventory locations" list so every consumer
/// (Waitlist/Coil, Setup, sample/mock paths) filters location lists and summed totals the
/// same way. The list is edited in Settings and persisted via <see cref="ILocalSettingsService"/>.
/// </summary>
public interface IIgnoredLocationsService
{
    /// <summary>Current ignored location codes (uppercased, distinct, sorted), defaulting when unset.</summary>
    Task<IReadOnlyList<string>> GetIgnoredLocationsAsync(CancellationToken cancellationToken = default);

    /// <summary>True when the given location code is currently in the ignored list (case-insensitive).</summary>
    Task<bool> IsLocationIgnoredAsync(string location, CancellationToken cancellationToken = default);
}
