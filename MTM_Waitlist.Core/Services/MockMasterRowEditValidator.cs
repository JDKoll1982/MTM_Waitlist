using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Core.Services;

/// <summary>
/// Pure client-side validation for editing a row of a Developer-editable <c>mock_*</c> master table before it is
/// written through the per-table insert/update SPs. The grid editor only ever passes business (non-audit)
/// columns; the SPs manage audit columns (id/public_id/created_utc/updated_utc) and default optional ones
/// internally. Deterministic and unit-testable.
/// </summary>
public static class MockMasterRowEditValidator
{
    /// <summary>
    /// Validates a row's <paramref name="values"/> (keyed by column name, case-insensitive) against the table's
    /// <paramref name="columns"/>. Returns user-safe messages when a supplied column is not an editable business
    /// column, or when a required (non-nullable, non-audit) business column is missing or blank.
    /// </summary>
    public static IReadOnlyList<string> Validate(
        IReadOnlyList<MockMasterColumnDefinition> columns,
        IReadOnlyDictionary<string, object?> values)
    {
        var errors = new List<string>();

        var editableByName = new Dictionary<string, MockMasterColumnDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var column in columns)
        {
            if (!column.IsAudit)
            {
                editableByName[column.ColumnName] = column;
            }
        }

        // Reject any supplied value for a non-editable / audit column.
        foreach (var key in values.Keys)
        {
            if (!editableByName.ContainsKey(key))
            {
                errors.Add($"Column '{key}' is not editable for this table.");
            }
        }

        // Ensure required business columns are present and non-blank.
        foreach (var column in columns)
        {
            if (column.IsAudit || column.IsNullable)
            {
                continue;
            }

            var hasValue = values.TryGetValue(column.ColumnName, out var raw)
                && raw is not null
                && raw != DBNull.Value
                && !IsBlankString(raw);
            if (!hasValue)
            {
                errors.Add($"Column '{column.ColumnName}' is required.");
            }
        }

        return errors;
    }

    private static bool IsBlankString(object value)
        => value is string text && string.IsNullOrWhiteSpace(text);
}
