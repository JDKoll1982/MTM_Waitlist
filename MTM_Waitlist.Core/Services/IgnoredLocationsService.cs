using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;

namespace MTM_Waitlist.Module_Core.Services;

/// <summary>
/// Default ignored Infor Visual inventory locations (single source of truth shared by the
/// Settings editor and every read consumer).
/// </summary>
public static class IgnoredLocationDefaults
{
    public const string SettingKey = "Feature.IgnoredLocations";

    public static readonly IReadOnlyList<string> Locations = new[]
    {
        "WC",
        "NCM",
        "V-WC",
        "NCM-VITS",
        "SHIP",
    };
}

/// <inheritdoc cref="IIgnoredLocationsService"/>
public sealed class IgnoredLocationsService : IIgnoredLocationsService
{
    private readonly ILocalSettingsService _localSettingsService;

    public IgnoredLocationsService(ILocalSettingsService localSettingsService)
    {
        _localSettingsService = localSettingsService;
    }

    public async Task<IReadOnlyList<string>> GetIgnoredLocationsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var stored = await _localSettingsService.ReadSettingAsync<List<string>?>(IgnoredLocationDefaults.SettingKey).ConfigureAwait(false);

        IReadOnlyList<string> source = stored is { Count: > 0 }
            ? stored
            : IgnoredLocationDefaults.Locations;

        var normalized = source
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return normalized;
    }

    public async Task<bool> IsLocationIgnoredAsync(string location, CancellationToken cancellationToken = default)
    {
        var normalizedLocation = (location ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedLocation))
        {
            return false;
        }

        var ignored = await GetIgnoredLocationsAsync(cancellationToken).ConfigureAwait(false);
        return ignored.Any(value => string.Equals(value, normalizedLocation, StringComparison.OrdinalIgnoreCase));
    }
}
