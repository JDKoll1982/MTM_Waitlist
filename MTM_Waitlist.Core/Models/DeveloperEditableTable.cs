namespace MTM_Waitlist.Module_Core.Models;

/// <summary>
/// A single table shown in the Developer-settings editor's table dropdown. Covers both the <c>mock_*</c> tables
/// (from <c>mock_master_tables_registry</c>) and the real (non-mock) request-type/subtype tables, so one list
/// drives the whole editor. <see cref="SourceKind"/> tells the UI which service routes reads/writes.
/// </summary>
public sealed record DeveloperEditableTable
{
    /// <summary>Stable key for routing (the <c>mock_*</c> table_name, or the real request table name).</summary>
    public string TableKey { get; init; } = string.Empty;

    public string TableName { get; init; } = string.Empty;

    public string UiDisplayName { get; init; } = string.Empty;

    public string DescriptionText { get; init; } = string.Empty;

    public DeveloperTableSourceKind SourceKind { get; init; } = DeveloperTableSourceKind.Mock;

    public int SortRank { get; init; }
}
