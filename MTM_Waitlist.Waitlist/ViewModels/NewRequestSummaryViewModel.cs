using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;

namespace MTM_Waitlist.Module_Waitlist.ViewModels;

/// <summary>
/// Final confirmation step of the New Request wizard. Replaces the inline
/// <c>ShowConfirmationAsync</c> dialog: it shows the request summary plus queue and
/// wait-time information and submits the request when the user confirms.
/// </summary>
public partial class NewRequestSummaryViewModel : ObservableRecipient, INavigationAware
{
    private readonly INavigationService _navigationService;
    private readonly IWaitlistRequestService _requestService;

    private NewRequestFlowState? _state;

    [ObservableProperty]
    public partial string WorkCenter
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial string RequestType
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial string SubtypeName
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial bool HasSubtype
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

    public NewRequestSummaryViewModel(INavigationService navigationService, IWaitlistRequestService requestService)
    {
        _navigationService = navigationService;
        _requestService = requestService;
    }

    public void OnNavigatedTo(object parameter)
    {
        if (parameter is not NewRequestFlowState state || state.RequestType is null)
        {
            _navigationService.GoBack();
            return;
        }

        _state = state;
        WorkCenter = state.WorkCenter;
        RequestType = state.RequestType.RequestType;
        SubtypeName = state.Subtype?.Name ?? string.Empty;
        HasSubtype = !string.IsNullOrWhiteSpace(SubtypeName);
        Detail = state.InputValue ?? string.Empty;
        HasDetail = !string.IsNullOrWhiteSpace(Detail);
        IsSubmitting = false;
        IsStatusVisible = false;
        IsStatusError = false;
        StatusMessage = string.Empty;
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
