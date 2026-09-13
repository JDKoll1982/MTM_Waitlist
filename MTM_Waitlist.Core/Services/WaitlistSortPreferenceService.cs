using MTM_Waitlist.Module_Core.Contracts.Services;

namespace MTM_Waitlist.Module_Core.Services;

/// <inheritdoc cref="IWaitlistSortPreferenceService"/>
/// <remarks>
/// One scalar string key through <see cref="ILocalSettingsService"/> — the shape
/// <see cref="UrgencySettingsService"/> already uses, so it works in both the MSIX and the unpackaged branch, and
/// the small-store pattern <c>LocalWaitlistMessageSeenStore</c> establishes elsewhere. Deliberately not a table
/// and not the database: the constitution keeps the stores for operational data, and no requirement asks for a
/// per-user column (§D7).
/// </remarks>
public sealed class WaitlistSortPreferenceService : IWaitlistSortPreferenceService
{
    /// <summary>The local-settings key the viewer's chosen order is stored under.</summary>
    public const string SettingsKey = "Waitlist.SortOrder";

    private readonly ILocalSettingsService _localSettingsService;

    public WaitlistSortPreferenceService(ILocalSettingsService localSettingsService)
    {
        ArgumentNullException.ThrowIfNull(localSettingsService);
        _localSettingsService = localSettingsService;
    }

    /// <inheritdoc />
    public async Task<string> GetSortOrderAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var stored = await _localSettingsService
            .ReadSettingAsync<string>(SettingsKey)
            .ConfigureAwait(false);

        return WaitlistSortOrder.Normalize(stored);
    }

    /// <inheritdoc />
    public async Task SetSortOrderAsync(string? sortOrder, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Normalized on the way in as well as on the way out, so a corrupt or stale value is never stored.
        await _localSettingsService
            .SaveSettingAsync(SettingsKey, WaitlistSortOrder.Normalize(sortOrder))
            .ConfigureAwait(false);
    }
}
