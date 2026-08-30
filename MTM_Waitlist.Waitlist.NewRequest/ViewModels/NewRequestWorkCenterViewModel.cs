using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Shared.Models;
using MTM_Waitlist.Module_Shared.Services;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;

namespace MTM_Waitlist.Module_Waitlist.ViewModels;

/// <summary>
/// First step of the New Request wizard. Replaces the dialog-era
/// <c>WorkCenterSelectionDialog</c>: the user picks an active work center, which
/// becomes the request's work center before the job-type step.
/// </summary>
public partial class NewRequestWorkCenterViewModel : ObservableRecipient, INavigationAware
{
    private const string DefaultWorkCenterImagePath = "Assets/Images/default-workstation-image.png";

    private readonly INavigationService _navigationService;
    private readonly IWorkCenterCatalogService _workCenterCatalogService;
    private readonly INewRequestFlowService _flowService;
    private readonly IBuildingSelectionService _buildingSelectionService;

    private NewRequestFlowState? _state;
    private HashSet<string> _activeJobWorkCenters = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _hotWorkCenterNames = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<WorkCenterSelectionItem> _allWorkCenters = Array.Empty<WorkCenterSelectionItem>();

    [ObservableProperty]
    public partial string WorkstationName
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial bool IsLoading
    {
        get; set;
    }

    [ObservableProperty]
    public partial bool IsNoActiveJobWarningVisible
    {
        get; set;
    }

    [ObservableProperty]
    public partial bool IsVerificationWarningVisible
    {
        get; set;
    }

    [ObservableProperty]
    public partial string VerificationMessage
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial string FilterText
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial string SelectedBuilding
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

    public ObservableCollection<WorkCenterSelectionItem> HotWorkCenters { get; } = new();

    public ObservableCollection<WorkCenterSelectionItem> OtherWorkCenters { get; } = new();

    public IReadOnlyList<string> Buildings => _buildingSelectionService.Buildings;

    public string OtherWorkCentersHeader => IsOtherWorkCentersExpanded
        ? "Hide Other Work Centers"
        : "Show Other Work Centers";

    partial void OnIsOtherWorkCentersExpandedChanged(bool value)
    {
        OnPropertyChanged(nameof(OtherWorkCentersHeader));
    }

    public NewRequestWorkCenterViewModel(
        INavigationService navigationService,
        IWorkCenterCatalogService workCenterCatalogService,
        INewRequestFlowService flowService,
        IBuildingSelectionService buildingSelectionService)
    {
        _navigationService = navigationService;
        _workCenterCatalogService = workCenterCatalogService;
        _flowService = flowService;
        _buildingSelectionService = buildingSelectionService;
    }

    public async void OnNavigatedTo(object parameter)
    {
        if (parameter is not NewRequestFlowState state)
        {
            _navigationService.GoBack();
            return;
        }

        _state = state;
        IsVerificationWarningVisible = false;
        VerificationMessage = string.Empty;
        IsNoActiveJobWarningVisible = false;

        _buildingSelectionService.BuildingChanged += OnBuildingChanged;
        SelectedBuilding = _buildingSelectionService.SelectedBuilding;
        FilterText = string.Empty;

        await LoadWorkCentersAsync().ConfigureAwait(true);
    }

    public void OnNavigatedFrom()
    {
        _buildingSelectionService.BuildingChanged -= OnBuildingChanged;
    }

    private async Task LoadWorkCentersAsync()
    {
        IsLoading = true;
        try
        {
            var workstationName = _workCenterCatalogService.GetCurrentComputerName();
            var catalog = await _workCenterCatalogService.GetCatalogAsync(workstationName).ConfigureAwait(true);
            var imageLookup = await _flowService.BuildWorkCenterImageLookupAsync().ConfigureAwait(true);

            WorkstationName = catalog.ComputerName;
            _activeJobWorkCenters = new HashSet<string>(
                catalog.ActiveJobWorkCenters
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value.Trim()),
                StringComparer.OrdinalIgnoreCase);

            _hotWorkCenterNames.Clear();
            foreach (var name in catalog.HotWorkCenters)
            {
                _hotWorkCenterNames.Add(name.Trim());
            }

            _allWorkCenters = CreateSelectionItems(
                catalog.HotWorkCenters.Concat(catalog.OtherWorkCenters).ToList(),
                imageLookup,
                catalog.WorkCenterDetails);

            ApplyFilter();
            UpdateWorkCenterSectionsVisibility();

            StartupDebugLog.Info("NewRequestWorkCenter", $"Catalog loaded. Workstation='{catalog.ComputerName}', HotCount={catalog.HotWorkCenters.Count}, OtherCount={catalog.OtherWorkCenters.Count}, ActiveJobCount={_activeJobWorkCenters.Count}.");
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("NewRequestWorkCenter", ex, "Failed to load the work center catalog.");
            WorkstationName = string.Empty;
            HotWorkCenters.Clear();
            OtherWorkCenters.Clear();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static IReadOnlyList<WorkCenterSelectionItem> CreateSelectionItems(
        IReadOnlyList<string> workCenterNames,
        IReadOnlyDictionary<string, string> imageLookup,
        IReadOnlyDictionary<string, WorkCenterDetail> workCenterDetails)
    {
        return workCenterNames
            .Select(workCenterName =>
            {
                workCenterDetails.TryGetValue(workCenterName, out var detail);
                return new WorkCenterSelectionItem
                {
                    WorkCenterName = workCenterName,
                    ResolvedImagePath = imageLookup.TryGetValue(workCenterName, out var resolvedImagePath)
                        ? resolvedImagePath
                        : DefaultWorkCenterImagePath,
                    Building = detail?.Building ?? string.Empty,
                    LastUpdatedUtc = detail?.LastUpdatedUtc,
                    HasActiveJob = detail?.HasActiveJob ?? false,
                    CurrentWorkOrder = detail?.CurrentWorkOrder ?? string.Empty,
                    CurrentPartNumber = detail?.CurrentPartNumber ?? string.Empty,
                    CurrentSequenceNumber = detail?.CurrentSequenceNumber ?? string.Empty,
                    IsSelected = false,
                };
            })
            .ToList();
    }

    private void ApplyFilter()
    {
        HotWorkCenters.Clear();
        OtherWorkCenters.Clear();

        var normalizedFilter = FilterText.Trim();
        var selectedBuilding = _buildingSelectionService.SelectedBuilding;
        var filteredItems = _allWorkCenters.Where(item =>
            string.Equals(item.Building, selectedBuilding, StringComparison.OrdinalIgnoreCase)
            && (string.IsNullOrWhiteSpace(normalizedFilter)
                || item.WorkCenterName.Contains(normalizedFilter, StringComparison.OrdinalIgnoreCase)
                || item.CurrentWorkOrder.Contains(normalizedFilter, StringComparison.OrdinalIgnoreCase)
                || item.CurrentSequenceNumber.Contains(normalizedFilter, StringComparison.OrdinalIgnoreCase)
                || item.CurrentPartNumber.Contains(normalizedFilter, StringComparison.OrdinalIgnoreCase)));

        foreach (var workCenter in filteredItems)
        {
            if (_hotWorkCenterNames.Contains(workCenter.WorkCenterName))
            {
                HotWorkCenters.Add(workCenter);
            }
            else
            {
                OtherWorkCenters.Add(workCenter);
            }
        }
    }

    private void UpdateWorkCenterSectionsVisibility()
    {
        var hasLocalWorkCenters = _hotWorkCenterNames.Count > 0;
        IsLocalWorkCentersVisible = hasLocalWorkCenters;
        // When this computer has no configured local work centers, show the others by default.
        IsOtherWorkCentersExpanded = !hasLocalWorkCenters;
    }

    partial void OnFilterTextChanged(string value)
    {
        ApplyFilter();
    }

    private void OnBuildingChanged(object? sender, EventArgs e)
    {
        SelectedBuilding = _buildingSelectionService.SelectedBuilding;
        ApplyFilter();
    }

    [RelayCommand]
    private void SelectWorkCenter(WorkCenterSelectionItem? workCenterItem)
    {
        if (_state is null || workCenterItem is null || string.IsNullOrWhiteSpace(workCenterItem.WorkCenterName))
        {
            return;
        }

        // The highlight is transient because the page navigates on click, but the
        // selected card is still marked so the blue outline renders before navigation.
        foreach (var item in HotWorkCenters)
        {
            item.IsSelected = ReferenceEquals(item, workCenterItem);
        }

        foreach (var item in OtherWorkCenters)
        {
            item.IsSelected = ReferenceEquals(item, workCenterItem);
        }

        var normalizedWorkCenter = workCenterItem.WorkCenterName.Trim();
        if (!_activeJobWorkCenters.Contains(normalizedWorkCenter))
        {
            StartupDebugLog.Info("NewRequestWorkCenter", $"Blocked workstation selection for '{normalizedWorkCenter}' because no active setup job exists.");
            IsNoActiveJobWarningVisible = true;
            IsVerificationWarningVisible = false;
            return;
        }

        var verification = NewRequestFlowRules.VerifyEmployeeIdentity("6229");
        if (!verification.IsValid)
        {
            IsNoActiveJobWarningVisible = false;
            IsVerificationWarningVisible = true;
            VerificationMessage = verification.Message;
            StartupDebugLog.Info("NewRequestWorkCenter", $"Employee verification blocked the request. Message='{verification.Message}'.");
            return;
        }

        _state.WorkCenter = normalizedWorkCenter;
        _state.RequesterEmployeeNumber = verification.EmployeeNumber;
        _state.RequesterEmployeeName = verification.EmployeeName;

        StartupDebugLog.Info("NewRequestWorkCenter", $"Selected workstation '{normalizedWorkCenter}'.");
        _navigationService.NavigateTo(typeof(NewRequestJobTypeViewModel).FullName!, _state);
    }

    [RelayCommand]
    private void Cancel()
    {
        _navigationService.GoBack();
    }
}
