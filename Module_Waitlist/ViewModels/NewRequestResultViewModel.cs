using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;

namespace MTM_Waitlist.Module_Waitlist.ViewModels;

public enum NewRequestResultPhase
{
    Success,
    Duplicate,
    Failure,
}

/// <summary>
/// Navigation payload for the New Request result step. Carries the accumulated wizard
/// state plus the phase (Success / Duplicate / Failure) and a message to display.
/// </summary>
public sealed class NewRequestResultNavigationData
{
    public NewRequestFlowState State { get; init; } = new();

    public NewRequestResultPhase Phase { get; init; }

    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// Terminal step of the New Request wizard. Replaces the duplicate-warning, submission
/// complete, and submission failure <c>ContentDialog</c>s that lived in
/// <c>WaitlistViewPage</c>: it renders the outcome and offers the matching actions
/// (Continue for a duplicate, Retry for a failure, Return/Add Another for success).
/// </summary>
public partial class NewRequestResultViewModel : ObservableRecipient, INavigationAware
{
    private readonly INavigationService _navigationService;
    private readonly IWaitlistRequestService _requestService;

    private NewRequestFlowState? _state;

    [ObservableProperty]
    public partial string Heading
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial string IconGlyph
    {
        get; set;
    } = "\uE73E";

    [ObservableProperty]
    public partial string Message
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial bool IsSuccess
    {
        get; set;
    }

    [ObservableProperty]
    public partial bool IsDuplicate
    {
        get; set;
    }

    [ObservableProperty]
    public partial bool IsFailure
    {
        get; set;
    }

    [ObservableProperty]
    public partial bool IsBusy
    {
        get; set;
    }

    public NewRequestResultViewModel(INavigationService navigationService, IWaitlistRequestService requestService)
    {
        _navigationService = navigationService;
        _requestService = requestService;
    }

    public void OnNavigatedTo(object parameter)
    {
        if (parameter is not NewRequestResultNavigationData data)
        {
            _navigationService.GoBack();
            return;
        }

        _state = data.State;
        ApplyPhase(data.Phase, data.Message);
    }

    public void OnNavigatedFrom()
    {
    }

    private void ApplyPhase(NewRequestResultPhase phase, string message)
    {
        IsSuccess = phase == NewRequestResultPhase.Success;
        IsDuplicate = phase == NewRequestResultPhase.Duplicate;
        IsFailure = phase == NewRequestResultPhase.Failure;
        Message = message;

        switch (phase)
        {
            case NewRequestResultPhase.Success:
                Heading = "Request completed";
                IconGlyph = "\uE73E";
                break;
            case NewRequestResultPhase.Duplicate:
                Heading = "Matching request already active";
                IconGlyph = "\uE7BA";
                break;
            default:
                Heading = "Request not submitted";
                IconGlyph = "\uEA39";
                break;
        }
    }

    [RelayCommand]
    private async Task ContinueDuplicateAsync()
    {
        await ResubmitAsync(allowDuplicate: true).ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task RetryAsync()
    {
        await ResubmitAsync(allowDuplicate: false).ConfigureAwait(true);
    }

    private async Task ResubmitAsync(bool allowDuplicate)
    {
        if (_state is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _requestService.SubmitAsync(_state.ToDraft(), allowDuplicate).ConfigureAwait(true);
            if (result.Status == WaitlistRequestSubmitStatus.Success)
            {
                ApplyPhase(NewRequestResultPhase.Success, result.Message);
            }
            else
            {
                ApplyPhase(NewRequestResultPhase.Failure, result.Message);
            }
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("NewRequestResult", ex, "Request resubmission threw an unexpected exception.");
            ApplyPhase(NewRequestResultPhase.Failure, "The request could not be submitted. Please try again.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ReturnToWaitlist()
    {
        _navigationService.NavigateTo(typeof(WaitlistViewViewModel).FullName!, null, true);
    }

    [RelayCommand]
    private void AddAnotherRequest()
    {
        var freshState = new NewRequestFlowState
        {
            Building = _state?.Building ?? string.Empty,
        };
        _navigationService.NavigateTo(typeof(NewRequestWorkCenterViewModel).FullName!, freshState, true);
    }

    [RelayCommand]
    private void CancelToWaitlist()
    {
        _navigationService.NavigateTo(typeof(WaitlistViewViewModel).FullName!, null, true);
    }
}
