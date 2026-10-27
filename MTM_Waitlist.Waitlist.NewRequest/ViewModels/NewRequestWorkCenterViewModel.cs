using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Shared.Models;
using MTM_Waitlist.Module_Shared.Helpers;
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
    private const string DefaultWorkCenterImagePath = ImagePicturePolicy.NoImagePath;

    private readonly INavigationService _navigationService;
    private readonly IWorkCenterCatalogService _workCenterCatalogService;
    private readonly INewRequestFlowService _flowService;
    private readonly IBuildingSelectionService _buildingSelectionService;

    /// <summary>
    /// The signed-in person's own identity, read from the launch pipeline's resolved person. The step attributes
    /// the request to them and to nobody else: a literal here is what attributed every request in the store to
    /// one man (FR-046).
    /// </summary>
    private readonly IPersonIdentity _personIdentity;

    /// <summary>
    /// The account records, read by the identifier the session carries. The directory is the authority on whether
    /// that person exists, so the rule never has to trust a name the caller happens to hold (FR-046).
    /// </summary>
    private readonly IEmployeeDirectoryService _employeeDirectoryService;

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

    /// <summary>
    /// The building this step shows work centers for: the shell's own selection, and nothing else.
    /// </summary>
    /// <remarks>
    /// The step used to offer a second, page-level building choice beside the one the shell carries in its header.
    /// Two choosers for one value meant the header could say one building while the cards below it came from another.
    /// The step now reads the shell's selection and follows it, including while the step is open
    /// (<see cref="OnBuildingChanged"/>).
    /// </remarks>
    public string SelectedBuilding => _buildingSelectionService.SelectedBuilding;

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
        IBuildingSelectionService buildingSelectionService,
        IPersonIdentity personIdentity,
        IEmployeeDirectoryService employeeDirectoryService)
    {
        _navigationService = navigationService;
        _workCenterCatalogService = workCenterCatalogService;
        _flowService = flowService;
        _buildingSelectionService = buildingSelectionService;
        _personIdentity = personIdentity;
        _employeeDirectoryService = employeeDirectoryService;
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

            AppLog.Info("NewRequestWorkCenter", $"Catalog loaded. Workstation='{catalog.ComputerName}', HotCount={catalog.HotWorkCenters.Count}, OtherCount={catalog.OtherWorkCenters.Count}, ActiveJobCount={_activeJobWorkCenters.Count}.");
        }
        catch (Exception ex)
        {
            AppLog.Error("NewRequestWorkCenter", ex, "Failed to load the work center catalog.");
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
        var selectedBuilding = BuildingScope.AsFilter(_buildingSelectionService.SelectedBuilding);
        var filteredItems = _allWorkCenters.Where(item =>
            (selectedBuilding is null
                || string.Equals(item.Building, selectedBuilding, StringComparison.OrdinalIgnoreCase))
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

    private void OnBuildingChanged(object? sender, EventArgs e) => ApplyFilter();

    /// <summary>
    /// Takes the chosen work centre and moves the wizard on, attributing the request to the person who is
    /// actually signed in (FR-046).
    /// </summary>
    /// <remarks>
    /// The identity is resolved from the account records by the identifier the session carries — never from a
    /// name and never from a literal. A person the directory does not account for is refused here, at the step
    /// where the request would otherwise be created under their name, and a store that cannot be read is
    /// reported as the outage it is rather than as a refusal they did not earn (FR-026).
    /// </remarks>
    [RelayCommand]
    private async Task SelectWorkCenterAsync(WorkCenterSelectionItem? workCenterItem)
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
            AppLog.Info("NewRequestWorkCenter", $"Blocked workstation selection for '{normalizedWorkCenter}' because no active setup job exists.");
            IsNoActiveJobWarningVisible = true;
            IsVerificationWarningVisible = false;
            return;
        }

        EmployeeVerificationResult verification;
        try
        {
            // No ConfigureAwait(false): the continuation sets bindable state, which is UI-thread-only.
            var employeeNumber = _personIdentity.EmployeeNumber ?? string.Empty;
            var storedEmployee = await _employeeDirectoryService
                .FindByEmployeeIdentifierAsync(employeeNumber)
                .ConfigureAwait(true);

            verification = NewRequestFlowRules.VerifyEmployeeIdentity(employeeNumber, storedEmployee);
        }
        catch (Exception ex)
        {
            AppLog.Error("NewRequestWorkCenter", ex, "The employee lookup failed; the request was not started.");
            IsNoActiveJobWarningVisible = false;
            IsVerificationWarningVisible = true;
            VerificationMessage = "The employee records could not be read, so this request could not be attributed. Try again.";
            return;
        }

        if (!verification.IsValid)
        {
            IsNoActiveJobWarningVisible = false;
            IsVerificationWarningVisible = true;
            VerificationMessage = verification.Message;
            AppLog.Info("NewRequestWorkCenter", $"Employee verification blocked the request. Message='{verification.Message}'.");
            return;
        }

        _state.WorkCenter = normalizedWorkCenter;
        _state.RequesterEmployeeNumber = verification.EmployeeNumber;
        _state.RequesterEmployeeName = verification.EmployeeName;

        // The request is filed under the building of the work centre it names. That is the same answer the shell
        // carries while one building is in effect, and it is the only answer while every building is: a request
        // cannot be filed against "every building", so the work centre decides it.
        var requestBuilding = string.IsNullOrWhiteSpace(workCenterItem.Building)
            ? BuildingScope.AsFilter(_buildingSelectionService.SelectedBuilding)
            : workCenterItem.Building.Trim();

        if (!string.IsNullOrWhiteSpace(requestBuilding))
        {
            _state.Building = requestBuilding;
        }

        AppLog.Info("NewRequestWorkCenter", $"Selected workstation '{normalizedWorkCenter}' for employee '{verification.EmployeeNumber}'.");
        _navigationService.NavigateTo(typeof(NewRequestJobTypeViewModel).FullName!, _state);
    }

    [RelayCommand]
    private void Cancel()
    {
        _navigationService.GoBack();
    }
}
