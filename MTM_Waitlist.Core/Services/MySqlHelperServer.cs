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

public sealed class MySqlHelperServer : IMySqlHelperServer, IReviewedQueueScriptReader
{
    /// <summary>
    /// The most rows one read returns. Shared by the stored-procedure path and the reviewed script route, so a
    /// procedure that answers more than a screen can use is bounded in one place rather than in each caller.
    /// </summary>
    /// <remarks>
    /// A read that legitimately needs more than this is a read that has to page, not a cap to raise: a list
    /// that cannot be shown in one screen cannot be shown better by making the response slower.
    /// </remarks>
    internal const int MaxRowsPerRead = 10_000;

    private const string WaitlistConnectionStringEnvironmentVariable = "MTM_WAITLIST_DB_CONNECTION_STRING";
    private const string WaitlistStartupConnectionStringEnvironmentVariable = "MTM_WAITLIST_STARTUP_DB_CONNECTION_STRING";
    private const string ReceivingConnectionStringEnvironmentVariable = "MTM_RECEIVING_APPLICATION_DB_CONNECTION_STRING";
    private const string WipApplicationConnectionStringEnvironmentVariable = "MTM_WIP_APPLICATION_DB_CONNECTION_STRING";
    private const string MockConnectionStringEnvironmentVariable = "MTM_MOCK_DB_CONNECTION_STRING";
    private const int DefaultCommandTimeoutSeconds = 15;

    private readonly WaitlistDatabaseOptions _waitlistDatabaseOptions;
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
        IOptions<WaitlistDatabaseOptions>? waitlistDatabaseOptions = null,
        IOptions<ReceivingDatabaseOptions>? receivingDatabaseOptions = null,
        IStoreAvailabilityTracker? storeAvailability = null,
        InternalStoreRetryPolicy? retryPolicy = null)
    {
        _waitlistDatabaseOptions = waitlistDatabaseOptions?.Value ?? new WaitlistDatabaseOptions();
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

    /// <inheritdoc />
    /// <remarks>
    /// The connection open is not retried either, even though it precedes any transaction: a strict caller is a
    /// write or a decision, and its own exceptions are what its caller shows. Nothing is logged here that the
    /// caller does not already log, so one failure is reported once.
    /// </remarks>
    public async Task<T> ExecuteStrictAsync<T>(
        MySqlStrictScopeRequest request,
        Func<IMySqlStrictScope, CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var connectionString = ResolveConnectionString(request.Target);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new MySqlStoreNotConfiguredException(GetDatabaseName(request.Target));
        }

        if (request.ConnectionTimeoutSeconds is { } connectionTimeoutSeconds)
        {
            connectionString = new MySqlConnectionStringBuilder(connectionString)
            {
                ConnectionTimeout = (uint)Math.Max(1, connectionTimeoutSeconds),
            }.ConnectionString;
        }

        var commandTimeoutSeconds = request.CommandTimeoutSeconds is { } requestedCommandTimeout
            ? Math.Max(1, requestedCommandTimeout)
            : DefaultCommandTimeoutSeconds;

        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        MySqlTransaction? transaction = null;
        try
        {
            if (request.UseTransaction)
            {
                transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            }

            var result = await operation(
                    new MySqlStrictScope(connection, transaction, commandTimeoutSeconds),
                    cancellationToken)
                .ConfigureAwait(false);

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }

            return result;
        }
        catch
        {
            if (transaction is not null)
            {
                await RollBackQuietlyAsync(transaction).ConfigureAwait(false);
            }

            throw;
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    public async Task<IReadOnlyList<Dictionary<string, object?>>?> ExecuteReviewedScriptQueryAsync(
        string reviewedScriptName,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default)
    {
        // simplified: reviewed queue scripts stay checked-in text instead of stored procedures, because
        // converting them is database authoring this feature does not do. When a procedure exists for the same
        // read, route to it instead of adding a name here, and drop the name from the reviewed set.
        var script = await WaitlistWipMySqlScriptStore
            .LoadAsync(reviewedScriptName, cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(script))
        {
            AppLog.Info(
                "MySqlHelperServer",
                $"Reviewed script '{reviewedScriptName}' is not installed or is not in the reviewed set; the read was refused.");
            return null;
        }

        return await ExecuteWithBoundedRetryAsync(
            nameof(ExecuteReviewedScriptQueryAsync),
            $"ReviewedScript='{reviewedScriptName}'",
            MySqlDatabaseTarget.MtmWipApplication,
            (IReadOnlyList<Dictionary<string, object?>>?)null,
            async (connection, token) =>
            {
                await using var command = CreateTextCommand(script, parameters, connection);
                return await ReadRowsAsync(command, token).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Runs statement text for the test project's database fixtures. Not reachable from application code.
    /// </summary>
    /// <remarks>
    /// The integration suites seed and inspect rows with raw statements so a converted call site can be proven
    /// against a live server; that is fixture setup, not application behaviour. Those suites reach this only
    /// because the test assembly is a friend of this one, so no service in the application can call it. See
    /// <see cref="MaxRowsPerRead"/> and <see cref="ExecuteReviewedScriptQueryAsync"/> for the two routes the
    /// application itself has.
    /// </remarks>
    internal Task<int> ExecuteFixtureStatementAsync(
        string sql,
        IReadOnlyDictionary<string, object?> parameters,
        MySqlDatabaseTarget databaseTarget,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        return ExecuteWithBoundedRetryAsync(
            nameof(ExecuteFixtureStatementAsync),
            $"Sql='{DescribeSql(sql)}'",
            databaseTarget,
            0,
            async (connection, token) =>
            {
                await using var command = CreateTextCommand(sql, parameters, connection);
                return await command.ExecuteNonQueryAsync(token).ConfigureAwait(false);
            },
            cancellationToken);
    }

    /// <summary>
    /// Reads rows from statement text for the test project's database fixtures. Not reachable from application
    /// code; see <see cref="ExecuteFixtureStatementAsync"/>.
    /// </summary>
    internal Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteFixtureQueryAsync(
        string sql,
        IReadOnlyDictionary<string, object?> parameters,
        MySqlDatabaseTarget databaseTarget,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        return ExecuteWithBoundedRetryAsync(
            nameof(ExecuteFixtureQueryAsync),
            $"Sql='{DescribeSql(sql)}'",
            databaseTarget,
            Array.Empty<Dictionary<string, object?>>(),
            async (connection, token) =>
            {
                await using var command = CreateTextCommand(sql, parameters, connection);
                return await ReadRowsAsync(command, token).ConfigureAwait(false);
            },
            cancellationToken);
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
            AppLog.Info("MySqlHelperServer", $"{operationName} skipped: no connection configured for '{GetDatabaseName(databaseTarget)}'.");
            return unavailableResult;
        }

        var databaseName = GetDatabaseName(databaseTarget);

        for (var attempt = 1; ; attempt++)
        {
            var attemptedUtc = DateTime.UtcNow;

            try
            {
                AppLog.Info("MySqlHelperServer", $"{operationName} started. {statementDescription}, Target='{databaseName}', Attempt={attempt}.");

                await using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                var result = await operation(connection, cancellationToken).ConfigureAwait(false);
                RecordAvailability(() => _storeAvailability?.RecordAvailable(databaseTarget, attemptedUtc, attempt));
                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // A permanent answer is not an outage, and no delay makes it a different answer: the procedure,
                // table or database is not there, or this login may not use it. Asking three times turns one
                // honest failure into a three-second stall and still reports the wrong cause, so the attempt
                // budget is spent only where the budget can change the outcome.
                var permanentReason = MySqlFailureClassification.PermanentReason(ex);

                AppLog.Error(
                    "MySqlHelperServer",
                    ex,
                    $"{operationName} failed. {statementDescription}, Target='{databaseName}', Attempt={attempt} of "
                    + $"{(permanentReason is null ? _retryPolicy.MaxAttempts : 1)}.");

                if (permanentReason is not null)
                {
                    RecordAvailability(() => _storeAvailability?.RecordUnavailable(
                        databaseTarget,
                        attemptedUtc,
                        attempt,
                        DateTime.UtcNow + _retryPolicy.NextRetryDelay,
                        $"The {databaseName} store cannot answer this read: {permanentReason}."));
                    return unavailableResult;
                }

                if (attempt >= _retryPolicy.MaxAttempts)
                {
                    RecordAvailability(() => _storeAvailability?.RecordUnavailable(
                        databaseTarget,
                        attemptedUtc,
                        attempt,
                        DateTime.UtcNow + _retryPolicy.NextRetryDelay,
                        $"The {databaseName} store did not answer after {attempt} attempts."));
                    return unavailableResult;
                }

                await _retryPolicy.DelayBeforeRetryAsync(attempt, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Records a store observation without letting the observation decide the read's outcome.
    /// </summary>
    /// <remarks>
    /// Availability is this seam's side channel to the screen, not part of the read. A screen that throws
    /// while being told the state — a thread-affine XAML write, a disposed screen — must not turn a read that
    /// already succeeded into a failed operation or an extra attempt, and must not be reported as the store
    /// itself having refused. The fault is logged where it actually belongs instead of being mislabelled as a
    /// database error and retried.
    /// </remarks>
    private static void RecordAvailability(Action record)
    {
        try
        {
            record();
        }
        catch (Exception ex)
        {
            AppLog.Error(
                "MySqlHelperServer",
                ex,
                "The store availability observation could not be recorded, so the screen was not told. The read's own outcome is unaffected.");
        }
    }

    private static MySqlCommand CreateStoredProcedureCommand(
        string storedProcedureName,
        IReadOnlyDictionary<string, object?> parameters,
        MySqlConnection connection,
        MySqlTransaction? transaction = null,
        int commandTimeoutSeconds = DefaultCommandTimeoutSeconds)
    {
        var command = new MySqlCommand(storedProcedureName, connection, transaction)
        {
            CommandType = System.Data.CommandType.StoredProcedure,
            CommandTimeout = commandTimeoutSeconds,
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
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await ReadRowsBoundedAsync(reader, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads a result set into rows, stopping at <see cref="MaxRowsPerRead"/>.
    /// </summary>
    /// <param name="reader">The open reader.</param>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>The rows read, at most <see cref="MaxRowsPerRead"/>.</returns>
    /// <remarks>
    /// The cap is applied while reading rather than after, so an oversized answer never occupies memory it
    /// cannot be shown from. Reaching it is logged, because a truncated read that says nothing is
    /// indistinguishable from a complete one at the caller.
    /// </remarks>
    internal static async Task<IReadOnlyList<Dictionary<string, object?>>> ReadRowsBoundedAsync(
        System.Data.Common.DbDataReader reader,
        CancellationToken cancellationToken)
    {
        var rows = new List<Dictionary<string, object?>>();
        while (rows.Count < MaxRowsPerRead
            && await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < reader.FieldCount; index++)
            {
                var value = reader.IsDBNull(index) ? null : reader.GetValue(index);
                row[reader.GetName(index)] = value;
            }

            rows.Add(row);
        }

        if (rows.Count == MaxRowsPerRead)
        {
            AppLog.Warn(
                "MySqlHelperServer",
                $"The read reached the shared row cap of {MaxRowsPerRead}; the rows beyond it were not returned.");
        }

        return rows;
    }

    /// <summary>
    /// Rolls back a strict transaction without letting the rollback's own failure replace the failure that caused
    /// it. The caller is about to see the original exception; a connection that has already gone away must not
    /// turn a refusal into something else.
    /// </summary>
    private static async Task RollBackQuietlyAsync(MySqlTransaction transaction)
    {
        try
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            AppLog.Error(
                "MySqlHelperServer",
                ex,
                "The strict transaction could not be rolled back explicitly; the connection's own close is the remaining undo.");
        }
    }

    /// <summary>
    /// The strict scope: the same stored-procedure calls the lenient route makes, bound to one connection and,
    /// when the request asked for it, to that connection's transaction.
    /// </summary>
    private sealed class MySqlStrictScope : IMySqlStrictScope
    {
        private readonly MySqlConnection _connection;
        private readonly MySqlTransaction? _transaction;
        private readonly int _commandTimeoutSeconds;

        internal MySqlStrictScope(
            MySqlConnection connection,
            MySqlTransaction? transaction,
            int commandTimeoutSeconds)
        {
            _connection = connection;
            _transaction = transaction;
            _commandTimeoutSeconds = commandTimeoutSeconds;
        }

        public async Task<IReadOnlyList<Dictionary<string, object?>>> QueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(storedProcedureName);

            await using var command = CreateStoredProcedureCommand(
                storedProcedureName,
                parameters,
                _connection,
                _transaction,
                _commandTimeoutSeconds);

            return await ReadRowsAsync(command, cancellationToken).ConfigureAwait(false);
        }

        public async Task<int> NonQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(storedProcedureName);

            await using var command = CreateStoredProcedureCommand(
                storedProcedureName,
                parameters,
                _connection,
                _transaction,
                _commandTimeoutSeconds);

            return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<object?> ScalarAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(storedProcedureName);

            await using var command = CreateStoredProcedureCommand(
                storedProcedureName,
                parameters,
                _connection,
                _transaction,
                _commandTimeoutSeconds);

            var answer = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return answer is DBNull ? null : answer;
        }
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
            ?? ReadConfiguredEnvironmentVariable(_waitlistDatabaseOptions.ConnectionStringEnvironmentVariable)
            ?? _waitlistDatabaseOptions.ConnectionString?.Trim();

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

        // The single place a store's connection string is handed to the driver. In a published (Release) build
        // this is the configured server, unchanged, and a server that does not answer fails loudly; only a
        // development build may repoint it at this machine (StoreHostResolution).
        return StoreHostResolution.Apply(builder.ConnectionString);
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

    /// <summary>
    /// Reads an environment variable by a name the configuration supplied, tolerating a name that was left blank
    /// so a caller that names its own variable is served without a null-name failure.
    /// </summary>
    /// <param name="name">The variable name to read, or a blank value.</param>
    /// <returns>The value, or <see langword="null" /> when the name is blank or the variable is unset.</returns>
    private static string? ReadConfiguredEnvironmentVariable(string? name) =>
        string.IsNullOrWhiteSpace(name) ? null : Environment.GetEnvironmentVariable(name)?.Trim();

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
