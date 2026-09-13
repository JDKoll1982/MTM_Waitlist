using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;

namespace MTM_Waitlist.Module_Waitlist.ViewModels;

/// <summary>
/// Final confirmation step of the New Request wizard. It shows the request summary and the coil on the job
/// current at confirmation time, and submits the request when the user confirms. It presents no queue or
/// wait figure: no source produces one, so there is nothing to show until a specification defines it.
/// </summary>
public partial class NewRequestSummaryViewModel : ObservableRecipient, INavigationAware
{
    private readonly INavigationService _navigationService;
    private readonly IWaitlistRequestService _requestService;
    private readonly ICoilAvailabilityService _coilAvailabilityService;

    private NewRequestFlowState? _state;

    [ObservableProperty]
    public partial string WorkCenter
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial string CategoryText
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial string ItemText
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial bool HasItem
    {
        get; set;
    }

    [ObservableProperty]
    public partial string Detail
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial bool HasDetail
    {
        get; set;
    }

    [ObservableProperty]
    public partial bool IsCoilVisible
    {
        get; set;
    }

    [ObservableProperty]
    public partial string CoilNumber
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial string CoilQuantityOnHand
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial string CoilDescription
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial string CoilAverageWeight
    {
        get; set;
    } = string.Empty;

    /// <summary>
    /// Whether an average coil weight was actually resolved for the coil on the job. The row is hidden when
    /// it was not, so the confirm step never shows a labelled value nobody produced (FR-012/FR-013).
    /// </summary>
    public bool HasCoilAverageWeight => !string.IsNullOrWhiteSpace(CoilAverageWeight);

    partial void OnCoilAverageWeightChanged(string value) => OnPropertyChanged(nameof(HasCoilAverageWeight));

    [ObservableProperty]
    public partial bool IsSubmitting
    {
        get; set;
    }

    [ObservableProperty]
    public partial bool IsStatusVisible
    {
        get; set;
    }

    [ObservableProperty]
    public partial bool IsStatusError
    {
        get; set;
    }

    [ObservableProperty]
    public partial string StatusMessage
    {
        get; set;
    } = string.Empty;

    public bool CanSubmit => !IsSubmitting;

    partial void OnIsSubmittingChanged(bool value) => OnPropertyChanged(nameof(CanSubmit));

    public NewRequestSummaryViewModel(INavigationService navigationService, IWaitlistRequestService requestService, ICoilAvailabilityService coilAvailabilityService)
    {
        _navigationService = navigationService;
        _requestService = requestService;
        _coilAvailabilityService = coilAvailabilityService;
    }

    public void OnNavigatedTo(object parameter)
    {
        if (parameter is not NewRequestFlowState state || state.Item is null)
        {
            _navigationService.GoBack();
            return;
        }

        _state = state;
        WorkCenter = state.WorkCenter;
        CategoryText = NewRequestItemViewModel.ResolveCategoryName(state.Category ?? state.Item.Category);
        ItemText = NewRequestItemViewModel.ResolveItemName(state.Item);
        HasItem = !string.IsNullOrWhiteSpace(ItemText);
        Detail = state.InputValue ?? string.Empty;
        HasDetail = !string.IsNullOrWhiteSpace(Detail);
        IsSubmitting = false;
        IsStatusVisible = false;
        IsStatusError = false;
        StatusMessage = string.Empty;

        ResetCoilDetail();
        if (IsCoilRequest(state))
        {
            _ = LoadCoilAsync();
        }
    }

    /// <summary>
    /// Whether to show the job's coil readout. The <b>job</b> has a coil — the availability snapshot says so —
    /// never a property of the Item the requester chose.
    /// </summary>
    private static bool IsCoilRequest(NewRequestFlowState state) => state.Availability.HasCoil;

    private void ResetCoilDetail()
    {
        IsCoilVisible = false;
        CoilNumber = string.Empty;
        CoilQuantityOnHand = string.Empty;
        CoilDescription = string.Empty;
        CoilAverageWeight = string.Empty;
    }

    private async Task LoadCoilAsync()
    {
        if (_state is null)
        {
            return;
        }

        try
        {
            var coil = await _coilAvailabilityService.GetCoilForJobAsync(_state.WorkCenter).ConfigureAwait(true);
            if (!coil.HasCoil || string.IsNullOrWhiteSpace(coil.CoilNumber))
            {
                ResetCoilDetail();
                return;
            }

            IsCoilVisible = true;
            CoilNumber = coil.CoilNumber;
            CoilQuantityOnHand = coil.QuantityOnHand;
            CoilDescription = coil.Description;
            CoilAverageWeight = coil.AverageWeight;
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("NewRequestSummary", ex, "Failed to resolve coil details for the confirm screen.");
            ResetCoilDetail();
        }
    }

    public void OnNavigatedFrom()
    {
    }

    [RelayCommand]
    private async Task SubmitAsync()
    {
        if (_state is null)
        {
            return;
        }

        var jobValidation = NewRequestFlowRules.ValidateCurrentJobState(_state.WorkCenter, _state.WorkCenter);
        if (!jobValidation.IsValid)
        {
            StatusMessage = jobValidation.Message;
            IsStatusError = true;
            IsStatusVisible = true;
            StartupDebugLog.Info("NewRequestSummary", $"Submission blocked because the active job changed for work center '{_state.WorkCenter}'.");
            return;
        }

        IsSubmitting = true;
        IsStatusVisible = false;
        try
        {
            var result = await _requestService.SubmitAsync(_state.ToDraft(), allowDuplicate: false).ConfigureAwait(true);
            switch (result.Status)
            {
                case WaitlistRequestSubmitStatus.Success:
                    NavigateToResult(NewRequestResultPhase.Success, result.Message);
                    break;
                case WaitlistRequestSubmitStatus.DuplicateWarningRequired:
                    NavigateToResult(NewRequestResultPhase.Duplicate, result.Message);
                    break;
                default:
                    NavigateToResult(NewRequestResultPhase.Failure, result.Message);
                    break;
            }
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("NewRequestSummary", ex, "Request submission threw an unexpected exception.");
            StatusMessage = "The request could not be submitted. Please try again.";
            IsStatusError = true;
            IsStatusVisible = true;
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    private void NavigateToResult(NewRequestResultPhase phase, string message)
    {
        if (_state is null)
        {
            return;
        }

        _navigationService.NavigateTo(
            typeof(NewRequestResultViewModel).FullName!,
            new NewRequestResultNavigationData { State = _state, Phase = phase, Message = message });
    }

    [RelayCommand]
    private void Back()
    {
        _navigationService.GoBack();
    }
}
