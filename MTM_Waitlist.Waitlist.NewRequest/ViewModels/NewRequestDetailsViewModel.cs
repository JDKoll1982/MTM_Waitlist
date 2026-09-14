using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;
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

    /// <summary>True when the configuration declares an answer the requester picks from a list.</summary>
    [ObservableProperty]
    public partial bool IsOptionPick
    {
        get; set;
    }

    /// <summary>True when the configuration declares an answer the requester types.</summary>
    [ObservableProperty]
    public partial bool IsTextInput
    {
        get; set;
    }

    /// <summary>The picked option when <see cref="IsOptionPick"/> is true.</summary>
    [ObservableProperty]
    public partial string? SelectedOption
    {
        get; set;
    }

    /// <summary>The options the configuration declares, in declared order.</summary>
    public ObservableCollection<string> Options { get; } = new();

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

    /// <summary>
    /// Renders the step from the chosen Item's stored configuration row and nothing else (FR-013): the prompt, the
    /// limits, the options and whether anything is asked for all come from the row, so changing the row changes
    /// this screen with no code change and no rebuild (FR-015). No branch here keys on the Item's identity.
    /// </summary>
    public void OnNavigatedTo(object parameter)
    {
        if (parameter is not NewRequestFlowState state || state.Item is null || state.ItemConfiguration is null)
        {
            _navigationService.GoBack();
            return;
        }

        _state = state;

        var configuration = state.ItemConfiguration;
        Heading = $"{NewRequestItemViewModel.ResolveCategoryName(state.Item.Category)} — {NewRequestItemViewModel.ResolveItemName(state.Item)}";
        PromptText = string.IsNullOrWhiteSpace(configuration.PromptText)
            ? $"Enter details for {NewRequestItemViewModel.ResolveItemName(state.Item)}"
            : configuration.PromptText!;
        MinLength = configuration.MinLength;
        MaxLength = configuration.MaxLength;

        Options.Clear();
        foreach (var option in RequestItemAnswerOptionsResolver.Resolve(configuration, state.Availability))
        {
            Options.Add(option);
        }

        // Which control renders is the configuration's decision, not the Item's.
        IsOptionPick = configuration.AnswerValueType == RequestItemValueType.Enum;
        IsTextInput = !IsOptionPick;

        InputValue = state.InputValue ?? string.Empty;
        SelectedOption = IsOptionPick ? state.InputValue : null;
        ValidationMessage = string.Empty;
        IsValidationVisible = false;
    }

    public void OnNavigatedFrom()
    {
    }

    [RelayCommand]
    private void Continue()
    {
        if (_state?.ItemConfiguration is null)
        {
            return;
        }

        if (IsOptionPick)
        {
            var chosen = SelectedOption?.Trim() ?? string.Empty;
            var known = Options.Count == 0
                || Options.Any(option => string.Equals(option, chosen, StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrWhiteSpace(chosen) || !known)
            {
                ValidationMessage = Options.Count == 0
                    ? "This item's answer options are not configured yet. A supervisor needs to check it."
                    : "Please choose one of the listed options.";
                IsValidationVisible = true;
                return;
            }

            _state.InputValue = chosen;
        }
        else
        {
            var value = InputValue?.Trim() ?? string.Empty;
            if (value.Length < MinLength || value.Length > MaxLength)
            {
                ValidationMessage = $"Please enter between {MinLength} and {MaxLength} characters.";
                IsValidationVisible = true;
                return;
            }

            _state.InputValue = value;
        }

        IsValidationVisible = false;

        // The answer is complete; never route back to this page.
        _navigationService.NavigateTo(typeof(NewRequestPreviewViewModel).FullName!, _state);
    }

    [RelayCommand]
    private void Back()
    {
        _navigationService.GoBack();
    }
}
