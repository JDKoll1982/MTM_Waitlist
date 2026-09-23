using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Shared.Helpers;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;

namespace MTM_Waitlist.Module_Waitlist.ViewModels;

/// <summary>
/// Die step of the New Request wizard (FR-054): it asks <b>which</b> dies the request is for, showing the dies the
/// requesting job carries as selectable cards — each written as the die's number and where the die lives — and
/// lets the operator take one or several of them, or all of them in a single action.
/// </summary>
/// <remarks>
/// <para>
/// The step is reached because the chosen Item's <b>stored configuration</b> declares an enumerated answer that
/// names the job's <c>die</c> list — read through
/// <see cref="RequestItemAnswerOptionsResolver.DeclaredJobListName"/> — and never because of the Item's identity
/// (FR-013).
/// </para>
/// <para>
/// A tap <b>marks</b> a card rather than leaving the step, unlike the dunnage step where one part is one answer.
/// Several dies is several requests, so the operator says which ones they need and then commits; the step records
/// every die they chose, and the confirmation step raises one request per die (FR-054).
/// </para>
/// <para>
/// Nothing is asked about <b>where</b> a die goes. A die always goes to the home location the job records on its
/// own row, which is why the cards can show that location in the first place (D22).
/// </para>
/// </remarks>
public partial class NewRequestDieViewModel : ObservableRecipient, INavigationAware
{
    private readonly INavigationService _navigationService;

    /// <summary>
    /// The reader this step resolves each die's picture through, before the card reaches the bound collection. Null
    /// on a headless host, which leaves every card on the one shared placeholder rather than drawing a blank space.
    /// </summary>
    private readonly IPartPictureResolver? _partPictureResolver;

    private NewRequestFlowState? _state;

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
    } = LocalizeOrDefault("NewRequest_Die.Prompt", "Which die is this request for? Choose one or more.");

    /// <summary>True when the step has something to report — a job with no die, or a continue that was refused.</summary>
    [ObservableProperty]
    public partial bool IsUnavailableVisible
    {
        get; set;
    }

    /// <summary>The plain-language report, resolved through the resource mechanism — never a bare resource key.</summary>
    [ObservableProperty]
    public partial string UnavailableMessage
    {
        get; set;
    } = string.Empty;

    /// <summary>The dies the requesting job carries, in the job's own order, marked where already chosen.</summary>
    public ObservableCollection<NewRequestDieOption> Options { get; } = new();

    public NewRequestDieViewModel(
        INavigationService navigationService,
        IPartPictureResolver? partPictureResolver = null)
    {
        _navigationService = navigationService;
        _partPictureResolver = partPictureResolver;
    }

    public async void OnNavigatedTo(object parameter)
    {
        if (parameter is not NewRequestFlowState state || state.Item is null)
        {
            _navigationService.GoBack();
            return;
        }

        _state = state;
        WorkCenterText = $"Work Center: {state.WorkCenter}";
        UnavailableMessage = string.Empty;
        IsUnavailableVisible = false;
        await LoadCardsAsync(state).ConfigureAwait(true);
    }

    public void OnNavigatedFrom()
    {
    }

    /// <summary>
    /// Binds the dies the job carries as cards, marking the ones the operator already chose so coming back to the
    /// step never hides what they picked. A job carrying no die leaves the step empty and says why, rather than
    /// offering a card that stands for nothing (FR-055, FR-026).
    /// </summary>
    /// <remarks>
    /// Each die's picture is resolved here, where the resolver is in hand, and carried on the card: the card draws
    /// a value it was handed rather than asking for one while it renders (FR-018).
    /// </remarks>
    private async Task LoadCardsAsync(NewRequestFlowState state)
    {
        Options.Clear();

        foreach (var die in state.Availability?.Dies ?? Array.Empty<RequestDiePart>())
        {
            Options.Add(new NewRequestDieOption
            {
                Die = die,
                Title = die.Label,
                Summary = die.Summary,
                ImagePath = await ResolvePartPictureAsync(die.PartNumber).ConfigureAwait(true),
                IsSelected = WasChosen(die, state),
            });
        }

        if (Options.Count == 0)
        {
            IsUnavailableVisible = true;
            UnavailableMessage = LocalizeOrDefault(
                "NewRequest_Die.NoDies.Message",
                "This job has no die saved on it, so there is no die to ask about. Go back and choose a different item.");
        }

        StartupDebugLog.Info(
            "NewRequestDie",
            $"Die step for work center '{state.WorkCenter}' bound {Options.Count} card(s) for item '{state.Item?.Id}'.");
    }

    /// <summary>The part's picture, or the one shared placeholder when the part cannot be pictured (FR-014, FR-023).</summary>
    private async Task<string> ResolvePartPictureAsync(string? partNumber)
    {
        if (_partPictureResolver is null || string.IsNullOrWhiteSpace(partNumber))
        {
            return ImagePicturePolicy.NoImagePath;
        }

        try
        {
            var resolved = await _partPictureResolver
                .ResolvePartPicturePathAsync(PartPictureLayout.VisualPartScope, partNumber.Trim())
                .ConfigureAwait(true);

            return string.IsNullOrWhiteSpace(resolved) ? ImagePicturePolicy.NoImagePath : resolved;
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error(
                "NewRequestDie",
                ex,
                $"Resolving the picture for die '{partNumber}' failed; the card will draw the no-image placeholder.");
            return ImagePicturePolicy.NoImagePath;
        }
    }

    /// <summary>
    /// Marks or un-marks the card the operator tapped. Choosing a die is never the way out of the step, because
    /// they are choosing which dies they need and there may be more than one.
    /// </summary>
    [RelayCommand]
    private void SelectDie(NewRequestDieOption? option)
    {
        if (option is null)
        {
            return;
        }

        option.IsSelected = !option.IsSelected;
        ClearRefusal();
    }

    /// <summary>Marks every die the job carries, so a request for the whole job is one action rather than several.</summary>
    [RelayCommand]
    private void SelectAll()
    {
        foreach (var option in Options)
        {
            option.IsSelected = true;
        }

        ClearRefusal();
    }

    /// <summary>
    /// Records every die the operator chose and moves on. Nothing chosen is refused rather than advancing, because
    /// a request is never raised for no die at all (FR-026).
    /// </summary>
    [RelayCommand]
    private void Continue()
    {
        if (_state is null)
        {
            return;
        }

        var chosen = Options.Where(option => option.IsSelected && option.Die is not null)
            .Select(option => option.Die!)
            .ToArray();

        if (chosen.Length == 0)
        {
            IsUnavailableVisible = true;
            UnavailableMessage = LocalizeOrDefault(
                "NewRequest_Die.NothingChosen.Message",
                "Choose at least one die before continuing.");
            return;
        }

        var state = _state;

        // The request's one value column carries which die the request is for, which is what keeps two entries
        // raised from one job apart; the whole list is kept so the confirmation step can raise one request each
        // (FR-054, D22).
        state.SelectedDies = chosen;
        state.InputValue = chosen[0].Label;

        StartupDebugLog.Info(
            "NewRequestDie",
            $"Die step for work center '{state.WorkCenter}' captured {chosen.Length} die(s): {string.Join(", ", chosen.Select(die => die.Label))}.");
        _navigationService.NavigateTo(NewRequestFlowRules.GetNextStepType(state).FullName!, state);
    }

    [RelayCommand]
    private void Back() => _navigationService.GoBack();

    private void ClearRefusal()
    {
        IsUnavailableVisible = false;
        UnavailableMessage = string.Empty;
    }

    /// <summary>
    /// Whether the operator already chose this die: either it is in the list the step recorded, or — for a state
    /// that came back carrying only the request's stored value — the stored value is this die's identifier.
    /// </summary>
    private static bool WasChosen(RequestDiePart die, NewRequestFlowState state) =>
        (state.SelectedDies ?? Array.Empty<RequestDiePart>())
            .Any(chosen => string.Equals(chosen.Label, die.Label, StringComparison.OrdinalIgnoreCase))
        || string.Equals(state.InputValue?.Trim(), die.Label, StringComparison.OrdinalIgnoreCase);

    private static string LocalizeOrDefault(string key, string fallback)
    {
        var localized = key.GetLocalized();
        return string.IsNullOrWhiteSpace(localized) || string.Equals(localized, key, StringComparison.Ordinal)
            ? fallback
            : localized;
    }
}
