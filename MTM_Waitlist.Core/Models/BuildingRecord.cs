namespace MTM_Waitlist.Module_Core.Models;

/// <summary>
/// One building as the plant's catalog holds it: the name every chooser offers, whether it is still offered, and
/// where it sits in the list.
/// </summary>
/// <remarks>
/// The name is the whole identity. `setup_work_centers_catalog.building` and
/// `core_computer_buildings.building_name` both record this string rather than an identifier, which is why a
/// rename rewrites both in the same transaction (`sp_core_buildings_upsert`) instead of leaving them naming a
/// building the catalog no longer holds.
/// </remarks>
public sealed class BuildingRecord
{
    /// <summary>The building's name, spelled exactly as a work centre or a computer records it.</summary>
    public string BuildingName { get; init; } = string.Empty;

    /// <summary>Whether the choosers still offer it. A withdrawn building is still named by the rows that chose it.</summary>
    public bool IsActive { get; init; } = true;

    /// <summary>Display order inside the choosers.</summary>
    public int SortRank { get; init; }
}
