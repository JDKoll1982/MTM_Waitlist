using Microsoft.Extensions.Options;
using MySqlConnector;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Models.UserManagement;

namespace MTM_Waitlist.Module_Core.Services;

/// <inheritdoc cref="IUserManagementRepository"/>
/// <remarks>
/// <para>
/// <b>Why this uses the seam's strict route rather than its lenient one.</b> The lenient route answers a failed
/// call with its neutral result — zero rows, zero affected — which is the right behaviour for a screen that shows
/// its own unavailable state. It is the wrong behaviour here for two reasons the requirements name. A duplicate
/// sign-in name must arrive as the provider's own <c>1062</c> answer rather than being folded into "something went
/// wrong" (FR-008, and the pinned <c>1062</c>), and a roster read that failed must not be handed on as an empty
/// list, because an empty roster is a different state with different words (FR-096). Both need the provider's
/// error, so both go through <see cref="IMySqlHelperServer.ExecuteStrictAsync{T}"/>, which propagates it and
/// retries nothing.
/// </para>
/// <para>
/// Nothing here composes statement text: every call names a stored procedure and passes parameters (constitution
/// III). No retry lives here either — the error is reported, never hidden behind a second attempt, which is what
/// keeps one press from creating two people. The one throw this repository used to make itself, for an
/// unconfigured store, still reaches the caller: the seam raises
/// <see cref="MySqlStoreNotConfiguredException"/>, which is the <see cref="InvalidOperationException"/> this class
/// threw under the same words.
/// </para>
/// </remarks>
public sealed class UserManagementRepository : IUserManagementRepository
{
    private const string ListProcedure = "sp_user_management_list";
    private const string GetProcedure = "sp_user_management_get";
    private const string CreateProcedure = "sp_user_management_create";
    private const string UpdateProcedure = "sp_user_management_update";
    private const string ResetPasswordProcedure = "sp_user_management_reset_password";
    private const string RecordAttemptProcedure = "sp_auth_temporary_credential_attempt_record";

    /// <summary>The provider's duplicate-key answer, which the contract pins as the already-exists result.</summary>
    private const int DuplicateKeyErrorNumber = 1062;

    /// <summary>A refusal this repository's procedures raise with <c>SIGNAL SQLSTATE '45000'</c>.</summary>
    private const int SignalledRefusalErrorNumber = 1644;

    private const string RankDeniedToken = "mtm_rank_denied";
    private const string SelfDeactivateDeniedToken = "mtm_self_deactivate_denied";
    private const string SelfRenameDeniedToken = "mtm_self_rename_denied";
    private const string TargetOutranksActorToken = "mtm_target_outranks_actor";
    private const string RoleUnknownToken = "mtm_role_unknown";

    private readonly WaitlistDatabaseOptions _waitlistDatabaseOptions;
    private readonly IMySqlHelperServer _mySqlHelperServer;

    public UserManagementRepository(
        IMySqlHelperServer mySqlHelperServer,
        IOptions<WaitlistDatabaseOptions> waitlistDatabaseOptions)
    {
        ArgumentNullException.ThrowIfNull(mySqlHelperServer);
        ArgumentNullException.ThrowIfNull(waitlistDatabaseOptions);

        _mySqlHelperServer = mySqlHelperServer;
        _waitlistDatabaseOptions = waitlistDatabaseOptions.Value;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserRosterRow>> ListAsync(
        string? searchText,
        string? roleCode,
        CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, object?>
        {
            ["p_search"] = NormalizeOrNull(searchText),
            ["p_role_code"] = NormalizeOrNull(roleCode),
        };

        var rows = new List<UserRosterRow>();
        await ExecuteReaderAsync(
            ListProcedure,
            parameters,
            cancellationToken,
            row => rows.Add(new UserRosterRow(
                ReadLong(row, "user_id"),
                ReadString(row, "public_id"),
                ReadString(row, "username_normalized"),
                ReadString(row, "display_name"),
                ReadString(row, "employee_identifier"),
                ReadString(row, "role_code"),
                ReadString(row, "role_name"),
                ReadInt(row, "role_rank"),
                ReadBool(row, "is_active"))))
            .ConfigureAwait(false);

        return rows;
    }

    /// <inheritdoc />
    public async Task<UserAccount?> GetAsync(long userId, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, object?> { ["p_user_id"] = userId };

        UserAccount? account = null;
        await ExecuteReaderAsync(
            GetProcedure,
            parameters,
            cancellationToken,
            row =>
            {
                if (account is null)
                {
                    account = new UserAccount(
                        ReadLong(row, "user_id"),
                        ReadString(row, "public_id"),
                        ReadString(row, "username_normalized"),
                        ReadString(row, "first_name"),
                        ReadString(row, "last_name"),
                        ReadString(row, "display_name"),
                        ReadString(row, "employee_identifier"),
                        ReadString(row, "role_code"),
                        ReadString(row, "role_name"),
                        ReadInt(row, "role_rank"),
                        ReadBool(row, "is_active"),
                        ReadInt(row, "temporary_credential_failed_attempts"));
                }
            })
            .ConfigureAwait(false);

        return account;
    }

    /// <inheritdoc />
    public Task<UserManagementResult> CreateAsync(
        string username,
        string firstName,
        string lastName,
        string displayName,
        string employeeIdentifier,
        string roleCode,
        long actorUserId,
        string passwordHash,
        byte[] passwordSalt,
        string changeGroupId,
        CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, object?>
        {
            ["p_username"] = username,
            ["p_first_name"] = firstName,
            ["p_last_name"] = lastName,
            ["p_employee_identifier"] = NormalizeOrNull(employeeIdentifier),
            ["p_role_code"] = roleCode,
            ["p_actor_user_id"] = actorUserId,
            ["p_password_hash"] = passwordHash,
            ["p_password_salt"] = passwordSalt,
            ["p_change_group_id"] = changeGroupId,
        };

        // The derived display name travels nowhere: the procedure derives it from the two name parts, so the
        // half that is bounded here and the half the store writes cannot drift apart.
        _ = displayName;

        return ExecuteWriteAsync(CreateProcedure, parameters, UserManagementOutcomeKind.InvalidInput, cancellationToken);
    }

    /// <inheritdoc />
    public Task<UserManagementResult> UpdateAsync(
        long userId,
        string username,
        string firstName,
        string lastName,
        string displayName,
        string employeeIdentifier,
        string roleCode,
        bool isActive,
        long actorUserId,
        string changeGroupId,
        CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, object?>
        {
            ["p_user_id"] = userId,
            ["p_username"] = username,
            ["p_first_name"] = firstName,
            ["p_last_name"] = lastName,
            ["p_employee_identifier"] = NormalizeOrNull(employeeIdentifier),
            ["p_role_code"] = roleCode,
            ["p_is_active"] = isActive ? 1 : 0,
            ["p_actor_user_id"] = actorUserId,
            ["p_change_group_id"] = changeGroupId,
        };

        _ = displayName;

        return ExecuteWriteAsync(UpdateProcedure, parameters, UserManagementOutcomeKind.InvalidInput, cancellationToken);
    }

    /// <inheritdoc />
    public Task<UserManagementResult> ResetPasswordAsync(
        long userId,
        long actorUserId,
        string passwordHash,
        byte[] passwordSalt,
        string changeGroupId,
        CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, object?>
        {
            ["p_user_id"] = userId,
            ["p_actor_user_id"] = actorUserId,
            ["p_password_hash"] = passwordHash,
            ["p_password_salt"] = passwordSalt,
            ["p_change_group_id"] = changeGroupId,
        };

        return ExecuteWriteAsync(ResetPasswordProcedure, parameters, UserManagementOutcomeKind.InvalidInput, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RecordTemporaryCredentialAttemptAsync(
        long userId,
        bool wasSuccessful,
        CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, object?>
        {
            ["p_user_id"] = userId,
            ["p_was_successful"] = wasSuccessful ? 1 : 0,
        };

        var result = await ExecuteWriteAsync(
                RecordAttemptProcedure,
                parameters,
                UserManagementOutcomeKind.StoreUnavailable,
                cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(
                $"Recording a temporary-credential attempt did not reach the store: {result.Message}");
        }
    }

    /// <summary>
    /// Runs one write through the seam's strict route, so the provider's own answer reaches
    /// <see cref="MapProviderFailure"/> rather than being folded into a neutral one.
    /// </summary>
    private async Task<UserManagementResult> ExecuteWriteAsync(
        string procedureName,
        IReadOnlyDictionary<string, object?> parameters,
        UserManagementOutcomeKind unavailableKind,
        CancellationToken cancellationToken)
    {
        try
        {
            await _mySqlHelperServer.ExecuteStrictAsync(
                CreateRequest(),
                async (scope, token) =>
                {
                    _ = await scope.NonQueryAsync(procedureName, parameters, token).ConfigureAwait(false);
                    return true;
                },
                cancellationToken).ConfigureAwait(false);

            return UserManagementResult.Succeeded();
        }
        catch (MySqlStoreNotConfiguredException)
        {
            return UserManagementResult.Failed(
                unavailableKind,
                UserManagementMessages.StoreUnavailableKey,
                UserManagementMessages.StoreUnavailable);
        }
        catch (OperationCanceledException)
        {
            // The reader asked to stop. Starting another attempt would be the blind retry this path must not have.
            throw;
        }
        catch (MySqlException ex)
        {
            return MapProviderFailure(ex, unavailableKind);
        }
        catch (Exception ex)
        {
            return UserManagementResult.Failed(
                unavailableKind,
                UserManagementMessages.StoreUnavailableKey,
                $"{UserManagementMessages.StoreUnavailable} ({ex.Message})");
        }
    }

    /// <summary>
    /// Turns the provider's own answer into one typed outcome. A duplicate key is the pinned already-exists
    /// answer, a refusal this feature raises is the token it signals, and anything else is the store not
    /// answering.
    /// </summary>
    private static UserManagementResult MapProviderFailure(MySqlException exception, UserManagementOutcomeKind unavailableKind)
    {
        if (exception.Number == DuplicateKeyErrorNumber)
        {
            return UserManagementResult.Failed(
                UserManagementOutcomeKind.DuplicateUsername,
                UserManagementMessages.DuplicateUsernameKey,
                UserManagementMessages.DuplicateUsername);
        }

        if (exception.Number == SignalledRefusalErrorNumber)
        {
            var message = exception.Message ?? string.Empty;

            if (message.Contains(RankDeniedToken, StringComparison.Ordinal))
            {
                return UserManagementResult.Failed(
                    UserManagementOutcomeKind.RoleDenied,
                    UserManagementMessages.RoleDeniedKey,
                    UserManagementMessages.RoleDenied);
            }

            if (message.Contains(SelfDeactivateDeniedToken, StringComparison.Ordinal))
            {
                return UserManagementResult.Failed(
                    UserManagementOutcomeKind.SelfDeactivateDenied,
                    UserManagementMessages.SelfDeactivateDeniedKey,
                    UserManagementMessages.SelfDeactivateDenied);
            }

            if (message.Contains(SelfRenameDeniedToken, StringComparison.Ordinal))
            {
                return UserManagementResult.Failed(
                    UserManagementOutcomeKind.SelfRenameDenied,
                    UserManagementMessages.SelfRenameDeniedKey,
                    UserManagementMessages.SelfRenameDenied);
            }

            if (message.Contains(TargetOutranksActorToken, StringComparison.Ordinal))
            {
                return UserManagementResult.Failed(
                    UserManagementOutcomeKind.TargetOutranksActor,
                    UserManagementMessages.TargetOutranksActorKey,
                    UserManagementMessages.TargetOutranksActor);
            }

            if (message.Contains(RoleUnknownToken, StringComparison.Ordinal))
            {
                return UserManagementResult.Failed(
                    UserManagementOutcomeKind.RoleUnknown,
                    UserManagementMessages.RoleUnknownKey,
                    UserManagementMessages.RoleUnknown);
            }
        }

        return UserManagementResult.Failed(
            unavailableKind,
            UserManagementMessages.StoreUnavailableKey,
            $"{UserManagementMessages.StoreUnavailable} ({exception.Message})");
    }

    /// <summary>
    /// Runs one read through the seam's strict route and hands each row to <paramref name="readRow"/>. A failure
    /// propagates, because an unreachable store and an empty roster must not look alike (FR-096), and an
    /// unconfigured store answers the <see cref="InvalidOperationException"/> this repository always answered.
    /// </summary>
    private async Task ExecuteReaderAsync(
        string procedureName,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken,
        Action<IReadOnlyDictionary<string, object?>> readRow)
    {
        var rows = await _mySqlHelperServer.ExecuteStrictAsync(
            CreateRequest(),
            (scope, token) => scope.QueryAsync(procedureName, parameters, token),
            cancellationToken).ConfigureAwait(false);

        foreach (var row in rows)
        {
            readRow(row);
        }
    }

    /// <summary>
    /// The strict request this repository makes: the waitlist store, with the connect timeout it has always
    /// applied over the connection string's own.
    /// </summary>
    private MySqlStrictScopeRequest CreateRequest() =>
        new(
            MySqlDatabaseTarget.MtmWaitlist,
            ConnectionTimeoutSeconds: _waitlistDatabaseOptions.ConnectionTimeoutSeconds);

    private static string? NormalizeOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string ReadString(IReadOnlyDictionary<string, object?> row, string columnName) =>
        row.TryGetValue(columnName, out var value) && value is not null && value is not DBNull
            ? Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)?.Trim() ?? string.Empty
            : string.Empty;

    private static long ReadLong(IReadOnlyDictionary<string, object?> row, string columnName) =>
        row.TryGetValue(columnName, out var value) && value is not null && value is not DBNull
            ? Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture)
            : 0L;

    private static int ReadInt(IReadOnlyDictionary<string, object?> row, string columnName) =>
        (int)ReadLong(row, columnName);

    private static bool ReadBool(IReadOnlyDictionary<string, object?> row, string columnName) =>
        ReadLong(row, columnName) != 0;
}
