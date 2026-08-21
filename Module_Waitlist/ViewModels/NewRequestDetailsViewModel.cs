using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;

namespace MTM_Waitlist.Module_Waitlist.ViewModels;

/// <summary>
/// Text-input step of the New Request wizard. Replaces the inline "Additional details"
/// <c>ContentDialog</c> that <c>WaitlistNewRequestDialogService</c> built in code:
/// the user types the request details subject to the configured min/max length.
/// </summary>
public partial class NewRequestDetailsViewModel : ObservableRecipient, INavigationAware
{
    private readonly INavigationService _navigationService;

    private NewRequestFlowState? _state;

    [ObservableProperty]
    public partial string Heading
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial string PromptText
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial string InputValue
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial string ValidationMessage
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial bool IsValidationVisible
    {
        get; set;
    }

    public int MinLength
    {
        get;
        private set;
    }

    public int MaxLength
    {
        get;
        private set;
    }

    public NewRequestDetailsViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    public void OnNavigatedTo(object parameter)
    {
        if (parameter is not NewRequestFlowState state || state.RequestType is null)
        {
            _navigationService.GoBack();
            return;
        }

        _state = state;

        var requestType = state.RequestType;
        var subtype = state.Subtype;
        var targetName = subtype is not null ? subtype.Name : requestType.RequestType;
        var promptText = subtype is not null ? subtype.PromptText : requestType.PromptText;

        Heading = subtype is null ? requestType.RequestType : $"{requestType.RequestType} / {subtype.Name}";
        PromptText = string.IsNullOrWhiteSpace(promptText)
            ? $"Enter details for {targetName}"
            : promptText;
        MinLength = subtype?.MinLength ?? requestType.MinLength;
        MaxLength = subtype?.MaxLength ?? requestType.MaxLength;
        InputValue = state.InputValue ?? string.Empty;
        ValidationMessage = string.Empty;
        IsValidationVisible = false;
    }

    public void OnNavigatedFrom()
    {
    }

    [RelayCommand]
    private void Continue()
    {
        if (_state is null || _state.RequestType is null)
        {
            return;
        }

        var value = InputValue?.Trim() ?? string.Empty;
        if (value.Length < MinLength || value.Length > MaxLength)
        {
            ValidationMessage = $"Please enter between {MinLength} and {MaxLength} characters.";
            IsValidationVisible = true;
            return;
        }

        _state.InputValue = value;

        // Text input is complete; never route back to this page. No-subtype flows go to
        // the intermediate preview, subtype flows go straight to confirmation.
        var nextStep = NewRequestFlowRules.ShouldShowIntermediateSummary(_state.RequestType, _state.Subtype)
            ? typeof(NewRequestPreviewViewModel)
            : typeof(NewRequestSummaryViewModel);
        _navigationService.NavigateTo(nextStep.FullName!, _state);
    }

    [RelayCommand]
    private void Back()
    {
        _navigationService.GoBack();
    }
}
