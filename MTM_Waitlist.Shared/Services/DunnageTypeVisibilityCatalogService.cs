using System.Globalization;
using System.Text;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Shared.Models;

namespace MTM_Waitlist.Module_Shared.Services;

public sealed class DunnageTypeVisibilityCatalogService : IDunnageTypeVisibilityCatalogService
{
    private readonly MySqlHelperServer _mySqlHelperServer;

    public DunnageTypeVisibilityCatalogService(MySqlHelperServer mySqlHelperServer)
    {
        _mySqlHelperServer = mySqlHelperServer;
    }

    public async Task<DunnageTypeVisibilityCatalogResult> GetCatalogAsync(CancellationToken cancellationToken = default)
    {
        var allTypes = await GetAllDunnageTypesAsync(cancellationToken).ConfigureAwait(false);
        var visibilityByTypeId = await GetVisibilityMapAsync(cancellationToken).ConfigureAwait(false);

        var visible = allTypes
            .Where(type => !visibilityByTypeId.TryGetValue(type.Id, out var isVisible) || isVisible)
            .Select(type => new DunnageTypeVisibilityOption
            {
                Id = type.Id,
                Name = type.Name,
            })
            .ToArray();

        var hidden = allTypes
            .Where(type => visibilityByTypeId.TryGetValue(type.Id, out var isVisible) && !isVisible)
            .Select(type => new DunnageTypeVisibilityOption
            {
                Id = type.Id,
                Name = type.Name,
            })
            .ToArray();

        StartupDebugLog.Info("SettingsDunnageVisibility", $"GetCatalogAsync completed. VisibleCount={visible.Length}, HiddenCount={hidden.Length}.");

        return new DunnageTypeVisibilityCatalogResult
        {
            VisibleDunnageTypes = visible,
            HiddenDunnageTypes = hidden,
        };
    }

    public async Task<IReadOnlyDictionary<string, bool>> GetVisibilityMapAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _mySqlHelperServer.ExecuteSqlQueryAsync(
            @"SELECT dunnage_type_id, is_visible
FROM config_dunnage_types_visibility;",
            new Dictionary<string, object?>(),
            MySqlDatabaseTarget.MtmWaitlist,
            cancellationToken).ConfigureAwait(false);

        var map = rows
            .Select(row => new
            {
                DunnageTypeId = Convert.ToString(GetValue(row, "dunnage_type_id"), CultureInfo.InvariantCulture),
                IsVisible = GetBoolean(row, "is_visible"),
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.DunnageTypeId))
            .GroupBy(item => item.DunnageTypeId!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().IsVisible, StringComparer.OrdinalIgnoreCase);

        return map;
    }

    public async Task<string?> SaveVisibleDunnageTypesAsync(IReadOnlyCollection<string> visibleDunnageTypeIds, CancellationToken cancellationToken = default)
    {
        StartupDebugLog.Info("SettingsDunnageVisibility", $"SaveVisibleDunnageTypesAsync started. RequestedVisibleCount={visibleDunnageTypeIds.Count}.");
        var visibleSet = visibleDunnageTypeIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var allTypes = await GetAllDunnageTypesAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            _ = await _mySqlHelperServer.ExecuteSqlNonQueryAsync(
                @"DELETE FROM config_dunnage_types_visibility;",
                new Dictionary<string, object?>(),
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken).ConfigureAwait(false);

            if (allTypes.Count > 0)
            {
                var insertSql = new StringBuilder();
                insertSql.AppendLine("INSERT INTO config_dunnage_types_visibility (");
                insertSql.AppendLine("    public_id,");
                insertSql.AppendLine("    dunnage_type_id,");
                insertSql.AppendLine("    dunnage_type_name,");
                insertSql.AppendLine("    is_visible,");
                insertSql.AppendLine("    created_by_user_id,");
                insertSql.AppendLine("    updated_by_user_id,");
                insertSql.AppendLine("    created_utc,");
                insertSql.AppendLine("    updated_utc");
                insertSql.AppendLine(") VALUES");

                var parameters = new Dictionary<string, object?>();
                for (var index = 0; index < allTypes.Count; index++)
                {
                    if (index > 0)
                    {
                        insertSql.AppendLine(",");
                    }

                    var type = allTypes[index];
                    insertSql.Append($"(UUID(), @p_dunnage_type_id_{index}, @p_dunnage_type_name_{index}, @p_is_visible_{index}, NULL, NULL, UTC_TIMESTAMP(), UTC_TIMESTAMP())");
                    parameters[$"p_dunnage_type_id_{index}"] = type.NumericId;
                    parameters[$"p_dunnage_type_name_{index}"] = type.Name;
                    parameters[$"p_is_visible_{index}"] = visibleSet.Contains(type.Id) ? 1 : 0;
                }

                _ = await _mySqlHelperServer.ExecuteSqlNonQueryAsync(
                    insertSql.ToString(),
                    parameters,
                    MySqlDatabaseTarget.MtmWaitlist,
                    cancellationToken).ConfigureAwait(false);
            }

            StartupDebugLog.Info("SettingsDunnageVisibility", $"SaveVisibleDunnageTypesAsync completed. PersistedRowCount={allTypes.Count}, VisibleCount={visibleSet.Count}.");
            return null;
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("SettingsDunnageVisibility", ex, "SaveVisibleDunnageTypesAsync failed.");
            return $"Unable to save dunnage type visibility: {ex.Message}";
        }
    }

    private async Task<IReadOnlyList<DunnageTypeRecord>> GetAllDunnageTypesAsync(CancellationToken cancellationToken)
    {
        var rows = await _mySqlHelperServer.ExecuteSqlQueryAsync(
            @"SELECT id, type_name
FROM dunnage_types
WHERE type_name IS NOT NULL
    AND TRIM(type_name) <> ''
ORDER BY type_name ASC;",
            new Dictionary<string, object?>(),
            MySqlDatabaseTarget.MtmReceivingApplication,
            cancellationToken).ConfigureAwait(false);

        var results = rows
            .Select(row => new DunnageTypeRecord
            {
                Id = Convert.ToString(GetValue(row, "id"), CultureInfo.InvariantCulture)?.Trim() ?? string.Empty,
                NumericId = GetInt64(row, "id"),
                Name = Convert.ToString(GetValue(row, "type_name"), CultureInfo.InvariantCulture)?.Trim() ?? string.Empty,
            })
            .Where(item => item.NumericId > 0 && !string.IsNullOrWhiteSpace(item.Id) && !string.IsNullOrWhiteSpace(item.Name))
            .GroupBy(item => item.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return results;
    }

    private static object? GetValue(IReadOnlyDictionary<string, object?> row, string key)
    {
        return row.TryGetValue(key, out var value) ? value : null;
    }

    private static bool GetBoolean(IReadOnlyDictionary<string, object?> row, string key)
    {
        var value = GetValue(row, key);
        if (value is null)
        {
            return true;
        }

        return Convert.ToInt32(value, CultureInfo.InvariantCulture) != 0;
    }

    private static long GetInt64(IReadOnlyDictionary<string, object?> row, string key)
    {
        var value = GetValue(row, key);
        if (value is null)
        {
            return 0;
        }

        return Convert.ToInt64(value, CultureInfo.InvariantCulture);
    }

    private sealed class DunnageTypeRecord
    {
        public string Id { get; init; } = string.Empty;

        public string Name { get; init; } = string.Empty;

        public long NumericId { get; init; }
    }
}
