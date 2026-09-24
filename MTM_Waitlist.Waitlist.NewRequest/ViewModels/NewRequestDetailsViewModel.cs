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
/// Additional-details step of the New Request wizard. Replaces the inline "Additional details"
/// <c>ContentDialog</c> that <c>WaitlistNewRequestDialogService</c> built in code: the person answers subject to
/// the configured min/max length, or picks one of the configured options.
/// </summary>
/// <remarks>
/// The step has no Continue button. The answer is the way out: the view executes <see cref="ContinueCommand"/> when
/// a listed answer is picked or Enter is pressed in the text box, so an answer that does not satisfy the row is
/// reported on the step the person is still looking at rather than ending the request elsewhere (FR-014).
/// </remarks>
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

    /// <summary>
    /// The listed answer this step was entered with, or null when it asked nothing before. Coming Back to the step
    /// restores the previous answer, and that restore raises <c>SelectionChanged</c> on the list; the view compares
    /// against this so a restore is not mistaken for a fresh choice — otherwise the step would leave again the
    /// instant it appeared and the answer could never be corrected.
    /// </summary>
    public string? RestoredAnswer
    {
        get;
        private set;
    }

    /// <summary>The options the configuration declares, in declared order.</summary>
    public ObservableCollection<string> Options { get; } = new();

    /// <summary>
    /// The same options as cards, for the list a person chooses from (FR-021, FR-023).
    /// </summary>
    /// <remarks>
    /// An answer is a word the configuration declares and has no picture of its own, so every card draws the one
    /// shared no-image picture and the answer is still offered and still selectable. The cards are the list; the
    /// answer behind them is unchanged, which is why the choice still flows through
    /// <see cref="SelectedOption"/> exactly as it did through the drop-down.
    /// </remarks>
    public ObservableCollection<AnswerOptionCard> OptionCards { get; } = new();

    /// <summary>Rebuilds the answer cards, marking the answer currently chosen.</summary>
    private void RefreshOptionCards()
    {
        OptionCards.Clear();

        foreach (var option in Options)
        {
            OptionCards.Add(new AnswerOptionCard(
                option,
                string.Equals(option, SelectedOption, StringComparison.OrdinalIgnoreCase)));
        }
    }

    /// <summary>Chooses an answer from the list of cards.</summary>
    /// <param name="answer">The answer whose card was chosen.</param>
    public void SelectAnswer(string? answer)
    {
        if (!string.IsNullOrWhiteSpace(answer))
        {
            SelectedOption = answer;
        }
    }

    partial void OnSelectedOptionChanged(string? value) => RefreshOptionCards();

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
        RefreshOptionCards();

        // Read before the list is bound: the binding applies this value to the list control, which reports it back
        // as a selection change. Recording it here means the view can tell that report from a person's pick without
        // depending on which of loading and loading-complete happens first.
        RestoredAnswer = SelectedOption;

        ValidationMessage = string.Empty;
        IsValidationVisible = false;
    }

    public void OnNavigatedFrom()
    {
    }

    /// <summary>
    /// Judges the answer against the chosen Item's configuration row and moves on to the confirmation step once it
    /// passes. An answer that does not — a typed one outside the configured length, a picked one that is blank or
    /// not on the list — is reported here and the flow stays on this step.
    /// </summary>
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

        // The answer is complete; never route back to this page. The confirmation step is the next and last
        // step before the request is raised.
        _navigationService.NavigateTo(typeof(NewRequestSummaryViewModel).FullName!, _state);
    }

    [RelayCommand]
    private void Back()
    {
        _navigationService.GoBack();
    }
}

/// <summary>
/// One listed answer, shaped for the shared part-picture card: the card needs a title, a picture to draw, a chosen
/// flag and two automation ids, and an answer the configuration declares has no picture of its own.
/// </summary>
public sealed class AnswerOptionCard
{
    public AnswerOptionCard(string answer, bool isSelected)
    {
        Answer = answer ?? string.Empty;
        IsSelected = isSelected;
        AutomationId = $"NewRequestDetailsPage_AnswerCard_{Answer}";
        ImageAutomationId = $"NewRequestDetailsPage_AnswerCardImage_{Answer}";
    }

    /// <summary>The answer this card stands for, which is what choosing it selects.</summary>
    public string Answer { get; }

    /// <summary>The card's label: the answer's own words.</summary>
    public string Title => Answer;

    /// <summary>The picture the card draws: the one shared no-image picture, never a blank space.</summary>
    public string ImagePath => MTM_Waitlist.Module_Shared.Helpers.ImagePicturePolicy.NoImagePath;

    /// <summary>Whether this is the answer currently chosen, which draws the card's selection outline.</summary>
    public bool IsSelected { get; }

    /// <summary>The card's automation id.</summary>
    public string AutomationId { get; }

    /// <summary>The card's picture box automation id.</summary>
    public string ImageAutomationId { get; }
}
