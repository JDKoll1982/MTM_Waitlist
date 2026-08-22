using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Setup.Models;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Shared.Models;
using MTM_Waitlist.Module_Shared.Services;
using MTM_Waitlist.Module_Startup.Models;

namespace MTM_Waitlist.Module_Setup.ViewModels;

public partial class SetupWorkstationViewModel : ObservableRecipient, INavigationAware
{
    private const string DefaultWorkstationImagePath = "Assets/Images/default-workstation-image.png";

    private static readonly string[] AllowedManageRoles =
    {
        "Setup Tech",
        "Admin",
        "Developer",
        "Plant Manager",
        "Setup Lead",
        "Production Lead",
    };

    private readonly INavigationService _navigationService;
    private readonly ISetupWorkflowService _workflowService;
    private readonly ISetupWorkstationService _workstationService;
    private readonly IImageLocationService _imageLocationService;
    private readonly IWorkCenterCatalogService _workCenterCatalogService;
    private readonly IBuildingSelectionService _buildingSelectionService;
    private readonly StartupState _startupState;
    private readonly HashSet<string> _hotWorkCenterNames = new(StringComparer.OrdinalIgnoreCase);
    private readonly ObservableCollection<SetupWorkstation> _displayedHotWorkstations = new();
    private readonly ObservableCollection<SetupWorkstation> _displayedOtherWorkstations = new();

    [ObservableProperty]
    public partial SetupWorkstation? SelectedWorkstation
    {
        get; set;
    }

    [ObservableProperty]
    public partial string WorkstationNameInput
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial string BuildingInput
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial string StatusMessage
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial bool IsBusy
    {
        get; set;
    }

    [ObservableProperty]
    public partial string FilterText
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial bool IsOtherWorkCentersExpanded
    {
        get; set;
    }

    [ObservableProperty]
    public partial bool IsLocalWorkCentersVisible
    {
        get; set;
    } = true;

    public SetupWorkflowState State => _workflowService.State;

    public ObservableCollection<SetupWorkstation> Workstations => State.Workstations;

    public ObservableCollection<SetupWorkstation> DisplayedHotWorkstations => _displayedHotWorkstations;

    public ObservableCollection<SetupWorkstation> DisplayedOtherWorkstations => _displayedOtherWorkstations;

    public string OtherWorkCentersHeader => IsOtherWorkCentersExpanded
        ? "Hide Other Work Centers"
        : "Show Other Work Centers";

    partial void OnIsOtherWorkCentersExpandedChanged(bool value)
    {
        OnPropertyChanged(nameof(OtherWorkCentersHeader));
    }

    public IReadOnlyList<string> Buildings => _buildingSelectionService.Buildings;

    public bool CanManageWorkstations => AllowedManageRoles.Any(role => string.Equals(role, _startupState.CurrentRole, StringComparison.OrdinalIgnoreCase));

    public SetupWorkstationViewModel(
        INavigationService navigationService,
        ISetupWorkflowService workflowService,
        ISetupWorkstationService workstationService,
        IImageLocationService imageLocationService,
        IWorkCenterCatalogService workCenterCatalogService,
        StartupState startupState,
        IBuildingSelectionService buildingSelectionService)
    {
        _navigationService = navigationService;
        _workflowService = workflowService;
        _workstationService = workstationService;
        _imageLocationService = imageLocationService;
        _workCenterCatalogService = workCenterCatalogService;
        _startupState = startupState;
        _buildingSelectionService = buildingSelectionService;
    }

    public void OnNavigatedTo(object parameter)
    {
        _buildingSelectionService.BuildingChanged += OnBuildingChanged;
        StatusMessage = State.StatusMessage;
        FilterText = string.Empty;
        BuildingInput = _buildingSelectionService.SelectedBuilding;
        SelectedWorkstation = State.Workstations.FirstOrDefault(item =>
            string.Equals(item.Building, _buildingSelectionService.SelectedBuilding, StringComparison.OrdinalIgnoreCase)
            && string.Equals(item.Name, State.SelectedWorkCenter, StringComparison.OrdinalIgnoreCase));
        _ = LoadWorkstationsAsync();
    }

    public void OnNavigatedFrom()
    {
        _buildingSelectionService.BuildingChanged -= OnBuildingChanged;
    }

    [RelayCommand]
    private async Task ContinueAsync()
    {
        if (SelectedWorkstation is null)
        {
            StatusMessage = "Choose a workstation to continue.";
            return;
        }

        State.SelectedWorkCenter = SelectedWorkstation.Name;
        State.StatusMessage = string.Empty;
        State.CurrentStep = SetupWorkflowStep.WorkOrderEntry;
        await Task.CompletedTask;
        _navigationService.NavigateTo(typeof(SetupWorkOrderViewModel).FullName!, null);
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadWorkstationsAsync().ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task AddWorkstationAsync()
    {
        if (!CanManageWorkstations)
        {
            StatusMessage = "You do not have permission to manage workstations.";
            return;
        }

        var result = await _workstationService.AddWorkstationAsync(WorkstationNameInput, BuildingInput).ConfigureAwait(true);
        StatusMessage = result.Message;
        if (result.Success)
        {
            WorkstationNameInput = string.Empty;
            BuildingInput = _buildingSelectionService.SelectedBuilding;
            await LoadWorkstationsAsync().ConfigureAwait(true);
        }
    }

    [RelayCommand]
    private async Task UpdateWorkstationAsync()
    {
        if (!CanManageWorkstations)
        {
            StatusMessage = "You do not have permission to manage workstations.";
            return;
        }

        if (SelectedWorkstation is null)
        {
            StatusMessage = "Select a workstation to edit.";
            return;
        }

        var result = await _workstationService.UpdateWorkstationAsync(SelectedWorkstation.Id, WorkstationNameInput, BuildingInput).ConfigureAwait(true);
        StatusMessage = result.Message;
        if (result.Success)
        {
            WorkstationNameInput = string.Empty;
            BuildingInput = _buildingSelectionService.SelectedBuilding;
            await LoadWorkstationsAsync().ConfigureAwait(true);
        }
    }

    [RelayCommand]
    private async Task RemoveWorkstationAsync()
    {
        if (!CanManageWorkstations)
        {
            StatusMessage = "You do not have permission to manage workstations.";
            return;
        }

        if (SelectedWorkstation is null)
        {
            StatusMessage = "Select a workstation to remove.";
            return;
        }

        var result = await _workstationService.RemoveWorkstationAsync(SelectedWorkstation.Id).ConfigureAwait(true);
        StatusMessage = result.Message;
        if (result.Success)
        {
            WorkstationNameInput = string.Empty;
            await LoadWorkstationsAsync().ConfigureAwait(true);
        }
    }

    partial void OnSelectedWorkstationChanged(SetupWorkstation? value)
    {
        foreach (var item in State.Workstations)
        {
            item.IsSelected = ReferenceEquals(item, value);
        }

        if (value is null)
        {
            return;
        }

        WorkstationNameInput = value.Name;
        BuildingInput = string.IsNullOrWhiteSpace(value.Building)
            ? _buildingSelectionService.SelectedBuilding
            : value.Building;
    }

    partial void OnFilterTextChanged(string value)
    {
        ApplyFilter();
    }

    private void OnBuildingChanged(object? sender, EventArgs e)
    {
        BuildingInput = _buildingSelectionService.SelectedBuilding;
        ApplyFilter();
    }

    private async Task LoadWorkstationsAsync()
    {
        IsBusy = true;
        try
        {
            var items = await _workstationService.GetWorkstationsAsync().ConfigureAwait(true);
            State.Workstations.Clear();
            foreach (var item in items)
            {
                item.ImagePath = await ResolveWorkstationImagePathAsync(item).ConfigureAwait(true);
                State.Workstations.Add(item);
            }

            await LoadHotWorkCenterNamesAsync().ConfigureAwait(true);
            ApplyFilter();

            var allVisible = _displayedHotWorkstations.Concat(_displayedOtherWorkstations).ToArray();
            if (SelectedWorkstation is null && allVisible.Length > 0)
            {
                SelectedWorkstation = allVisible.FirstOrDefault(item => string.Equals(item.Name, State.SelectedWorkCenter, StringComparison.OrdinalIgnoreCase))
                    ?? allVisible[0];
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadHotWorkCenterNamesAsync(CancellationToken cancellationToken = default)
    {
        _hotWorkCenterNames.Clear();
        try
        {
            var catalog = await _workCenterCatalogService
                .GetCatalogAsync(_workCenterCatalogService.GetCurrentWorkstationName(), cancellationToken)
                .ConfigureAwait(true);
            foreach (var name in catalog.HotWorkCenters)
            {
                _hotWorkCenterNames.Add(name.Trim());
            }

            StartupDebugLog.Info("SetupWorkstation", $"Local work centers loaded for the setup selection screen. Count={_hotWorkCenterNames.Count}.");
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("SetupWorkstation", ex, "Failed to load Local work centers for the setup selection screen.");
        }

        UpdateWorkCenterSectionsVisibility();
    }

    private void UpdateWorkCenterSectionsVisibility()
    {
        var hasLocalWorkCenters = _hotWorkCenterNames.Count > 0;
        IsLocalWorkCentersVisible = hasLocalWorkCenters;
        // When this computer has no configured local work centers, show the others by default.
        IsOtherWorkCentersExpanded = !hasLocalWorkCenters;
    }

    private async Task<string> ResolveWorkstationImagePathAsync(SetupWorkstation workstation, CancellationToken cancellationToken = default)
    {
        if (_imageLocationService is null
            || !_imageLocationService.IsInitialized
            || string.IsNullOrWhiteSpace(workstation.Id))
        {
            return DefaultWorkstationImagePath;
        }

        try
        {
            var resolvedPath = await _imageLocationService
                .ResolveWorkCenterImagePathAsync(workstation.Id, cancellationToken)
                .ConfigureAwait(true);
            return string.IsNullOrWhiteSpace(resolvedPath)
                ? DefaultWorkstationImagePath
                : resolvedPath;
        }
        catch (Exception)
        {
            // A broken image override or catalog query should never blank the card.
            return DefaultWorkstationImagePath;
        }
    }

    private void ApplyFilter()
    {
        _displayedHotWorkstations.Clear();
        _displayedOtherWorkstations.Clear();

        var normalizedFilter = FilterText.Trim();
        var selectedBuilding = _buildingSelectionService.SelectedBuilding;
        var filteredItems = State.Workstations.Where(item =>
            string.Equals(item.Building, selectedBuilding, StringComparison.OrdinalIgnoreCase)
            && (string.IsNullOrWhiteSpace(normalizedFilter)
                || item.Name.Contains(normalizedFilter, StringComparison.OrdinalIgnoreCase)
                || item.CurrentWorkOrder.Contains(normalizedFilter, StringComparison.OrdinalIgnoreCase)
                || item.CurrentSequenceNumber.Contains(normalizedFilter, StringComparison.OrdinalIgnoreCase)
                || item.CurrentPartNumber.Contains(normalizedFilter, StringComparison.OrdinalIgnoreCase)));

        foreach (var workstation in filteredItems)
        {
            if (_hotWorkCenterNames.Contains(workstation.Name))
            {
                _displayedHotWorkstations.Add(workstation);
            }
            else
            {
                _displayedOtherWorkstations.Add(workstation);
            }
        }

        if (SelectedWorkstation is not null
            && !_displayedHotWorkstations.Contains(SelectedWorkstation)
            && !_displayedOtherWorkstations.Contains(SelectedWorkstation))
        {
            SelectedWorkstation = _displayedHotWorkstations.FirstOrDefault()
                ?? _displayedOtherWorkstations.FirstOrDefault();
        }
    }
}
