using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;

namespace MTM_Waitlist.Module_Waitlist.ViewModels;

/// <summary>
/// Second step of the New Request wizard. Replaces the dialog-era
/// <c>NewRequestJobTypeDialog</c>: the user picks a job type (Coil, Pickup, Other, ...).
/// </summary>
public partial class NewRequestJobTypeViewModel : ObservableRecipient, INavigationAware
{
    private readonly INavigationService _navigationService;
    private readonly INewRequestFlowService _flowService;
    private readonly ICoilAvailabilityService _coilAvailabilityService;

    private NewRequestFlowState? _state;
    private IReadOnlyList<NewRequestTypeDefinition> _loadedRequestTypes = Array.Empty<NewRequestTypeDefinition>();

    [ObservableProperty]
    public partial string WorkCenterText
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial bool IsLoading
    {
        get; set;
    }

    [ObservableProperty]
    public partial bool IsLoadFailed
    {
        get; set;
    }

    [ObservableProperty]
    public partial bool IsCoilBannerVisible
    {
        get; set;
    }

    [ObservableProperty]
    public partial string CoilBannerText
    {
        get; set;
    } = string.Empty;

    public ObservableCollection<NewRequestOptionItem> JobTypes { get; } = new();

    public NewRequestJobTypeViewModel(INavigationService navigationService, INewRequestFlowService flowService, ICoilAvailabilityService coilAvailabilityService)
    {
        _navigationService = navigationService;
        _flowService = flowService;
        _coilAvailabilityService = coilAvailabilityService;
    }

    public async void OnNavigatedTo(object parameter)
    {
        if (parameter is not NewRequestFlowState state)
        {
            _navigationService.GoBack();
            return;
        }

        _state = state;
        WorkCenterText = $"Work Center: {state.WorkCenter}";
        await LoadJobTypesAsync().ConfigureAwait(true);
    }

    public void OnNavigatedFrom()
    {
    }

    private async Task LoadJobTypesAsync()
    {
        IsLoading = true;
        IsLoadFailed = false;
        try
        {
            var workCenter = _state?.WorkCenter?.Trim() ?? string.Empty;
            var coil = await _coilAvailabilityService.GetCoilForJobAsync(workCenter).ConfigureAwait(true);
            StartupDebugLog.Info("NewRequestJobType", $"Coil availability for work center '{workCenter}': HasCoil={coil.HasCoil}.");

            // Up-front coil readout: show the coil on the job (or a clear 'none') before the
            // worker chooses a request type. Hide the banner when we have no meaningful state
            // (e.g. live source not wired -> HasCoil true with no coil number).
            var showCoil = coil.HasCoil && !string.IsNullOrWhiteSpace(coil.CoilNumber);
            var showNoCoil = !coil.HasCoil;
            IsCoilBannerVisible = showCoil || showNoCoil;
            CoilBannerText = showCoil
                ? $"Current coil {coil.CoilNumber} - {coil.Description} - {coil.QuantityOnHand} on hand - {coil.AverageWeight}"
                : showNoCoil ? $"No coil is loaded on this job." : string.Empty;

            var requestTypes = NewRequestFlowRules.ApplyActiveJobEligibility(
                await _flowService.LoadRequestTypesAsync().ConfigureAwait(true),
                hasCoilData: coil.HasCoil,
                hasFlatstockData: true,
                hasPartData: true,
                hasWorkOrderData: true);

            _loadedRequestTypes = requestTypes;

            JobTypes.Clear();
            foreach (var requestType in requestTypes.Where(item => !string.IsNullOrWhiteSpace(item.RequestType)))
            {
                var requestTypeName = requestType.RequestType.Trim();
                JobTypes.Add(new NewRequestOptionItem
                {
                    Name = requestTypeName,
                    Summary = requestType.Subtypes.Count == 0
                        ? "No subtype required"
                        : $"{requestType.Subtypes.Count} subtype option(s)",
                    ImagePath = await _flowService.ResolveRequestTypeImagePathAsync(requestTypeName).ConfigureAwait(true),
                });
            }
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("NewRequestJobType", ex, "Failed to load job types.");
            IsLoadFailed = true;
            JobTypes.Clear();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void SelectJobType(NewRequestOptionItem? item)
    {
        if (_state is null || item is null || string.IsNullOrWhiteSpace(item.Name))
        {
            return;
        }

        var selectedRequestType = _loadedRequestTypes.FirstOrDefault(candidate =>
            string.Equals(candidate.RequestType.Trim(), item.Name.Trim(), StringComparison.OrdinalIgnoreCase));
        if (selectedRequestType is null)
        {
            return;
        }

        _state.RequestType = selectedRequestType;
        _state.Subtype = null;
        _state.InputValue = null;

        StartupDebugLog.Info("NewRequestJobType", $"Selected job type '{selectedRequestType.RequestType}' for work center '{_state.WorkCenter}'.");

        if (selectedRequestType.Subtypes.Count > 0)
        {
            _navigationService.NavigateTo(typeof(NewRequestSubtypeViewModel).FullName!, _state);
            return;
        }

        _navigationService.NavigateTo(NewRequestFlowRules.GetNextStepType(_state).FullName!, _state);
    }

    [RelayCommand]
    private void Back()
    {
        _navigationService.GoBack();
    }
}
