using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;

namespace MTM_Waitlist.Module_Waitlist.Services;

/// <summary>
/// Stores the per-request "last looked at" times in the app's per-user local settings file, so the
/// new-message indicator survives a restart rather than re-flagging everything the viewer has already read.
/// </summary>
/// <remarks>
/// Persisted as one dictionary under a single key: the values are tiny and always read together. The map is
/// trimmed to the most recent <see cref="MaxTrackedRequests"/> entries so it cannot grow without bound on a
/// long-lived installation.
/// </remarks>
public sealed class LocalWaitlistMessageSeenStore : IWaitlistMessageSeenStore
{
    /// <summary>The local-settings key holding the per-request last-seen times.</summary>
    public const string SettingsKey = "Waitlist.MessageSeen";

    private const int MaxTrackedRequests = 500;

    private readonly ILocalSettingsService _settings;

    private Task<Dictionary<string, DateTimeOffset>>? _load;

    public LocalWaitlistMessageSeenStore(ILocalSettingsService settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _settings = settings;
    }

    /// <inheritdoc />
    public async Task<DateTimeOffset?> GetLastSeenUtcAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        var map = await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        return map.TryGetValue(requestId.ToString(), out var seen) ? seen : null;
    }

    /// <inheritdoc />
    public async Task MarkSeenAsync(Guid requestId, DateTimeOffset seenUtc, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var map = await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);

        var key = requestId.ToString();
        if (map.TryGetValue(key, out var existing) && existing >= seenUtc)
        {
            // Already seen at least this far: nothing to write, so a page that refreshes every 30 seconds
            // does not rewrite the settings file just for being open.
            return;
        }

        map[key] = seenUtc;
        Trim(map);

        try
        {
            await _settings.SaveSettingAsync(SettingsKey, map).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // A last-seen time that cannot be written costs a lingering indicator, not a broken screen.
            StartupDebugLog.Error("Waitlist", ex, "Recording the last-seen time for a request failed; the new-message indicator may repeat.");
        }
    }

    private Task<Dictionary<string, DateTimeOffset>> EnsureLoadedAsync(CancellationToken cancellationToken)
        => _load ??= LoadAsync(cancellationToken);

    private async Task<Dictionary<string, DateTimeOffset>> LoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            var stored = await _settings
                .ReadSettingAsync<Dictionary<string, DateTimeOffset>>(SettingsKey)
                .ConfigureAwait(false);

            return stored is null
                ? new Dictionary<string, DateTimeOffset>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, DateTimeOffset>(stored, StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            // An unreadable settings file means "nothing has been seen yet", which at worst shows an
            // indicator the viewer has already read — never a crash on the list.
            StartupDebugLog.Error("Waitlist", ex, "Reading the last-seen times failed; treating every request as unread this time.");
            return new Dictionary<string, DateTimeOffset>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static void Trim(Dictionary<string, DateTimeOffset> map)
    {
        if (map.Count <= MaxTrackedRequests)
        {
            return;
        }

        foreach (var key in map
            .OrderByDescending(pair => pair.Value)
            .Skip(MaxTrackedRequests)
            .Select(pair => pair.Key)
            .ToArray())
        {
            map.Remove(key);
        }
    }
}
