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
    private readonly IGenericRequestPreviewLauncher _genericPreviewLauncher;
    private bool _suppressGenericWrite;
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

    /// <summary>
    /// Whether requests raised at the selected work center use the generic request flow (016 FR-001).
    /// </summary>
    /// <remarks>
    /// Two-way with the page's toggle. A change is written only when it disagrees with the selected work center's own
    /// value, so selecting a different card — which pushes that card's value in — never writes anything, and the only
    /// write that ever happens is one the person made.
    /// </remarks>
    [ObservableProperty]
    public partial bool SelectedWorkCenterIsGeneric
    {
        get; set;
    }

    /// <summary>
    /// Whether the preview of the generic request flow can be opened for the selected work center.
    /// </summary>
    /// <remarks>
    /// True only when the signed-in person may manage work centers, that work center is marked generic, and the
    /// preview host is actually on this machine. A machine without it shows nothing rather than a button that cannot
    /// work.
    /// </remarks>
    [ObservableProperty]
    public partial bool CanOpenGenericPreview
    {
        get; set;
    }

    /// <summary>The toggle's heading, naming the work center it would change.</summary>
    public string GenericToggleHeader => SelectedWorkCenter is { } workCenter
        ? string.Format(
            System.Globalization.CultureInfo.CurrentCulture,
            "Setup_WorkCenter_GenericToggle.Header".GetLocalized(),
            workCenter.Name)
        : "Setup_WorkCenter_GenericToggle.None".GetLocalized();

    /// <summary>The preview button's label, read for the automation name as well as drawn as its content.</summary>
    public string GenericPreviewLabel => "Setup_WorkCenter_GenericPreview.Content".GetLocalized();

    public SetupWorkCenterViewModel(
        INavigationService navigationService,
        ISetupWorkflowService workflowService,
        ISetupWorkCenterService workCenterService,
        IWorkCenterImageService imageLocationService,
        IWorkCenterCatalogService workCenterCatalogService,
        IBuildingSelectionService buildingSelectionService,
        IPermissionService permissionService,
        IGenericRequestPreviewLauncher genericPreviewLauncher)
    {
        _navigationService = navigationService;
        _workflowService = workflowService;
        _workCenterService = workCenterService;
        _imageLocationService = imageLocationService;
        _workCenterCatalogService = workCenterCatalogService;
        _buildingSelectionService = buildingSelectionService;
        _permissionService = permissionService;
        _genericPreviewLauncher = genericPreviewLauncher;
    }

    public void OnNavigatedTo(object parameter)
    {
        _buildingSelectionService.BuildingChanged += OnBuildingChanged;
        StatusMessage = State.StatusMessage;
        FilterText = string.Empty;
        BuildingInput = ResolveBuildingInput();
        var buildingFilter = BuildingScope.AsFilter(_buildingSelectionService.SelectedBuilding);
        SelectedWorkCenter = State.WorkCenters.FirstOrDefault(item =>
            (buildingFilter is null
                || string.Equals(item.Building, buildingFilter, StringComparison.OrdinalIgnoreCase))
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
            AppLog.Error(
                "SetupWorkCenters",
                ex,
                "The work-centre setup permission could not be read, so the management controls stay hidden rather than being offered on a guess.");
        }

        // The preview offer depends on this answer, so it is re-evaluated once the permission read has landed.
        RefreshGenericPreviewAvailability();
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
    private void ViewHistory(SetupWorkCenter? workCenter)
    {
        if (workCenter is null)
        {
            return;
        }

        _navigationService.NavigateTo(
            typeof(SetupWorkCenterHistoryViewModel).FullName!,
            new SetupWorkCenterHistoryNavigationData(workCenter.Name, workCenter.Building));
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
            BuildingInput = ResolveBuildingInput();
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
            BuildingInput = ResolveBuildingInput();
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
            // Nothing selected means nothing to designate: the toggle goes back to off rather than keeping the value of
            // the card that has just been cleared, which would offer a write for a work center that is not chosen.
            _suppressGenericWrite = true;
            SelectedWorkCenterIsGeneric = false;
            _suppressGenericWrite = false;
            OnPropertyChanged(nameof(GenericToggleHeader));
            RefreshGenericPreviewAvailability();
            return;
        }

        WorkCenterNameInput = value.Name;
        BuildingInput = string.IsNullOrWhiteSpace(value.Building)
            ? ResolveBuildingInput()
            : value.Building;

        // The card's own value is pushed into the toggle, so selecting a card never counts as a decision.
        _suppressGenericWrite = true;
        SelectedWorkCenterIsGeneric = value.IsGeneric;
        _suppressGenericWrite = false;
        OnPropertyChanged(nameof(GenericToggleHeader));
        RefreshGenericPreviewAvailability();
    }

    /// <summary>
    /// Writes the designation the person just chose, and only when the permission and the value both allow it.
    /// </summary>
    /// <remarks>
    /// The control is absent for anyone without the permission (FR-002), so this is a second lock rather than the
    /// lock: a reachable command must still refuse. A refused write puts the toggle back where it was, because a
    /// toggle that stays flipped while nothing was stored is the screen lying about the plant.
    /// </remarks>
    partial void OnSelectedWorkCenterIsGenericChanged(bool value)
    {
        if (_suppressGenericWrite || SelectedWorkCenter is not { } workCenter || workCenter.IsGeneric == value)
        {
            return;
        }

        _ = ApplyGenericDesignationAsync(workCenter, value);
    }

    private async Task ApplyGenericDesignationAsync(SetupWorkCenter workCenter, bool isGeneric)
    {
        if (!CanManageWorkCenters)
        {
            StatusMessage = "You do not have permission to manage workstations.";
            RevertGenericToggle(workCenter);
            return;
        }

        var result = await _workCenterService
            .SetWorkCenterGenericAsync(workCenter.Id, isGeneric)
            .ConfigureAwait(true);

        StatusMessage = result.Message;

        if (!result.Success)
        {
            RevertGenericToggle(workCenter);
            return;
        }

        workCenter.IsGeneric = isGeneric;
        RefreshGenericPreviewAvailability();
    }

    private void RevertGenericToggle(SetupWorkCenter workCenter)
    {
        _suppressGenericWrite = true;
        SelectedWorkCenterIsGeneric = workCenter.IsGeneric;
        _suppressGenericWrite = false;
    }

    /// <summary>
    /// Offers the preview only when it would actually work: the permission, the mark, and a preview on this machine.
    /// </summary>
    private void RefreshGenericPreviewAvailability() =>
        CanOpenGenericPreview = CanManageWorkCenters
            && SelectedWorkCenter is { IsGeneric: true }
            && _genericPreviewLauncher.ResolvePreviewExecutable() is not null;

    /// <summary>
    /// Opens the generic request preview for the selected work center, so the flow that mark produces can be walked
    /// before the live reads exist.
    /// </summary>
    [RelayCommand]
    private async Task OpenGenericPreviewAsync()
    {
        if (!CanManageWorkCenters || SelectedWorkCenter is not { } workCenter)
        {
            return;
        }

        if (!workCenter.IsGeneric)
        {
            StatusMessage = "Mark this work center generic first, so the preview opens the flow its requests will use.";
            return;
        }

        var result = await _genericPreviewLauncher.LaunchAsync(workCenter.Name).ConfigureAwait(true);
        StatusMessage = result.Message;
        RefreshGenericPreviewAvailability();
    }

    partial void OnFilterTextChanged(string value)
    {
        ApplyFilter();
    }

    private void OnBuildingChanged(object? sender, EventArgs e)
    {
        BuildingInput = ResolveBuildingInput();
        ApplyFilter();
    }

    /// <summary>
    /// The building a work centre added from this screen is filed under: the one in effect, or nothing while every
    /// building is in effect, because "every building" is not a building a work centre can stand in.
    /// </summary>
    private string ResolveBuildingInput() =>
        BuildingScope.AsFilter(_buildingSelectionService.SelectedBuilding) ?? string.Empty;

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

            AppLog.Info("SetupWorkstation", $"Local work centers loaded for the setup selection screen. Count={_hotWorkCenterNames.Count}.");
        }
        catch (Exception ex)
        {
            AppLog.Error("SetupWorkstation", ex, "Failed to load Local work centers for the setup selection screen.");
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
        var selectedBuilding = BuildingScope.AsFilter(_buildingSelectionService.SelectedBuilding);
        var filteredItems = State.WorkCenters.Where(item =>
            (selectedBuilding is null
                || string.Equals(item.Building, selectedBuilding, StringComparison.OrdinalIgnoreCase))
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
