using MTM_Waitlist.Module_Core.Contracts.Services;
using Microsoft.Extensions.Options;
using MTM_Waitlist.Module_Startup.Models;
using MySqlConnector;

namespace MTM_Waitlist.Module_Core.Services;

public enum MySqlDatabaseTarget
{
    MtmWaitlist,
    MtmReceivingApplication,
}

public sealed class MySqlHelperServer
{
    private const string RecvMockDataSettingKey = "Feature.RecvMockData";
    private const string WaitlistConnectionStringEnvironmentVariable = "MTM_WAITLIST_DB_CONNECTION_STRING";
    private const string WaitlistStartupConnectionStringEnvironmentVariable = "MTM_WAITLIST_STARTUP_DB_CONNECTION_STRING";
    private const string ReceivingConnectionStringEnvironmentVariable = "MTM_RECEIVING_APPLICATION_DB_CONNECTION_STRING";
    private const int DefaultCommandTimeoutSeconds = 15;

    private readonly ILocalSettingsService _localSettingsService;
    private readonly ISampleDataService _sampleDataService;
    private readonly StartupDatabaseOptions _startupDatabaseOptions;

    public MySqlHelperServer(ILocalSettingsService localSettingsService, ISampleDataService sampleDataService)
        : this(localSettingsService, sampleDataService, Options.Create(new StartupDatabaseOptions()))
    {
    }

    public MySqlHelperServer(
        ILocalSettingsService localSettingsService,
        ISampleDataService sampleDataService,
        IOptions<StartupDatabaseOptions> startupDatabaseOptions)
    {
        _localSettingsService = localSettingsService;
        _sampleDataService = sampleDataService;
        _startupDatabaseOptions = startupDatabaseOptions?.Value ?? new StartupDatabaseOptions();
    }

    public async Task<IReadOnlyList<object>> ExecuteReadWriteAsync(string operationName, string? parameter = null)
    {
        var useMockData = await IsMockDataEnabledAsync(MySqlDatabaseTarget.MtmWaitlist).ConfigureAwait(false);
        if (useMockData)
        {
            return _sampleDataService.GetSampleOrders(parameter);
        }

        return Array.Empty<object>();
    }

    public async Task<T> ExecuteReadWriteAsync<T>(
        string operationName,
        string? parameter,
        Func<Task<T>> mockAction,
        Func<Task<T>> backendAction)
    {
        return await ExecuteReadWriteAsync(operationName, parameter, MySqlDatabaseTarget.MtmWaitlist, mockAction, backendAction).ConfigureAwait(false);
    }

    public async Task<T> ExecuteReadWriteAsync<T>(
        string operationName,
        string? parameter,
        MySqlDatabaseTarget databaseTarget,
        Func<Task<T>> mockAction,
        Func<Task<T>> backendAction)
    {
        var useMockData = await IsMockDataEnabledAsync(databaseTarget).ConfigureAwait(false);
        if (useMockData)
        {
            _ = _sampleDataService.GetSampleOrders(parameter);
            return await mockAction().ConfigureAwait(false);
        }

        return await backendAction().ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureQueryAsync(
        string storedProcedureName,
        IReadOnlyDictionary<string, object?> parameters,
        MySqlDatabaseTarget databaseTarget,
        CancellationToken cancellationToken = default)
    {
        var connectionString = ResolveConnectionString(databaseTarget);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return Array.Empty<Dictionary<string, object?>>();
        }

        try
        {
            await using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = new MySqlCommand(storedProcedureName, connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure,
                CommandTimeout = DefaultCommandTimeoutSeconds,
            };

            foreach (var entry in parameters)
            {
                var parameterName = entry.Key.StartsWith("@", StringComparison.Ordinal) ? entry.Key : $"@{entry.Key}";
                _ = command.Parameters.AddWithValue(parameterName, entry.Value ?? DBNull.Value);
            }

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
        catch
        {
            return Array.Empty<Dictionary<string, object?>>();
        }
    }

    public async Task<int> ExecuteStoredProcedureNonQueryAsync(
        string storedProcedureName,
        IReadOnlyDictionary<string, object?> parameters,
        MySqlDatabaseTarget databaseTarget,
        CancellationToken cancellationToken = default)
    {
        var connectionString = ResolveConnectionString(databaseTarget);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return 0;
        }

        try
        {
            await using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = new MySqlCommand(storedProcedureName, connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure,
                CommandTimeout = DefaultCommandTimeoutSeconds,
            };

            foreach (var entry in parameters)
            {
                var parameterName = entry.Key.StartsWith("@", StringComparison.Ordinal) ? entry.Key : $"@{entry.Key}";
                _ = command.Parameters.AddWithValue(parameterName, entry.Value ?? DBNull.Value);
            }

            return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            return 0;
        }
    }

    public async Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteSqlQueryAsync(
        string sql,
        IReadOnlyDictionary<string, object?> parameters,
        MySqlDatabaseTarget databaseTarget,
        CancellationToken cancellationToken = default)
    {
        var connectionString = ResolveConnectionString(databaseTarget);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return Array.Empty<Dictionary<string, object?>>();
        }

        try
        {
            await using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = new MySqlCommand(sql, connection)
            {
                CommandType = System.Data.CommandType.Text,
                CommandTimeout = DefaultCommandTimeoutSeconds,
            };

            foreach (var entry in parameters)
            {
                var parameterName = entry.Key.StartsWith("@", StringComparison.Ordinal) ? entry.Key : $"@{entry.Key}";
                _ = command.Parameters.AddWithValue(parameterName, entry.Value ?? DBNull.Value);
            }

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
        catch
        {
            return Array.Empty<Dictionary<string, object?>>();
        }
    }

    public async Task<int> ExecuteSqlNonQueryAsync(
        string sql,
        IReadOnlyDictionary<string, object?> parameters,
        MySqlDatabaseTarget databaseTarget,
        CancellationToken cancellationToken = default)
    {
        var connectionString = ResolveConnectionString(databaseTarget);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return 0;
        }

        try
        {
            await using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = new MySqlCommand(sql, connection)
            {
                CommandType = System.Data.CommandType.Text,
                CommandTimeout = DefaultCommandTimeoutSeconds,
            };

            foreach (var entry in parameters)
            {
                var parameterName = entry.Key.StartsWith("@", StringComparison.Ordinal) ? entry.Key : $"@{entry.Key}";
                _ = command.Parameters.AddWithValue(parameterName, entry.Value ?? DBNull.Value);
            }

            return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            return 0;
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
                Environment.GetEnvironmentVariable(ReceivingConnectionStringEnvironmentVariable)?.Trim(),
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
        return databaseTarget == MySqlDatabaseTarget.MtmReceivingApplication
            ? "mtm_receiving_application"
            : "mtm_waitlist";
    }

    private async Task<bool> IsMockDataEnabledAsync(MySqlDatabaseTarget databaseTarget)
    {
        if (databaseTarget == MySqlDatabaseTarget.MtmReceivingApplication)
        {
            return await _localSettingsService.ReadSettingAsync<bool?>(RecvMockDataSettingKey).ConfigureAwait(false) ?? false;
        }

        return await _localSettingsService.ReadSettingAsync<bool?>(RecvMockDataSettingKey).ConfigureAwait(false) ?? false;
    }
}
