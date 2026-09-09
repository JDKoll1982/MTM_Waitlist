namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// A managed NCM defect type row (<c>mtm_waitlist.waitlist_defect_types</c>), used by the Pickup NCM
/// Item (Phase 3/4/5) and the Module_Settings defect editor.
/// </summary>
public sealed class DefectTypeDefinition
{
    public long Id { get; init; }

    public string PublicId { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public int SortOrder { get; init; }

    public bool IsActive { get; init; }
}
