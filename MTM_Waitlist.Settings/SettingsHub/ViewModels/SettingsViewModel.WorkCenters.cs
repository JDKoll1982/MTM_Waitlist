using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Options;
using Microsoft.UI.Xaml;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Permissions;
using MTM_Waitlist.Module_Shared.Helpers;
using MTM_Waitlist.Module_Shared.Models;
using MTM_Waitlist.Module_Shared.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Logging;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;
using Windows.ApplicationModel;

namespace MTM_Waitlist.Module_Settings.ViewModels;

public partial class SettingsViewModel : ObservableRecipient, INavigationAware
{
    [RelayCommand]
    private async Task AddHotWorkCenterAsync(string? workCenter)
    {
        if (!CanManageHotWorkCenters)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(workCenter))
        {
            return;
        }

        var normalizedWorkCenter = workCenter.Trim();
        AppLog.Info("SettingsHotWorkCenters", $"AddHotWorkCenterAsync started. WorkCenter='{normalizedWorkCenter}', Workstation='{SelectedWorkstation}'.");
        if (HotWorkCenters.Any(value => string.Equals(value, normalizedWorkCenter, StringComparison.OrdinalIgnoreCase)))
        {
            AppLog.Info("SettingsHotWorkCenters", $"AddHotWorkCenterAsync skipped because '{normalizedWorkCenter}' is already pinned.");
            return;
        }

        var existingOthers = _allOtherWorkCenters
            .Where(value => !string.Equals(value, normalizedWorkCenter, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        HotWorkCenters.Add(normalizedWorkCenter);
        SortCollection(HotWorkCenters);
        _allOtherWorkCenters = existingOthers;
        ApplyWorkCenterBuildingFilter();

        await SaveCurrentHotWorkCentersAsync().ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task RemoveHotWorkCenterAsync(string? workCenter)
    {
        if (!CanManageHotWorkCenters)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(workCenter))
        {
            return;
        }

        var normalizedWorkCenter = workCenter.Trim();
        AppLog.Info("SettingsHotWorkCenters", $"RemoveHotWorkCenterAsync started. WorkCenter='{normalizedWorkCenter}', Workstation='{SelectedWorkstation}'.");
        var updatedHot = HotWorkCenters
            .Where(value => !string.Equals(value, normalizedWorkCenter, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        ReplaceCollectionValues(HotWorkCenters, updatedHot);

        if (!_allOtherWorkCenters.Any(value => string.Equals(value, normalizedWorkCenter, StringComparison.OrdinalIgnoreCase)))
        {
            _allOtherWorkCenters = _allOtherWorkCenters.Append(normalizedWorkCenter).ToArray();
        }

        ApplyWorkCenterBuildingFilter();

        await SaveCurrentHotWorkCentersAsync().ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task ShowDunnageTypeAsync(string? dunnageTypeId)
    {
        if (!CanManageDunnageTypeVisibility)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(dunnageTypeId))
        {
            return;
        }

        var normalizedId = dunnageTypeId.Trim();
        var selectedOption = HiddenDunnageTypes.FirstOrDefault(item => string.Equals(item.Id, normalizedId, StringComparison.OrdinalIgnoreCase));
        if (selectedOption is null)
        {
            return;
        }

        AppLog.Info("SettingsDunnageVisibility", $"ShowDunnageTypeAsync started. DunnageTypeId='{normalizedId}'.");

        var nextHidden = HiddenDunnageTypes.Where(item => !string.Equals(item.Id, normalizedId, StringComparison.OrdinalIgnoreCase)).ToArray();
        var nextVisible = VisibleDunnageTypes.Concat(new[] { selectedOption }).ToArray();

        ReplaceDunnageTypeValues(HiddenDunnageTypes, nextHidden);
        ReplaceDunnageTypeValues(VisibleDunnageTypes, nextVisible);
        RefreshSearchVisibility();

        await SaveCurrentDunnageTypeVisibilityAsync().ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task HideDunnageTypeAsync(string? dunnageTypeId)
    {
        if (!CanManageDunnageTypeVisibility)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(dunnageTypeId))
        {
            return;
        }

        var normalizedId = dunnageTypeId.Trim();
        var selectedOption = VisibleDunnageTypes.FirstOrDefault(item => string.Equals(item.Id, normalizedId, StringComparison.OrdinalIgnoreCase));
        if (selectedOption is null)
        {
            return;
        }

        AppLog.Info("SettingsDunnageVisibility", $"HideDunnageTypeAsync started. DunnageTypeId='{normalizedId}'.");

        var nextVisible = VisibleDunnageTypes.Where(item => !string.Equals(item.Id, normalizedId, StringComparison.OrdinalIgnoreCase)).ToArray();
        var nextHidden = HiddenDunnageTypes.Concat(new[] { selectedOption }).ToArray();

        ReplaceDunnageTypeValues(VisibleDunnageTypes, nextVisible);
        ReplaceDunnageTypeValues(HiddenDunnageTypes, nextHidden);
        RefreshSearchVisibility();

        await SaveCurrentDunnageTypeVisibilityAsync().ConfigureAwait(true);
    }

    /// <summary>
    /// Reads the stored new-request alert preference without blocking the interface thread.
    /// </summary>
    /// <remarks>
    /// The suppression flag covers only the write of the value just read, so it is never set while the read is in
    /// flight: a person who toggles while it loads still has their choice stored.
    /// </remarks>
    private async Task InitializeNewRequestAlertsAsync()
    {
        var stored = await _newRequestAlertService.GetEnabledAsync().ConfigureAwait(true);

        _newRequestAlertInitializing = true;
        try
        {
            NewRequestAlertsEnabled = stored;
        }
        finally
        {
            _newRequestAlertInitializing = false;
        }
    }

    private async Task InitializeHotWorkCentersAsync()
    {
        AppLog.Info("SettingsViewModel", "InitializeHotWorkCentersAsync started.");
        IsHotWorkCentersBusy = true;
        try
        {
            var workstations = await _workCenterCatalogService.GetAvailableComputersAsync().ConfigureAwait(true);
            ReplaceCollectionValues(AvailableWorkstations, workstations);

            var currentWorkstation = _workCenterCatalogService.GetCurrentComputerName();
            var resolvedWorkstation = (AvailableWorkstations.FirstOrDefault(item =>
                                           string.Equals(item.Key, currentWorkstation, StringComparison.OrdinalIgnoreCase))
                                       ?? AvailableWorkstations.FirstOrDefault())?.Key
                ?? currentWorkstation;

            var workstationChanged = !string.Equals(SelectedWorkstation, resolvedWorkstation, StringComparison.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(SelectedWorkstation) || workstationChanged)
            {
                // The property change is what starts the catalog read, so the wait is on that read rather than on a
                // second one started here.
                SelectedWorkstation = resolvedWorkstation;
                await _catalogLoad.ConfigureAwait(true);
            }
            else
            {
                await LoadCatalogForWorkstationAsync(SelectedWorkstation).ConfigureAwait(true);
            }

            RefreshSearchVisibility();

            AppLog.Info("SettingsViewModel", $"InitializeHotWorkCentersAsync completed. Workstation='{SelectedWorkstation}', AvailableCount={AvailableWorkstations.Count}.");
        }
        finally
        {
            IsHotWorkCentersBusy = false;
        }
    }

    private async Task LoadCatalogForWorkstationAsync(string workstationName)
    {
        AppLog.Info("SettingsViewModel", $"LoadCatalogForWorkstationAsync started. Workstation='{workstationName}'.");
        IsHotWorkCentersBusy = true;
        try
        {
            var catalog = await _workCenterCatalogService.GetCatalogAsync(workstationName).ConfigureAwait(true);
            ReplaceCollectionValues(HotWorkCenters, catalog.HotWorkCenters);

            // The candidates are held before the building filter is applied, so switching the filter narrows what
            // was read rather than reading the catalog again.
            _allOtherWorkCenters = catalog.OtherWorkCenters.ToArray();
            _workCenterBuildings = catalog.WorkCenterDetails
                .Where(detail => !string.IsNullOrWhiteSpace(detail.Value.Building))
                .ToDictionary(
                    detail => detail.Key,
                    detail => detail.Value.Building,
                    StringComparer.OrdinalIgnoreCase);

            await LoadWorkstationBuildingAsync(workstationName).ConfigureAwait(true);
            ApplyWorkCenterBuildingFilter();
            HotWorkCentersStatusMessage = string.Empty;
            RefreshSearchVisibility();
            AppLog.Info("SettingsViewModel", $"LoadCatalogForWorkstationAsync completed. Workstation='{workstationName}', HotCount={HotWorkCenters.Count}, OtherCount={OtherWorkCenters.Count}, Building='{SelectedWorkstationBuilding}'.");
        }
        catch (Exception ex)
        {
            AppLog.Error("SettingsViewModel", ex, $"LoadCatalogForWorkstationAsync failed. Workstation='{workstationName}'.");
            HotWorkCentersStatusMessage = $"Unable to load work centers: {ex.Message}";
        }
        finally
        {
            IsHotWorkCentersBusy = false;
        }
    }

    /// <summary>
    /// Reads the building the selected computer was set up in, which is what the filter's "Saved Building" mode
    /// narrows to and what the toggle states.
    /// </summary>
    private async Task LoadWorkstationBuildingAsync(string workstationName)
    {
        if (_buildingCatalogService is null)
        {
            return;
        }

        try
        {
            SelectedWorkstationBuilding = await _buildingCatalogService
                .GetComputerBuildingAsync(workstationName)
                .ConfigureAwait(true) ?? string.Empty;
        }
        catch (Exception ex)
        {
            AppLog.Error(
                "SettingsHotWorkCenters",
                ex,
                $"The saved building for workstation '{workstationName}' could not be read, so the Local Work Centers filter falls back to every building.");
            SelectedWorkstationBuilding = string.Empty;
        }

        OnPropertyChanged(nameof(BuildingFilterToggleLabel));

        if (IsBuildingFilterEnabled)
        {
            SelectedWorkCenterBuilding = SelectedWorkstationBuilding;
        }
    }

    /// <summary>
    /// Narrows the candidate work centres to the building the filter asks for: the selected computer's saved
    /// building while the filter is on, otherwise the building chosen beside it, or every building when that choice
    /// is <see cref="BuildingScope.AllBuildings"/>.
    /// </summary>
    /// <remarks>
    /// A work centre the catalog holds no building for is always offered: hiding a work centre this screen cannot
    /// classify would be a guess, and a person could not explain why it had disappeared.
    /// </remarks>
    private void ApplyWorkCenterBuildingFilter()
    {
        var buildingFilter = BuildingScope.AsFilter(
            IsBuildingFilterEnabled ? SelectedWorkstationBuilding : SelectedWorkCenterBuilding);

        var visible = _allOtherWorkCenters.Where(name =>
            buildingFilter is null
            || !_workCenterBuildings.TryGetValue(name, out var building)
            || string.Equals(building, buildingFilter, StringComparison.OrdinalIgnoreCase));

        ReplaceCollectionValues(OtherWorkCenters, visible);
        RefreshSearchVisibility();
    }

    /// <summary>
    /// Turns the Local Work Centers filter on or off. Turning it on follows the selected computer's saved building;
    /// turning it off leaves the building choice beside it, which starts on every building.
    /// </summary>
    partial void OnIsBuildingFilterEnabledChanged(bool value)
    {
        SelectedWorkCenterBuilding = value && !string.IsNullOrWhiteSpace(SelectedWorkstationBuilding)
            ? SelectedWorkstationBuilding
            : BuildingScope.AllBuildings;

        OnPropertyChanged(nameof(BuildingFilterToggleLabel));
        OnPropertyChanged(nameof(IsBuildingChoiceEnabled));
        ApplyWorkCenterBuildingFilter();
    }

    partial void OnSelectedWorkCenterBuildingChanged(string value) => ApplyWorkCenterBuildingFilter();

    partial void OnSelectedWorkstationBuildingChanged(string value) =>
        OnPropertyChanged(nameof(BuildingFilterToggleLabel));

    /// <summary>Clears the Buildings panel so the next save adds a building rather than renaming one.</summary>
    [RelayCommand]
    private void BeginNewBuilding()
    {
        SelectedBuildingForEdit = null;
        BuildingNameInput = string.Empty;
        BuildingsStatusMessage = string.Empty;
    }

    /// <summary>Loads one building into the panel so it can be renamed.</summary>
    /// <param name="building">The building to edit. A blank one changes nothing.</param>
    [RelayCommand]
    private void EditBuilding(string? building)
    {
        if (string.IsNullOrWhiteSpace(building))
        {
            return;
        }

        SelectedBuildingForEdit = building.Trim();
        BuildingsStatusMessage = string.Empty;
    }

    /// <summary>What the panel is about to do, so Save is never pressed without knowing whether it adds or renames.</summary>
    public string BuildingEditHeading => string.IsNullOrWhiteSpace(SelectedBuildingForEdit)
        ? "Adding a new building."
        : $"Renaming '{SelectedBuildingForEdit}'.";

    /// <summary>Choosing a building to rename fills the name box with the name it already has.</summary>
    partial void OnSelectedBuildingForEditChanged(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            BuildingNameInput = value;
        }

        OnPropertyChanged(nameof(BuildingEditHeading));
    }

    /// <summary>
    /// Adds the building named in the panel, or renames the one chosen there.
    /// </summary>
    /// <remarks>
    /// The write goes through <see cref="IBuildingCatalogService"/>, whose procedure also rewrites the work centres
    /// and the computers that name the old spelling, so a rename cannot leave anything naming a building the
    /// catalog no longer holds. The selection service is asked to read again afterwards, which is what carries a
    /// new or renamed building into the shell's chooser without restarting the session.
    /// </remarks>
    [RelayCommand]
    private async Task SaveBuildingAsync()
    {
        if (!CanManageBuildings)
        {
            BuildingsStatusMessage = "You do not have permission to change the building list.";
            return;
        }

        if (_buildingCatalogService is null || IsBuildingsBusy)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(BuildingNameInput))
        {
            BuildingsStatusMessage = "A building name is required.";
            return;
        }

        IsBuildingsBusy = true;
        try
        {
            var result = await _buildingCatalogService
                .SaveBuildingAsync(SelectedBuildingForEdit, BuildingNameInput)
                .ConfigureAwait(true);

            BuildingsStatusMessage = result.Message;

            if (!result.Succeeded)
            {
                return;
            }

            SelectedBuildingForEdit = null;
            BuildingNameInput = string.Empty;
            await LoadBuildingsAsync().ConfigureAwait(true);

            if (_buildingSelectionService is not null)
            {
                await _buildingSelectionService.RefreshAsync().ConfigureAwait(true);
            }

            AppLog.Info("SettingsBuildings", $"Building saved. Message='{BuildingsStatusMessage}'.");
        }
        catch (Exception ex)
        {
            AppLog.Error("SettingsBuildings", ex, "SaveBuildingAsync failed.");
            BuildingsStatusMessage = $"Unable to save the building: {ex.Message}";
        }
        finally
        {
            IsBuildingsBusy = false;
        }
    }

    /// <summary>Reads the plant's buildings for both the Buildings panel and the Local Work Centers filter.</summary>
    private async Task InitializeBuildingsAsync()
    {
        IsBuildingsBusy = true;
        try
        {
            await LoadBuildingsAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            AppLog.Error("SettingsBuildings", ex, "The plant's building list could not be read.");
            BuildingsStatusMessage = $"Unable to load the buildings: {ex.Message}";
        }
        finally
        {
            IsBuildingsBusy = false;
        }
    }

    /// <summary>
    /// Fills the Buildings panel and the filter's building choice from the catalog, in the catalog's own order, with
    /// <see cref="BuildingScope.AllBuildings"/> last in the choice.
    /// </summary>
    private async Task LoadBuildingsAsync()
    {
        if (_buildingCatalogService is null)
        {
            return;
        }

        var buildings = await _buildingCatalogService.GetBuildingsAsync().ConfigureAwait(true);
        var active = buildings
            .Where(building => building.IsActive && !string.IsNullOrWhiteSpace(building.BuildingName))
            .Select(building => building.BuildingName)
            .ToArray();

        EditableBuildings.Clear();
        foreach (var building in active)
        {
            EditableBuildings.Add(building);
        }

        BuildingChoices.Clear();
        foreach (var building in active)
        {
            BuildingChoices.Add(building);
        }

        BuildingChoices.Add(BuildingScope.AllBuildings);

        // A building that was renamed or withdrawn is no longer a choice, so the filter falls back to every
        // building rather than to a name the list does not hold.
        if (!BuildingChoices.Contains(SelectedWorkCenterBuilding, StringComparer.OrdinalIgnoreCase))
        {
            SelectedWorkCenterBuilding = BuildingScope.AllBuildings;
        }

        RefreshSearchVisibility();
    }

    private async Task SaveCurrentHotWorkCentersAsync()
    {
        try
        {
            AppLog.Info("SettingsHotWorkCenters", $"SaveCurrentHotWorkCentersAsync started. Workstation='{SelectedWorkstation}', Count={HotWorkCenters.Count}.");
            var saveMessage = await _workCenterCatalogService
                .SaveHotWorkCentersAsync(SelectedWorkstation, HotWorkCenters.ToArray())
                .ConfigureAwait(true);

            HotWorkCentersStatusMessage = string.IsNullOrWhiteSpace(saveMessage)
                ? "Local workcenters saved."
                : saveMessage;

            AppLog.Info("SettingsHotWorkCenters", $"SaveCurrentHotWorkCentersAsync completed. Workstation='{SelectedWorkstation}', Message='{HotWorkCentersStatusMessage}'.");
        }
        catch (Exception ex)
        {
            AppLog.Error("SettingsHotWorkCenters", ex, $"SaveCurrentHotWorkCentersAsync failed. Workstation='{SelectedWorkstation}', Count={HotWorkCenters.Count}.");
            HotWorkCentersStatusMessage = $"Unable to save Local workcenters: {ex.Message}";
        }
    }

    private async Task InitializeDunnageTypeVisibilityAsync()
    {
        AppLog.Info("SettingsDunnageVisibility", "InitializeDunnageTypeVisibilityAsync started.");
        IsDunnageTypeVisibilityBusy = true;
        try
        {
            var catalog = await _dunnageTypeVisibilityCatalogService.GetCatalogAsync().ConfigureAwait(true);
            ReplaceDunnageTypeValues(VisibleDunnageTypes, catalog.VisibleDunnageTypes);
            ReplaceDunnageTypeValues(HiddenDunnageTypes, catalog.HiddenDunnageTypes);
            DunnageTypeVisibilityStatusMessage = string.Empty;
            RefreshSearchVisibility();
            AppLog.Info("SettingsDunnageVisibility", $"InitializeDunnageTypeVisibilityAsync completed. VisibleCount={VisibleDunnageTypes.Count}, HiddenCount={HiddenDunnageTypes.Count}.");
        }
        catch (Exception ex)
        {
            AppLog.Error("SettingsDunnageVisibility", ex, "InitializeDunnageTypeVisibilityAsync failed.");
            DunnageTypeVisibilityStatusMessage = $"Unable to load dunnage visibility: {ex.Message}";
        }
        finally
        {
            IsDunnageTypeVisibilityBusy = false;
        }
    }

    private async Task SaveCurrentDunnageTypeVisibilityAsync()
    {
        try
        {
            IsDunnageTypeVisibilityBusy = true;
            var saveMessage = await _dunnageTypeVisibilityCatalogService
                .SaveVisibleDunnageTypesAsync(VisibleDunnageTypes.Select(item => item.Id).ToArray())
                .ConfigureAwait(true);

            DunnageTypeVisibilityStatusMessage = string.IsNullOrWhiteSpace(saveMessage)
                ? "Dunnage visibility saved."
                : saveMessage;

            AppLog.Info("SettingsDunnageVisibility", $"SaveCurrentDunnageTypeVisibilityAsync completed. VisibleCount={VisibleDunnageTypes.Count}, HiddenCount={HiddenDunnageTypes.Count}, Message='{DunnageTypeVisibilityStatusMessage}'.");
        }
        catch (Exception ex)
        {
            AppLog.Error("SettingsDunnageVisibility", ex, "SaveCurrentDunnageTypeVisibilityAsync failed.");
            DunnageTypeVisibilityStatusMessage = $"Unable to save dunnage visibility: {ex.Message}";
        }
        finally
        {
            IsDunnageTypeVisibilityBusy = false;
        }
    }
}
