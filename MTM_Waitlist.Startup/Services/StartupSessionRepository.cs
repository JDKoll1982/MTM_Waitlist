using Microsoft.Extensions.Options;

using MySqlConnector;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;

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

    /// <summary>
    /// Whether one account still holds its temporary default password, resolved before the sign-in form is
    /// shown. Returns the same identity columns as the logon read plus a 0/1 verdict, and never any credential
    /// material.
    /// </summary>
    private const string PasswordResetRequiredProcedure = "sp_auth_password_reset_required_get";

    /// <summary>
    /// Moves the wrong-attempt count on a temporary credential: one up on a failure, to zero on a success.
    /// Nothing else clears it and nothing expires it.
    /// </summary>
    private const string TemporaryCredentialAttemptProcedure = "sp_auth_temporary_credential_attempt_record";

    /// <summary>
    /// How many wrong attempts a temporary credential is accepted for. The limit is what gives a four-digit
    /// credential any strength at all, so the count is held with the account and survives a restart.
    /// </summary>
    public const int TemporaryCredentialAttemptLimit = 5;

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
                UserId = userRow.UserId,
                CurrentRoleCode = userRow.RoleCode,
                CurrentRole = userRow.RoleName,
                DisplayName = userRow.DisplayName,
                EmployeeIdentifier = userRow.EmployeeIdentifier,
                HasDatabaseSession = sessionExpiry.HasValue,
                DatabaseSessionExpiresUtc = sessionExpiry
            };
        }, cancellationToken);
    }

    public async Task<StartupPasswordResetRequirement> ReadPasswordResetRequirementAsync(
        string username,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return StartupPasswordResetRequirement.None;
        }

        var connectionString = ResolveConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return StartupPasswordResetRequirement.None;
        }

        var timeoutConnectionString = BuildTimeoutConnectionString(connectionString);
        return await ExecuteWithRetryAsync(async token =>
        {
            await using var connection = new MySqlConnection(timeoutConnectionString);
            await connection.OpenAsync(token);

            await using var command = new MySqlCommand(PasswordResetRequiredProcedure, connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            // Normalized exactly as the credential check does: the column is `username_normalized`, which the
            // store holds in upper case, so the comparison is made in upper case whatever case was typed.
            command.Parameters.AddWithValue("@p_username", NormalizeUsername(username));

            await using var reader = await command.ExecuteReaderAsync(token);
            if (!await reader.ReadAsync(token))
            {
                return StartupPasswordResetRequirement.None;
            }

            var isRequired = !reader.IsDBNull(5) && Convert.ToInt32(reader[5]) == 1;
            if (!isRequired)
            {
                return StartupPasswordResetRequirement.None;
            }

            return new StartupPasswordResetRequirement
            {
                IsRequired = true,
                UserId = reader.GetInt64(0),
                CurrentRoleCode = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                CurrentRole = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                DisplayName = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                EmployeeIdentifier = reader.IsDBNull(4) ? string.Empty : reader.GetString(4)
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

        // The read runs under the retry policy, the count is moved outside it. A retry that re-ran the count
        // would add a second failure for one try, and one press of Sign in must move the count exactly once.
        var outcome = await ExecuteWithRetryAsync(async token =>
        {
            await using var connection = new MySqlConnection(timeoutConnectionString);
            await connection.OpenAsync(token);

            await using var command = new MySqlCommand(CredentialsCheckProcedure, connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            // Upper case, because the column is `username_normalized` and the store holds that form. The column's
            // collation folds case, so a name typed either way matches the one stored form (FR-002).
            command.Parameters.AddWithValue("@p_username", NormalizeUsername(username));

            await using var reader = await command.ExecuteReaderAsync(token);
            if (!await reader.ReadAsync(token))
            {
                return null;
            }

            var userId = reader.GetInt64(0);
            var currentRoleCode = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
            var currentRole = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
            var passwordHash = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);
            var passwordSalt = reader.IsDBNull(4) ? null : (byte[])reader[4];
            var requirePasswordChange = !reader.IsDBNull(5) && reader.GetBoolean(5);
            var displayName = reader.IsDBNull(6) ? string.Empty : reader.GetString(6);
            var employeeIdentifier = reader.IsDBNull(7) ? string.Empty : reader.GetString(7);
            var failedAttempts = reader.IsDBNull(8)
                ? 0
                : Convert.ToInt32(reader.GetValue(8), System.Globalization.CultureInfo.InvariantCulture);

            // "Holds a temporary credential" is exactly what sp_auth_password_reset_required_get computes: the
            // forced-change flag is set, or the stored hash is empty or the legacy marker. A reset stores a real
            // salted hash of the PIN, so the flag is what marks it temporary, and the limit has to apply to it.
            var holdsTemporaryCredential = requirePasswordChange || IsTemporaryDefaultPassword(passwordHash);
            var requiresPasswordChange = requirePasswordChange || IsTemporaryDefaultPassword(passwordHash);

            // The limit is checked BEFORE the value is compared, so the attempt after the fifth failing one is
            // refused even when it holds the right credential (FR-031). Nothing here expires the count.
            if (holdsTemporaryCredential && failedAttempts >= TemporaryCredentialAttemptLimit)
            {
                return new CredentialCheckOutcome(
                    StartupCredentialCheckResult.Failed() with
                    {
                        HoldsTemporaryCredential = true,
                        TemporaryCredentialFailedAttempts = failedAttempts,
                        TemporaryCredentialAttemptLimitReached = true,
                    },
                    RecordAttempt: false,
                    UserId: userId);
            }

            // The legacy marker account accepts only the literal marker, exactly as it did before this feature;
            // every other account is checked against its salted hash.
            var credentialAccepted = IsTemporaryDefaultPassword(passwordHash)
                ? string.Equals(password, TemporaryDefaultPasswordHashMarker, StringComparison.Ordinal)
                : PasswordSecretHasher.Verify(password, passwordHash, passwordSalt);

            // A wrong password on an ordinary account is refused and moves no count, so the limit cannot be used
            // to find out which sign-in names exist (FR-044).
            if (!credentialAccepted)
            {
                return new CredentialCheckOutcome(
                    StartupCredentialCheckResult.Failed() with
                    {
                        HoldsTemporaryCredential = holdsTemporaryCredential,
                        TemporaryCredentialFailedAttempts = holdsTemporaryCredential ? failedAttempts + 1 : 0,
                    },
                    RecordAttempt: holdsTemporaryCredential,
                    UserId: userId);
            }

            // A successful sign-in with a temporary credential clears the count (FR-039); a successful sign-in on
            // an ordinary account had nothing to clear.
            return new CredentialCheckOutcome(
                StartupCredentialCheckResult.Success(userId, currentRole, requiresPasswordChange, displayName, employeeIdentifier, currentRoleCode) with
                {
                    HoldsTemporaryCredential = holdsTemporaryCredential,
                    TemporaryCredentialFailedAttempts = 0,
                },
                RecordAttempt: holdsTemporaryCredential && failedAttempts > 0,
                UserId: userId);
        }, cancellationToken).ConfigureAwait(false);

        if (outcome is null)
        {
            return StartupCredentialCheckResult.Failed();
        }

        if (outcome.RecordAttempt)
        {
            await RecordTemporaryCredentialAttemptAsync(
                    outcome.UserId,
                    outcome.Result.IsAuthenticated,
                    timeoutConnectionString,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        return outcome.Result;
    }

    /// <summary>
    /// Moves the wrong-attempt count for one account, in one statement, so no screen has to remember to record
    /// a failure and a successful sign-in clears what earlier failures left (FR-036, FR-039).
    /// </summary>
    private static async Task RecordTemporaryCredentialAttemptAsync(
        long userId,
        bool wasSuccessful,
        string timeoutConnectionString,
        CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(timeoutConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new MySqlCommand(TemporaryCredentialAttemptProcedure, connection)
        {
            CommandType = System.Data.CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@p_user_id", userId);
        command.Parameters.AddWithValue("@p_was_successful", wasSuccessful ? 1 : 0);

        await command.ExecuteNonQueryAsync(cancellationToken);
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
            var salt = PasswordSecretHasher.NewSalt();
            var hash = PasswordSecretHasher.Hash(newPassword, salt);

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

    private static async Task<UserRow> ReadUserRowAsync(
        MySqlConnection connection,
        string username,
        CancellationToken cancellationToken)
    {
        await using var command = new MySqlCommand(UserRowProcedure, connection)
        {
            CommandType = System.Data.CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@p_username", NormalizeUsername(username));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return UserRow.Missing;
        }

        return new UserRow(
            true,
            reader.GetInt64(0),
            reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
            reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
            reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
            reader.IsDBNull(4) ? string.Empty : reader.GetString(4));
    }

    /// <summary>
    /// The stored form of a sign-in name. The column is <c>username_normalized</c> and the store holds upper
    /// case, so both the write and the comparison use this one form (FR-002).
    /// </summary>
    private static string NormalizeUsername(string username) => username.Trim().ToUpperInvariant();

    /// <summary>One matched account row, or <see cref="Missing"/> when no account matched.</summary>
    private readonly record struct UserRow(
        bool IsUserMatched,
        long UserId,
        string RoleCode,
        string RoleName,
        string DisplayName,
        string EmployeeIdentifier)
    {
        public static UserRow Missing { get; } = new(false, 0, string.Empty, string.Empty, string.Empty, string.Empty);
    }

    /// <summary>
    /// One credential check's answer plus whether the count has to move for it. The count is moved by the caller
    /// after the retried read has returned, so one attempt never becomes two.
    /// </summary>
    /// <remarks>
    /// <paramref name="UserId"/> is carried separately from <see cref="StartupCredentialCheckResult.UserId"/>,
    /// because the refused answers are built from <c>Failed()</c> and hold no identity: recording an attempt
    /// against that answer would write to user 0 and leave the count unmoved, so the limit would never bite. The
    /// account id is known here whatever the verdict, so it travels beside the verdict.
    /// </remarks>
    private sealed record CredentialCheckOutcome(StartupCredentialCheckResult Result, bool RecordAttempt, long UserId);

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
                return MySqlHostFallback.Apply(environmentConnectionString);
            }
        }

        // Same fallback the store reads use: the configured host when it answers, else the local server when
        // that answers, else unchanged so startup blocks and reports the outage as it always has.
        return MySqlHostFallback.Apply(_startupDatabaseOptions.ConnectionString?.Trim());
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
}
