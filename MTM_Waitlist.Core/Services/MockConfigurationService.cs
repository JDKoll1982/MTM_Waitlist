using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Module_Core.Services;

/// <inheritdoc cref="IMockConfigurationService"/>
public sealed class MockConfigurationService : IMockConfigurationService
{
    private readonly IMySqlHelperServer _mySqlHelperServer;

    public MockConfigurationService(IMySqlHelperServer mySqlHelperServer)
    {
        _mySqlHelperServer = mySqlHelperServer;
    }

    public async Task<MockSettingState> GetMockSettingAsync(ConnectionSource source, CancellationToken cancellationToken = default)
    {
        var key = SettingKeyFor(source);
        var rows = await _mySqlHelperServer
            .ExecuteStoredProcedureQueryAsync(
                "sp_config_settings_get_effective",
                new Dictionary<string, object?>
                {
                    ["p_setting_key"] = key,
                    ["p_computer_id"] = null,
                    ["p_user_id"] = null,
                },
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken)
            .ConfigureAwait(false);

        if (rows.Count == 0)
        {
            return new MockSettingState { SettingKey = key, IsPresent = false, IsMockEnabled = false };
        }

        var raw = AsValue(rows[0], "setting_value_bool") ?? AsValue(rows[0], "setting_value");
        var enabled = raw is not null && ParseBool(raw);
        return new MockSettingState { SettingKey = key, IsPresent = true, IsMockEnabled = enabled };
    }

    public async Task<bool> SetMockSettingAsync(ConnectionSource source, bool enabled, long? updatedByUserId, CancellationToken cancellationToken = default)
    {
        var key = SettingKeyFor(source);
        var affected = await _mySqlHelperServer
            .ExecuteStoredProcedureNonQueryAsync(
                "sp_config_settings_upsert",
                new Dictionary<string, object?>
                {
                    ["p_setting_key"] = key,
                    ["p_scope_type"] = "all_users",
                    ["p_computer_id"] = null,
                    ["p_user_id"] = null,
                    ["p_setting_value"] = null,
                    ["p_setting_value_int"] = null,
                    ["p_setting_value_bool"] = enabled,
                    ["p_setting_value_decimal"] = null,
                    ["p_setting_value_datetime_utc"] = null,
                    ["p_value_type"] = "bool",
                    ["p_updated_by_user_id"] = updatedByUserId,
                },
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken)
            .ConfigureAwait(false);
        StartupDebugLog.Info("MockConfig", $"Upserted central mock setting '{key}' = {enabled} (rows={affected}).");
        return affected > 0;
    }

    private static string SettingKeyFor(ConnectionSource source) => MockSettingKeys.For(source);

    private static object? AsValue(IReadOnlyDictionary<string, object?> row, string column)
    {
        if (!row.TryGetValue(column, out var value) || value is null || value == DBNull.Value)
        {
            return null;
        }

        return value;
    }

    private static bool ParseBool(object value) => value switch
    {
        bool b => b,
        byte by => by != 0,
        short s => s != 0,
        int i => i != 0,
        long l => l != 0,
        string str when long.TryParse(str, out var parsed) => parsed != 0,
        string str when bool.TryParse(str, out var parsedBool) => parsedBool,
        string str when string.Equals(str.Trim(), "1", StringComparison.Ordinal) => true,
        _ => false,
    };
}
