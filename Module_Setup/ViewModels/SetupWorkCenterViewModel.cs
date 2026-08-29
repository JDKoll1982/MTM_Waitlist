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
using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Setup.ViewModels;

public partial class SetupWorkCenterViewModel : ObservableRecipient, INavigationAware
{
    private const string DefaultWorkCenterImagePath = "Assets/Images/default-workstation-image.png";

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
    private readonly ISetupWorkCenterService _workCenterService;
    private readonly IImageLocationService _imageLocationService;
    private readonly IWorkCenterCatalogService _workCenterCatalogService;
    private readonly IBuildingSelectionService _buildingSelectionService;
    private readonly StartupState _startupState;
    private readonly HashSet<string> _hotWorkCenterNames = new(StringComparer.OrdinalIgnoreCase);
    private readonly ObservableCollection<SetupWorkCenter> _displayedHotWorkCenters = new();
    private readonly ObservableCollection<SetupWorkCenter> _displayedOtherWorkCenters = new();

    [ObservableProperty]
    public partial SetupWorkCenter? SelectedWorkCenter
    {
        get; set;
    }

    [ObservableProperty]
    public partial string WorkCenterNameInput
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

    public ObservableCollection<SetupWorkCenter> Workstations => State.WorkCenters;

    public ObservableCollection<SetupWorkCenter> DisplayedHotWorkCenters => _displayedHotWorkCenters;

    public ObservableCollection<SetupWorkCenter> DisplayedOtherWorkCenters => _displayedOtherWorkCenters;

    public string OtherWorkCentersHeader => IsOtherWorkCentersExpanded
        ? "Hide Other Work Centers"
        : "Show Other Work Centers";

    partial void OnIsOtherWorkCentersExpandedChanged(bool value)
    {
        OnPropertyChanged(nameof(OtherWorkCentersHeader));
    }

    public IReadOnlyList<string> Buildings => _buildingSelectionService.Buildings;

    public bool CanManageWorkCenters => AllowedManageRoles.Any(role => string.Equals(role, _startupState.CurrentRole, StringComparison.OrdinalIgnoreCase));

    public SetupWorkCenterViewModel(
        INavigationService navigationService,
        ISetupWorkflowService workflowService,
        ISetupWorkCenterService workCenterService,
        IImageLocationService imageLocationService,
        IWorkCenterCatalogService workCenterCatalogService,
        StartupState startupState,
        IBuildingSelectionService buildingSelectionService)
    {
        _navigationService = navigationService;
        _workflowService = workflowService;
        _workCenterService = workCenterService;
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
        SelectedWorkCenter = State.WorkCenters.FirstOrDefault(item =>
            string.Equals(item.Building, _buildingSelectionService.SelectedBuilding, StringComparison.OrdinalIgnoreCase)
            && string.Equals(item.Name, State.SelectedWorkCenter, StringComparison.OrdinalIgnoreCase));
        _ = LoadWorkCentersAsync();
    }

    public void OnNavigatedFrom()
    {
        _buildingSelectionService.BuildingChanged -= OnBuildingChanged;
    }

    [RelayCommand]
    private async Task ContinueAsync()
    {
        if (SelectedWorkCenter is null)
        {
            StatusMessage = "Choose a workstation to continue.";
            return;
        }

        State.SelectedWorkCenter = SelectedWorkCenter.Name;
        State.StatusMessage = string.Empty;
        State.CurrentStep = SetupWorkflowStep.WorkOrderEntry;
        await Task.CompletedTask;
        _navigationService.NavigateTo(typeof(SetupWorkOrderViewModel).FullName!, null);
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadWorkCentersAsync().ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task AddWorkCenterAsync()
    {
        if (!CanManageWorkCenters)
        {
            StatusMessage = "You do not have permission to manage workstations.";
            return;
        }

        var result = await _workCenterService.AddWorkCenterAsync(WorkCenterNameInput, BuildingInput).ConfigureAwait(true);
        StatusMessage = result.Message;
        if (result.Success)
        {
            WorkCenterNameInput = string.Empty;
            BuildingInput = _buildingSelectionService.SelectedBuilding;
            await LoadWorkCentersAsync().ConfigureAwait(true);
        }
    }

    [RelayCommand]
    private async Task UpdateWorkCenterAsync()
    {
        if (!CanManageWorkCenters)
        {
            StatusMessage = "You do not have permission to manage workstations.";
            return;
        }

        if (SelectedWorkCenter is null)
        {
            StatusMessage = "Select a workstation to edit.";
            return;
        }

        var result = await _workCenterService.UpdateWorkCenterAsync(SelectedWorkCenter.Id, WorkCenterNameInput, BuildingInput).ConfigureAwait(true);
        StatusMessage = result.Message;
        if (result.Success)
        {
            WorkCenterNameInput = string.Empty;
            BuildingInput = _buildingSelectionService.SelectedBuilding;
            await LoadWorkCentersAsync().ConfigureAwait(true);
        }
    }

    [RelayCommand]
    private async Task RemoveWorkCenterAsync()
    {
        if (!CanManageWorkCenters)
        {
            StatusMessage = "You do not have permission to manage workstations.";
            return;
        }

        if (SelectedWorkCenter is null)
        {
            StatusMessage = "Select a workstation to remove.";
            return;
        }

        var result = await _workCenterService.RemoveWorkCenterAsync(SelectedWorkCenter.Id).ConfigureAwait(true);
        StatusMessage = result.Message;
        if (result.Success)
        {
            WorkCenterNameInput = string.Empty;
            await LoadWorkCentersAsync().ConfigureAwait(true);
        }
    }

    partial void OnSelectedWorkCenterChanged(SetupWorkCenter? value)
    {
        foreach (var item in State.WorkCenters)
        {
            item.IsSelected = ReferenceEquals(item, value);
        }

        if (value is null)
        {
            return;
        }

        WorkCenterNameInput = value.Name;
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

    private async Task LoadWorkCentersAsync()
    {
        IsBusy = true;
        try
        {
            var items = await _workCenterService.GetWorkCentersAsync().ConfigureAwait(true);
            State.WorkCenters.Clear();
            foreach (var item in items)
            {
                item.ImagePath = await ResolveWorkCenterImagePathAsync(item).ConfigureAwait(true);
                State.WorkCenters.Add(item);
            }

            await LoadHotWorkCenterNamesAsync().ConfigureAwait(true);
            ApplyFilter();

            var allVisible = _displayedHotWorkCenters.Concat(_displayedOtherWorkCenters).ToArray();
            if (SelectedWorkCenter is null && allVisible.Length > 0)
            {
                SelectedWorkCenter = allVisible.FirstOrDefault(item => string.Equals(item.Name, State.SelectedWorkCenter, StringComparison.OrdinalIgnoreCase))
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
                .GetCatalogAsync(_workCenterCatalogService.GetCurrentComputerName(), cancellationToken)
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

    private async Task<string> ResolveWorkCenterImagePathAsync(SetupWorkCenter workstation, CancellationToken cancellationToken = default)
    {
        if (_imageLocationService is null
            || !_imageLocationService.IsInitialized
            || string.IsNullOrWhiteSpace(workstation.Id))
        {
            return DefaultWorkCenterImagePath;
        }

        try
        {
            var resolvedPath = await _imageLocationService
                .ResolveWorkCenterImagePathAsync(workstation.Id, cancellationToken)
                .ConfigureAwait(true);
            return string.IsNullOrWhiteSpace(resolvedPath)
                ? DefaultWorkCenterImagePath
                : resolvedPath;
        }
        catch (Exception)
        {
            // A broken image override or catalog query should never blank the card.
            return DefaultWorkCenterImagePath;
        }
    }

    private void ApplyFilter()
    {
        _displayedHotWorkCenters.Clear();
        _displayedOtherWorkCenters.Clear();

        var normalizedFilter = FilterText.Trim();
        var selectedBuilding = _buildingSelectionService.SelectedBuilding;
        var filteredItems = State.WorkCenters.Where(item =>
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
                _displayedHotWorkCenters.Add(workstation);
            }
            else
            {
                _displayedOtherWorkCenters.Add(workstation);
            }
        }

        if (SelectedWorkCenter is not null
            && !_displayedHotWorkCenters.Contains(SelectedWorkCenter)
            && !_displayedOtherWorkCenters.Contains(SelectedWorkCenter))
        {
            SelectedWorkCenter = _displayedHotWorkCenters.FirstOrDefault()
                ?? _displayedOtherWorkCenters.FirstOrDefault();
        }
    }
}
