using MTM_Waitlist.Module_Core.Contracts.Services;

namespace MTM_Waitlist.Module_Core.Services;

/// <inheritdoc cref="IWaitlistSortPreferenceService"/>
/// <remarks>
/// One text value in the store, held against the person, so the order a viewer chose is already there on the next
/// computer they sign in at (FR-023, SC-008). The normalisation and the declared default are unchanged; only where
/// the value lives has moved.
/// </remarks>
public sealed class WaitlistSortPreferenceService : IWaitlistSortPreferenceService
{
    /// <summary>The scoped-preference key the viewer's chosen order is stored under.</summary>
    public const string SettingsKey = "Waitlist.SortOrder";

    private readonly IScopedPreferenceStore _preferences;

    public WaitlistSortPreferenceService(IScopedPreferenceStore preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        _preferences = preferences;
    }

    /// <inheritdoc />
    public async Task<string> GetSortOrderAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var stored = await _preferences
            .ReadTextAsync(SettingsKey, PreferenceScope.Person, cancellationToken)
            .ConfigureAwait(false);

        return WaitlistSortOrder.Normalize(stored);
    }

    /// <inheritdoc />
    public async Task SetSortOrderAsync(string? sortOrder, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Normalized on the way in as well as on the way out, so a corrupt or stale value is never stored.
        await _preferences
            .WriteTextAsync(SettingsKey, PreferenceScope.Person, WaitlistSortOrder.Normalize(sortOrder), cancellationToken)
            .ConfigureAwait(false);
    }
}
