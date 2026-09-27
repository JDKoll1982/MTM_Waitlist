using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Module_Startup.Services;

/// <summary>
/// The person identity contract's implementation: the resolved person, and the store read that resolves them.
/// </summary>
/// <remarks>
/// <para>
/// <b>The launch pipeline is the only writer (FR-022).</b> <see cref="ResolveAsync"/> and <see cref="Apply"/>
/// exist for the sign-in step to call; <see cref="Clear"/> exists for sign-out (FR-011). No consumer reads a
/// setter: consumers depend on <see cref="IPersonIdentity"/>, which has none. The type is registered as a
/// singleton and both the concrete type and the interface resolve to the same instance, so the pipeline and the
/// consumers see one person rather than two copies of one.
/// </para>
/// <para>
/// <b>Roles come from the store only.</b> <see cref="ResolveAsync"/> reads
/// <c>sp_auth_user_row_get</c> — the existing "resolve one active user's identity and role at logon" read — so
/// the role in force is the store's answer and never a local claim. The development allow-list in
/// <c>appsettings.json</c> is retired (D16).
/// </para>
/// <para>
/// <b>Held roles.</b> <see cref="Apply"/> takes every role code the sign-in step resolved, which is what
/// answers <see cref="Holds"/> for a person who holds more than one. <see cref="ResolveAsync"/> itself knows the
/// one role the store's logon read returns, so it applies that as the single held role; the wider set is the
/// sign-in step's to apply when it reads the person's assignments.
/// </para>
/// </remarks>
public sealed class PersonIdentityService : IPersonIdentity
{
    /// <summary>The store read that resolves one active person's identity and role at logon.</summary>
    private const string IdentityProcedure = "sp_auth_user_row_get";

    /// <summary>The scope-key prefix a role code may be written with, as in <c>role:developer</c>.</summary>
    private const string RoleCodePrefix = "role:";

    private readonly IMySqlHelperServer _mySqlHelperServer;
    private readonly HashSet<string> _heldRoleCodes = new(StringComparer.OrdinalIgnoreCase);

    private long _userId;
    private string _signInName = string.Empty;
    private string _displayName = string.Empty;
    private string? _employeeNumber;
    private string _currentRoleCode = string.Empty;

    public PersonIdentityService(IMySqlHelperServer mySqlHelperServer)
    {
        _mySqlHelperServer = mySqlHelperServer ?? throw new ArgumentNullException(nameof(mySqlHelperServer));
    }

    /// <inheritdoc />
    public long UserId => _userId;

    /// <inheritdoc />
    public string SignInName => _signInName;

    /// <inheritdoc />
    public string DisplayName => _displayName;

    /// <inheritdoc />
    public string? EmployeeNumber => _employeeNumber;

    /// <inheritdoc />
    public string CurrentRoleCode => _currentRoleCode;

    /// <inheritdoc />
    public IReadOnlyList<string> HeldRoleCodes => [.. _heldRoleCodes];

    /// <inheritdoc />
    public bool IsSignedIn => _userId > 0;

    /// <inheritdoc />
    public bool Holds(string roleCode)
    {
        var normalized = NormalizeRoleCode(roleCode);

        return normalized.Length > 0 && _heldRoleCodes.Contains(normalized);
    }

    /// <summary>
    /// Reads one active person by sign-in name and applies the answer. Called by the launch pipeline's sign-in
    /// step; nothing else writes here.
    /// </summary>
    /// <param name="signInName">The sign-in name the credential check resolved, in the store's upper case.</param>
    /// <param name="cancellationToken">Cancels the store read.</param>
    /// <returns><see langword="true"/> when the store holds the person, otherwise <see langword="false"/>.</returns>
    /// <remarks>
    /// A blank name resolves nobody, so it ends any identity in force rather than leaving it standing. The answer
    /// is "nobody is signed in" either way, and a caller that resolved nothing must never find the previous person
    /// still there.
    /// </remarks>
    public async Task<bool> ResolveAsync(string signInName, CancellationToken cancellationToken = default)
    {
        var normalizedSignInName = signInName?.Trim() ?? string.Empty;
        if (normalizedSignInName.Length == 0)
        {
            // Nothing was asked for, so there is no person to resolve and no row that could answer. End the
            // identity, which is the same answer the store's empty read gives below: a resolve that finds nobody
            // never leaves the previous person signed in.
            Clear();

            return false;
        }

        var rows = await _mySqlHelperServer
            .ExecuteStoredProcedureQueryAsync(
                IdentityProcedure,
                new Dictionary<string, object?> { ["p_username"] = normalizedSignInName },
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken)
            .ConfigureAwait(false);

        var row = rows.FirstOrDefault();
        if (row is null)
        {
            Clear();
            return false;
        }

        var roleCode = ReadString(row, "role_code");

        Apply(
            ReadInt64(row, "id"),
            normalizedSignInName,
            ReadString(row, "display_name"),
            NullableString(row, "employee_identifier"),
            roleCode,
            [roleCode]);

        return IsSignedIn;
    }

    /// <summary>
    /// Applies the person the pipeline resolved. The launch pipeline is the only caller.
    /// </summary>
    /// <param name="userId">The person's <c>core_users_profiles.id</c>.</param>
    /// <param name="signInName">The person's normalised sign-in name.</param>
    /// <param name="displayName">The name the badge and the tooltips show.</param>
    /// <param name="employeeNumber">The employee identifier, or <c>null</c> when the store holds none.</param>
    /// <param name="currentRoleCode">The role in force for this session.</param>
    /// <param name="heldRoleCodes">
    /// Every role code the person holds. When omitted, the role in force is the only held role.
    /// </param>
    public void Apply(
        long userId,
        string signInName,
        string displayName,
        string? employeeNumber,
        string currentRoleCode,
        IEnumerable<string>? heldRoleCodes = null)
    {
        _userId = userId;
        _signInName = signInName?.Trim() ?? string.Empty;
        _displayName = displayName?.Trim() ?? string.Empty;
        _employeeNumber = string.IsNullOrWhiteSpace(employeeNumber) ? null : employeeNumber.Trim();
        _currentRoleCode = currentRoleCode?.Trim() ?? string.Empty;

        _heldRoleCodes.Clear();

        foreach (var roleCode in heldRoleCodes ?? Array.Empty<string>())
        {
            var normalized = NormalizeRoleCode(roleCode);
            if (normalized.Length > 0)
            {
                _heldRoleCodes.Add(normalized);
            }
        }

        // The role in force is always held, even when the caller passed no set at all: a person is never
        // answered as not holding the role the store put them in.
        var current = NormalizeRoleCode(_currentRoleCode);
        if (current.Length > 0)
        {
            _heldRoleCodes.Add(current);
        }
    }

    /// <summary>
    /// Ends the identity, so the next read reports nobody signed in (FR-011). Called by sign-out; nothing is
    /// persisted on the machine either way, because the identity is never stored locally.
    /// </summary>
    public void Clear()
    {
        _userId = 0;
        _signInName = string.Empty;
        _displayName = string.Empty;
        _employeeNumber = null;
        _currentRoleCode = string.Empty;
        _heldRoleCodes.Clear();
    }

    /// <summary>
    /// A role code in the form the catalogue holds it: trimmed, with an optional <c>role:</c> scope-key prefix
    /// removed, so <c>role:developer</c> and <c>developer</c> are the same code.
    /// </summary>
    public static string NormalizeRoleCode(string? roleCode)
    {
        var trimmed = roleCode?.Trim() ?? string.Empty;

        return trimmed.StartsWith(RoleCodePrefix, StringComparison.OrdinalIgnoreCase)
            ? trimmed[RoleCodePrefix.Length..].Trim()
            : trimmed;
    }

    private static string ReadString(IReadOnlyDictionary<string, object?> row, string key) =>
        NullableString(row, key) ?? string.Empty;

    private static string? NullableString(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        var text = Convert.ToString(value)?.Trim();

        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static long ReadInt64(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null)
        {
            return 0;
        }

        // TINYINT(1)/BIT columns are surfaced by the MySQL driver as a bool, and Convert.ToInt64 on a bool
        // throws InvalidCastException, so the type is guarded BEFORE converting rather than caught afterwards.
        if (value is bool boolValue)
        {
            return boolValue ? 1 : 0;
        }

        if (value is long longValue)
        {
            return longValue;
        }

        try
        {
            return Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
        {
            return 0;
        }
    }
}
