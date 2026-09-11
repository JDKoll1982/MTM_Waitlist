using Microsoft.Extensions.Options;

using MySqlConnector;
using System.Security.Cryptography;
using System.Text;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Startup.Services;

public sealed class StartupSessionRepository : IStartupSessionRepository
{
    /// <summary>The database server's UTC clock — session validity is judged against the server, not the client.</summary>
    private const string ServerUtcNowProcedure = "sp_server_utc_now_get";

    /// <summary>Credential material for one active user, so the caller can verify a password.</summary>
    private const string CredentialsCheckProcedure = "sp_auth_credentials_check";

    /// <summary>Stores a new password hash/salt and clears the forced-change flag.</summary>
    private const string PasswordUpdateProcedure = "sp_auth_user_password_update";

    /// <summary>Whether the presented machine is a registered workstation.</summary>
    private const string ComputerRegisteredProcedure = "sp_auth_computer_registered_get";

    /// <summary>One active user's identity and role.</summary>
    private const string UserRowProcedure = "sp_auth_user_row_get";

    /// <summary>The newest live session's expiry for one user, if any.</summary>
    private const string SessionExpiryProcedure = "sp_auth_session_expiry_get";

    private const int PasswordSaltLengthBytes = 16;
    private const int PasswordHashLengthBytes = 32;
    private const int PasswordIterations = 100_000;
    private const string TemporaryDefaultPasswordHashMarker = "0000";

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

            await using var command = new MySqlCommand(ServerUtcNowProcedure, connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
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
                IsComputerRegistered = false,
                IsComputerRegistrationAuthoritative = false,
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

            var computerRegistered = await ReadComputerRegisteredAsync(connection, hostnameNormalized, macAddressNormalized, token);
            var userRow = await ReadUserRowAsync(connection, username, token);

            if (!userRow.IsUserMatched)
            {
                return new StartupSessionSnapshot
                {
                    IsUserMatched = false,
                    IsComputerRegistered = computerRegistered,
                    IsComputerRegistrationAuthoritative = true,
                    CurrentRole = string.Empty,
                    DisplayName = string.Empty,
                    EmployeeIdentifier = string.Empty,
                    HasDatabaseSession = false,
                    DatabaseSessionExpiresUtc = null
                };
            }

            var sessionExpiry = await ReadSessionExpiryUtcAsync(connection, userRow.UserId, token);

            return new StartupSessionSnapshot
            {
                IsUserMatched = true,
                IsComputerRegistered = computerRegistered,
                IsComputerRegistrationAuthoritative = true,
                CurrentRole = userRow.CurrentRole,
                DisplayName = userRow.DisplayName,
                EmployeeIdentifier = userRow.EmployeeIdentifier,
                HasDatabaseSession = sessionExpiry.HasValue,
                DatabaseSessionExpiresUtc = sessionExpiry
            };
        }, cancellationToken);
    }

    public async Task<StartupCredentialCheckResult> CheckCredentialsAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
        {
            return StartupCredentialCheckResult.Failed();
        }

        var connectionString = ResolveConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return StartupCredentialCheckResult.Failed();
        }

        var timeoutConnectionString = BuildTimeoutConnectionString(connectionString);
        return await ExecuteWithRetryAsync(async token =>
        {
            await using var connection = new MySqlConnection(timeoutConnectionString);
            await connection.OpenAsync(token);

            await using var command = new MySqlCommand(CredentialsCheckProcedure, connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@p_username", username.Trim().ToLowerInvariant());

            await using var reader = await command.ExecuteReaderAsync(token);
            if (!await reader.ReadAsync(token))
            {
                return StartupCredentialCheckResult.Failed();
            }

            var userId = reader.GetInt64(0);
            var currentRole = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
            var passwordHash = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
            var passwordSalt = reader.IsDBNull(3) ? null : (byte[])reader[3];
            var requirePasswordChange = !reader.IsDBNull(4) && reader.GetBoolean(4);
            var displayName = reader.IsDBNull(5) ? string.Empty : reader.GetString(5);
            var employeeIdentifier = reader.IsDBNull(6) ? string.Empty : reader.GetString(6);

            if (IsTemporaryDefaultPassword(passwordHash))
            {
                if (!string.Equals(password, TemporaryDefaultPasswordHashMarker, StringComparison.Ordinal))
                {
                    return StartupCredentialCheckResult.Failed();
                }

                return StartupCredentialCheckResult.Success(userId, currentRole, true, displayName, employeeIdentifier);
            }

            if (!VerifyPassword(password, passwordHash, passwordSalt))
            {
                return StartupCredentialCheckResult.Failed();
            }

            return StartupCredentialCheckResult.Success(userId, currentRole, requirePasswordChange, displayName, employeeIdentifier);
        }, cancellationToken);
    }

    public async Task<bool> UpdatePasswordAsync(
        long userId,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || string.IsNullOrWhiteSpace(newPassword))
        {
            return false;
        }

        var connectionString = ResolveConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }

        var timeoutConnectionString = BuildTimeoutConnectionString(connectionString);
        return await ExecuteWithRetryAsync(async token =>
        {
            var salt = new byte[PasswordSaltLengthBytes];
            RandomNumberGenerator.Fill(salt);
            var hash = HashPassword(newPassword, salt);

            await using var connection = new MySqlConnection(timeoutConnectionString);
            await connection.OpenAsync(token);

            await using var command = new MySqlCommand(PasswordUpdateProcedure, connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@p_password_hash", hash);
            command.Parameters.AddWithValue("@p_password_salt", salt);
            command.Parameters.AddWithValue("@p_user_id", userId);

            var rows = await command.ExecuteNonQueryAsync(token);
            return rows > 0;
        }, cancellationToken);
    }

    private static async Task<bool> ReadComputerRegisteredAsync(
        MySqlConnection connection,
        string hostnameNormalized,
        string macAddressNormalized,
        CancellationToken cancellationToken)
    {
        await using var command = new MySqlCommand(ComputerRegisteredProcedure, connection)
        {
            CommandType = System.Data.CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@p_hostname_normalized", hostnameNormalized);
        command.Parameters.AddWithValue("@p_mac_address_normalized", macAddressNormalized);

        var scalar = await command.ExecuteScalarAsync(cancellationToken);
        var count = scalar is null ? 0 : Convert.ToInt32(scalar);
        return count > 0;
    }

    private static async Task<(bool IsUserMatched, long UserId, string CurrentRole, string DisplayName, string EmployeeIdentifier)> ReadUserRowAsync(
        MySqlConnection connection,
        string username,
        CancellationToken cancellationToken)
    {
        await using var command = new MySqlCommand(UserRowProcedure, connection)
        {
            CommandType = System.Data.CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@p_username", username);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return (false, 0, string.Empty, string.Empty, string.Empty);
        }

        var userId = reader.GetInt64(0);
        var role = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
        var displayName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
        var employeeIdentifier = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);
        return (true, userId, role, displayName, employeeIdentifier);
    }

    private static async Task<DateTimeOffset?> ReadSessionExpiryUtcAsync(
        MySqlConnection connection,
        long userId,
        CancellationToken cancellationToken)
    {
        await using var command = new MySqlCommand(SessionExpiryProcedure, connection)
        {
            CommandType = System.Data.CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@p_user_id", userId);

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

    private static bool IsTemporaryDefaultPassword(string passwordHash)
    {
        return string.IsNullOrWhiteSpace(passwordHash)
            || string.Equals(passwordHash, TemporaryDefaultPasswordHashMarker, StringComparison.Ordinal);
    }

    private static bool VerifyPassword(string password, string storedHash, byte[]? storedSalt)
    {
        if (string.IsNullOrWhiteSpace(storedHash) || storedSalt is null || storedSalt.Length == 0)
        {
            return false;
        }

        byte[] expectedHash;
        try
        {
            expectedHash = Convert.FromBase64String(storedHash);
        }
        catch (FormatException)
        {
            return false;
        }

        var computedHash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            storedSalt,
            PasswordIterations,
            HashAlgorithmName.SHA256,
            PasswordHashLengthBytes);

        return CryptographicOperations.FixedTimeEquals(expectedHash, computedHash);
    }

    private static string HashPassword(string password, byte[] salt)
    {
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            PasswordIterations,
            HashAlgorithmName.SHA256,
            PasswordHashLengthBytes);

        return Convert.ToBase64String(hash);
    }
}
