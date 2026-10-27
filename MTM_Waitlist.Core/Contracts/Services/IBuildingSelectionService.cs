using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// The building in effect for this session: the list the choosers offer, and which of them is chosen.
/// </summary>
/// <remarks>
/// <para>
/// The choice starts from what this computer was told at setup — the building recorded against it — and from then
/// on belongs to the person using the session. It is held here rather than on a page because the shell's header,
/// the two work-centre steps and the waitlist all read the same answer.
/// </para>
/// <para>
/// <see cref="BuildingScope.AllBuildings"/> is a legitimate choice and is offered last by every chooser. It is not
/// a building: <see cref="BuildingScope.AsFilter"/> turns it into "no building filter" for a list, and a request
/// is filed under the building of the work centre it names.
/// </para>
/// </remarks>
public interface IBuildingSelectionService
{
    /// <summary>Raised when the building in effect changes.</summary>
    event EventHandler? BuildingChanged;

    /// <summary>Raised when the offered list changes, which is after the catalog was read or a building was edited.</summary>
    event EventHandler? BuildingsChanged;

    /// <summary>The buildings a chooser may offer, in the catalog's display order. Never includes the All entry.</summary>
    IReadOnlyList<string> Buildings { get; }

    /// <summary>The building in effect, or <see cref="BuildingScope.AllBuildings"/> when every building is wanted.</summary>
    string SelectedBuilding { get; set; }

    /// <summary>
    /// Reads the catalog and this computer's own building, and chooses that building unless somebody has already
    /// chosen one. Called once as the shell starts; a store that cannot be read leaves the defaults in place.
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the offered list again after the catalog was edited, keeping the current choice when it is still
    /// offered and falling back to this computer's building when it is not.
    /// </summary>
    Task RefreshAsync(CancellationToken cancellationToken = default);
}
