using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Core.Services;

/// <summary>
/// The building in effect, resolved from the plant's catalog and from what this computer was told at setup.
/// </summary>
/// <remarks>
/// <para>
/// <b>The list is data.</b> It used to be two names written in this class, which meant a building added on the
/// settings screen was not offered anywhere until the application shipped again. It is read from
/// <see cref="IBuildingCatalogService"/> now, and the two names it shipped with remain only as the fallback an
/// unreachable store leaves behind — the same answer a first run gets.
/// </para>
/// <para>
/// <b>The starting choice is this computer's own building.</b> A workstation was asked which building it stands in
/// while it was being set up, so that answer is what the session opens on rather than the first name in the list.
/// A choice the person makes afterwards is theirs and is not overwritten by the read.
/// </para>
/// </remarks>
public class BuildingSelectionService : IBuildingSelectionService
{
    /// <summary>The buildings this service answered with before the list was data, kept as the unreachable-store fallback.</summary>
    private static readonly string[] s_fallbackBuildings = ["Expo Drive", "Vits Drive"];

    private readonly IBuildingCatalogService? _catalog;

    private IReadOnlyList<string> _buildings = s_fallbackBuildings;

    private string _selectedBuilding;

    private bool _initialized;

    /// <summary>Whether somebody has chosen a building here, which is what a later read must not overwrite.</summary>
    private bool _choiceWasMade;

    public BuildingSelectionService()
    {
        _selectedBuilding = _buildings[0];
    }

    public BuildingSelectionService(IBuildingCatalogService? catalog)
        : this()
    {
        _catalog = catalog;
    }

    public event EventHandler? BuildingChanged;

    public event EventHandler? BuildingsChanged;

    public IReadOnlyList<string> Buildings => _buildings;

    public string SelectedBuilding
    {
        get => _selectedBuilding;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            if (string.Equals(_selectedBuilding, value, StringComparison.Ordinal))
            {
                return;
            }

            _selectedBuilding = value;
            _choiceWasMade = true;
            BuildingChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized || _catalog is null)
        {
            _initialized = true;
            return;
        }

        _initialized = true;

        try
        {
            var buildings = await _catalog.GetSelectableBuildingNamesAsync(cancellationToken).ConfigureAwait(false);
            if (buildings.Count == 0)
            {
                return;
            }

            SetBuildings(buildings);

            if (_choiceWasMade)
            {
                return;
            }

            var computerBuilding = await _catalog.GetCurrentComputerBuildingAsync(cancellationToken).ConfigureAwait(false);
            ApplySelection(ResolveStartingBuilding(computerBuilding));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            AppLog.Error(
                "Buildings",
                ex,
                "The building list could not be read at start-up, so this machine keeps the default it would have shown on a first run.");
        }
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (_catalog is null)
        {
            return;
        }

        try
        {
            var buildings = await _catalog.GetSelectableBuildingNamesAsync(cancellationToken).ConfigureAwait(false);
            if (buildings.Count > 0)
            {
                SetBuildings(buildings);
            }

            // The chosen building may have been renamed or withdrawn by the edit that caused this refresh. All
            // Buildings is not a catalog row, so it survives for as long as there are buildings to show.
            if (BuildingScope.IsAllBuildings(_selectedBuilding) && _buildings.Count > 0)
            {
                return;
            }

            if (_buildings.Any(building => string.Equals(building, _selectedBuilding, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            var computerBuilding = await _catalog.GetCurrentComputerBuildingAsync(cancellationToken).ConfigureAwait(false);
            ApplySelection(ResolveStartingBuilding(computerBuilding));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            AppLog.Error("Buildings", ex, "The building list could not be read again after it was edited.");
        }
    }

    /// <summary>
    /// This machine's own building when the catalog still holds it, otherwise the first building the catalog
    /// offers, and finally whatever was already chosen.
    /// </summary>
    private string ResolveStartingBuilding(string? computerBuilding)
    {
        if (!string.IsNullOrWhiteSpace(computerBuilding)
            && _buildings.Any(building => string.Equals(building, computerBuilding, StringComparison.OrdinalIgnoreCase)))
        {
            return computerBuilding;
        }

        return _buildings.Count > 0 ? _buildings[0] : _selectedBuilding;
    }

    private void SetBuildings(IReadOnlyList<string> buildings)
    {
        var distinct = buildings
            .Where(building => !string.IsNullOrWhiteSpace(building))
            .Select(building => building.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (_buildings.SequenceEqual(distinct, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        _buildings = distinct;
        BuildingsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ApplySelection(string building)
    {
        if (string.IsNullOrWhiteSpace(building) || string.Equals(_selectedBuilding, building, StringComparison.Ordinal))
        {
            return;
        }

        _selectedBuilding = building;
        BuildingChanged?.Invoke(this, EventArgs.Empty);
    }
}
