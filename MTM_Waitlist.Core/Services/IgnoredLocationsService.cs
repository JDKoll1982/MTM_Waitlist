using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Permissions;

namespace MTM_Waitlist.Module_Core.Services;

/// <summary>
/// Default ignored Infor Visual inventory locations (single source of truth shared by the
/// Settings editor and every read consumer).
/// </summary>
public static class IgnoredLocationDefaults
{
    /// <summary>
    /// The plant-wide scoped-preference key the list is stored under. One list for the whole site, so every
    /// computer and every person reads the same answer (FR-024).
    /// </summary>
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
/// <remarks>
/// <para>
/// <b>The list belongs to the plant, not to the machine.</b> It is stored once under the store's
/// <c>all_users</c> scope, so a location hidden by IT Department is hidden on every computer rather than only on
/// the one it was hidden from (FR-024). Reading it stays open to everyone; changing it does not.
/// </para>
/// <para>
/// <b>The edit entitlement is enforced where the write happens.</b> <see cref="SaveIgnoredLocationsAsync"/>
/// answers false for a person who does not hold <c>permission.settings.ignored_locations_edit</c>, so a caller
/// that reaches the write without going through the screen's own gate is still refused. Leaving that to the
/// drawing of the control would make a hidden control the permission, which it is not.
/// </para>
/// </remarks>
public sealed class IgnoredLocationsService : IIgnoredLocationsService
{
    /// <summary>The single list is stored as text, so the codes are joined and split on this separator.</summary>
    private const char CodeSeparator = ',';

    /// <summary>The area a store fault is recorded under, so the fault has a name beside it.</summary>
    private const string LogModule = "IgnoredLocations";

    private readonly IScopedPreferenceStore _preferences;
    private readonly IPermissionService _permissionService;

    public IgnoredLocationsService(
        IScopedPreferenceStore preferences,
        IPermissionService permissionService)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        ArgumentNullException.ThrowIfNull(permissionService);

        _preferences = preferences;
        _permissionService = permissionService;
    }

    public async Task<IReadOnlyList<string>> GetIgnoredLocationsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string? stored;

        try
        {
            stored = await _preferences
                .ReadTextAsync(IgnoredLocationDefaults.SettingKey, PreferenceScope.Plant, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // An unreadable list must not empty the filter: treating every location as interesting would show
            // rows the plant has decided to hide, which is a worse answer than the shipped defaults.
            AppLog.Error(LogModule, ex, "The ignored-locations list could not be read; the shipped defaults were used.");
            stored = null;
        }

        var parsed = Parse(stored);

        var source = parsed.Count > 0
            ? parsed
            : IgnoredLocationDefaults.Locations;

        return Normalize(source);
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

    /// <inheritdoc />
    public async Task<bool> SaveIgnoredLocationsAsync(
        IReadOnlyList<string> locations,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!await _permissionService
                .HasPermissionAsync(PermissionKeys.SettingsIgnoredLocationsEdit, cancellationToken)
                .ConfigureAwait(false))
        {
            // Refused where the write happens rather than where the control is drawn. A person who holds only the
            // read entitlement can see the list and cannot change it (FR-024).
            AppLog.Info(LogModule, "The ignored-locations list was not saved because the person does not hold the edit entitlement.");
            return false;
        }

        var normalized = Normalize(locations ?? Array.Empty<string>());
        var stored = string.Join(CodeSeparator, normalized);

        await _preferences
            .WriteTextAsync(IgnoredLocationDefaults.SettingKey, PreferenceScope.Plant, stored, cancellationToken)
            .ConfigureAwait(false);

        return true;
    }

    /// <summary>Splits the stored text back into codes, answering an empty list for a missing or blank value.</summary>
    private static IReadOnlyList<string> Parse(string? stored)
        => string.IsNullOrWhiteSpace(stored)
            ? Array.Empty<string>()
            : stored.Split(CodeSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>Trims, upper-cases, drops blanks, de-duplicates and sorts, so one list is stored one way.</summary>
    private static IReadOnlyList<string> Normalize(IEnumerable<string> source)
        => source
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
