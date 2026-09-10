using System.Globalization;

namespace MTM_Waitlist.Mock.Services;

/// <summary>
/// Converts raw provider rows into the strongly typed values the read shapes expose.
/// </summary>
/// <remarks>
/// Live reads come back from <c>Microsoft.Data.SqlClient</c> and cached reads from
/// <c>MySqlConnector</c>, whose value types do not always agree (for example a bit versus a tinyint, or
/// a numeric versus a decimal). Converting through the row reader keeps a shape's result identical
/// whichever source answered (FR-004, SC-004).
/// </remarks>
internal static class VisualRowReader
{
    /// <summary>Reads a trimmed string, or an empty string when the column is absent, null, or blank.</summary>
    public static string GetString(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (row.TryGetValue(key, out var value) && value is not null)
        {
            return Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
        }

        return string.Empty;
    }

    /// <summary>Reads a decimal quantity, or zero when the column is absent, null, or unparseable.</summary>
    public static decimal GetDecimal(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null)
        {
            return 0m;
        }

        return value switch
        {
            decimal decimalValue => decimalValue,
            double doubleValue => Convert.ToDecimal(doubleValue, CultureInfo.InvariantCulture),
            float floatValue => Convert.ToDecimal(floatValue, CultureInfo.InvariantCulture),
            int intValue => intValue,
            long longValue => longValue,
            short shortValue => shortValue,
            byte byteValue => byteValue,
            _ => decimal.TryParse(
                Convert.ToString(value, CultureInfo.InvariantCulture),
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var parsed)
                ? parsed
                : 0m,
        };
    }

    /// <summary>Reads a flag, or <see langword="false"/> when the column is absent, null, or unparseable.</summary>
    public static bool GetBoolean(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null)
        {
            return false;
        }

        return value switch
        {
            bool boolValue => boolValue,
            byte byteValue => byteValue != 0,
            sbyte signedByteValue => signedByteValue != 0,
            short shortValue => shortValue != 0,
            int intValue => intValue != 0,
            long longValue => longValue != 0,
            decimal decimalValue => decimalValue != 0m,
            _ => bool.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var parsed) && parsed,
        };
    }
}
