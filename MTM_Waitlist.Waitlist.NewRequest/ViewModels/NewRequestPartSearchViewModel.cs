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
/// Fuzzy incorrect-part step of the New Request wizard (FR-082 to FR-085): the incorrect part on the two
/// wrong-part Items is typed through a live search rather than as free text.
/// </summary>
/// <remarks>
/// <para>
/// The step is reached because the chosen Item's <b>stored configuration</b> declares an answer labelled
/// <c>Coil number</c> or <c>Part</c> — read through <see cref="NewRequestFlowRules.PartSearchAnswerLabel"/> — and
/// never because of the Item's identity (FR-013).
/// </para>
/// <para>
/// The term is put through <see cref="PartMatchSearchInput"/> first: upper-cased, and zero-padded to ten
/// characters when it starts with a known prefix. An exact match resolves at once; anything else offers every part
/// whose number contains the term, and the operator picks one (FR-082, FR-083).
/// </para>
/// <para>
/// <b>Degrading is the designed behaviour.</b> The read is live only and is never cached, so when the plant system
/// cannot be reached the operator is told the search is unavailable and the value they typed is used as entered
/// (FR-084, FR-085). A read that fails for another reason is reported as itself, and still lets the operator
/// proceed — a correction the plant cannot answer for right now must not become a request that cannot be raised.
/// </para>
/// <para>
/// The answer is recorded under the label the Item declares and never in the request's single value: the
/// wrong-part Items already answer their reason there, and the incorrect part is a further answer the card reads
/// from the answers map (<c>data-model.md</c> §10).
/// </para>
/// </remarks>
public partial class NewRequestPartSearchViewModel : ObservableRecipient, INavigationAware
{
    private readonly INavigationService _navigationService;

    /// <summary>
    /// The search this step asks. Null on a host without one, which reports the search as unavailable and still
    /// lets the operator proceed with the number they typed (FR-085).
    /// </summary>
    private readonly IPartMatchSearchService? _searchService;

    private NewRequestFlowState? _state;

    /// <summary>The label the Item declares its incorrect part under, resolved as the step loads.</summary>
    private string _answerLabel = string.Empty;

    /// <summary>Whether the last search could not answer, which is what lets the typed value stand in (FR-085).</summary>
    private bool _searchCouldNotAnswer;

    [ObservableProperty]
    public partial string WorkCenterText
    {
        get; set;
    } = string.Empty;

    /// <summary>The question the step asks, resolved through the resource mechanism (FR-022).</summary>
    [ObservableProperty]
    public partial string PromptText
    {
        get; set;
    } = LocalizeOrDefault(
        "NewRequest_PartSearch.Prompt",
        "Type the part number you need. We will find every part that matches it.");

    /// <summary>What the operator typed.</summary>
    [ObservableProperty]
    public partial string TypedPart
    {
        get; set;
    } = string.Empty;

    /// <summary>The part the step will record: an exact match, a part chosen from the list, or the typed value.</summary>
    [ObservableProperty]
    public partial string? SelectedPart
    {
        get; set;
    }

    /// <summary>True while the live read is running, so the step can say it is still looking.</summary>
    [ObservableProperty]
    public partial bool IsSearching
    {
        get; set;
    }

    /// <summary>True when the step has something to report — an unavailable search, or a refused continue.</summary>
    [ObservableProperty]
    public partial bool IsStatusVisible
    {
        get; set;
    }

    /// <summary>The plain-language report, resolved through the resource mechanism — never a bare resource key.</summary>
    [ObservableProperty]
    public partial string StatusMessage
    {
        get; set;
    } = string.Empty;

    /// <summary>True when a list of parts is offered, so the step draws the list rather than an empty space.</summary>
    [ObservableProperty]
    public partial bool HasMatches
    {
        get; set;
    }

    /// <summary>The parts whose numbers contain the term, in the read's own order (FR-083).</summary>
    public ObservableCollection<PartMatchChoice> Matches { get; } = new();

    public NewRequestPartSearchViewModel(
        INavigationService navigationService,
        IPartMatchSearchService? searchService = null)
    {
        _navigationService = navigationService;
        _searchService = searchService;
    }

    public void OnNavigatedTo(object parameter)
    {
        if (parameter is not NewRequestFlowState state || state.Item is null)
        {
            _navigationService.GoBack();
            return;
        }

        _state = state;
        WorkCenterText = $"Work Center: {state.WorkCenter}";
        IsStatusVisible = false;
        StatusMessage = string.Empty;
        _searchCouldNotAnswer = false;
        ClearMatches();

        _answerLabel = NewRequestFlowRules.PartSearchAnswerLabel(state.ItemConfiguration) ?? string.Empty;
        if (_answerLabel.Length == 0)
        {
            // A row that declares no incorrect-part field has nothing for this step to answer into, so the step
            // says so rather than capturing a value no cell reads (FR-013).
            Report(
                "NewRequest_PartSearch_NotConfigured.Message",
                "This item's incorrect-part question is not configured yet. A supervisor needs to check it.");
            return;
        }

        // Coming back to the step shows the answer already captured rather than an empty box (FR-026).
        var captured = state.AnswerFor(_answerLabel) ?? string.Empty;
        TypedPart = captured;
        SelectedPart = captured.Length == 0 ? null : captured;
    }

    public void OnNavigatedFrom()
    {
    }

    /// <summary>
    /// Searches the plant system for the term the operator typed and offers what came back: the exact match as the
    /// answer, or every part whose number contains the term (FR-083).
    /// </summary>
    [RelayCommand]
    private async Task SearchAsync()
    {
        if (_state is null)
        {
            return;
        }

        var typed = TypedPart?.Trim() ?? string.Empty;
        if (typed.Length == 0)
        {
            Report("NewRequest_PartSearch_NothingTyped.Message", "Type a part number to search for.");
            return;
        }

        if (_searchService is null)
        {
            _searchCouldNotAnswer = true;
            Report(
                "NewRequest_PartSearch_Unavailable.Message",
                "The part search is not available right now. The number you typed will be used as entered.");
            return;
        }

        IsSearching = true;
        IsStatusVisible = false;
        StatusMessage = string.Empty;
        _searchCouldNotAnswer = false;
        ClearMatches();

        try
        {
            var result = await _searchService.FindMatchesAsync(typed).ConfigureAwait(true);
            Apply(result);
        }
        finally
        {
            IsSearching = false;
        }
    }

    /// <summary>Chooses one of the parts the search offered (FR-083).</summary>
    /// <param name="choice">The part whose row was clicked.</param>
    [RelayCommand]
    private void SelectMatch(PartMatchChoice? choice)
    {
        if (choice is null || string.IsNullOrWhiteSpace(choice.PartNumber))
        {
            return;
        }

        SelectedPart = choice.PartNumber.Trim();

        foreach (var candidate in Matches)
        {
            candidate.IsSelected = ReferenceEquals(candidate, choice);
        }

        ClearStatus();
    }

    /// <summary>
    /// Records the part and moves on. What is recorded is the exact match, the part the operator chose, or — when
    /// the search could not answer — the value they typed, through the search's own input rule (FR-085).
    /// </summary>
    [RelayCommand]
    private void Continue()
    {
        if (_state is null)
        {
            return;
        }

        if (_answerLabel.Length == 0)
        {
            Report(
                "NewRequest_PartSearch_NotConfigured.Message",
                "This item's incorrect-part question is not configured yet. A supervisor needs to check it.");
            return;
        }

        var chosen = SelectedPart?.Trim() ?? string.Empty;
        if (chosen.Length == 0 && _searchCouldNotAnswer)
        {
            chosen = PartMatchSearchInput.Normalize(TypedPart);
        }

        if (chosen.Length == 0)
        {
            Report(
                "NewRequest_PartSearch_NothingChosen.Message",
                Matches.Count > 0
                    ? "Choose one of the parts listed, or search for the number you need."
                    : "Search for the part number before continuing.");
            return;
        }

        var state = _state;
        state.RecordAnswer(_answerLabel, chosen);
        ClearStatus();

        AppLog.Info(
            "NewRequestPartSearch",
            $"Incorrect part '{chosen}' captured under '{_answerLabel}' for work center '{state.WorkCenter}'.");

        _navigationService.NavigateTo(NewRequestFlowRules.GetNextStepType(state).FullName!, state);
    }

    [RelayCommand]
    private void Back() => _navigationService.GoBack();

    /// <summary>
    /// Turns the search's answer into what the step shows: the exact match as the answer, the containing parts as
    /// a list, or the plain-language report of a search that could not answer (FR-083, FR-085).
    /// </summary>
    private void Apply(PartMatchSearchResult result)
    {
        switch (result.Status)
        {
            case PartMatchSearchStatus.Resolved:
                SelectedPart = result.ExactMatch;
                ClearStatus();
                break;

            case PartMatchSearchStatus.Contains:
                FillMatches(result.Matches);
                SelectedPart = null;
                ClearStatus();
                break;

            case PartMatchSearchStatus.Unavailable:
                _searchCouldNotAnswer = true;
                Report(
                    "NewRequest_PartSearch_Unavailable.Message",
                    "The part search is not available right now. The number you typed will be used as entered.");
                break;

            case PartMatchSearchStatus.Failed:
                _searchCouldNotAnswer = true;
                Report(
                    "NewRequest_PartSearch_Failed.Message",
                    "The part search could not be completed. The number you typed will be used as entered.");
                break;

            default:
                Report("NewRequest_PartSearch_NothingTyped.Message", "Type a part number to search for.");
                break;
        }
    }

    private void FillMatches(IReadOnlyList<string> partNumbers)
    {
        Matches.Clear();

        foreach (var partNumber in partNumbers)
        {
            Matches.Add(new PartMatchChoice { PartNumber = partNumber });
        }

        HasMatches = Matches.Count > 0;
    }

    private void ClearMatches()
    {
        Matches.Clear();
        HasMatches = false;
    }

    partial void OnTypedPartChanged(string value)
    {
        // A new term makes the previous answer stale: the list and the chosen part describe the old search.
        ClearMatches();
        SelectedPart = null;
        _searchCouldNotAnswer = false;
        ClearStatus();
    }

    private void Report(string key, string fallback)
    {
        StatusMessage = LocalizeOrDefault(key, fallback);
        IsStatusVisible = true;
    }

    private void ClearStatus()
    {
        IsStatusVisible = false;
        StatusMessage = string.Empty;
    }

    private static string LocalizeOrDefault(string key, string fallback)
    {
        var localized = key.GetLocalized();
        return string.IsNullOrWhiteSpace(localized) || string.Equals(localized, key, StringComparison.Ordinal)
            ? fallback
            : localized;
    }
}
