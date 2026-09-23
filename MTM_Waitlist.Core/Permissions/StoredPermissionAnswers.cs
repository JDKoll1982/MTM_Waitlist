namespace MTM_Waitlist.Module_Core.Permissions;

/// <summary>
/// Turns the permission rows the store holds into one answer per key, in FR-049's order: the person's own stored
/// row wins, their role's row is inherited, and a key with neither is answered by the shipped fallback.
/// </summary>
/// <remarks>
/// <para>
/// <b>One composition, two readers.</b> The client application reads the rows through
/// <c>IMySqlHelperServer</c> and the on-host service reads them over its own connection, but both must answer the
/// same question the same way. This type is where that answer lives, so the service host does not compose its own
/// copy of the rule (FR-050, FR-053, FR-060).
/// </para>
/// <para>
/// It reads no configuration and no state of its own: a caller supplies rows and receives answers.
/// </para>
/// </remarks>
public static class StoredPermissionAnswers
{
    /// <summary>
    /// The read that returns the rows stored for one person and for their role, resolving the role from the
    /// person's own assignment inside the procedure.
    /// </summary>
    public const string StoredPermissionsProcedure = "sp_config_permissions_user_get";

    /// <summary>The scope type of a row written for one person.</summary>
    public const string UserScopeType = "user";

    /// <summary>The scope type of a row written for a role's baseline.</summary>
    public const string RoleScopeType = "role";

    /// <summary>
    /// Composes the stored rows into one answer per declared key.
    /// </summary>
    /// <param name="rows">The procedure's rows, in any order.</param>
    /// <returns>
    /// The answers the store holds. A key the store does not hold is absent, which is what makes the caller fall
    /// back to the declaration rather than to a guess.
    /// </returns>
    public static Dictionary<string, bool> Compose(IEnumerable<IReadOnlyDictionary<string, object?>> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        var answers = new Dictionary<string, bool>(StringComparer.Ordinal);

        // Role rows are applied first and the person's own rows second, so the person wins whatever order the
        // procedure returned them in. Relying on the ORDER BY would put this rule in the store's SELECT list.
        foreach (var row in rows.OrderBy(row => string.Equals(ReadString(row, "scope_type"), UserScopeType, StringComparison.OrdinalIgnoreCase) ? 1 : 0))
        {
            var key = ReadString(row, "setting_key");
            if (string.IsNullOrWhiteSpace(key) || !PermissionRegistry.IsDeclared(key))
            {
                // A stored row for a key the declaration does not hold is not an answer: the declaration is the
                // only place a permission exists (FR-046), and the two-direction check reports the drift.
                continue;
            }

            var scopeType = ReadString(row, "scope_type");
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

    /// <summary>
    /// The answer for one key: what the store holds, otherwise the declaration's shipped fallback, so an
    /// unanswerable store is a stated fallback rather than a refusal (FR-050, FR-060).
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="permissionKey"/> is not held by the declaration.</exception>
    public static bool AnswerFor(string permissionKey, IReadOnlyDictionary<string, bool> storedAnswers)
    {
        ArgumentNullException.ThrowIfNull(storedAnswers);

        var entry = PermissionRegistry.Find(permissionKey)
            ?? throw new ArgumentException(
                $"'{permissionKey}' is not a permission the declaration holds. A gate that reads an undeclared key is a failure rather than a silent refusal (FR-061).",
                nameof(permissionKey));

        return storedAnswers.TryGetValue(permissionKey, out var storedAnswer) ? storedAnswer : entry.Fallback;
    }

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
