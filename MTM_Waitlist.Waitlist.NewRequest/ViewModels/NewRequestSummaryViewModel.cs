using System.Globalization;

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

    /// <summary>
    /// One row per entry this confirmation will raise — every die the operator chose, in the order they will be
    /// raised — rather than the request's single value, which showed one die while several were raised (FR-054).
    /// </summary>
    [ObservableProperty]
    public partial IReadOnlyList<string> DetailLines
    {
        get; set;
    } = Array.Empty<string>();

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

    /// <summary>
    /// Whether this confirmation raises <b>more than one</b> request. A job carrying several dies is one request
    /// per die the operator chose, so the step says so rather than submitting several entries silently (FR-054).
    /// </summary>
    [ObservableProperty]
    public partial bool HasMultipleRequests
    {
        get; set;
    }

    /// <summary>
    /// How many requests this confirmation raises, or raised — resolved through the resource mechanism, so it is
    /// plain language and never a bare resource key (FR-022). Empty for the ordinary single request.
    /// </summary>
    [ObservableProperty]
    public partial string RequestCountText
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
        DetailLines = state.DetailLines();
        HasDetail = DetailLines.Count > 0;
        IsSubmitting = false;
        IsStatusVisible = false;
        IsStatusError = false;
        StatusMessage = string.Empty;

        AnnounceHowManyRequests(state);

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
            var drafts = _state.ToDrafts();
            WaitlistRequestSubmitResult? result = null;
            var raised = 0;

            foreach (var draft in drafts)
            {
                result = await _requestService.SubmitAsync(draft, allowDuplicate: false).ConfigureAwait(true);
                if (result.Status != WaitlistRequestSubmitStatus.Success)
                {
                    // A die that could not be raised stops the run rather than being skipped silently: the
                    // operator is told which failure it was, and the entries already raised stand.
                    break;
                }

                raised++;
            }

            if (result is null)
            {
                StatusMessage = "The request could not be submitted. Please try again.";
                IsStatusError = true;
                IsStatusVisible = true;
                return;
            }

            if (raised > 1)
            {
                RequestCountText = FormatRequestCount(
                    "NewRequest_Summary.RequestsRaised",
                    "{0} requests were raised — one for each die you chose.",
                    raised);
            }

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

    /// <summary>
    /// Says how many requests this confirmation will raise, before the operator commits — one per die they chose,
    /// and nothing at all for the ordinary single request (FR-054).
    /// </summary>
    private void AnnounceHowManyRequests(NewRequestFlowState state)
    {
        var count = state.ToDrafts().Count;

        HasMultipleRequests = count > 1;
        RequestCountText = HasMultipleRequests
            ? FormatRequestCount(
                "NewRequest_Summary.MultipleRequests",
                "This will raise {0} requests — one for each die you chose.",
                count)
            : string.Empty;
    }

    private static string FormatRequestCount(string key, string fallback, int count) =>
        string.Format(CultureInfo.CurrentCulture, LocalizeOrDefault(key, fallback), count);

    private static string LocalizeOrDefault(string key, string fallback)
    {
        var localized = key.GetLocalized();
        return string.IsNullOrWhiteSpace(localized) || string.Equals(localized, key, StringComparison.Ordinal)
            ? fallback
            : localized;
    }

    [RelayCommand]
    private void Back()
    {
        _navigationService.GoBack();
    }
}
