using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// The plant's building list and the building each computer stands in: the one read of both, and the one write of
/// a building.
/// </summary>
/// <remarks>
/// <para>
/// Four surfaces read this: the shell's facility chooser, the two work-centre steps that filter on a building, the
/// Local Work Centers panel that filters one computer's press list, and the Operations screen that edits the list.
/// They read through one service so a building added on that screen is offered everywhere without a second copy
/// of the list in code.
/// </para>
/// <para>
/// Every read and write is a stored procedure (constitution III). A store that cannot be reached answers with an
/// empty list and no computer building rather than throwing: the callers fall back to their own default, which is
/// the same thing they do on a first run.
/// </para>
/// </remarks>
public interface IBuildingCatalogService
{
    /// <summary>The buildings the catalog holds, in its display order, withdrawn ones included.</summary>
    Task<IReadOnlyList<BuildingRecord>> GetBuildingsAsync(CancellationToken cancellationToken = default);

    /// <summary>The buildings a chooser may offer, in display order: the active ones only.</summary>
    Task<IReadOnlyList<string>> GetSelectableBuildingNamesAsync(CancellationToken cancellationToken = default);

    /// <summary>The building one computer stands in, or <c>null</c> when it has never been set up.</summary>
    /// <param name="computerName">A registry computer name or its normalised hostname.</param>
    Task<string?> GetComputerBuildingAsync(string computerName, CancellationToken cancellationToken = default);

    /// <summary>The building this computer stands in, or <c>null</c> when this computer has never been set up.</summary>
    Task<string?> GetCurrentComputerBuildingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Records which building one computer stands in. A computer with no row gains one; a computer that already
    /// has an answer moves to the building given here.
    /// </summary>
    /// <param name="computerName">A registry computer name or its normalised hostname.</param>
    /// <param name="buildingName">The building it stands in.</param>
    Task<bool> SetComputerBuildingAsync(
        string computerName,
        string buildingName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a building, or renames one that exists. A rename also rewrites the work centres and the computers
    /// that name the old spelling, so nothing is left naming a building the catalog no longer holds.
    /// </summary>
    /// <param name="originalName">The building being edited, or <c>null</c> to add a new one.</param>
    /// <param name="buildingName">The name the building is to carry.</param>
    Task<BuildingSaveResult> SaveBuildingAsync(
        string? originalName,
        string buildingName,
        CancellationToken cancellationToken = default);
}
