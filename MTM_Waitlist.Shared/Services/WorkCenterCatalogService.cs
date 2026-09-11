using Microsoft.Extensions.Options;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Shared.Models;
using MTM_Waitlist.Module_Core.Models;
using MySqlConnector;
using System.Globalization;

namespace MTM_Waitlist.Module_Shared.Services;

public sealed class WorkCenterCatalogService : IWorkCenterCatalogService
{
    /// <summary>One computer's active hot work centers, with the catalog's display order.</summary>
    private const string HotWorkCentersGetProcedure = "sp_config_hot_workcenters_get_for_computer";

    /// <summary>Clears one computer's hot work centers.</summary>
    private const string HotWorkCentersDeleteProcedure = "sp_config_hot_workcenters_delete_for_computer";

    /// <summary>Inserts or reactivates one hot work-center assignment.</summary>
    private const string HotWorkCentersUpsertProcedure = "sp_config_hot_workcenters_upsert";

    /// <summary>The active work-center catalog, ordered by the catalog's own display order.</summary>
    private const string WorkCentersGetAllProcedure = "sp_setup_work_centers_get_all";

    /// <summary>The registry's registered machines, in display order — the computer picker's source.</summary>
    private const string RegisteredComputersProcedure = "sp_core_computers_registry_registered_get";

    /// <summary>Resolves one registry row from a computer name or its normalized hostname.</summary>
    private const string ComputerLookupByNameProcedure = "sp_core_computers_registry_lookup_by_name_get";

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

    public async Task<IReadOnlyList<ComputerOption>> GetAvailableComputersAsync(CancellationToken cancellationToken = default)
    {
        // Registered machines only: the picker must not offer one an operator has retired, which is why this
        // is not sp_core_computers_registry_get_all (that one serves the registry editor and returns every row).
        var rows = await _mySqlHelperServer.ExecuteStoredProcedureQueryAsync(
            RegisteredComputersProcedure,
            new Dictionary<string, object?>(),
            MySqlDatabaseTarget.MtmWaitlist,
            cancellationToken).ConfigureAwait(false);

        var computers = rows
            .Select(row => new ComputerOption
            {
                Key = GetValue(row, "computer_name"),
                Label = new ComputerRecord
                {
                    ComputerName = GetValue(row, "computer_name"),
                    DisplayName = GetValue(row, "display_name"),
                }.GetDisplayLabel(),
            })
            .Where(option => !string.IsNullOrWhiteSpace(option.Key))
            .GroupBy(option => option.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(option => option.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var currentWorkstation = await ResolveCurrentComputerNameAsync(cancellationToken).ConfigureAwait(false);
        if (!computers.Any(option => string.Equals(option.Key, currentWorkstation, StringComparison.OrdinalIgnoreCase)))
        {
            computers.Insert(0, new ComputerOption { Key = currentWorkstation, Label = currentWorkstation });
        }

        return computers;
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

        // Historical note (FR-015): this read used to carry its own JOIN statement text. The procedure is
        // that same statement — same joins, same is_active filters, same sort_rank ordering, same two
        // output columns — so nothing but the call site changed. The procedure is named "_for_computer"
        // because the table it filters on is core_computers_registry.
        var hotRows = await _mySqlHelperServer.ExecuteStoredProcedureQueryAsync(
            HotWorkCentersGetProcedure,
            new Dictionary<string, object?>
            {
                ["p_computer_name"] = normalizedWorkstationName,
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

        var workstationRows = await _mySqlHelperServer.ExecuteStoredProcedureQueryAsync(
            ComputerLookupByNameProcedure,
            new Dictionary<string, object?>
            {
                ["p_name"] = normalizedWorkstationName,
            },
            MySqlDatabaseTarget.MtmWaitlist,
            cancellationToken).ConfigureAwait(false);

        var workstationId = GetInt64(workstationRows.FirstOrDefault(), "id");
        if (workstationId <= 0)
        {
            StartupDebugLog.Info("WorkCenterCatalog", $"SaveHotWorkCentersAsync aborted. Workstation '{normalizedWorkstationName}' was not found.");
            return "Unable to save Local workcenters: workstation not found.";
        }

        // The same procedure also serves the available-work-center list below: it returns id, building,
        // work_center_name, is_active and updated_utc over the active-work-center view.
        var availableRows = await _mySqlHelperServer.ExecuteStoredProcedureQueryAsync(
            WorkCentersGetAllProcedure,
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

            // The clear and the re-assignment stay in one transaction so a failure between them cannot
            // leave the computer with no hot work centers. Both are procedure calls now: the delete is
            // that same statement, and the upsert is that same INSERT ... ON DUPLICATE KEY UPDATE done one
            // row at a time, which is the only shape the procedure has (FR-015).
            await using (var deleteCommand = new MySqlCommand(HotWorkCentersDeleteProcedure, connection, transaction)
            {
                CommandType = System.Data.CommandType.StoredProcedure,
            })
            {
                deleteCommand.CommandTimeout = Math.Max(1, _startupDatabaseOptions.ConnectionTimeoutSeconds);
                deleteCommand.Parameters.AddWithValue("@p_computer_id", workstationId);
                _ = await deleteCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            foreach (var item in resolvedHotWorkCenters)
            {
                await using var upsertCommand = new MySqlCommand(HotWorkCentersUpsertProcedure, connection, transaction)
                {
                    CommandType = System.Data.CommandType.StoredProcedure,
                };

                upsertCommand.CommandTimeout = Math.Max(1, _startupDatabaseOptions.ConnectionTimeoutSeconds);
                upsertCommand.Parameters.AddWithValue("@p_computer_id", workstationId);
                upsertCommand.Parameters.AddWithValue("@p_work_center_id", item.WorkCenterId);
                upsertCommand.Parameters.AddWithValue("@p_sort_rank", item.SortRank);
                upsertCommand.Parameters.AddWithValue("@p_modified_by_user_id", DBNull.Value);

                _ = await upsertCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
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
        // The active-work-center catalog, in the catalog's own display order (FR-015).
        var rows = await _mySqlHelperServer.ExecuteStoredProcedureQueryAsync(
            WorkCentersGetAllProcedure,
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

        var rows = await _mySqlHelperServer.ExecuteStoredProcedureQueryAsync(
            ComputerLookupByNameProcedure,
            new Dictionary<string, object?>
            {
                ["p_name"] = key,
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

        if (value is bool boolValue)
        {
            return boolValue ? 1 : 0;
        }

        try
        {
            return Convert.ToInt64(value);
        }
        catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
        {
            return 0;
        }
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
