using System.Text.Json;

using Microsoft.Extensions.Options;
using MySqlConnector;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Permissions;

namespace MTM_Waitlist.Module_Core.Services;

/// <inheritdoc cref="IPermissionAdministrationService"/>
/// <remarks>
/// <para>
/// <b>Every read and write goes through a stored procedure</b> (constitution III): `sp_config_permissions_user_get`
/// composes nothing and returns what is stored, `sp_config_permissions_user_set` takes the whole change set in one
/// call, and `sp_config_permissions_reversal_get` returns the recorded history of the last one. No statement text
/// lives here.
/// </para>
/// <para>
/// <b>Why this talks to the provider directly rather than through <see cref="IMySqlHelperServer"/>.</b> The helper
/// server retries a failed call and answers with its neutral result. Both halves of that are wrong here, and both
/// were found by running it. A refusal this feature's procedures raise is a <b>refusal</b>, not an outage, so
/// folding it into zero affected rows reports a change that was refused as a change that landed. And a write is
/// not a read: retrying one after a connection dropped mid-transaction is how a single reader's single act becomes
/// two. So the connection is opened here, the provider's own error is read, and nothing is retried.
/// </para>
/// <para>
/// <b>The composition is here, not in the store, and there is only one of it.</b> A key with a row of the person's
/// own takes it, a key with only a row for their role inherits it, and a key with neither answers from the
/// declaration's shipped fallback. That is FR-049's order; keeping it in one place is what stops the page and the
/// gate it describes from disagreeing.
/// </para>
/// <para>
/// <b>A save and its reversal are the same call.</b> Both go to <see cref="ApplyAsync"/>, so both meet the same
/// rank rule, the same fixed row and the same moved-value check, and a save of several permissions is either all
/// applied or none of it is (FR-069, FR-071).
/// </para>
/// </remarks>
public sealed class PermissionAdministrationService : IPermissionAdministrationService
{
    private const string GetProcedure = "sp_config_permissions_user_get";
    private const string SetProcedure = "sp_config_permissions_user_set";
    private const string ReversalProcedure = "sp_config_permissions_reversal_get";
    private const string BaselineProcedure = "sp_config_permissions_feature_baseline_get";
    private const string HoldersProcedure = "sp_config_permissions_feature_holders_get";

    /// <summary>The fixed row, which this service never writes and the store refuses as well (FR-059).</summary>
    private const string FixedKey = PermissionKeys.AdminPermissions;

    /// <summary>A refusal this feature's procedures raise with <c>SIGNAL SQLSTATE '45000'</c>.</summary>
    private const int SignalledRefusalErrorNumber = 1644;

    private const string OutranksToken = "mtm_target_outranks_actor";
    private const string GateFixedToken = "mtm_permission_gate_fixed";
    private const string KeyInvalidToken = "mtm_permission_key_invalid";
    private const string ValueMovedToken = "mtm_permission_value_moved";

    private const string UserScopePrefix = "user:";
    private const string PermissionNamespace = "permission.";

    private const string WaitlistConnectionStringEnvironmentVariable = "MTM_WAITLIST_DB_CONNECTION_STRING";
    private const int DefaultCommandTimeoutSeconds = 15;

    private readonly StartupDatabaseOptions _startupDatabaseOptions;
    private readonly IPermissionService _permissionService;
    private readonly StartupState _startupState;

    public PermissionAdministrationService(
        IOptions<StartupDatabaseOptions> startupDatabaseOptions,
        IPermissionService permissionService,
        StartupState startupState)
    {
        ArgumentNullException.ThrowIfNull(startupDatabaseOptions);

        _startupDatabaseOptions = startupDatabaseOptions.Value;
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        _startupState = startupState ?? throw new ArgumentNullException(nameof(startupState));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PermissionValueRow>> GetForPersonAsync(
        long userId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Dictionary<string, object?>> stored;
        try
        {
            stored = await ReadRowsAsync(
                    GetProcedure,
                    new Dictionary<string, object?> { ["p_user_id"] = userId },
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error(
                "Permissions",
                ex,
                "The stored permission rows could not be read; the page shows its unavailable state rather than an answer it does not have.");

            throw;
        }

        var chosen = new Dictionary<string, bool>(StringComparer.Ordinal);
        var inherited = new Dictionary<string, bool>(StringComparer.Ordinal);

        foreach (var row in stored)
        {
            var key = ReadString(row, "setting_key");
            if (key.Length == 0)
            {
                continue;
            }

            // A row whose boolean is absent carries no answer, so it answers nothing: a value that is not there
            // must not be read as `false` and quietly deny somebody a thing nobody denied them.
            var value = ReadNullableBool(row, "setting_value_bool");
            if (value is null)
            {
                continue;
            }

            var scopeType = ReadString(row, "scope_type");            if (string.Equals(scopeType, "user", StringComparison.OrdinalIgnoreCase))
            {
                chosen[key] = value.Value;
            }
            else if (string.Equals(scopeType, "role", StringComparison.OrdinalIgnoreCase))
            {
                inherited[key] = value.Value;
            }
        }

        var composed = new List<PermissionValueRow>(PermissionRegistry.All.Count);
        foreach (var entry in PermissionRegistry.All)
        {
            if (chosen.TryGetValue(entry.Key, out var ownValue))
            {
                composed.Add(new PermissionValueRow(entry.Key, ownValue, PermissionProvenance.Chosen));
            }
            else if (inherited.TryGetValue(entry.Key, out var baselineValue))
            {
                composed.Add(new PermissionValueRow(entry.Key, baselineValue, PermissionProvenance.Inherited));
            }
            else
            {
                composed.Add(new PermissionValueRow(entry.Key, entry.Fallback, PermissionProvenance.Fallback));
            }
        }

        return composed;
    }

    /// <inheritdoc />
    public async Task<PermissionChangeResult> ApplyAsync(
        long userId,
        IReadOnlyList<PermissionChange> changes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(changes);

        if (changes.Count == 0)
        {
            // Nothing to write, and the store would write nothing either. Reported as a save rather than as a
            // refusal, because nothing was asked for and nothing was refused.
            return PermissionChangeResult.Succeeded();
        }

        var payload = JsonSerializer.Serialize(changes.Select(change => new
        {
            key = change.Key,
            from = change.From,
            to = change.To,
        }));

        try
        {
            await ExecuteNonQueryAsync(
                    SetProcedure,
                    new Dictionary<string, object?>
                    {
                        ["p_user_id"] = userId,
                        ["p_actor_user_id"] = _startupState.UserId,
                        ["p_changes_json"] = payload,
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // The reader asked to stop. Nothing was committed, and a second attempt is not started here.
            throw;
        }
        catch (MySqlConnector.MySqlException ex) when (ex.Number == SignalledRefusalErrorNumber)
        {
            return MapRefusal(ex.Message ?? string.Empty);
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("Permissions", ex, "The change set did not reach the store; nothing was written.");
            return PermissionChangeResult.Failed(
                PermissionChangeOutcomeKind.StoreUnavailable,
                PermissionAdministrationMessages.StoreUnavailableKey,
                PermissionAdministrationMessages.StoreUnavailable);
        }

        // The answer the gates hold is now out of date: the next lookup must read the store rather than the value
        // it cached before this save (FR-072).
        _permissionService.Invalidate();

        return PermissionChangeResult.Succeeded();
    }

    /// <inheritdoc />
    public async Task<PermissionChangeResult> ReverseLastSaveAsync(
        long userId,
        bool restoreDespiteMovedValue = false,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Dictionary<string, object?>> recorded;
        try
        {
            recorded = await ReadRowsAsync(
                    ReversalProcedure,
                    new Dictionary<string, object?> { ["p_user_id"] = userId },
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("Permissions", ex, "The recorded history could not be read; nothing was reversed.");
            return PermissionChangeResult.Failed(
                PermissionChangeOutcomeKind.StoreUnavailable,
                PermissionAdministrationMessages.StoreUnavailableKey,
                PermissionAdministrationMessages.StoreUnavailable);
        }

        if (recorded.Count == 0)
        {
            return PermissionChangeResult.Failed(
                PermissionChangeOutcomeKind.NothingToReverse,
                PermissionAdministrationMessages.NothingToReverseKey,
                PermissionAdministrationMessages.NothingToReverse);
        }

        var changes = new List<PermissionChange>(recorded.Count);

        // The second half of FR-070: the reader has seen what the value is now and wants the restore anyway, so
        // the value now in force is what the store is asked to check against rather than the value the save left.
        Dictionary<string, bool?>? valueNow = null;
        if (restoreDespiteMovedValue)
        {
            var current = await GetForPersonAsync(userId, cancellationToken).ConfigureAwait(false);

            // Only a value the person actually has of their own is a stored value; an inherited answer means there
            // is no row, and the store is asked to expect none.
            valueNow = current.ToDictionary(
                row => row.Key,
                row => row.Provenance == PermissionProvenance.Chosen ? (bool?)row.Value : null,
                StringComparer.Ordinal);
        }

        foreach (var row in recorded)
        {
            var key = ReadString(row, "setting_key");
            if (key.Length == 0)
            {
                continue;
            }

            // Back the other way round: what the save wrote is now the `from`, and the value that was in force
            // before it is what to write. Where the person had no choice of their own before the save, that value
            // is their role's baseline, and where the key has no baseline either it is the shipped fallback. The
            // person's own row is written rather than removed, because this history table names the value row it
            // changed and the foreign key refuses a row naming one that is gone.
            bool? from;
            if (valueNow is not null && valueNow.TryGetValue(key, out var now))
            {
                from = now;
            }
            else
            {
                from = ReadNullableBool(row, "changed_setting_value_bool");
            }

            var previous = ReadNullableBool(row, "previous_setting_value_bool")
                ?? ReadNullableBool(row, "baseline_setting_value_bool")
                ?? PermissionRegistry.Find(key)?.Fallback
                ?? false;

            changes.Add(new PermissionChange(key, from, previous));
        }

        if (changes.Count == 0)
        {
            return PermissionChangeResult.Failed(
                PermissionChangeOutcomeKind.NothingToReverse,
                PermissionAdministrationMessages.NothingToReverseKey,
                PermissionAdministrationMessages.NothingToReverse);
        }

        return await ApplyAsync(userId, changes, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Turns the store's own refusal into the one typed answer the caller reports, keeping the key a moved or
    /// invalid value named.
    /// </summary>
    private static PermissionChangeResult MapRefusal(string message)
    {
        if (message.Contains(ValueMovedToken, StringComparison.Ordinal))
        {
            return PermissionChangeResult.Failed(
                PermissionChangeOutcomeKind.ValueMoved,
                PermissionAdministrationMessages.ValueMovedKey,
                PermissionAdministrationMessages.ValueMoved,
                KeyAfter(message, ValueMovedToken));
        }

        if (message.Contains(GateFixedToken, StringComparison.Ordinal))
        {
            return PermissionChangeResult.Failed(
                PermissionChangeOutcomeKind.GateFixed,
                PermissionAdministrationMessages.GateFixedKey,
                PermissionAdministrationMessages.GateFixed,
                FixedKey);
        }

        if (message.Contains(KeyInvalidToken, StringComparison.Ordinal))
        {
            return PermissionChangeResult.Failed(
                PermissionChangeOutcomeKind.KeyInvalid,
                PermissionAdministrationMessages.KeyInvalidKey,
                PermissionAdministrationMessages.KeyInvalid,
                KeyAfter(message, KeyInvalidToken));
        }

        if (message.Contains(OutranksToken, StringComparison.Ordinal))
        {
            return PermissionChangeResult.Failed(
                PermissionChangeOutcomeKind.TargetOutranksActor,
                PermissionAdministrationMessages.OutrankedKey,
                PermissionAdministrationMessages.Outranked);
        }

        // A refusal the tokens do not name is still a refusal, and nothing was written.
        return PermissionChangeResult.Failed(
            PermissionChangeOutcomeKind.StoreUnavailable,
            PermissionAdministrationMessages.StoreUnavailableKey,
            PermissionAdministrationMessages.StoreUnavailable);
    }

    /// <inheritdoc />
    public async Task<PermissionHolders> GetHoldersAsync(
        string permissionKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(permissionKey))
        {
            return new PermissionHolders([], []);
        }

        var roles = await ReadRowsAsync(
                BaselineProcedure,
                new Dictionary<string, object?> { ["p_setting_key"] = permissionKey },
                cancellationToken)
            .ConfigureAwait(false);

        var people = await ReadRowsAsync(
                HoldersProcedure,
                new Dictionary<string, object?> { ["p_setting_key"] = permissionKey },
                cancellationToken)
            .ConfigureAwait(false);

        var roleCodes = roles
            .Select(row => ReadString(row, "role_code"))
            .Where(code => code.Length > 0)
            .ToArray();

        var holders = people
            .Select(row => new PermissionHolder(
                ReadLong(row, "user_id"),
                ReadString(row, "display_name"),
                ReadString(row, "employee_identifier"),
                ReadString(row, "role_code"),
                IsSwitchedOff: !ReadBool(row, "is_active"),
                IsGranted: ReadNullableBool(row, "setting_value_bool") ?? false))
            .ToArray();

        return new PermissionHolders(roleCodes, holders);
    }

    /// <summary>The key a token names, from the <c>token:key</c> form the procedures raise.</summary>
    private static string KeyAfter(string message, string token)
    {
        var index = message.IndexOf(token, StringComparison.Ordinal);
        if (index < 0)
        {
            return string.Empty;
        }

        var rest = message[(index + token.Length)..];

        // The provider wraps a signalled message in its own prose, so the key ends at the first separator rather
        // than at the end of the string. A permission key contains no space and no newline.
        var end = rest.IndexOfAny([' ', '\r', '\n', '\'']);
        var key = (end < 0 ? rest : rest[..end]).TrimStart(':').Trim();

        return key.StartsWith(PermissionNamespace, StringComparison.Ordinal) ? key : string.Empty;
    }

    /// <summary>
    /// Runs one stored-procedure read and returns its rows as name/value pairs, so the procedures'
    /// <c>setting_key</c> / <c>scope_type</c> columns are read by name rather than by position.
    /// </summary>
    private async Task<IReadOnlyList<Dictionary<string, object?>>> ReadRowsAsync(
        string procedureName,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = BuildCommand(connection, procedureName, parameters);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        var rows = new List<Dictionary<string, object?>>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var ordinal = 0; ordinal < reader.FieldCount; ordinal++)
            {
                row[reader.GetName(ordinal)] = reader.IsDBNull(ordinal) ? null : reader.GetValue(ordinal);
            }

            rows.Add(row);
        }

        return rows;
    }

    /// <summary>
    /// Runs one stored-procedure write and reads its affected-row count. The provider's own error propagates, so a
    /// refusal the procedure raised reaches <see cref="ApplyAsync"/> and is reported rather than swallowed.
    /// </summary>
    private async Task<int> ExecuteNonQueryAsync(
        string procedureName,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = BuildCommand(connection, procedureName, parameters);

        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<MySqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connectionString = ResolveConnectionString()
            ?? throw new InvalidOperationException("No connection is configured for the mtm_waitlist store.");

        var builder = new MySqlConnectionStringBuilder(connectionString)
        {
            ConnectionTimeout = (uint)Math.Max(1, _startupDatabaseOptions.ConnectionTimeoutSeconds),
        };

        // Resolved through the same host fallback every other reader uses: the configured host is the shared
        // plant server, so a workstation off the plant network would otherwise time out here while the
        // databases are present on the local server.
        var connection = new MySqlConnection(
            MySqlHostFallback.Apply(builder.ConnectionString) ?? builder.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        return connection;
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
    /// The store this service reads and writes. The environment variable wins, then the variable the startup options
    /// name, then the configured string, so a test can point every store at a live server without editing appsettings.
    /// </summary>
    private string? ResolveConnectionString()
    {
        var fromEnvironment = Environment.GetEnvironmentVariable(WaitlistConnectionStringEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return fromEnvironment;
        }

        var configuredName = _startupDatabaseOptions.ConnectionStringEnvironmentVariable;
        if (!string.IsNullOrWhiteSpace(configuredName))
        {
            var fromConfiguredName = Environment.GetEnvironmentVariable(configuredName);
            if (!string.IsNullOrWhiteSpace(fromConfiguredName))
            {
                return fromConfiguredName;
            }
        }

        return string.IsNullOrWhiteSpace(_startupDatabaseOptions.ConnectionString)
            ? null
            : _startupDatabaseOptions.ConnectionString;
    }

    private static string ReadString(IReadOnlyDictionary<string, object?> row, string columnName) =>
        row.TryGetValue(columnName, out var value) && value is not null && value is not DBNull
            ? Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)?.Trim() ?? string.Empty
            : string.Empty;

    private static long ReadLong(IReadOnlyDictionary<string, object?> row, string columnName) =>
        row.TryGetValue(columnName, out var value) && value is not null && value is not DBNull
            ? Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture)
            : 0L;

    private static bool ReadBool(IReadOnlyDictionary<string, object?> row, string columnName) =>
        ReadNullableBool(row, columnName) ?? false;

    private static bool? ReadNullableBool(IReadOnlyDictionary<string, object?> row, string columnName)
    {
        if (!row.TryGetValue(columnName, out var value) || value is null || value is DBNull)
        {
            return null;
        }

        return Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture) != 0;
    }
}
