using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using Microsoft.Extensions.Options;
using MTM_Waitlist.Module_Core.Models;
using MySqlConnector;

namespace MTM_Waitlist.Module_Core.Services;

public enum MySqlDatabaseTarget
{
    MtmWaitlist,
    MtmReceivingApplication,
    MtmWipApplication,

    /// <summary>
    /// The dedicated Infor Visual mirror cache. Read-only to the application: the only procedure it
    /// calls is <c>sp_visual_&lt;shape&gt;_get</c>. Never authoritative for internal application data (FR-027).
    /// </summary>
    MtmMock,
}

public sealed class MySqlHelperServer : IMySqlHelperServer
{
    private const string WaitlistConnectionStringEnvironmentVariable = "MTM_WAITLIST_DB_CONNECTION_STRING";
    private const string WaitlistStartupConnectionStringEnvironmentVariable = "MTM_WAITLIST_STARTUP_DB_CONNECTION_STRING";
    private const string ReceivingConnectionStringEnvironmentVariable = "MTM_RECEIVING_APPLICATION_DB_CONNECTION_STRING";
    private const string WipApplicationConnectionStringEnvironmentVariable = "MTM_WIP_APPLICATION_DB_CONNECTION_STRING";
    private const string MockConnectionStringEnvironmentVariable = "MTM_MOCK_DB_CONNECTION_STRING";
    private const int DefaultCommandTimeoutSeconds = 15;

    private readonly StartupDatabaseOptions _startupDatabaseOptions;
    private readonly ReceivingDatabaseOptions _receivingDatabaseOptions;
    private readonly IStoreAvailabilityTracker? _storeAvailability;
    private readonly InternalStoreRetryPolicy _retryPolicy;

    /// <summary>
    /// Creates the helper server.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This type previously took a local-settings service and a sample-data service purely so it could
    /// short-circuit a MySQL target to demo data behind a retired demo setting. Internal stores are never
    /// mocked (FR-001, constitution II), so the gate and both dependencies are gone.
    /// </para>
    /// <para>
    /// Every internal-store read now runs under a <b>bounded</b> retry policy — up to three attempts with
    /// delays of approximately 1 s, 2 s, and 4 s — and records its outcome in an optional
    /// <see cref="IStoreAvailabilityTracker"/>, which is what a screen reads to show its own
    /// <c>Unavailable</c> state with a manual retry (FR-021, `data-model.md` §10). Both are optional, so a
    /// caller that does not care (an integration test, a console probe) keeps the previous behaviour.
    /// </para>
    /// </remarks>
    public MySqlHelperServer(
        IOptions<StartupDatabaseOptions>? startupDatabaseOptions = null,
        IOptions<ReceivingDatabaseOptions>? receivingDatabaseOptions = null,
        IStoreAvailabilityTracker? storeAvailability = null,
        InternalStoreRetryPolicy? retryPolicy = null)
    {
        _startupDatabaseOptions = startupDatabaseOptions?.Value ?? new StartupDatabaseOptions();
        _receivingDatabaseOptions = receivingDatabaseOptions?.Value ?? new ReceivingDatabaseOptions();
        _storeAvailability = storeAvailability;
        _retryPolicy = retryPolicy ?? InternalStoreRetryPolicy.Default;
    }

    public async Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureQueryAsync(
        string storedProcedureName,
        IReadOnlyDictionary<string, object?> parameters,
        MySqlDatabaseTarget databaseTarget,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storedProcedureName);

        return await ExecuteWithBoundedRetryAsync(
            nameof(ExecuteStoredProcedureQueryAsync),
            $"Procedure='{storedProcedureName}'",
            databaseTarget,
            Array.Empty<Dictionary<string, object?>>(),
            async (connection, token) =>
            {
                await using var command = CreateStoredProcedureCommand(storedProcedureName, parameters, connection);
                return await ReadRowsAsync(command, token).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> ExecuteStoredProcedureNonQueryAsync(
        string storedProcedureName,
        IReadOnlyDictionary<string, object?> parameters,
        MySqlDatabaseTarget databaseTarget,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storedProcedureName);

        return await ExecuteWithBoundedRetryAsync(
            nameof(ExecuteStoredProcedureNonQueryAsync),
            $"Procedure='{storedProcedureName}'",
            databaseTarget,
            0,
            async (connection, token) =>
            {
                await using var command = CreateStoredProcedureCommand(storedProcedureName, parameters, connection);
                return await command.ExecuteNonQueryAsync(token).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteSqlQueryAsync(
        string sql,
        IReadOnlyDictionary<string, object?> parameters,
        MySqlDatabaseTarget databaseTarget,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        return await ExecuteWithBoundedRetryAsync(
            nameof(ExecuteSqlQueryAsync),
            $"Sql='{DescribeSql(sql)}'",
            databaseTarget,
            Array.Empty<Dictionary<string, object?>>(),
            async (connection, token) =>
            {
                await using var command = CreateTextCommand(sql, parameters, connection);
                return await ReadRowsAsync(command, token).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> ExecuteSqlNonQueryAsync(
        string sql,
        IReadOnlyDictionary<string, object?> parameters,
        MySqlDatabaseTarget databaseTarget,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        return await ExecuteWithBoundedRetryAsync(
            nameof(ExecuteSqlNonQueryAsync),
            $"Sql='{DescribeSql(sql)}'",
            databaseTarget,
            0,
            async (connection, token) =>
            {
                await using var command = CreateTextCommand(sql, parameters, connection);
                return await command.ExecuteNonQueryAsync(token).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Runs one operation against one internal store under the bounded retry policy, recording the outcome.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A store that is not configured at all is <b>not</b> an unavailable store: the seam returns its
    /// neutral result without contacting anything and without recording a state, because there is nothing to
    /// report an outage about. That keeps a machine with no receiving-store connection from showing a
    /// failure the operator cannot act on.
    /// </para>
    /// <para>
    /// Caller cancellation is propagated rather than retried or reported — the operator asked to stop.
    /// </para>
    /// </remarks>
    private async Task<T> ExecuteWithBoundedRetryAsync<T>(
        string operationName,
        string statementDescription,
        MySqlDatabaseTarget databaseTarget,
        T unavailableResult,
        Func<MySqlConnection, CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        var connectionString = ResolveConnectionString(databaseTarget);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            StartupDebugLog.Info("MySqlHelperServer", $"{operationName} skipped: no connection configured for '{GetDatabaseName(databaseTarget)}'.");
            return unavailableResult;
        }

        var databaseName = GetDatabaseName(databaseTarget);

        for (var attempt = 1; ; attempt++)
        {
            var attemptedUtc = DateTime.UtcNow;

            try
            {
                StartupDebugLog.Info("MySqlHelperServer", $"{operationName} started. {statementDescription}, Target='{databaseName}', Attempt={attempt}.");

                await using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                var result = await operation(connection, cancellationToken).ConfigureAwait(false);
                _storeAvailability?.RecordAvailable(databaseTarget, attemptedUtc, attempt);
                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                StartupDebugLog.Error(
                    "MySqlHelperServer",
                    ex,
                    $"{operationName} failed. {statementDescription}, Target='{databaseName}', Attempt={attempt} of {_retryPolicy.MaxAttempts}.");

                if (attempt >= _retryPolicy.MaxAttempts)
                {
                    _storeAvailability?.RecordUnavailable(
                        databaseTarget,
                        attemptedUtc,
                        attempt,
                        DateTime.UtcNow + _retryPolicy.NextRetryDelay,
                        $"The {databaseName} store did not answer after {attempt} attempts.");
                    return unavailableResult;
                }

                await _retryPolicy.DelayBeforeRetryAsync(attempt, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static MySqlCommand CreateStoredProcedureCommand(
        string storedProcedureName,
        IReadOnlyDictionary<string, object?> parameters,
        MySqlConnection connection)
    {
        var command = new MySqlCommand(storedProcedureName, connection)
        {
            CommandType = System.Data.CommandType.StoredProcedure,
            CommandTimeout = DefaultCommandTimeoutSeconds,
        };

        AddParameters(command, parameters);
        return command;
    }

    private static MySqlCommand CreateTextCommand(
        string sql,
        IReadOnlyDictionary<string, object?> parameters,
        MySqlConnection connection)
    {
        var command = new MySqlCommand(sql, connection)
        {
            CommandType = System.Data.CommandType.Text,
            CommandTimeout = DefaultCommandTimeoutSeconds,
        };

        AddParameters(command, parameters);
        return command;
    }

    private static void AddParameters(MySqlCommand command, IReadOnlyDictionary<string, object?> parameters)
    {
        foreach (var entry in parameters)
        {
            var parameterName = entry.Key.StartsWith("@", StringComparison.Ordinal) ? entry.Key : $"@{entry.Key}";
            _ = command.Parameters.AddWithValue(parameterName, entry.Value ?? DBNull.Value);
        }
    }

    private static async Task<IReadOnlyList<Dictionary<string, object?>>> ReadRowsAsync(
        MySqlCommand command,
        CancellationToken cancellationToken)
    {
        var rows = new List<Dictionary<string, object?>>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < reader.FieldCount; index++)
            {
                var value = reader.IsDBNull(index) ? null : reader.GetValue(index);
                row[reader.GetName(index)] = value;
            }

            rows.Add(row);
        }

        return rows;
    }

    private string? ResolveConnectionString(MySqlDatabaseTarget databaseTarget)
    {
        var environmentConnectionString = databaseTarget switch
        {
            MySqlDatabaseTarget.MtmWaitlist =>
                Environment.GetEnvironmentVariable(WaitlistConnectionStringEnvironmentVariable)?.Trim()
                ?? Environment.GetEnvironmentVariable(WaitlistStartupConnectionStringEnvironmentVariable)?.Trim(),
            MySqlDatabaseTarget.MtmReceivingApplication =>
                Environment.GetEnvironmentVariable(ReceivingConnectionStringEnvironmentVariable)?.Trim()
                ?? _receivingDatabaseOptions.ConnectionString?.Trim(),
            MySqlDatabaseTarget.MtmWipApplication =>
                Environment.GetEnvironmentVariable(WipApplicationConnectionStringEnvironmentVariable)?.Trim(),
            // The mock cache normally shares the MySQL host with the application stores, so when no
            // dedicated connection string is configured the waitlist host is reused with the database
            // name overridden to mtm_mock below.
            MySqlDatabaseTarget.MtmMock =>
                Environment.GetEnvironmentVariable(MockConnectionStringEnvironmentVariable)?.Trim(),
            _ => null,
        };

        var fallbackConnectionString = Environment.GetEnvironmentVariable(WaitlistConnectionStringEnvironmentVariable)?.Trim()
            ?? Environment.GetEnvironmentVariable(WaitlistStartupConnectionStringEnvironmentVariable)?.Trim()
            ?? _startupDatabaseOptions.ConnectionString?.Trim();

        var resolvedConnectionString = string.IsNullOrWhiteSpace(environmentConnectionString)
            ? fallbackConnectionString
            : environmentConnectionString;

        if (string.IsNullOrWhiteSpace(resolvedConnectionString))
        {
            return null;
        }

        var builder = new MySqlConnectionStringBuilder(resolvedConnectionString)
        {
            Database = GetDatabaseName(databaseTarget),
        };

        return builder.ConnectionString;
    }

    private static string GetDatabaseName(MySqlDatabaseTarget databaseTarget)
    {
        return databaseTarget switch
        {
            MySqlDatabaseTarget.MtmReceivingApplication => "mtm_receiving_application",
            MySqlDatabaseTarget.MtmWipApplication => "mtm_wip_application_winforms",
            MySqlDatabaseTarget.MtmMock => "mtm_mock",
            _ => "mtm_waitlist",
        };
    }

    private static string DescribeSql(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return "<empty>";
        }

        var trimmed = sql.Trim().ReplaceLineEndings(" ");
        return trimmed.Length <= 140 ? trimmed : trimmed[..140] + "...";
    }
}
