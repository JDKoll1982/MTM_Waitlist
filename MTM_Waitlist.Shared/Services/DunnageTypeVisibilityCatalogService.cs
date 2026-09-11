using System.Globalization;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Shared.Models;

namespace MTM_Waitlist.Module_Shared.Services;

public sealed class DunnageTypeVisibilityCatalogService : IDunnageTypeVisibilityCatalogService
{
    /// <summary>The persisted visibility map, one row per stored type.</summary>
    private const string VisibilityGetProcedure = "sp_config_dunnage_types_visibility_get";

    /// <summary>Clears the map before it is rewritten.</summary>
    private const string VisibilityDeleteAllProcedure = "sp_config_dunnage_types_visibility_delete_all";

    /// <summary>Writes one type's visibility row.</summary>
    private const string VisibilityInsertRowProcedure = "sp_config_dunnage_types_visibility_insert_row";

    /// <summary>The receiving store's usable dunnage types, alphabetically.</summary>
    private const string ReceivingDunnageTypesProcedure = "sp_receiving_dunnage_types_get_all";

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
        var rows = await _mySqlHelperServer.ExecuteStoredProcedureQueryAsync(
            VisibilityGetProcedure,
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
            _ = await _mySqlHelperServer.ExecuteStoredProcedureNonQueryAsync(
                VisibilityDeleteAllProcedure,
                new Dictionary<string, object?>(),
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken).ConfigureAwait(false);

            // One call per type: MySQL 5.7 cannot iterate a collection inside a routine, and the only value that
            // would have to travel in a list is the free-text type name, which a delimiter cannot carry safely.
            // The delete-then-write shape is unchanged from the statement this replaces (see the artifact header).
            foreach (var type in allTypes)
            {
                _ = await _mySqlHelperServer.ExecuteStoredProcedureNonQueryAsync(
                    VisibilityInsertRowProcedure,
                    new Dictionary<string, object?>
                    {
                        ["p_dunnage_type_id"] = type.NumericId,
                        ["p_dunnage_type_name"] = type.Name,
                        ["p_is_visible"] = visibleSet.Contains(type.Id) ? 1 : 0,
                    },
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
        var rows = await _mySqlHelperServer.ExecuteStoredProcedureQueryAsync(
            ReceivingDunnageTypesProcedure,
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

        if (value is bool boolValue)
        {
            return boolValue;
        }

        return TryConvertToInt64(value, out var number) ? number != 0 : true;
    }

    private static long GetInt64(IReadOnlyDictionary<string, object?> row, string key)
    {
        var value = GetValue(row, key);
        if (value is null)
        {
            return 0;
        }

        return TryConvertToInt64(value, out var number) ? number : 0;
    }

    private static bool TryConvertToInt64(object? value, out long result)
    {
        result = 0;
        if (value is null)
        {
            return false;
        }

        if (value is bool boolValue)
        {
            result = boolValue ? 1 : 0;
            return true;
        }

        if (value is long longValue)
        {
            result = longValue;
            return true;
        }

        try
        {
            result = Convert.ToInt64(value, CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
        {
            return false;
        }
    }

    private sealed class DunnageTypeRecord
    {
        public string Id { get; init; } = string.Empty;

        public string Name { get; init; } = string.Empty;

        public long NumericId { get; init; }
    }
}
