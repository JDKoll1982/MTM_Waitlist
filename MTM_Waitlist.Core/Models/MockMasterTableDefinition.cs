namespace MTM_Waitlist.Module_Core.Models;

/// <summary>
/// A row of the Developer-editable mock master registry (<c>mock_master_tables_registry</c>),
/// describing one mock master table with its UI-readable name and plain-language description.
/// </summary>
public sealed class MockMasterTableDefinition
{
    public long Id { get; init; }

    public string TableName { get; init; } = string.Empty;

    public string UiDisplayName { get; init; } = string.Empty;

    public string DescriptionText { get; init; } = string.Empty;

    public bool IsActive { get; init; } = true;

    public int SortRank { get; init; }
}
