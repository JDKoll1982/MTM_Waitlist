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

    public string GetCurrentComputerName()
    {
        if (!string.IsNullOrWhiteSpace(_startupState.HostnameNormalized))
        {
            return _startupState.HostnameNormalized;
        }

        return Environment.MachineName;
    }

    public async Task<IReadOnlyList<string>> GetAvailableComputersAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _mySqlHelperServer.ExecuteSqlQueryAsync(
            @"SELECT computer_name
FROM core_computers_registry
WHERE is_registered = 1
ORDER BY computer_name ASC;",
            new Dictionary<string, object?>(),
            MySqlDatabaseTarget.MtmWaitlist,
            cancellationToken).ConfigureAwait(false);

        var workstations = rows
            .Select(row => GetValue(row, "computer_name"))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var currentWorkstation = await ResolveCurrentComputerNameAsync(cancellationToken).ConfigureAwait(false);
        if (!workstations.Any(value => string.Equals(value, currentWorkstation, StringComparison.OrdinalIgnoreCase)))
        {
            workstations.Insert(0, currentWorkstation);
        }

        return workstations;
    }

    public async Task<WorkCenterCatalogResult> GetCatalogAsync(string workstationName, CancellationToken cancellationToken = default)
    {
        var normalizedWorkstationName = string.IsNullOrWhiteSpace(workstationName)
            ? await ResolveCurrentComputerNameAsync(cancellationToken).ConfigureAwait(false)
            : workstationName.Trim();

        StartupDebugLog.Info("WorkCenterCatalog", $"GetCatalogAsync started. Workstation='{normalizedWorkstationName}'.");

        var availableRows = await GetAvailableWorkCenterRowsAsync(cancellationToken).ConfigureAwait(false);
        var allWorkCenters = availableRows
            .Select(row => GetValue(row, "work_center_name"))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var hotRows = await _mySqlHelperServer.ExecuteSqlQueryAsync(
            @"SELECT
    swc.work_center_name AS work_center_name,
    cwhc.sort_rank
FROM config_computer_hot_work_centers cwhc
INNER JOIN core_computers_registry cwr ON cwr.id = cwhc.computer_id
INNER JOIN setup_work_centers_catalog swc ON swc.id = cwhc.work_center_id
WHERE cwhc.is_active = 1
  AND swc.is_active = 1
  AND (
        cwr.computer_name = @p_workstation_name
        OR cwr.hostname_normalized = @p_workstation_name
      )
ORDER BY cwhc.sort_rank ASC, swc.work_center_name ASC;",
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

        // Latest active setup job (work order / part / sequence) per work center.
        // The stored procedure returns exactly the work centers that have an active
        // job, so it also drives the ActiveJobWorkCenters membership.
        var latestJobRows = await _mySqlHelperServer.ExecuteStoredProcedureQueryAsync(
            "sp_setup_active_jobs_latest_by_work_center_get",
            new Dictionary<string, object?>(),
            MySqlDatabaseTarget.MtmWaitlist,
            cancellationToken).ConfigureAwait(false);

        var activeJobLookup = latestJobRows
            .Select(row => GetValue(row, "work_center"))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var activeJobWorkCenters = allWorkCenters
            .Where(workCenter => activeJobLookup.Any(active => string.Equals(active, workCenter, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var jobsByWorkCenter = latestJobRows
            .Where(row => !string.IsNullOrWhiteSpace(GetValue(row, "work_center")))
            .ToDictionary(
                row => GetValue(row, "work_center"),
                row => row,
                StringComparer.OrdinalIgnoreCase);

        var workCenterDetails = new Dictionary<string, WorkCenterDetail>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in availableRows)
        {
            var workCenterName = GetValue(row, "work_center_name");
            if (string.IsNullOrWhiteSpace(workCenterName) || workCenterDetails.ContainsKey(workCenterName))
            {
                continue;
            }

            jobsByWorkCenter.TryGetValue(workCenterName, out var activeJobRow);
            workCenterDetails[workCenterName] = new WorkCenterDetail
            {
                Building = GetValue(row, "building"),
                LastUpdatedUtc = ParseUtcDateTime(GetValue(row, "updated_utc")),
                HasActiveJob = activeJobRow is not null,
                CurrentWorkOrder = activeJobRow is null ? string.Empty : GetValue(activeJobRow, "work_order"),
                CurrentPartNumber = activeJobRow is null ? string.Empty : GetValue(activeJobRow, "part_number"),
                CurrentSequenceNumber = activeJobRow is null ? string.Empty : GetValue(activeJobRow, "sequence_number"),
            };
        }

        var result = new WorkCenterCatalogResult
        {
            ComputerName = normalizedWorkstationName,
            HotWorkCenters = hotWorkCenters,
            OtherWorkCenters = otherWorkCenters,
            ActiveJobWorkCenters = activeJobWorkCenters,
            WorkCenterDetails = workCenterDetails,
        };

        StartupDebugLog.Info("WorkCenterCatalog", $"GetCatalogAsync completed. Workstation='{normalizedWorkstationName}', HotCount={hotWorkCenters.Count}, OtherCount={otherWorkCenters.Count}, ActiveJobCount={activeJobWorkCenters.Count}.");
        return result;
    }

    public async Task<string?> SaveHotWorkCentersAsync(string workstationName, IReadOnlyCollection<string> hotWorkCenters, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedWorkstationName = string.IsNullOrWhiteSpace(workstationName)
            ? await ResolveCurrentComputerNameAsync(cancellationToken).ConfigureAwait(false)
            : workstationName.Trim();

        StartupDebugLog.Info("WorkCenterCatalog", $"SaveHotWorkCentersAsync started. Workstation='{normalizedWorkstationName}', RequestedCount={hotWorkCenters.Count}.");

        var workstationRows = await _mySqlHelperServer.ExecuteSqlQueryAsync(
            @"SELECT id
FROM core_computers_registry
WHERE computer_name = @p_workstation_name
   OR hostname_normalized = @p_workstation_name
ORDER BY CASE WHEN computer_name = @p_workstation_name THEN 0 ELSE 1 END
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
            return "Unable to save Local workcenters: workstation not found.";
        }

        var availableRows = await _mySqlHelperServer.ExecuteSqlQueryAsync(
            @"SELECT id, work_center_name
FROM setup_work_centers_catalog
WHERE is_active = 1;",
            new Dictionary<string, object?>(),
            MySqlDatabaseTarget.MtmWaitlist,
            cancellationToken).ConfigureAwait(false);

        var workCenterIdByName = availableRows
            .Select(row => new
            {
                Id = GetInt64(row, "id"),
                Name = GetValue(row, "work_center_name"),
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
                WorkCenterId = workCenterIdByName.TryGetValue(workCenterName, out var setupWorkstationId)
                    ? setupWorkstationId
                    : 0L,
            })
            .Where(item => item.WorkCenterId > 0)
            .ToArray();

        var connectionString = ResolveConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            StartupDebugLog.Info("WorkCenterCatalog", $"SaveHotWorkCentersAsync aborted. No database connection was available for workstation '{normalizedWorkstationName}'.");
            return "Unable to save Local workcenters: database connection is not configured.";
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
DELETE FROM config_computer_hot_work_centers
WHERE computer_id = @p_core_workstation_id;", connection, transaction))
            {
                deleteCommand.CommandTimeout = Math.Max(1, _startupDatabaseOptions.ConnectionTimeoutSeconds);
                deleteCommand.Parameters.AddWithValue("@p_core_workstation_id", workstationId);
                _ = await deleteCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            if (resolvedHotWorkCenters.Length > 0)
            {
                var insertSql = new StringBuilder();
                insertSql.AppendLine("INSERT INTO config_computer_hot_work_centers (");
                insertSql.AppendLine("    computer_id,");
                insertSql.AppendLine("    work_center_id,");
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
                    insertCommand.Parameters.AddWithValue($"@p_setup_workstation_id_{index}", item.WorkCenterId);
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
            return $"Unable to save Local workcenters: {ex.Message}";
        }
    }

    private async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetAvailableWorkCenterRowsAsync(CancellationToken cancellationToken)
    {
        var rows = await _mySqlHelperServer.ExecuteSqlQueryAsync(
            @"SELECT work_center_name, building, updated_utc
FROM setup_work_centers_catalog
WHERE is_active = 1
ORDER BY sort_rank ASC, work_center_name ASC;",
            new Dictionary<string, object?>(),
            MySqlDatabaseTarget.MtmWaitlist,
            cancellationToken).ConfigureAwait(false);

        return rows;
    }

    private async Task<string> ResolveCurrentComputerNameAsync(CancellationToken cancellationToken)
    {
        var key = GetCurrentComputerName();
        if (string.IsNullOrWhiteSpace(key))
        {
            return Environment.MachineName;
        }

        var rows = await _mySqlHelperServer.ExecuteSqlQueryAsync(
            @"SELECT computer_name
FROM core_computers_registry
WHERE computer_name = @p_workstation_name
   OR hostname_normalized = @p_workstation_name
ORDER BY CASE WHEN computer_name = @p_workstation_name THEN 0 ELSE 1 END
LIMIT 1;",
            new Dictionary<string, object?>
            {
                ["p_workstation_name"] = key,
            },
            MySqlDatabaseTarget.MtmWaitlist,
            cancellationToken).ConfigureAwait(false);

        var workstationName = GetValue(rows.FirstOrDefault(), "computer_name");
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

    private static DateTime? ParseUtcDateTime(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return DateTime.TryParse(
            raw,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var parsed)
                ? parsed
                : null;
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
