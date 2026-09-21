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
            answers[key] = StoredPermissionAnswers.AnswerFor(key, stored);
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
                    StoredPermissionAnswers.StoredPermissionsProcedure,
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

        return StoredPermissionAnswers.Compose(rows);
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
}
