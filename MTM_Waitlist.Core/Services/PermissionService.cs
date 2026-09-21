using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Permissions;

namespace MTM_Waitlist.Module_Core.Services;

/// <inheritdoc cref="IPermissionService"/>
/// <remarks>
/// <para>
/// One read of <c>sp_config_permissions_user_get</c> answers every permission for one person: the procedure
/// returns the rows stored for them and for their role, and this type composes the answer in FR-049's order. A
/// person's own row wins, a role row is inherited, and a key with neither is answered by the shipped fallback.
/// </para>
/// <para>
/// A store that cannot be reached is answered from the fallback rather than by a refusal (FR-050), so a settings
/// failure does not turn every control in the application into a refusal. The failure is recorded, never
/// swallowed silently.
/// </para>
/// <para>
/// The read is cached for the session and keyed on the person it belongs to, so a sign-in that resolves somebody
/// else reads again rather than handing back the previous person's answers.
/// </para>
/// </remarks>
public sealed class PermissionService : IPermissionService
{
    /// <summary>The permission rows stored for one person and for their role.</summary>
    private const string StoredPermissionsProcedure = "sp_config_permissions_user_get";

    private const string UserScopeType = "user";
    private const string RoleScopeType = "role";

    private readonly IMySqlHelperServer _mySqlHelperServer;
    private readonly StartupState _startupState;
    private readonly SemaphoreSlim _readGate = new(1, 1);

    private Dictionary<string, bool>? _storedAnswers;
    private long _cachedUserId = long.MinValue;
    private string _cachedRoleCode = string.Empty;

    public PermissionService(IMySqlHelperServer mySqlHelperServer, StartupState startupState)
    {
        _mySqlHelperServer = mySqlHelperServer ?? throw new ArgumentNullException(nameof(mySqlHelperServer));
        _startupState = startupState ?? throw new ArgumentNullException(nameof(startupState));
    }

    /// <inheritdoc />
    public async Task<bool> HasPermissionAsync(string permissionKey, CancellationToken cancellationToken = default)
    {
        var answers = await HasPermissionsAsync(new[] { permissionKey }, cancellationToken).ConfigureAwait(false);
        return answers[permissionKey];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, bool>> HasPermissionsAsync(
        IEnumerable<string> permissionKeys,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(permissionKeys);

        var requested = permissionKeys
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Select(key => key.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        foreach (var key in requested)
        {
            RequireDeclared(key);
        }

        var stored = await ReadStoredAnswersAsync(cancellationToken).ConfigureAwait(false);

        var answers = new Dictionary<string, bool>(StringComparer.Ordinal);
        foreach (var key in requested)
        {
            answers[key] = stored.TryGetValue(key, out var storedAnswer)
                ? storedAnswer
                : PermissionRegistry.Find(key)!.Fallback;
        }

        return answers;
    }

    /// <inheritdoc />
    public void Invalidate()
    {
        _storedAnswers = null;
        _cachedUserId = long.MinValue;
        _cachedRoleCode = string.Empty;
    }

    /// <summary>
    /// The answers the store holds for the signed-in person, re-read when the person or their role has changed.
    /// </summary>
    private async Task<Dictionary<string, bool>> ReadStoredAnswersAsync(CancellationToken cancellationToken)
    {
        var userId = _startupState.UserId;
        var roleCode = _startupState.CurrentRoleCode?.Trim() ?? string.Empty;

        await _readGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_storedAnswers is not null && _cachedUserId == userId && string.Equals(_cachedRoleCode, roleCode, StringComparison.Ordinal))
            {
                return _storedAnswers;
            }

            var answers = await TryReadStoredAnswersAsync(userId, cancellationToken).ConfigureAwait(false);

            _storedAnswers = answers;
            _cachedUserId = userId;
            _cachedRoleCode = roleCode;

            return answers;
        }
        finally
        {
            _readGate.Release();
        }
    }

    /// <summary>
    /// Reads the stored rows and composes them, or answers an empty set when the store cannot be reached.
    /// </summary>
    /// <remarks>
    /// An empty set is not an outage anyone sees as a refusal: every key then resolves to its shipped fallback,
    /// which is what FR-050 asks for. There is no person id to ask about before sign-in has resolved one, and the
    /// fallback is the honest answer then too.
    /// </remarks>
    private async Task<Dictionary<string, bool>> TryReadStoredAnswersAsync(long userId, CancellationToken cancellationToken)
    {
        var answers = new Dictionary<string, bool>(StringComparer.Ordinal);

        if (userId <= 0)
        {
            return answers;
        }

        IReadOnlyList<Dictionary<string, object?>> rows;
        try
        {
            rows = await _mySqlHelperServer
                .ExecuteStoredProcedureQueryAsync(
                    StoredPermissionsProcedure,
                    new Dictionary<string, object?> { ["p_user_id"] = userId },
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
            StartupDebugLog.Error(
                "Permissions",
                ex,
                "Could not read the stored permission rows; every gate answers from the shipped fallback rather than refusing (FR-050).");

            return answers;
        }

        // Role rows are applied first and the person's own rows second, so the person wins whatever order the
        // procedure returned them in. Relying on the ORDER BY would put this rule in the store's SELECT list.
        foreach (var row in rows.OrderBy(row => ReadScopeType(row) == UserScopeType ? 1 : 0))
        {
            var key = ReadString(row, "setting_key");
            if (string.IsNullOrWhiteSpace(key) || !PermissionRegistry.IsDeclared(key))
            {
                // A stored row for a key the declaration does not hold is not an answer: the declaration is the
                // only place a permission exists (FR-046), and the two-direction check reports the drift.
                continue;
            }

            var scopeType = ReadScopeType(row);
            if (!string.Equals(scopeType, UserScopeType, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(scopeType, RoleScopeType, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = ReadBool(row, "setting_value_bool");
            if (value is null)
            {
                continue;
            }

            answers[key] = value.Value;
        }

        return answers;
    }

    private static void RequireDeclared(string permissionKey)
    {
        if (!PermissionRegistry.IsDeclared(permissionKey))
        {
            throw new ArgumentException(
                $"'{permissionKey}' is not a permission the declaration holds. A gate that reads an undeclared key is a failure rather than a silent refusal (FR-061).",
                nameof(permissionKey));
        }
    }

    private static string ReadScopeType(IReadOnlyDictionary<string, object?> row) => ReadString(row, "scope_type");

    private static string ReadString(IReadOnlyDictionary<string, object?> row, string columnName)
    {
        if (!row.TryGetValue(columnName, out var value) || value is null || value is DBNull)
        {
            return string.Empty;
        }

        return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
    }

    private static bool? ReadBool(IReadOnlyDictionary<string, object?> row, string columnName)
    {
        if (!row.TryGetValue(columnName, out var value) || value is null || value is DBNull)
        {
            return null;
        }

        return value switch
        {
            bool boolean => boolean,
            sbyte number => number != 0,
            byte number => number != 0,
            short number => number != 0,
            int number => number != 0,
            long number => number != 0,
            _ => bool.TryParse(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture), out var parsed)
                ? parsed
                : null,
        };
    }
}
