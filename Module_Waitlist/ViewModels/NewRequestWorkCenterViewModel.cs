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

    private NewRequestFlowState? _state;
    private HashSet<string> _activeJobWorkCenters = new(StringComparer.OrdinalIgnoreCase);

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

    public ObservableCollection<WorkCenterSelectionItem> HotWorkCenters { get; } = new();

    public ObservableCollection<WorkCenterSelectionItem> OtherWorkCenters { get; } = new();

    public NewRequestWorkCenterViewModel(
        INavigationService navigationService,
        IWorkCenterCatalogService workCenterCatalogService,
        INewRequestFlowService flowService)
    {
        _navigationService = navigationService;
        _workCenterCatalogService = workCenterCatalogService;
        _flowService = flowService;
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
        await LoadWorkCentersAsync().ConfigureAwait(true);
    }

    public void OnNavigatedFrom()
    {
    }

    private async Task LoadWorkCentersAsync()
    {
        IsLoading = true;
        try
        {
            var workstationName = _workCenterCatalogService.GetCurrentWorkstationName();
            var catalog = await _workCenterCatalogService.GetCatalogAsync(workstationName).ConfigureAwait(true);
            var imageLookup = await _flowService.BuildWorkCenterImageLookupAsync().ConfigureAwait(true);

            WorkstationName = catalog.WorkstationName;
            _activeJobWorkCenters = new HashSet<string>(
                catalog.ActiveJobWorkCenters
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value.Trim()),
                StringComparer.OrdinalIgnoreCase);

            Populate(HotWorkCenters, catalog.HotWorkCenters, imageLookup);
            Populate(OtherWorkCenters, catalog.OtherWorkCenters, imageLookup);

            StartupDebugLog.Info("NewRequestWorkCenter", $"Catalog loaded. Workstation='{catalog.WorkstationName}', HotCount={catalog.HotWorkCenters.Count}, OtherCount={catalog.OtherWorkCenters.Count}, ActiveJobCount={_activeJobWorkCenters.Count}.");
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

    private static void Populate(ObservableCollection<WorkCenterSelectionItem> target, IReadOnlyList<string> names, IReadOnlyDictionary<string, string> imageLookup)
    {
        target.Clear();
        foreach (var item in CreateSelectionItems(names, imageLookup))
        {
            target.Add(item);
        }
    }

    private static IReadOnlyList<WorkCenterSelectionItem> CreateSelectionItems(
        IReadOnlyList<string> workCenterNames,
        IReadOnlyDictionary<string, string> imageLookup)
    {
        return workCenterNames
            .Select(workCenterName => new WorkCenterSelectionItem
            {
                WorkCenterName = workCenterName,
                ResolvedImagePath = imageLookup.TryGetValue(workCenterName, out var resolvedImagePath)
                    ? resolvedImagePath
                    : DefaultWorkCenterImagePath,
            })
            .ToList();
    }

    [RelayCommand]
    private void SelectWorkCenter(WorkCenterSelectionItem? workCenterItem)
    {
        if (_state is null || workCenterItem is null || string.IsNullOrWhiteSpace(workCenterItem.WorkCenterName))
        {
            return;
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
