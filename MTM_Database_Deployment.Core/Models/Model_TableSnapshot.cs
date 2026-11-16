namespace MTM_Database_Deployment.Models;

/// <summary>
/// One column as reported by information_schema, used for expected/live comparison.
/// </summary>
public sealed class Model_ColumnDefinition
{
    public required string Name { get; init; }
    public required string DataType { get; init; }
    public required bool IsNullable { get; init; }
    public string? DefaultValue { get; init; }
    public string Extra { get; init; } = string.Empty;
    public string Comment { get; init; } = string.Empty;
    public string? CharacterSet { get; init; }
    public string? Collation { get; init; }
    public int OrdinalPosition { get; init; }

    /// <summary>
    /// Full SQL fragment for a column definition (without the leading name).
    /// </summary>
    public string ToColumnDefinitionSql()
    {
        var builder = new System.Text.StringBuilder();
        builder.Append(DataType);
        if (!string.IsNullOrWhiteSpace(CharacterSet) && !string.IsNullOrWhiteSpace(Collation))
        {
            builder.Append(" CHARACTER SET ").Append(CharacterSet).Append(" COLLATE ").Append(Collation);
        }

        builder.Append(IsNullable ? " NULL" : " NOT NULL");

        if (!string.IsNullOrWhiteSpace(DefaultValue))
        {
            builder.Append(" DEFAULT ").Append(FormatDefault(DefaultValue, DataType, Extra));
        }

        if (Extra.Contains("auto_increment", StringComparison.OrdinalIgnoreCase))
        {
            builder.Append(" AUTO_INCREMENT");
        }

        if (Extra.Contains("on update", StringComparison.OrdinalIgnoreCase))
        {
            builder.Append(" ON UPDATE CURRENT_TIMESTAMP");
        }

        if (!string.IsNullOrWhiteSpace(Comment))
        {
            builder.Append(" COMMENT '").Append(Comment.Replace("'", "''")).Append('\'');
        }

        return builder.ToString();
    }

    /// <summary>
    /// information_schema reports a string default as the bare literal, so emitting it raw produces
    /// <c>DEFAULT Quantity</c> and MySQL rejects the statement. Numeric and expression defaults are
    /// already valid SQL and must stay unquoted.
    /// </summary>
    private static string FormatDefault(string value, string dataType, string extra)
    {
        if (extra.Contains("DEFAULT_GENERATED", StringComparison.OrdinalIgnoreCase)
            || IsExpressionDefault(value)
            || IsNumericType(dataType))
        {
            return value;
        }

        return "'" + value.Replace("'", "''") + "'";
    }

    private static bool IsExpressionDefault(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            return false;
        }

        return trimmed.Contains('(')
            || trimmed.StartsWith("b'", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("_binary", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("CURRENT_TIMESTAMP", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("CURRENT_DATE", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("CURRENT_TIME", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("NULL", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsNumericType(string dataType)
    {
        var typeName = dataType
            .Split(['(', ' '], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault() ?? string.Empty;
        return NumericTypeNames.Contains(typeName);
    }

    private static readonly HashSet<string> NumericTypeNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "tinyint", "smallint", "mediumint", "int", "integer", "bigint",
        "decimal", "numeric", "dec", "fixed", "float", "double", "real", "year"
    };

    public string Signature => $"{DataType}|{IsNullable}|{DefaultValue ?? "<null>"}|{Extra}|{Comment}|{Collation ?? string.Empty}";
}

/// <summary>
/// One index as reported by information_schema.
/// </summary>
public sealed class Model_IndexDefinition
{
    public required string Name { get; init; }
    public required bool IsUnique { get; init; }
    public IReadOnlyList<string> Columns { get; init; } = Array.Empty<string>();

    public string Signature => $"{(IsUnique ? "UNIQUE" : "INDEX")}|{string.Join(",", Columns)}";

    public string ToCreateSql(string tableName, string schemaName)
    {
        var columnList = string.Join(", ", Columns.Select(column => "`" + column + "`"));
        var uniqueKeyword = IsUnique ? "UNIQUE " : string.Empty;
        return $"ALTER TABLE `{schemaName}`.`{tableName}` ADD {uniqueKeyword}INDEX `{Name}` ({columnList});";
    }
}

/// <summary>
/// One foreign key as reported by information_schema.
/// </summary>
public sealed class Model_ForeignKeyDefinition
{
    public required string Name { get; init; }
    public IReadOnlyList<string> Columns { get; init; } = Array.Empty<string>();
    public string ReferencedTable { get; init; } = string.Empty;
    public IReadOnlyList<string> ReferencedColumns { get; init; } = Array.Empty<string>();
    public string UpdateRule { get; init; } = string.Empty;
    public string DeleteRule { get; init; } = string.Empty;

    public string Signature =>
        $"{string.Join(",", Columns)}->{ReferencedTable}({string.Join(",", ReferencedColumns)})|{UpdateRule}|{DeleteRule}";

    public string ToCreateSql(string tableName, string schemaName)
    {
        var columnList = string.Join(", ", Columns.Select(column => "`" + column + "`"));
        var referencedList = string.Join(", ", ReferencedColumns.Select(column => "`" + column + "`"));
        return $"ALTER TABLE `{schemaName}`.`{tableName}` ADD CONSTRAINT `{Name}` FOREIGN KEY ({columnList}) " +
               $"REFERENCES `{ReferencedTable}` ({referencedList}) ON UPDATE {UpdateRule} ON DELETE {DeleteRule};";
    }
}

/// <summary>
/// All metadata captured for one table.
/// </summary>
public sealed class Model_TableSnapshot
{
    public required string Name { get; init; }
    public IReadOnlyList<Model_ColumnDefinition> Columns { get; init; } = Array.Empty<Model_ColumnDefinition>();
    public IReadOnlyList<Model_IndexDefinition> Indexes { get; init; } = Array.Empty<Model_IndexDefinition>();
    public IReadOnlyList<Model_ForeignKeyDefinition> ForeignKeys { get; init; } = Array.Empty<Model_ForeignKeyDefinition>();

    public Model_ColumnDefinition? FindColumn(string name) =>
        Columns.FirstOrDefault(column => string.Equals(column.Name, name, StringComparison.OrdinalIgnoreCase));
}
