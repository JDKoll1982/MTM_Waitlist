using MTM_Waitlist.Module_Logging;
using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// The plant-wide answer to "how much does the log store keep", read, written and adopted from one place.
/// </summary>
/// <remarks>
/// <para>
/// <b>One reader and one writer, so the screen and the launch cannot disagree.</b> The value is a
/// <c>all_users</c> row in <c>config_settings_values</c> held by the settings screen, and the launch applies it
/// to the same <see cref="LogStoreLevel"/> the logging seam writes through. Nothing else spells the key.
/// </para>
/// <para>
/// <b>Applying is separate from reading, because the two callers want different halves.</b> The launch applies
/// the stored value without wanting to show it; the screen does both, and shows what it applied so the box and
/// what is in force cannot drift apart.
/// </para>
/// </remarks>
public interface ILogStoreLevelService
{
    /// <summary>Reads the stored level, answering the shipped default when nothing is stored.</summary>
    /// <param name="cancellationToken">The caller's cancellation.</param>
    /// <returns>The level in force.</returns>
    Task<LogSeverity> ReadStoredLevelAsync(CancellationToken cancellationToken = default);

    /// <summary>Reads the stored level and adopts it, so the next entry raised is kept or dropped by it.</summary>
    /// <param name="cancellationToken">The caller's cancellation.</param>
    /// <returns>The level now in force.</returns>
    Task<LogSeverity> ApplyStoredLevelAsync(CancellationToken cancellationToken = default);

    /// <summary>Stores a level for every computer and adopts it straight away.</summary>
    /// <param name="severity">The least severe entry the store should keep.</param>
    /// <param name="actorUserId">Who changed it, for the setting's own audit trail.</param>
    /// <param name="cancellationToken">The caller's cancellation.</param>
    Task SaveLevelAsync(LogSeverity severity, long? actorUserId, CancellationToken cancellationToken = default);
}

/// <inheritdoc />
public sealed class LogStoreLevelService : ILogStoreLevelService
{
    /// <summary>The scope every plant-wide setting is held at.</summary>
    private const string AllUsers = "all_users";

    private readonly IConfigSettingsValueService _settings;
    private readonly LogStoreLevel _level;

    /// <summary>Creates the service.</summary>
    /// <param name="settings">The configuration store the setting lives in.</param>
    /// <param name="level">The threshold the logging seam keeps entries at.</param>
    public LogStoreLevelService(IConfigSettingsValueService settings, LogStoreLevel level)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _level = level ?? throw new ArgumentNullException(nameof(level));
    }

    /// <inheritdoc />
    public async Task<LogSeverity> ReadStoredLevelAsync(CancellationToken cancellationToken = default)
    {
        var stored = await _settings
            .GetSettingValueAsync(ConfigSettingKeys.LogStoreMinimumLevel, AllUsers)
            .ConfigureAwait(false);

        return LogStoreLevel.ParseSeverity(stored?.SettingValue);
    }

    /// <inheritdoc />
    public async Task<LogSeverity> ApplyStoredLevelAsync(CancellationToken cancellationToken = default)
    {
        var severity = await ReadStoredLevelAsync(cancellationToken).ConfigureAwait(false);

        _level.Adopt(severity);

        return severity;
    }

    /// <inheritdoc />
    public async Task SaveLevelAsync(
        LogSeverity severity,
        long? actorUserId,
        CancellationToken cancellationToken = default)
    {
        await _settings
            .SetSettingValueAsync(
                new ConfigSettingValue
                {
                    SettingKey = ConfigSettingKeys.LogStoreMinimumLevel,
                    ScopeType = AllUsers,
                    ScopeKey = AllUsers,
                    SettingValue = severity.ToString(),
                    ValueType = "text",
                },
                actorUserId)
            .ConfigureAwait(false);

        _level.Adopt(severity);
    }
}
