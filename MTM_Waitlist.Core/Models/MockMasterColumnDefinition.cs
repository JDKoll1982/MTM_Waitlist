namespace MTM_Waitlist.Module_Core.Models;

/// <summary>
/// Describes one column of a Developer-editable mock master table so the editor can build a grid.
/// </summary>
public sealed class MockMasterColumnDefinition
{
    public string ColumnName { get; init; } = string.Empty;

    /// <summary>MySQL data type (e.g. varchar, tinyint, decimal, bigint).</summary>
    public string DataType { get; init; } = string.Empty;

    public bool IsNullable { get; init; }

    /// <summary>Whether the column is read-only metadata (id / public_id / created_utc / updated_utc).</summary>
    public bool IsAudit { get; init; }
}
