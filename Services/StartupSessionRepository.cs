using Microsoft.Extensions.Options;

using MySqlConnector;

using MTM_Waitlist.Contracts.Services;
using MTM_Waitlist.Models;

namespace MTM_Waitlist.Services;

public sealed class StartupSessionRepository : IStartupSessionRepository
{
    private readonly StartupDatabaseOptions _startupDatabaseOptions;

    public StartupSessionRepository(IOptions<StartupDatabaseOptions> startupDatabaseOptions)
    {
        ArgumentNullException.ThrowIfNull(startupDatabaseOptions);
        _startupDatabaseOptions = startupDatabaseOptions.Value;
    }

    public async Task<DateTimeOffset?> ReadServerTimeUtcAsync(CancellationToken cancellationToken = default)
    {
        var connectionString = ResolveConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return null;
        }

        var timeoutConnectionString = BuildTimeoutConnectionString(connectionString);
        var scalar = await ExecuteWithRetryAsync(async token =>
        {
            await using var connection = new MySqlConnection(timeoutConnectionString);
            await connection.OpenAsync(token);

            await using var command = new MySqlCommand("SELECT fn_server_utc_now();", connection);
            return await command.ExecuteScalarAsync(token);
        }, cancellationToken);

        if (scalar is DateTime dateTime)
        {
            return new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc));
        }

        return null;
    }

    public async Task<StartupSessionSnapshot> ReadSessionSnapshotAsync(
        string username,
        string hostnameNormalized,
        string macAddressNormalized,
        CancellationToken cancellationToken = default)
    {
        var connectionString = ResolveConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return new StartupSessionSnapshot
            {
                IsUserMatched = false,
                IsWorkstationRegistered = false,
                IsWorkstationRegistrationAuthoritative = false,
                CurrentRole = string.Empty,
                HasDatabaseSession = false,
                DatabaseSessionExpiresUtc = null
            };
        }

        var timeoutConnectionString = BuildTimeoutConnectionString(connectionString);
        return await ExecuteWithRetryAsync(async token =>
        {
            await using var connection = new MySqlConnection(timeoutConnectionString);
            await connection.OpenAsync(token);

            var workstationRegistered = await ReadWorkstationRegisteredAsync(connection, hostnameNormalized, macAddressNormalized, token);
            var userRow = await ReadUserRowAsync(connection, username, token);

            if (!userRow.IsUserMatched)
            {
                return new StartupSessionSnapshot
                {
                    IsUserMatched = false,
                    IsWorkstationRegistered = workstationRegistered,
                    IsWorkstationRegistrationAuthoritative = true,
                    CurrentRole = string.Empty,
                    HasDatabaseSession = false,
                    DatabaseSessionExpiresUtc = null
                };
            }

            var sessionExpiry = await ReadSessionExpiryUtcAsync(connection, userRow.UserId, token);

            return new StartupSessionSnapshot
            {
                IsUserMatched = true,
                IsWorkstationRegistered = workstationRegistered,
                IsWorkstationRegistrationAuthoritative = true,
                CurrentRole = userRow.CurrentRole,
                HasDatabaseSession = sessionExpiry.HasValue,
                DatabaseSessionExpiresUtc = sessionExpiry
            };
        }, cancellationToken);
    }

    private static async Task<bool> ReadWorkstationRegisteredAsync(
        MySqlConnection connection,
        string hostnameNormalized,
        string macAddressNormalized,
        CancellationToken cancellationToken)
    {
        await using var command = new MySqlCommand(
            """
            SELECT COUNT(1)
            FROM core_workstations_registry
            WHERE hostname_normalized = @hostname
              AND mac_address_normalized = @macAddress
              AND is_registered = 1;
            """,
            connection);

        command.Parameters.AddWithValue("@hostname", hostnameNormalized);
        command.Parameters.AddWithValue("@macAddress", macAddressNormalized);

        var scalar = await command.ExecuteScalarAsync(cancellationToken);
        var count = scalar is null ? 0 : Convert.ToInt32(scalar);
        return count > 0;
    }

    private static async Task<(bool IsUserMatched, long UserId, string CurrentRole)> ReadUserRowAsync(
        MySqlConnection connection,
        string username,
        CancellationToken cancellationToken)
    {
        await using var command = new MySqlCommand(
            """
            SELECT u.id, COALESCE(r.role_name, '') AS role_name
            FROM core_users_profiles u
            LEFT JOIN auth_roles_assignments ra ON ra.user_id = u.id
            LEFT JOIN auth_roles_catalog r ON r.id = ra.role_id
            WHERE u.username_normalized = @username
              AND u.is_active = 1
            ORDER BY ra.assigned_utc DESC
            LIMIT 1;
            """,
            connection);

        command.Parameters.AddWithValue("@username", username);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return (false, 0, string.Empty);
        }

        var userId = reader.GetInt64(0);
        var role = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
        return (true, userId, role);
    }

    private static async Task<DateTimeOffset?> ReadSessionExpiryUtcAsync(
        MySqlConnection connection,
        long userId,
        CancellationToken cancellationToken)
    {
        await using var command = new MySqlCommand(
            """
            SELECT expires_utc
            FROM auth_sessions_tokens
            WHERE user_id = @userId
              AND is_active = 1
              AND revoked_utc IS NULL
            ORDER BY expires_utc DESC
            LIMIT 1;
            """,
            connection);

        command.Parameters.AddWithValue("@userId", userId);

        var scalar = await command.ExecuteScalarAsync(cancellationToken);
        if (scalar is DateTime dateTime)
        {
            return new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc));
        }

        return null;
    }

    private string? ResolveConnectionString()
    {
        var environmentVariableName = _startupDatabaseOptions.ConnectionStringEnvironmentVariable?.Trim();
        if (!string.IsNullOrWhiteSpace(environmentVariableName))
        {
            var environmentConnectionString = Environment.GetEnvironmentVariable(environmentVariableName)?.Trim();
            if (!string.IsNullOrWhiteSpace(environmentConnectionString))
            {
                return environmentConnectionString;
            }
        }

        return _startupDatabaseOptions.ConnectionString?.Trim();
    }

    private async Task<T> ExecuteWithRetryAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action);

        var maxRetryCount = Math.Max(0, _startupDatabaseOptions.MaxRetryCount);
        var retryBaseDelayMilliseconds = Math.Max(1, _startupDatabaseOptions.RetryBaseDelayMilliseconds);

        Exception? lastException = null;
        for (var attempt = 0; attempt <= maxRetryCount; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await action(cancellationToken);
            }
            catch (Exception ex) when (IsRetryable(ex) && attempt < maxRetryCount)
            {
                lastException = ex;
                var delayMilliseconds = retryBaseDelayMilliseconds * (int)Math.Pow(2, attempt);
                await Task.Delay(TimeSpan.FromMilliseconds(delayMilliseconds), cancellationToken);
            }
            catch
            {
                throw;
            }
        }

        throw new InvalidOperationException("Startup database operation failed after retries.", lastException);
    }

    private static bool IsRetryable(Exception exception)
    {
        return exception is MySqlException
            || exception is TimeoutException;
    }

    private string BuildTimeoutConnectionString(string connectionString)
    {
        var builder = new MySqlConnectionStringBuilder(connectionString)
        {
            ConnectionTimeout = (uint)Math.Max(1, _startupDatabaseOptions.ConnectionTimeoutSeconds)
        };

        return builder.ConnectionString;
    }
}
