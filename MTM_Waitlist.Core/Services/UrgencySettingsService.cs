using MTM_Waitlist.Module_Core.Contracts.Services;

namespace MTM_Waitlist.Module_Core.Services;

/// <inheritdoc cref="IUrgencySettingsService"/>
public sealed class UrgencySettingsService : IUrgencySettingsService
{
    /// <summary>Default max-allotted minutes applied when a sub-type has no stored override.</summary>
    public const int DefaultMinutes = 30;

    /// <summary>Per-sub-type keys are stored as separate scalar int keys (safe for every local-settings store).</summary>
    public const string KeyPrefix = "Urgency.MaxAllottedMinutes.";

    private readonly ILocalSettingsService _localSettingsService;

    public UrgencySettingsService(ILocalSettingsService localSettingsService)
    {
        _localSettingsService = localSettingsService;
    }

    public TimeSpan DefaultMaxAllotted => TimeSpan.FromMinutes(DefaultMinutes);

    public async Task<TimeSpan> GetMaxAllottedAsync(string subtype, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var minutes = await ReadMinutesAsync(Key(subtype)).ConfigureAwait(false) ?? DefaultMinutes;
        return TimeSpan.FromMinutes(minutes);
    }

    public async Task SetMaxAllottedAsync(string subtype, int minutes, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var value = Math.Clamp(minutes, 1, 24 * 60); // at least 1 minute, at most 24 hours
        await _localSettingsService.SaveSettingAsync(Key(subtype), value).ConfigureAwait(false);
    }

    private static string Key(string subtype) => KeyPrefix + (subtype ?? string.Empty).Trim();

    private async Task<int?> ReadMinutesAsync(string key)
        => await _localSettingsService.ReadSettingAsync<int?>(key).ConfigureAwait(false);
}
