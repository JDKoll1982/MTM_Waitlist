using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Permissions;
using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Setup.Models;
using MTM_Waitlist.Module_Shared.Models;
using MTM_Waitlist.Module_Shared.Helpers;
using MTM_Waitlist.Module_Shared.Services;
using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Setup.ViewModels;

public partial class SetupWorkCenterViewModel : ObservableRecipient, INavigationAware
{
    private const string DefaultWorkCenterImagePath = ImagePicturePolicy.NoImagePath;

    private readonly INavigationService _navigationService;
    private readonly ISetupWorkflowService _workflowService;
    private readonly ISetupWorkCenterService _workCenterService;
    private readonly IWorkCenterImageService _imageLocationService;
    private readonly IWorkCenterCatalogService _workCenterCatalogService;
    private readonly IBuildingSelectionService _buildingSelectionService;
    private readonly IPermissionService _permissionService;
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

    /// <summary>
    /// Whether the signed-in person may add, rename or remove a work centre, answered from
    /// <c>permission.setup.work_centers</c> rather than from a list of role names kept here (FR-054).
    /// </summary>
    /// <remarks>
    /// False until the page's permission read returns, so the management controls are never offered on a guess.
    /// The retired list carried a <c>Setup Tech</c> entry that matched no role the catalogue holds, so a plain
    /// Setup person was given work-centre setup by default; the owner flips the permission on for whoever needs
    /// it instead (FR-107).
    /// </remarks>
    [ObservableProperty]
    public partial bool CanManageWorkCenters
    {
        get; set;
    }

    public SetupWorkCenterViewModel(
        INavigationService navigationService,
        ISetupWorkflowService workflowService,
        ISetupWorkCenterService workCenterService,
        IWorkCenterImageService imageLocationService,
        IWorkCenterCatalogService workCenterCatalogService,
        IBuildingSelectionService buildingSelectionService,
        IPermissionService permissionService)
    {
        _navigationService = navigationService;
        _workflowService = workflowService;
        _workCenterService = workCenterService;
        _imageLocationService = imageLocationService;
        _workCenterCatalogService = workCenterCatalogService;
        _buildingSelectionService = buildingSelectionService;
        _permissionService = permissionService;
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
        _ = LoadPermissionsAsync();
    }

    /// <summary>
    /// Asks the permission service for the page's one gate when the page is reached, so the answer is read per
    /// visit rather than decided from stored session state at construction (FR-114).
    /// </summary>
    /// <remarks>
    /// The read is issued here rather than in a layout pass, so the page is never drawn from an answer that has
    /// not arrived and the interface thread is never blocked waiting for one. An unreachable store answers from
    /// the key's shipped fallback rather than refusing (FR-050); the failure is recorded rather than swallowed.
    /// </remarks>
    private async Task LoadPermissionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            CanManageWorkCenters = await _permissionService
                .HasPermissionAsync(PermissionKeys.SetupWorkCenters, cancellationToken)
                .ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error(
                "SetupWorkCenters",
                ex,
                "The work-centre setup permission could not be read, so the management controls stay hidden rather than being offered on a guess.");
        }
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
        if (_imageLocationService is null || string.IsNullOrWhiteSpace(workstation.Id))
        {
            return DefaultWorkCenterImagePath;
        }

        // Initialized on demand rather than assumed. Nothing initializes this service at startup, so a guard that
        // bailed out while it was not ready yet left every card on the placeholder for the whole first visit, and
        // the pictures configured for the work centers only appeared once some other screen had got there first.
        if (!await _imageLocationService.EnsureInitializedAsync(cancellationToken).ConfigureAwait(true))
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
