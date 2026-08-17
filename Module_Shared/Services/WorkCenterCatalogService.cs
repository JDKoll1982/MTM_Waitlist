using Microsoft.Extensions.Options;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Shared.Models;
using MTM_Waitlist.Module_Startup.Models;
using MySqlConnector;
using System.Globalization;
using System.Text;

namespace MTM_Waitlist.Module_Shared.Services;

public sealed class WorkCenterCatalogService : IWorkCenterCatalogService
{
    private readonly MySqlHelperServer _mySqlHelperServer;
    private readonly StartupState _startupState;
    private readonly StartupDatabaseOptions _startupDatabaseOptions;

    public WorkCenterCatalogService(
        MySqlHelperServer mySqlHelperServer,
        StartupState startupState,
        IOptions<StartupDatabaseOptions> startupDatabaseOptions)
    {
        _mySqlHelperServer = mySqlHelperServer;
        _startupState = startupState;
        _startupDatabaseOptions = startupDatabaseOptions?.Value ?? new StartupDatabaseOptions();
    }

    public string GetCurrentWorkstationName()
    {
        if (!string.IsNullOrWhiteSpace(_startupState.HostnameNormalized))
        {
            return _startupState.HostnameNormalized;
        }

        return Environment.MachineName;
    }

    public async Task<IReadOnlyList<string>> GetAvailableWorkstationsAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _mySqlHelperServer.ExecuteSqlQueryAsync(
            @"SELECT workstation_name
FROM core_workstations_registry
WHERE is_registered = 1
ORDER BY workstation_name ASC;",
            new Dictionary<string, object?>(),
            MySqlDatabaseTarget.MtmWaitlist,
            cancellationToken).ConfigureAwait(false);

        var workstations = rows
            .Select(row => GetValue(row, "workstation_name"))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var currentWorkstation = await ResolveCurrentWorkstationNameAsync(cancellationToken).ConfigureAwait(false);
        if (!workstations.Any(value => string.Equals(value, currentWorkstation, StringComparison.OrdinalIgnoreCase)))
        {
            workstations.Insert(0, currentWorkstation);
        }

        return workstations;
    }

    public async Task<WorkCenterCatalogResult> GetCatalogAsync(string workstationName, CancellationToken cancellationToken = default)
    {
        var normalizedWorkstationName = string.IsNullOrWhiteSpace(workstationName)
            ? await ResolveCurrentWorkstationNameAsync(cancellationToken).ConfigureAwait(false)
            : workstationName.Trim();

        StartupDebugLog.Info("WorkCenterCatalog", $"GetCatalogAsync started. Workstation='{normalizedWorkstationName}'.");

        var allWorkCenters = await GetAvailableWorkCentersAsync(cancellationToken).ConfigureAwait(false);
        var hotRows = await _mySqlHelperServer.ExecuteSqlQueryAsync(
            @"SELECT
    swc.workstation_name AS work_center_name,
    cwhc.sort_rank
FROM config_workstation_hot_workcenters cwhc
INNER JOIN core_workstations_registry cwr ON cwr.id = cwhc.core_workstation_id
INNER JOIN setup_workstations_catalog swc ON swc.id = cwhc.setup_workstation_id
WHERE cwhc.is_active = 1
  AND swc.is_active = 1
  AND (
        cwr.workstation_name = @p_workstation_name
        OR cwr.hostname_normalized = @p_workstation_name
      )
ORDER BY cwhc.sort_rank ASC, swc.workstation_name ASC;",
            new Dictionary<string, object?>
            {
                ["p_workstation_name"] = normalizedWorkstationName,
            },
            MySqlDatabaseTarget.MtmWaitlist,
            cancellationToken).ConfigureAwait(false);

        var hotLookup = hotRows
            .Select(row => GetValue(row, "work_center_name"))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var hotWorkCenters = allWorkCenters
            .Where(workCenter => hotLookup.Any(hot => string.Equals(hot, workCenter, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var otherWorkCenters = allWorkCenters
            .Where(workCenter => !hotWorkCenters.Any(hot => string.Equals(hot, workCenter, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var activeJobRows = await _mySqlHelperServer.ExecuteSqlQueryAsync(
            @"SELECT DISTINCT work_center
FROM setup_active_jobs
WHERE is_active = 1
  AND work_center IS NOT NULL
  AND TRIM(work_center) <> '';",
            new Dictionary<string, object?>(),
            MySqlDatabaseTarget.MtmWaitlist,
            cancellationToken).ConfigureAwait(false);

        var activeJobLookup = activeJobRows
            .Select(row => GetValue(row, "work_center"))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var activeJobWorkCenters = allWorkCenters
            .Where(workCenter => activeJobLookup.Any(active => string.Equals(active, workCenter, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var result = new WorkCenterCatalogResult
        {
            WorkstationName = normalizedWorkstationName,
            HotWorkCenters = hotWorkCenters,
            OtherWorkCenters = otherWorkCenters,
            ActiveJobWorkCenters = activeJobWorkCenters,
        };

        StartupDebugLog.Info("WorkCenterCatalog", $"GetCatalogAsync completed. Workstation='{normalizedWorkstationName}', HotCount={hotWorkCenters.Count}, OtherCount={otherWorkCenters.Count}, ActiveJobCount={activeJobWorkCenters.Count}.");
        return result;
    }

    public async Task<string?> SaveHotWorkCentersAsync(string workstationName, IReadOnlyCollection<string> hotWorkCenters, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedWorkstationName = string.IsNullOrWhiteSpace(workstationName)
            ? await ResolveCurrentWorkstationNameAsync(cancellationToken).ConfigureAwait(false)
            : workstationName.Trim();

        StartupDebugLog.Info("WorkCenterCatalog", $"SaveHotWorkCentersAsync started. Workstation='{normalizedWorkstationName}', RequestedCount={hotWorkCenters.Count}.");

        var workstationRows = await _mySqlHelperServer.ExecuteSqlQueryAsync(
            @"SELECT id
FROM core_workstations_registry
WHERE workstation_name = @p_workstation_name
   OR hostname_normalized = @p_workstation_name
ORDER BY CASE WHEN workstation_name = @p_workstation_name THEN 0 ELSE 1 END
LIMIT 1;",
            new Dictionary<string, object?>
            {
                ["p_workstation_name"] = normalizedWorkstationName,
            },
            MySqlDatabaseTarget.MtmWaitlist,
            cancellationToken).ConfigureAwait(false);

        var workstationId = GetInt64(workstationRows.FirstOrDefault(), "id");
        if (workstationId <= 0)
        {
            StartupDebugLog.Info("WorkCenterCatalog", $"SaveHotWorkCentersAsync aborted. Workstation '{normalizedWorkstationName}' was not found.");
            return "Unable to save hot workcenters: workstation not found.";
        }

        var availableRows = await _mySqlHelperServer.ExecuteSqlQueryAsync(
            @"SELECT id, workstation_name
FROM setup_workstations_catalog
WHERE is_active = 1;",
            new Dictionary<string, object?>(),
            MySqlDatabaseTarget.MtmWaitlist,
            cancellationToken).ConfigureAwait(false);

        var workCenterIdByName = availableRows
            .Select(row => new
            {
                Id = GetInt64(row, "id"),
                Name = GetValue(row, "workstation_name"),
            })
            .Where(item => item.Id > 0 && !string.IsNullOrWhiteSpace(item.Name))
            .GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Id, StringComparer.OrdinalIgnoreCase);

        var orderedHotWorkCenters = hotWorkCenters
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var resolvedHotWorkCenters = orderedHotWorkCenters
            .Select((workCenterName, index) => new
            {
                WorkCenterName = workCenterName,
                SortRank = index + 1,
                SetupWorkstationId = workCenterIdByName.TryGetValue(workCenterName, out var setupWorkstationId)
                    ? setupWorkstationId
                    : 0L,
            })
            .Where(item => item.SetupWorkstationId > 0)
            .ToArray();

        var connectionString = ResolveConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            StartupDebugLog.Info("WorkCenterCatalog", $"SaveHotWorkCentersAsync aborted. No database connection was available for workstation '{normalizedWorkstationName}'.");
            return "Unable to save hot workcenters: database connection is not configured.";
        }

        try
        {
            var builder = new MySqlConnectionStringBuilder(connectionString)
            {
                ConnectionTimeout = (uint)Math.Max(1, _startupDatabaseOptions.ConnectionTimeoutSeconds),
                Database = "mtm_waitlist",
            };

            await using var connection = new MySqlConnection(builder.ConnectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            await using (var deleteCommand = new MySqlCommand(@"
DELETE FROM config_workstation_hot_workcenters
WHERE core_workstation_id = @p_core_workstation_id;", connection, transaction))
            {
                deleteCommand.CommandTimeout = Math.Max(1, _startupDatabaseOptions.ConnectionTimeoutSeconds);
                deleteCommand.Parameters.AddWithValue("@p_core_workstation_id", workstationId);
                _ = await deleteCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            if (resolvedHotWorkCenters.Length > 0)
            {
                var insertSql = new StringBuilder();
                insertSql.AppendLine("INSERT INTO config_workstation_hot_workcenters (");
                insertSql.AppendLine("    core_workstation_id,");
                insertSql.AppendLine("    setup_workstation_id,");
                insertSql.AppendLine("    public_id,");
                insertSql.AppendLine("    sort_rank,");
                insertSql.AppendLine("    is_active,");
                insertSql.AppendLine("    created_by_user_id,");
                insertSql.AppendLine("    updated_by_user_id,");
                insertSql.AppendLine("    created_utc,");
                insertSql.AppendLine("    updated_utc");
                insertSql.AppendLine(") VALUES");

                for (var index = 0; index < resolvedHotWorkCenters.Length; index++)
                {
                    var parameterSuffix = index.ToString(CultureInfo.InvariantCulture);
                    if (index > 0)
                    {
                        insertSql.AppendLine(",");
                    }

                    insertSql.Append($"(@p_core_workstation_id, @p_setup_workstation_id_{parameterSuffix}, UUID(), @p_sort_rank_{parameterSuffix}, 1, @p_modified_by_user_id, @p_modified_by_user_id, UTC_TIMESTAMP(), UTC_TIMESTAMP())");
                }

                insertSql.AppendLine();
                insertSql.AppendLine("ON DUPLICATE KEY UPDATE");
                insertSql.AppendLine("    sort_rank = VALUES(sort_rank),");
                insertSql.AppendLine("    is_active = 1,");
                insertSql.AppendLine("    updated_by_user_id = VALUES(updated_by_user_id),");
                insertSql.AppendLine("    updated_utc = UTC_TIMESTAMP();");

                await using var insertCommand = new MySqlCommand(insertSql.ToString(), connection, transaction);
                insertCommand.CommandTimeout = Math.Max(1, _startupDatabaseOptions.ConnectionTimeoutSeconds);
                insertCommand.Parameters.AddWithValue("@p_core_workstation_id", workstationId);
                insertCommand.Parameters.AddWithValue("@p_modified_by_user_id", DBNull.Value);

                for (var index = 0; index < resolvedHotWorkCenters.Length; index++)
                {
                    var item = resolvedHotWorkCenters[index];
                    insertCommand.Parameters.AddWithValue($"@p_setup_workstation_id_{index}", item.SetupWorkstationId);
                    insertCommand.Parameters.AddWithValue($"@p_sort_rank_{index}", item.SortRank);
                }

                _ = await insertCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            StartupDebugLog.Info("WorkCenterCatalog", $"SaveHotWorkCentersAsync completed. Workstation='{normalizedWorkstationName}', PersistedCount={resolvedHotWorkCenters.Length}.");
            return null;
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("WorkCenterCatalog", ex, $"SaveHotWorkCentersAsync failed. Workstation='{normalizedWorkstationName}', RequestedCount={orderedHotWorkCenters.Length}.");
            return $"Unable to save hot workcenters: {ex.Message}";
        }
    }

    private async Task<IReadOnlyList<string>> GetAvailableWorkCentersAsync(CancellationToken cancellationToken)
    {
        var rows = await _mySqlHelperServer.ExecuteSqlQueryAsync(
            @"SELECT workstation_name
FROM setup_workstations_catalog
WHERE is_active = 1
ORDER BY sort_rank ASC, workstation_name ASC;",
            new Dictionary<string, object?>(),
            MySqlDatabaseTarget.MtmWaitlist,
            cancellationToken).ConfigureAwait(false);

        var workCenters = rows
            .Select(row => GetValue(row, "workstation_name"))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return workCenters;
    }

    private async Task<string> ResolveCurrentWorkstationNameAsync(CancellationToken cancellationToken)
    {
        var key = GetCurrentWorkstationName();
        if (string.IsNullOrWhiteSpace(key))
        {
            return Environment.MachineName;
        }

        var rows = await _mySqlHelperServer.ExecuteSqlQueryAsync(
            @"SELECT workstation_name
FROM core_workstations_registry
WHERE workstation_name = @p_workstation_name
   OR hostname_normalized = @p_workstation_name
ORDER BY CASE WHEN workstation_name = @p_workstation_name THEN 0 ELSE 1 END
LIMIT 1;",
            new Dictionary<string, object?>
            {
                ["p_workstation_name"] = key,
            },
            MySqlDatabaseTarget.MtmWaitlist,
            cancellationToken).ConfigureAwait(false);

        var workstationName = GetValue(rows.FirstOrDefault(), "workstation_name");
        return string.IsNullOrWhiteSpace(workstationName) ? key : workstationName;
    }

    private static long GetInt64(IReadOnlyDictionary<string, object?>? row, string key)
    {
        if (row is null)
        {
            return 0;
        }

        if (!row.TryGetValue(key, out var value) || value is null)
        {
            return 0;
        }

        return Convert.ToInt64(value);
    }

    private static string GetValue(IReadOnlyDictionary<string, object?>? row, string key)
    {
        if (row is null)
        {
            return string.Empty;
        }

        if (!row.TryGetValue(key, out var value) || value is null)
        {
            return string.Empty;
        }

        return Convert.ToString(value)?.Trim() ?? string.Empty;
    }

    private string? ResolveConnectionString()
    {
        var environmentConnectionString = Environment.GetEnvironmentVariable("MTM_WAITLIST_DB_CONNECTION_STRING")?.Trim()
            ?? Environment.GetEnvironmentVariable("MTM_WAITLIST_STARTUP_DB_CONNECTION_STRING")?.Trim();

        var fallbackConnectionString = Environment.GetEnvironmentVariable("MTM_WAITLIST_DB_CONNECTION_STRING")?.Trim()
            ?? Environment.GetEnvironmentVariable("MTM_WAITLIST_STARTUP_DB_CONNECTION_STRING")?.Trim()
            ?? _startupDatabaseOptions.ConnectionString?.Trim();

        var resolvedConnectionString = string.IsNullOrWhiteSpace(environmentConnectionString)
            ? fallbackConnectionString
            : environmentConnectionString;

        if (string.IsNullOrWhiteSpace(resolvedConnectionString))
        {
            return null;
        }

        return resolvedConnectionString;
    }
}
