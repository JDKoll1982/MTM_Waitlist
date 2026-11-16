using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.UI.Xaml;

using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Models;
using MTM_Waitlist.Module_Core.Helpers;

namespace MTM_Waitlist.GenericRequest.ViewModels;

/// <summary>
/// The die step: a search of Infor Visual, then the die or dies the request needs.
/// </summary>
/// <remarks>
/// <para>
/// A generic request has no job to read a die from, so this step is the search itself (020 FR-001). It asks the seam
/// one question and the answer says which of the three presentations to draw, so a term that matched nothing is told
/// apart from a source that could not be asked without the screen guessing at either.
/// </para>
/// <para>
/// A tap marks a die rather than leaving the step: a request may need several, and each one becomes its own request
/// (FR-010). A die already marked stays marked when a later search returns it, so narrowing a search never quietly
/// drops a choice.
/// </para>
/// </remarks>
public sealed partial class GenericRequestDieViewModel : GenericRequestStepViewModel
{
    private readonly IGenericRequestStepData _data;

    /// <summary>Every die the requester has marked, in the order they marked them.</summary>
    private readonly List<GenericRequestDieOption> _chosen = new();

    /// <summary>
    /// The die the running job carries, waiting to be proved by the search. It is a candidate, not a choice, so a die
    /// the search does not return leaves nothing chosen (FR-010).
    /// </summary>
    private string _pendingJobDie = string.Empty;

    /// <summary>
    /// Which search is the current one. Typing fires a read per keystroke, and a read that lands after a later one
    /// describes a term the person has already typed past, so its answer is dropped rather than drawn.
    /// </summary>
    private int _searchGeneration;

    public GenericRequestDieViewModel(IGenericRequestStepData data, IGenericRequestNavigator navigator)
        : base(navigator)
    {
        ArgumentNullException.ThrowIfNull(data);

        _data = data;
        SearchCommand = new AsyncRelayCommand(LoadAsync);

        // Nothing has been asked yet, so nothing may be claimed. A step starts in the loading state, and the frame
        // hides a step's body behind any settled presentation that is not populated — which is what left this
        // screen showing "No die in Infor Visual matches that search" before anybody had searched, with the search
        // box hidden along with the body and no way to ask anything at all. Starting settled and populated keeps
        // the box on screen, and the list simply holds nothing until the first search.
        Presentation = GenericRequestPresentation.Populated;
    }

    public override GenericRequestStep Step => GenericRequestStep.Die;

    public override string TitleKey => "GenericRequest_Die_Title.Text";

    public override string IntroKey => "GenericRequest_Die_Intro.Text";

    public override string EmptyKey => "GenericRequest_Die_Empty.Text";

    /// <summary>Run the search again with the text as typed.</summary>
    public IAsyncRelayCommand SearchCommand { get; }

    /// <summary>
    /// Mark or unmark one die, which is what clicking its card does. A request may need several dies, so the card is
    /// a mark rather than the step's single selection (FR-010).
    /// </summary>
    [RelayCommand]
    private void ToggleDie(GenericRequestDieOption? option)
    {
        if (option is null)
        {
            return;
        }

        // Flipping the flag raises the option's own change notification, which is what records the choice; the same
        // path a checkbox used to take, kept so the choice is recorded in exactly one place.
        option.IsSelected = !option.IsSelected;
    }

    /// <summary>The dies this search matched, each one a choice.</summary>
    public ObservableCollection<GenericRequestDieOption> Options { get; } = new();

    /// <summary>What the requester typed, which a failed read keeps so the retry asks the same question.</summary>
    /// <remarks>
    /// The search runs as this changes, which is how the other search fields in this application answer: a person
    /// typing a die or part number sees the dies appear as they narrow it, instead of having to type the whole
    /// number and press a button.
    /// </remarks>
    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    /// <summary>How many dies this search matched, drawn above the list so a partial list cannot pass for a full one.</summary>
    [ObservableProperty]
    public partial int MatchCount { get; set; }

    /// <summary>Whether a search has been run at all, which is what makes a "nothing matched" line meaningful.</summary>
    [ObservableProperty]
    public partial bool HasSearched { get; set; }

    /// <summary>Whether the search that just ran matched no die, drawn in the step rather than in place of it.</summary>
    [ObservableProperty]
    public partial bool HasNoMatch { get; set; }

    /// <summary>
    /// What the step says when a search matched nothing, drawn beside the box that can ask again rather than in
    /// place of it: the frame's own empty state takes the body away, and the body is where the box is.
    /// </summary>
    public string NoMatchText => "GenericRequest_Die_Empty.Text".GetLocalized();

    /// <summary>What the box asks for, so the field says what to type before anything is typed in it.</summary>
    public string SearchPlaceholderText => "GenericRequest_Die_SearchPlaceholder.Text".GetLocalized();

    /// <summary>A search having run is what makes the hint worth drawing, so both follow it.</summary>
    partial void OnHasSearchedChanged(bool value)
    {
        OnPropertyChanged(nameof(HintText));
        OnPropertyChanged(nameof(HasHint));
    }

    /// <summary>A miss takes the hint away with it, because there is nothing left to choose from.</summary>
    partial void OnHasNoMatchChanged(bool value)
    {
        OnPropertyChanged(nameof(HintText));
        OnPropertyChanged(nameof(HasHint));
    }

    /// <summary>The match count as the step draws it, or empty when there is nothing to count.</summary>
    public string CountText => MatchCount switch
    {
        0 => string.Empty,
        1 => "GenericRequest_Die_Count_One.Text".GetLocalized(),
        _ => string.Format(
            CultureInfo.CurrentCulture,
            "GenericRequest_Die_Count.Text".GetLocalized(),
            MatchCount),
    };

    /// <summary>Whether there is a count to draw at all.</summary>
    public bool HasCount => MatchCount > 0;

    /// <summary>
    /// The line the step shows while nothing is marked, so a Continue that cannot be pressed has its reason beside
    /// it rather than looking broken.
    /// </summary>
    /// <remarks>
    /// Only when there is something to choose from: before the first search there is no list, and after a search
    /// that matched nothing there is still no list — telling somebody to choose a die from an empty list is the kind
    /// of instruction that reads as a broken screen.
    /// </remarks>
    public string HintText => HasSearched && IsPopulated && !HasNoMatch && Options.Count > 0 && _chosen.Count == 0
        ? "GenericRequest_Die_NothingChosen.Text".GetLocalized()
        : string.Empty;

    /// <summary>Whether the step has a hint to draw.</summary>
    public bool HasHint => HintText.Length > 0;

    protected override bool CanAnswer => _chosen.Count > 0;

    /// <summary>
    /// The qualifier that says these dies came from the running job, or empty when they did not or the requester has
    /// changed the choice since (FR-006, FR-007).
    /// </summary>
    public string FromJobText => Draft.JobFill is { IsDieFromJob: true } fill && IsChosen(fill.DieNumber)
        ? string.Format(
            CultureInfo.CurrentCulture,
            "GenericRequest_JobFill_FromJob.Text".GetLocalized(),
            fill.WorkOrder)
        : string.Empty;

    /// <summary>Whether the qualifier is drawn at all.</summary>
    public bool HasFromJob => FromJobText.Length > 0;

    public Visibility FromJobVisibility => HasFromJob ? Visibility.Visible : Visibility.Collapsed;

    protected override async Task LoadCoreAsync()
    {
        SeedChosenDies();

        var term = SearchText.Trim();
        var generation = ++_searchGeneration;

        if (term.Length == 0)
        {
            // A blank box is not a question, so nothing is asked and nothing is claimed: the step stays settled
            // and populated with no dies and no line about them.
            ClearMatches();
            HasSearched = false;
            HasNoMatch = false;
            Presentation = GenericRequestPresentation.Populated;
            AttemptedAtUtc = null;
            return;
        }

        var result = await _data.ReadDiesAsync(term);

        // A read that lands after a later one describes a term the person has already typed past. Applying it would
        // put another term's dies on screen, so it is dropped.
        if (generation != _searchGeneration)
        {
            return;
        }

        foreach (var previous in Options)
        {
            previous.PropertyChanged -= OnOptionChanged;
        }

        Options.Clear();

        foreach (var option in result.Items)
        {
            option.IsSelected = IsChosen(option.DieNumber);
            option.PropertyChanged += OnOptionChanged;
            Options.Add(option);
        }

        AdoptFreshRows();
        AdoptPendingJobDie();

        MatchCount = result.Count;
        OnPropertyChanged(nameof(CountText));
        OnPropertyChanged(nameof(HasCount));

        HasSearched = true;

        // A miss is drawn inside the step, beside the box that can ask again, and the step stays populated so that
        // box is still on screen. Only a read that failed takes the body away, and then with the attempt time and a
        // retry (FR-005).
        var failed = result.Presentation == GenericRequestPresentation.Unavailable;

        HasNoMatch = result.Presentation == GenericRequestPresentation.Empty;
        Presentation = failed ? GenericRequestPresentation.Unavailable : GenericRequestPresentation.Populated;
        AttemptedAtUtc = failed ? result.AttemptedAtUtc : null;

        NotifyChoiceChanged();
    }
    /// <summary>
    /// Start this step with the dies the draft already holds, and put the running job's die in front of the requester
    /// as a candidate the search can prove.
    /// </summary>
    /// <remarks>
    /// A marked die survives leaving and returning to the step, because the draft is what remembers the choice. The
    /// job's die is only a search term here: it is marked once the search returns it, so a die Infor Visual no longer
    /// records leaves nothing chosen and the step cannot advance (FR-010, 020 FR-002).
    /// </remarks>
    private void SeedChosenDies()
    {
        if (_chosen.Count == 0)
        {
            _chosen.AddRange(Draft.SelectedDies);

            if (_chosen.Count == 0
                && Draft.JobFill is { IsDieFromJob: true } fill
                && fill.DieNumber.Length > 0)
            {
                _pendingJobDie = fill.DieNumber;
            }
        }

        if (SearchText.Trim().Length == 0)
        {
            // The search is what proves a die exists, so the job's die is searched for exactly as a typed one is.
            SearchText = Draft.DieNumber.Length > 0
                ? Draft.DieNumber
                : _pendingJobDie.Length > 0
                    ? _pendingJobDie
                    : _chosen.Count > 0 ? _chosen[0].DieNumber : string.Empty;
        }

        OnPropertyChanged(nameof(FromJobText));
        OnPropertyChanged(nameof(HasFromJob));
        OnPropertyChanged(nameof(FromJobVisibility));
    }

    /// <summary>
    /// Mark the job's die if the search that just ran returned it, and forget it either way.
    /// </summary>
    /// <remarks>
    /// A die the search did not return is not offered, so the requester is asked to choose one rather than raising a
    /// request for a die the plant's system does not record.
    /// </remarks>
    private void AdoptPendingJobDie()
    {
        if (_pendingJobDie.Length == 0)
        {
            return;
        }

        var match = Options.FirstOrDefault(option => SameDie(option.DieNumber, _pendingJobDie));
        _pendingJobDie = string.Empty;

        if (match is not null)
        {
            match.IsSelected = true;
        }
    }

    /// <summary>Drop everything the previous search matched, without asking anything.</summary>
    private void ClearMatches()
    {
        foreach (var previous in Options)
        {
            previous.PropertyChanged -= OnOptionChanged;
        }

        Options.Clear();
        MatchCount = 0;
        OnPropertyChanged(nameof(CountText));
        OnPropertyChanged(nameof(HasCount));
    }

    protected override void CommitToDraft()
    {
        Draft.SelectedDies.Clear();
        Draft.SelectedDies.AddRange(_chosen);
        Draft.DieNumber = _chosen.Count == 0 ? string.Empty : _chosen[0].DieNumber;
    }

    /// <summary>
    /// Replace a marked die with the row this search returned for it, so the request records the die's current
    /// description and location rather than a copy from an earlier search.
    /// </summary>
    private void AdoptFreshRows()
    {
        for (var index = 0; index < _chosen.Count; index++)
        {
            var fresh = Options.FirstOrDefault(option => SameDie(option.DieNumber, _chosen[index].DieNumber));
            if (fresh is not null)
            {
                _chosen[index] = fresh;
            }
        }
    }

    private bool IsChosen(string dieNumber) => _chosen.Any(chosen => SameDie(chosen.DieNumber, dieNumber));

    private void OnOptionChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (sender is not GenericRequestDieOption option
            || !string.Equals(args.PropertyName, nameof(GenericRequestDieOption.IsSelected), StringComparison.Ordinal))
        {
            return;
        }

        _chosen.RemoveAll(chosen => SameDie(chosen.DieNumber, option.DieNumber));

        if (option.IsSelected)
        {
            _chosen.Add(option);
        }

        // A set of dies that no longer includes the job's is the requester's own choice, so the step stops saying the
        // job supplied it (FR-007).
        if (Draft.JobFill is { IsDieFromJob: true } fill
            && !_chosen.Any(chosen => SameDie(chosen.DieNumber, fill.DieNumber)))
        {
            Draft.JobFill.ClearDie();
            OnPropertyChanged(nameof(FromJobText));
            OnPropertyChanged(nameof(HasFromJob));
            OnPropertyChanged(nameof(FromJobVisibility));
        }

        NotifyChoiceChanged();
    }

    private void NotifyChoiceChanged()
    {
        OnPropertyChanged(nameof(HintText));
        OnPropertyChanged(nameof(HasHint));
        OnPropertyChanged(nameof(FromJobText));
        OnPropertyChanged(nameof(HasFromJob));
        OnPropertyChanged(nameof(FromJobVisibility));
        NotifyAnswerChanged();
    }

    private static bool SameDie(string first, string second) =>
        string.Equals(first, second, StringComparison.OrdinalIgnoreCase);
}
