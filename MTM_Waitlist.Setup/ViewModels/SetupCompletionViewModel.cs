using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Setup.Contracts.Services;

namespace MTM_Waitlist.Module_Setup.ViewModels;

public sealed class SetupCompletionNavigationData
{
    public bool Success { get; init; }

    public string Message { get; init; } = string.Empty;
}

public partial class SetupCompletionViewModel : ObservableRecipient, INavigationAware
{
    private readonly INavigationService _navigationService;
    private readonly ISetupWorkflowService _workflowService;

    private const string WaitlistRoute = "MTM_Waitlist.Module_Waitlist.ViewModels.WaitlistViewViewModel";

    [ObservableProperty]
    public partial bool IsSuccess
    {
        get; set;
    }

    [ObservableProperty]
    public partial string ResultMessage
    {
        get; set;
    } = string.Empty;

    private static string LocalizeOrDefault(string key, string fallback)
    {
        var localized = key.GetLocalized();
        return string.Equals(localized, key, StringComparison.Ordinal) ? fallback : localized;
    }

    public string PageTitle => LocalizeOrDefault("Setup_Completion.Title", "Setup Result");

    public string ResultHeading => IsSuccess
        ? LocalizeOrDefault("Setup_Completion.SuccessHeading", "Success")
        : LocalizeOrDefault("Setup_Completion.FailureHeading", "Failure");

    public string ResultIconGlyph => IsSuccess ? "\uE73E" : "\uEA39";

    public Visibility FailureReasonVisibility => IsSuccess ? Visibility.Collapsed : Visibility.Visible;

    public SetupCompletionViewModel(INavigationService navigationService, ISetupWorkflowService workflowService)
    {
        _navigationService = navigationService;
        _workflowService = workflowService;
    }

    public void OnNavigatedTo(object parameter)
    {
        var data = parameter as SetupCompletionNavigationData;
        if (data is null)
        {
            IsSuccess = false;
            ResultMessage = LocalizeOrDefault("Setup_Completion.UnknownFailure", "Setup save failed. Please review the reason and try again.");
        }
        else
        {
            IsSuccess = data.Success;
            ResultMessage = string.IsNullOrWhiteSpace(data.Message)
                ? (IsSuccess
                    ? LocalizeOrDefault("Setup_Completion.SuccessMessage", "Setup was saved successfully.")
                    : LocalizeOrDefault("Setup_Completion.UnknownFailure", "Setup save failed. Please review the reason and try again."))
                : data.Message;
        }

        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(ResultHeading));
        OnPropertyChanged(nameof(ResultIconGlyph));
        OnPropertyChanged(nameof(FailureReasonVisibility));
    }

    public void OnNavigatedFrom()
    {
    }

    [RelayCommand]
    private async Task StartOverAsync()
    {
        await _workflowService.ResetAsync().ConfigureAwait(true);
        _navigationService.NavigateTo(typeof(SetupWorkCenterViewModel).FullName!, null, true);
    }

    [RelayCommand]
    private void ReturnToWaitlist()
    {
        _navigationService.NavigateTo(WaitlistRoute, null, true);
    }
}
