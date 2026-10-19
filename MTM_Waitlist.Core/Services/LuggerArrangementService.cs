using System.Globalization;

using Microsoft.Extensions.Options;
using MySqlConnector;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Core.Services;

/// <summary>
/// The connection string this feature's writes use, resolved exactly the way every other reader and writer in the
/// application resolves one.
/// </summary>
/// <remarks>
/// <para>
/// The configured host is the shared plant server, so a workstation off the plant network cannot reach it while the
/// same databases are present on the local server. Every other writer in the application answers that with
/// <see cref="MySqlHostFallback"/>; this one did not, and the omission was invisible until someone made a change:
/// the arrangement read fine — the read goes through the helper, which does fall back — and then every write timed
/// out against the plant host. Resolving here the same way keeps the two paths from disagreeing about which server
/// the arrangement lives on.
/// </para>
/// <para>
/// The database is named rather than taken from the string, so a connection string that points at something else
/// can never send an arrangement write into the wrong store.
/// </para>
/// </remarks>
internal static class LuggerWriteConnection
{
    /// <summary>The store the arrangement lives in.</summary>
    internal const string DatabaseName = "mtm_waitlist";

    /// <summary>
    /// The connection string to open, with the host fallback applied, or <see langword="null" /> when nothing is
    /// configured.
    /// </summary>
    /// <param name="configured">The connection string the options or the environment named.</param>
    internal static string? Resolve(string? configured)
    {
        if (string.IsNullOrWhiteSpace(configured))
        {
            return null;
        }

        var builder = new MySqlConnectionStringBuilder(configured)
        {
            Database = DatabaseName,
        };

        return MySqlHostFallback.Apply(builder.ConnectionString) ?? builder.ConnectionString;
    }

    /// <summary>
    /// The same decision with the reachability check supplied by the caller, so both branches can be exercised
    /// without opening a socket.
    /// </summary>
    /// <param name="configured">The connection string the options or the environment named.</param>
    /// <param name="isReachable">Reports whether a server is listening on the given host and port.</param>
    internal static string? Resolve(string? configured, Func<string, uint, bool> isReachable)
    {
        if (string.IsNullOrWhiteSpace(configured))
        {
            return null;
        }

        var builder = new MySqlConnectionStringBuilder(configured)
        {
            Database = DatabaseName,
        };

        return MySqlHostFallback.Apply(builder.ConnectionString, isReachable) ?? builder.ConnectionString;
    }
}
/// <inheritdoc cref="ILuggerArrangementService"/>
/// <remarks>
/// <para>
/// <b>Reads go through <see cref="IMySqlHelperServer"/>; writes open the connection here.</b> The helper answers a
/// failed call with its neutral result and retries it, which is right for a read a screen can show as unavailable.
/// It is wrong for a write twice over: a refusal the procedures raise with <c>SIGNAL SQLSTATE '45000'</c> would be
/// folded into "nothing matched", reporting a refused change as one that landed, and a write retried after a
/// dropped connection is how one person's one act becomes two. The same reasoning, and the same shape, is used by
/// <see cref="PermissionAdministrationService"/> and <see cref="UserManagementRepository"/>.
/// </para>
/// <para>
/// <b>Nothing here composes statement text.</b> Every call names a stored procedure and passes parameters
/// (constitution III). The write procedures answer one row, <c>affected_rows</c>, as their last statement because a
/// transaction's <c>COMMIT</c> is what the provider would otherwise report; reading that row is how a lost race
/// answers 0 rather than a false success.
/// </para>
/// </remarks>
public sealed class LuggerArrangementService : ILuggerArrangementService
{
    private const string ArrangementListProcedure = "sp_waitlist_lugger_arrangement_list";
    private const string LocationsListProcedure = "sp_waitlist_lugger_locations_list";
    private const string LocationsInsertProcedure = "sp_waitlist_lugger_locations_insert";
    private const string LocationsRenameProcedure = "sp_waitlist_lugger_locations_rename";
    private const string LocationsPositionSetProcedure = "sp_waitlist_lugger_locations_position_set";
    private const string LocationsDeleteProcedure = "sp_waitlist_lugger_locations_delete";
    private const string LocationsStatusSetProcedure = "sp_waitlist_lugger_locations_status_set";
    private const string LocationsPromoteProcedure = "sp_waitlist_lugger_locations_promote";
    private const string AssignmentsListProcedure = "sp_waitlist_lugger_scrap_assignments_list";
    private const string AssignmentsSetProcedure = "sp_waitlist_lugger_scrap_assignments_set";
    private const string AssignmentsDeleteProcedure = "sp_waitlist_lugger_scrap_assignments_delete";
    private const string ChangesListProcedure = "sp_waitlist_lugger_arrangement_changes_list";

    private const string WaitlistConnectionStringEnvironmentVariable = "MTM_WAITLIST_DB_CONNECTION_STRING";
    private const int DefaultCommandTimeoutSeconds = 15;

    /// <summary>A refusal this feature's procedures raise with <c>SIGNAL SQLSTATE '45000'</c>.</summary>
    private const int SignalledRefusalErrorNumber = 1644;

    /// <summary>MySQL's "table doesn't exist": the store is behind the build rather than refusing the change.</summary>
    private const int MissingTableErrorNumber = 1146;

    /// <summary>MySQL's "procedure does not exist", reported the same way as a missing table.</summary>
    private const int MissingProcedureErrorNumber = 1305;

    private const string LogModule = "LuggerArrangement";

    private readonly IMySqlHelperServer _mySqlHelperServer;
    private readonly WaitlistDatabaseOptions _waitlistDatabaseOptions;

    public LuggerArrangementService(
        IMySqlHelperServer mySqlHelperServer,
        IOptions<WaitlistDatabaseOptions> waitlistDatabaseOptions)
    {
        ArgumentNullException.ThrowIfNull(mySqlHelperServer);
        ArgumentNullException.ThrowIfNull(waitlistDatabaseOptions);

        _mySqlHelperServer = mySqlHelperServer;
        _waitlistDatabaseOptions = waitlistDatabaseOptions.Value;
    }

    /// <inheritdoc />
    public async Task<LuggerArrangementSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var rows = await ReadAsync(ArrangementListProcedure, new Dictionary<string, object?>(), cancellationToken)
            .ConfigureAwait(false);

        if (rows is null)
        {
            return LuggerArrangementSnapshot.Empty;
        }

        return LuggerArrangementSnapshot.Create(rows.Select(MapArrangementRow));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Lugger>> GetLuggersAsync(CancellationToken cancellationToken = default)
    {
        var rows = await ReadAsync(LocationsListProcedure, new Dictionary<string, object?>(), cancellationToken)
            .ConfigureAwait(false);

        return rows is null
            ? Array.Empty<Lugger>()
            : rows.Select(MapLugger).ToArray();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ScrapAssignment>> GetAssignmentsAsync(
        string scrapTypeKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scrapTypeKey);

        var rows = await ReadAsync(
                AssignmentsListProcedure,
                new Dictionary<string, object?> { ["p_scrap_type_key"] = scrapTypeKey },
                cancellationToken)
            .ConfigureAwait(false);

        return rows is null
            ? Array.Empty<ScrapAssignment>()
            : rows.Select(MapAssignmentRow).ToArray();
    }

    /// <inheritdoc />
    public async Task<long> GetChangeWatermarkAsync(CancellationToken cancellationToken = default)
    {
        var rows = await ReadAsync(
                ChangesListProcedure,
                new Dictionary<string, object?> { ["p_after_id"] = null },
                cancellationToken)
            .ConfigureAwait(false);

        return rows is { Count: > 0 } ? ReadLong(rows[0], "id") : 0L;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ArrangementChange>> GetChangesAfterAsync(
        long afterId,
        CancellationToken cancellationToken = default)
    {
        var rows = await ReadAsync(
                ChangesListProcedure,
                new Dictionary<string, object?> { ["p_after_id"] = afterId },
                cancellationToken)
            .ConfigureAwait(false);

        return rows is null
            ? Array.Empty<ArrangementChange>()
            : rows.Select(MapChange).ToArray();
    }

    /// <inheritdoc />
    public Task<int> AddLuggerAsync(
        string luggerName,
        int displayPosition,
        string actorEmployeeNumber,
        string actorEmployeeName,
        CancellationToken cancellationToken = default) =>
        ExecuteWriteAsync(
            LocationsInsertProcedure,
            new Dictionary<string, object?>
            {
                ["p_lugger_name"] = luggerName,
                ["p_display_position"] = displayPosition,
                ["p_actor_employee_number"] = actorEmployeeNumber,
                ["p_actor_employee_name"] = actorEmployeeName,
            },
            cancellationToken);

    /// <inheritdoc />
    public Task<int> RenameLuggerAsync(
        string luggerPublicId,
        string luggerName,
        string actorEmployeeNumber,
        string actorEmployeeName,
        CancellationToken cancellationToken = default) =>
        ExecuteWriteAsync(
            LocationsRenameProcedure,
            new Dictionary<string, object?>
            {
                ["p_lugger_public_id"] = luggerPublicId,
                ["p_lugger_name"] = luggerName,
                ["p_actor_employee_number"] = actorEmployeeNumber,
                ["p_actor_employee_name"] = actorEmployeeName,
            },
            cancellationToken);

    /// <inheritdoc />
    public Task<int> SetLuggerPositionAsync(
        string luggerPublicId,
        int displayPosition,
        string actorEmployeeNumber,
        string actorEmployeeName,
        CancellationToken cancellationToken = default) =>
        ExecuteWriteAsync(
            LocationsPositionSetProcedure,
            new Dictionary<string, object?>
            {
                ["p_lugger_public_id"] = luggerPublicId,
                ["p_display_position"] = displayPosition,
                ["p_actor_employee_number"] = actorEmployeeNumber,
                ["p_actor_employee_name"] = actorEmployeeName,
            },
            cancellationToken);

    /// <inheritdoc />
    public Task<int> RemoveLuggerAsync(
        string luggerPublicId,
        string actorEmployeeNumber,
        string actorEmployeeName,
        CancellationToken cancellationToken = default) =>
        ExecuteWriteAsync(
            LocationsDeleteProcedure,
            new Dictionary<string, object?>
            {
                ["p_lugger_public_id"] = luggerPublicId,
                ["p_actor_employee_number"] = actorEmployeeNumber,
                ["p_actor_employee_name"] = actorEmployeeName,
            },
            cancellationToken);

    /// <inheritdoc />
    public Task<int> SetStatusAsync(
        string luggerPublicId,
        string status,
        string? reason,
        string actorEmployeeNumber,
        string actorEmployeeName,
        CancellationToken cancellationToken = default) =>
        ExecuteWriteAsync(
            LocationsStatusSetProcedure,
            new Dictionary<string, object?>
            {
                ["p_lugger_public_id"] = luggerPublicId,
                ["p_status"] = status,
                ["p_reason"] = reason,
                ["p_actor_employee_number"] = actorEmployeeNumber,
                ["p_actor_employee_name"] = actorEmployeeName,
            },
            cancellationToken);

    /// <inheritdoc />
    public Task<int> PromoteLuggerAsync(
        string luggerPublicId,
        string scrapTypeKey,
        string actorEmployeeNumber,
        string actorEmployeeName,
        CancellationToken cancellationToken = default) =>
        ExecuteWriteAsync(
            LocationsPromoteProcedure,
            new Dictionary<string, object?>
            {
                ["p_lugger_public_id"] = luggerPublicId,
                ["p_scrap_type_key"] = scrapTypeKey,
                ["p_actor_employee_number"] = actorEmployeeNumber,
                ["p_actor_employee_name"] = actorEmployeeName,
            },
            cancellationToken);

    /// <inheritdoc />
    public Task<int> AssignScrapTypeAsync(
        string scrapType,
        string luggerPublicId,
        int sortPosition,
        string actorEmployeeNumber,
        string actorEmployeeName,
        CancellationToken cancellationToken = default) =>
        ExecuteWriteAsync(
            AssignmentsSetProcedure,
            new Dictionary<string, object?>
            {
                ["p_scrap_type"] = scrapType,
                ["p_lugger_public_id"] = luggerPublicId,
                ["p_sort_position"] = sortPosition,
                ["p_actor_employee_number"] = actorEmployeeNumber,
                ["p_actor_employee_name"] = actorEmployeeName,
            },
            cancellationToken);

    /// <inheritdoc />
    public Task<int> UnassignScrapTypeAsync(
        string scrapTypeKey,
        string luggerPublicId,
        string actorEmployeeNumber,
        string actorEmployeeName,
        CancellationToken cancellationToken = default) =>
        ExecuteWriteAsync(
            AssignmentsDeleteProcedure,
            new Dictionary<string, object?>
            {
                ["p_scrap_type_key"] = scrapTypeKey,
                ["p_lugger_public_id"] = luggerPublicId,
                ["p_actor_employee_number"] = actorEmployeeNumber,
                ["p_actor_employee_name"] = actorEmployeeName,
            },
            cancellationToken);

    /// <summary>
    /// Reads through the seam, answering <c>null</c> when the store cannot be reached so the caller can tell an
    /// outage from an empty arrangement.
    /// </summary>
    private async Task<IReadOnlyList<Dictionary<string, object?>>?> ReadAsync(
        string procedureName,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _mySqlHelperServer
                .ExecuteStoredProcedureQueryAsync(
                    procedureName,
                    parameters,
                    MySqlDatabaseTarget.MtmWaitlist,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            AppLog.Error(
                LogModule,
                ex,
                $"'{procedureName}' could not be read; the arrangement is reported as unavailable rather than as empty.");
            return null;
        }
    }

    /// <summary>
    /// Runs one write and answers its <c>affected_rows</c>, surfacing the store's refusal as
    /// <see cref="LuggerArrangementRefusalException"/>.
    /// </summary>
    private async Task<int> ExecuteWriteAsync(
        string procedureName,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        var connectionString = ResolveConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("The waitlist store is not configured.");
        }

        try
        {
            await using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var command = BuildCommand(connection, procedureName, parameters);

            var answer = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return answer is null or DBNull
                ? 0
                : Convert.ToInt32(answer, CultureInfo.InvariantCulture);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (MySqlException ex) when (ex.Number == SignalledRefusalErrorNumber)
        {
            throw new LuggerArrangementRefusalException(ex.Message, ex);
        }
        catch (MySqlException ex) when (ex.Number is MissingTableErrorNumber or MissingProcedureErrorNumber)
        {
            // The procedure exists in this build and not in the store: a deployment gap, not a refusal and not a
            // store that is down, so it is reported as the one thing that will fix it.
            AppLog.Error(LogModule, ex, $"'{procedureName}' is missing from the waitlist store; the schema is behind this build.");

            throw new LuggerArrangementStoreOutOfDateException(ex.Message, ex);
        }
    }

    private static MySqlCommand BuildCommand(
        MySqlConnection connection,
        string procedureName,
        IReadOnlyDictionary<string, object?> parameters)
    {
        var command = new MySqlCommand(procedureName, connection)
        {
            CommandType = System.Data.CommandType.StoredProcedure,
            CommandTimeout = DefaultCommandTimeoutSeconds,
        };

        foreach (var parameter in parameters)
        {
            command.Parameters.AddWithValue("@" + parameter.Key, parameter.Value ?? DBNull.Value);
        }

        return command;
    }

    /// <summary>
    /// The store this service writes to. The environment variable wins, then the variable the waitlist database
    /// options name, then the configured string, so a test can point every store at a live server without editing
    /// appsettings.
    /// </summary>
    private string? ResolveConnectionString()
    {
        var fromEnvironment = Environment.GetEnvironmentVariable(WaitlistConnectionStringEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return LuggerWriteConnection.Resolve(fromEnvironment);
        }

        var configuredName = _waitlistDatabaseOptions.ConnectionStringEnvironmentVariable;
        if (!string.IsNullOrWhiteSpace(configuredName))
        {
            var fromConfiguredName = Environment.GetEnvironmentVariable(configuredName);
            if (!string.IsNullOrWhiteSpace(fromConfiguredName))
            {
                return LuggerWriteConnection.Resolve(fromConfiguredName);
            }
        }

        return string.IsNullOrWhiteSpace(_waitlistDatabaseOptions.ConnectionString)
            ? null
            : LuggerWriteConnection.Resolve(_waitlistDatabaseOptions.ConnectionString);
    }

    private static Lugger MapLugger(IReadOnlyDictionary<string, object?> row) => new()
    {
        Id = ReadLong(row, "id"),
        PublicId = ReadString(row, "public_id"),
        Name = ReadString(row, "lugger_name"),
        Status = ReadString(row, "status"),
        StatusReason = ReadNullableString(row, "status_reason"),
        DisplayPosition = (int)ReadLong(row, "display_position"),
        CreatedUtc = ReadUtc(row, "created_utc"),
        UpdatedUtc = ReadUtc(row, "updated_utc"),
    };

    private static ScrapAssignment MapArrangementRow(IReadOnlyDictionary<string, object?> row) => new()
    {
        AssignmentPublicId = ReadNullableString(row, "assignment_public_id"),
        LuggerPublicId = ReadString(row, "lugger_public_id"),
        LuggerName = ReadString(row, "lugger_name"),
        LuggerStatus = ReadString(row, "status"),
        LuggerStatusReason = ReadNullableString(row, "status_reason"),
        LuggerDisplayPosition = ReadNullableInt(row, "lugger_display_position"),
        ScrapType = ReadNullableString(row, "scrap_type"),
        ScrapTypeKey = ReadNullableString(row, "scrap_type_key"),
        IsDumpPoint = ReadNullableBool(row, "is_dump_point"),
        SortPosition = ReadNullableInt(row, "assignment_sort_position"),
    };

    private static ScrapAssignment MapAssignmentRow(IReadOnlyDictionary<string, object?> row) => new()
    {
        AssignmentId = ReadLong(row, "id"),
        AssignmentPublicId = ReadNullableString(row, "public_id"),
        LuggerPublicId = ReadString(row, "lugger_public_id"),
        LuggerName = ReadString(row, "lugger_name"),
        LuggerStatus = ReadString(row, "status"),
        LuggerStatusReason = ReadNullableString(row, "status_reason"),
        ScrapType = ReadNullableString(row, "scrap_type"),
        ScrapTypeKey = ReadNullableString(row, "scrap_type_key"),
        IsDumpPoint = ReadNullableBool(row, "is_dump_point"),
        SortPosition = ReadNullableInt(row, "sort_position"),
    };

    private static ArrangementChange MapChange(IReadOnlyDictionary<string, object?> row) => new()
    {
        Id = ReadLong(row, "id"),
        PublicId = ReadString(row, "public_id"),
        ChangeKind = ReadString(row, "change_kind"),
        LuggerName = ReadString(row, "lugger_name"),
        ScrapType = ReadNullableString(row, "scrap_type"),
        PreviousValue = ReadNullableString(row, "previous_value"),
        NewValue = ReadNullableString(row, "new_value"),
        ChangeReason = ReadNullableString(row, "change_reason"),
        ChangedByEmployeeName = ReadString(row, "changed_by_employee_name"),
        ChangedUtc = ReadUtc(row, "changed_utc"),
    };

    private static string ReadString(IReadOnlyDictionary<string, object?> row, string columnName) =>
        ReadNullableString(row, columnName) ?? string.Empty;

    private static string? ReadNullableString(IReadOnlyDictionary<string, object?> row, string columnName) =>
        row.TryGetValue(columnName, out var value) && value is not null and not DBNull
            ? Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty
            : null;

    private static long ReadLong(IReadOnlyDictionary<string, object?> row, string columnName) =>
        row.TryGetValue(columnName, out var value) && value is not null and not DBNull
            ? Convert.ToInt64(value, CultureInfo.InvariantCulture)
            : 0L;

    private static int? ReadNullableInt(IReadOnlyDictionary<string, object?> row, string columnName) =>
        row.TryGetValue(columnName, out var value) && value is not null and not DBNull
            ? Convert.ToInt32(value, CultureInfo.InvariantCulture)
            : null;

    private static bool? ReadNullableBool(IReadOnlyDictionary<string, object?> row, string columnName) =>
        row.TryGetValue(columnName, out var value) && value is not null and not DBNull
            ? Convert.ToInt64(value, CultureInfo.InvariantCulture) != 0
            : null;

    private static DateTime ReadUtc(IReadOnlyDictionary<string, object?> row, string columnName) =>
        row.TryGetValue(columnName, out var value) && value is not null and not DBNull
            ? DateTime.SpecifyKind(Convert.ToDateTime(value, CultureInfo.InvariantCulture), DateTimeKind.Utc)
            : default;
}
