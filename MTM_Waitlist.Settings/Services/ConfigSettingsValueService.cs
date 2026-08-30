using Microsoft.Extensions.Logging;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// Implementation of IConfigSettingsValueService.
/// Reads and writes overridable configuration values in config_settings_values
/// through the MySQL helper server.
/// </summary>
public sealed class ConfigSettingsValueService : IConfigSettingsValueService
{
    private readonly IMySqlHelperServer _mySqlHelperServer;
    private readonly ILogger<ConfigSettingsValueService> _logger;

    public ConfigSettingsValueService(
        IMySqlHelperServer mySqlHelperServer,
        ILogger<ConfigSettingsValueService> logger)
    {
        _mySqlHelperServer = mySqlHelperServer ?? throw new ArgumentNullException(nameof(mySqlHelperServer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ConfigSettingValue?> GetSettingValueAsync(string settingKey, string scopeKey)
    {
        if (string.IsNullOrWhiteSpace(settingKey))
        {
            throw new ArgumentException("Setting key cannot be null or empty.", nameof(settingKey));
        }

        var effectiveScopeKey = string.IsNullOrWhiteSpace(scopeKey) ? "all_users" : scopeKey.Trim();

        try
        {
            var rows = await _mySqlHelperServer.ExecuteSqlQueryAsync(
                @"SELECT
    id,
    public_id,
    setting_key,
    scope_type,
    scope_key,
    computer_id,
    user_id,
    setting_value,
    setting_value_int,
    setting_value_bool,
    setting_value_decimal,
    setting_value_datetime_utc,
    value_type,
    updated_by_user_id,
    updated_utc
FROM config_settings_values
WHERE setting_key = @p_setting_key
  AND scope_key = @p_scope_key
LIMIT 1;",
                new Dictionary<string, object?>
                {
                    ["p_setting_key"] = settingKey.Trim(),
                    ["p_scope_key"] = effectiveScopeKey
                },
                MySqlDatabaseTarget.MtmWaitlist,
                CancellationToken.None).ConfigureAwait(false);

            if (rows.Count == 0)
            {
                _logger.LogDebug("No configuration override found for {SettingKey} in scope {ScopeKey}",
                                 settingKey, effectiveScopeKey);
                return null;
            }

            return ParseConfigSettingValue(rows[0]);
        }
        catch (Exception ex)
        {
            // A missing override must never break configuration resolution; the caller falls back to appsettings.
            _logger.LogWarning(ex, "Failed to read configuration override for {SettingKey} in scope {ScopeKey}",
                               settingKey, effectiveScopeKey);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task SetSettingValueAsync(ConfigSettingValue setting, long? updatedByUserId = null)
    {
        if (setting is null)
        {
            throw new ArgumentNullException(nameof(setting));
        }

        if (string.IsNullOrWhiteSpace(setting.SettingKey))
        {
            throw new ArgumentException("Setting key cannot be null or empty.", nameof(setting));
        }

        try
        {
            await _mySqlHelperServer.ExecuteStoredProcedureQueryAsync(
                "sp_config_settings_upsert",
                new Dictionary<string, object?>
                {
                    ["p_setting_key"] = setting.SettingKey.Trim(),
                    ["p_scope_type"] = string.IsNullOrWhiteSpace(setting.ScopeType) ? "all_users" : setting.ScopeType.Trim(),
                    ["p_computer_id"] = setting.ComputerId,
                    ["p_user_id"] = setting.UserId,
                    ["p_setting_value"] = setting.SettingValue,
                    ["p_setting_value_int"] = setting.SettingValueInt,
                    ["p_setting_value_bool"] = setting.SettingValueBool,
                    ["p_setting_value_decimal"] = setting.SettingValueDecimal,
                    ["p_setting_value_datetime_utc"] = setting.SettingValueDatetimeUtc,
                    ["p_value_type"] = string.IsNullOrWhiteSpace(setting.ValueType) ? "text" : setting.ValueType.Trim(),
                    ["p_updated_by_user_id"] = updatedByUserId ?? setting.UpdatedByUserId
                },
                MySqlDatabaseTarget.MtmWaitlist,
                CancellationToken.None).ConfigureAwait(false);

            _logger.LogInformation("Saved configuration override for {SettingKey} in scope {ScopeType}",
                                   setting.SettingKey, setting.ScopeType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save configuration override for {SettingKey}", setting.SettingKey);
            throw new InvalidOperationException(
                $"Failed to save configuration value '{setting.SettingKey}'.", ex);
        }
    }

    /// <inheritdoc />
    public async Task DeleteSettingValueAsync(string settingKey, string scopeKey)
    {
        if (string.IsNullOrWhiteSpace(settingKey))
        {
            throw new ArgumentException("Setting key cannot be null or empty.", nameof(settingKey));
        }

        var effectiveScopeKey = string.IsNullOrWhiteSpace(scopeKey) ? "all_users" : scopeKey.Trim();

        try
        {
            await _mySqlHelperServer.ExecuteSqlQueryAsync(
                @"DELETE FROM config_settings_values
WHERE setting_key = @p_setting_key
  AND scope_key = @p_scope_key;",
                new Dictionary<string, object?>
                {
                    ["p_setting_key"] = settingKey.Trim(),
                    ["p_scope_key"] = effectiveScopeKey
                },
                MySqlDatabaseTarget.MtmWaitlist,
                CancellationToken.None).ConfigureAwait(false);

            _logger.LogInformation("Deleted configuration override for {SettingKey} in scope {ScopeKey}",
                                   settingKey, effectiveScopeKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete configuration override for {SettingKey}", settingKey);
            throw new InvalidOperationException(
                $"Failed to delete configuration value '{settingKey}'.", ex);
        }
    }

    private static ConfigSettingValue ParseConfigSettingValue(IReadOnlyDictionary<string, object?> row)
    {
        return new ConfigSettingValue
        {
            Id = GetInt64(row, "id"),
            PublicId = GetString(row, "public_id"),
            SettingKey = GetString(row, "setting_key"),
            ScopeType = GetString(row, "scope_type"),
            ScopeKey = GetString(row, "scope_key"),
            ComputerId = GetNullableInt64(row, "computer_id"),
            UserId = GetNullableInt64(row, "user_id"),
            SettingValue = GetNullableString(row, "setting_value"),
            SettingValueInt = GetNullableInt64(row, "setting_value_int"),
            SettingValueBool = GetNullableBoolean(row, "setting_value_bool"),
            SettingValueDecimal = GetNullableDecimal(row, "setting_value_decimal"),
            SettingValueDatetimeUtc = GetNullableDateTime(row, "setting_value_datetime_utc"),
            ValueType = GetString(row, "value_type"),
            UpdatedByUserId = GetNullableInt64(row, "updated_by_user_id"),
            UpdatedUtc = GetNullableDateTime(row, "updated_utc") ?? DateTime.UtcNow
        };
    }

    private static string GetString(IReadOnlyDictionary<string, object?> row, string key) =>
        GetNullableString(row, key) ?? string.Empty;

    private static string? GetNullableString(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return value.ToString();
    }

    private static long GetInt64(IReadOnlyDictionary<string, object?> row, string key) =>
        GetNullableInt64(row, key) ?? 0;

    private static long? GetNullableInt64(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return value is long longValue ? longValue : Convert.ToInt64(value);
    }

    private static bool? GetNullableBoolean(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return value is bool boolValue ? boolValue : Convert.ToInt32(value) != 0;
    }

    private static decimal? GetNullableDecimal(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return value is decimal decimalValue ? decimalValue : Convert.ToDecimal(value);
    }

    private static DateTime? GetNullableDateTime(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return value is DateTime dateTime ? dateTime : Convert.ToDateTime(value);
    }
}
