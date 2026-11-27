using System.Collections.ObjectModel;
using System.Globalization;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.UI.Xaml;

using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Models;
using MTM_Waitlist.Module_Core.Helpers;

namespace MTM_Waitlist.GenericRequest.ViewModels;

/// <summary>
/// The part step: state the part, choose the sequence it is standing at, and see how much is on hand.
/// </summary>
/// <remarks>
/// <para>
/// The screen carries three independent reads, and the frame's state belongs to the first of them. The sequence list
/// and the quantity region each report their own failure with their own attempt time and retry, so a failed
/// quantity read leaves the list the requester is looking at visible (plan OD-1).
/// </para>
/// <para>
/// Nothing is asked that the app can work out. The requester never says whether the part is work in progress or a
/// finished good, and never types a quantity: the chosen sequence's operation state decides who owns the figure
/// (FR-009, FR-012).
/// </para>
/// </remarks>
public sealed partial class GenericRequestPartViewModel : GenericRequestStepViewModel
{
    private readonly IGenericRequestPartData _data;

    /// <summary>
    /// Which completion read is the newest. A read that lands after a later one must not draw: the requester is
    /// typing, and only the term currently on screen describes what they want to see. A read started by a typing pause
    /// takes the pause's own number, so an answer is judged by the attempt it belongs to and never by the order the
    /// answers happen to come back in (PERF-001).
    /// </summary>
    private long _searchGeneration;

    public GenericRequestPartViewModel(IGenericRequestPartData data, IGenericRequestNavigator navigator)
        : base(navigator)
    {
        ArgumentNullException.ThrowIfNull(data);

        _data = data;
        RetrySequencesCommand = new AsyncRelayCommand(LoadSequencesAsync);
        RetryQuantityCommand = new AsyncRelayCommand(RetryQuantityAsync);
    }

    public override GenericRequestStep Step => GenericRequestStep.Part;

    public override string TitleKey => "GenericRequest_Part_Title.Text";

    public override string IntroKey => "GenericRequest_Part_Intro.Text";

    public override string EmptyKey => "GenericRequest_Part_Empty.Text";

    /// <summary>The completions the typed term produced, each with the system that supplied it.</summary>
    public ObservableCollection<GenericRequestPartOption> Completions { get; } = new();

    /// <summary>Every sequence attributable to the accepted part, each number once.</summary>
    public ObservableCollection<GenericRequestSequenceOption> Sequences { get; } = new();

    /// <summary>Re-read the sequence list, which only the list's own failure shows.</summary>
    public IAsyncRelayCommand RetrySequencesCommand { get; }

    /// <summary>Re-read the quantity, which only the quantity region's own failure shows.</summary>
    public IAsyncRelayCommand RetryQuantityCommand { get; }

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    /// <summary>The part the requester picked. A number they typed but did not pick is never accepted (FR-002).</summary>
    [ObservableProperty]
    public partial GenericRequestPartOption? AcceptedPart { get; set; }

    [ObservableProperty]
    public partial GenericRequestSequenceOption? SelectedSequence { get; set; }

    /// <summary>Which presentation the sequence list draws, independent of the part read (plan OD-1).</summary>
    [ObservableProperty]
    public partial GenericRequestPresentation SequencesPresentation { get; set; } = GenericRequestPresentation.Empty;

    [ObservableProperty]
    public partial DateTimeOffset? SequencesAttemptedAtUtc { get; set; }

    /// <summary>Which presentation the quantity region draws, independent of the other two reads (plan OD-1).</summary>
    [ObservableProperty]
    public partial GenericRequestPresentation QuantityPresentation { get; set; } = GenericRequestPresentation.Empty;

    [ObservableProperty]
    public partial DateTimeOffset? QuantityAttemptedAtUtc { get; set; }

    /// <summary>The reading the chosen sequence produced, or null before one is chosen.</summary>
    [ObservableProperty]
    public partial GenericRequestQuantityReading? QuantityReading { get; set; }

    /// <summary>The figure as a person reads it, or empty when there is no figure to read (FR-013).</summary>
    [ObservableProperty]
    public partial string QuantityFigure { get; set; } = string.Empty;

    /// <summary>The system that answered, or empty when nothing did.</summary>
    [ObservableProperty]
    public partial string QuantityAnsweredBy { get; set; } = string.Empty;

    /// <summary>The stage the chosen sequence's operation state gave, derived and never asked (FR-012).</summary>
    [ObservableProperty]
    public partial string StageLabel { get; set; } = string.Empty;

    /// <summary>True when the typed number is one neither system knows (FR-002).</summary>
    [ObservableProperty]
    public partial bool HasNoMatch { get; set; }

    /// <summary>True when the part is known and no sequence can be attributed to it (FR-003).</summary>
    [ObservableProperty]
    public partial bool HasNoSequence { get; set; }

    /// <summary>True when the owning system answered with nothing on hand, which warns without blocking (FR-015).</summary>
    [ObservableProperty]
    public partial bool HasNothingOnHand { get; set; }

    /// <summary>
    /// The resource the part goes to after the chosen operation, which is the line a WIP tag prints. Empty for the
    /// last operation, which has nowhere further to go.
    /// </summary>
    [ObservableProperty]
    public partial string NextResourceDisplay { get; set; } = string.Empty;

    /// <summary>True when the chosen operation hands the part on to another resource.</summary>
    public bool HasNextResource => NextResourceDisplay.Length > 0;

    /// <summary>
    /// The qualifier that says this part came from the running job, or empty when it did not or the requester has
    /// changed it since (FR-006, FR-007).
    /// </summary>
    public string FromJobText => Draft.JobFill is { IsPartFromJob: true } fill
        ? string.Format(
            CultureInfo.CurrentCulture,
            "GenericRequest_JobFill_FromJob.Text".GetLocalized(),
            fill.WorkOrder)
        : string.Empty;

    /// <summary>Whether the qualifier is drawn at all.</summary>
    public bool HasFromJob => FromJobText.Length > 0;

    public Visibility FromJobVisibility => HasFromJob ? Visibility.Visible : Visibility.Collapsed;

    // The three reads draw independently, so each region's presence is its own projection rather than one state
    // standing for the whole body (plan OD-1).
    public Visibility NoMatchVisibility => HasNoMatch ? Visibility.Visible : Visibility.Collapsed;

    public Visibility NoSequenceVisibility => HasNoSequence ? Visibility.Visible : Visibility.Collapsed;

    public Visibility SequenceListVisibility => HasSequences ? Visibility.Visible : Visibility.Collapsed;

    public Visibility SequencesUnavailableVisibility => IsSequencesUnavailable ? Visibility.Visible : Visibility.Collapsed;

    public Visibility QuantityAnsweredVisibility => IsQuantityAnswered && !IsCoilOrFlatstock ? Visibility.Visible : Visibility.Collapsed;

    public Visibility NextResourceVisibility => HasNextResource ? Visibility.Visible : Visibility.Collapsed;

    public Visibility NothingOnHandVisibility => HasNothingOnHand && !IsCoilOrFlatstock ? Visibility.Visible : Visibility.Collapsed;

    public Visibility QuantityUnavailableVisibility => IsQuantityUnavailable ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Whether the accepted part is a coil or a flatstock. Those two kinds are stocked as whole pieces and are not
    /// attributed to a work-order sequence, so the step states the part alone: the sequence list gives way to one
    /// detail card, and no sequence is recorded.
    /// </summary>
    public bool IsCoilOrFlatstock => GenericRequestPartKind.IsCoilOrFlatstock(AcceptedPart?.PartNumber ?? Draft.PartNumber);

    /// <summary>Whether the sequence choices are drawn. A coil or flatstock has none.</summary>
    public Visibility SequenceSectionVisibility => IsCoilOrFlatstock ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>Whether the one detail card is drawn in place of the sequence choices.</summary>
    public Visibility PartDetailVisibility => IsCoilOrFlatstock ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Whether the on-hand heading is drawn. A coil's or a flatstock's figure is on the detail card, so the heading
    /// only stays for a read that failed and still needs its retry.
    /// </summary>
    public Visibility QuantityHeadingVisibility =>
        IsCoilOrFlatstock && !IsQuantityUnavailable ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>The part the detail card names.</summary>
    public string DetailPartNumber => AcceptedPart?.PartNumber ?? string.Empty;

    /// <summary>The part's own description, or empty when the supplying system holds none.</summary>
    public string DetailDescription => AcceptedPart?.Description ?? string.Empty;

    /// <summary>
    /// How much of the part is on hand across the whole part, rounded up, because a coil and a flatstock are counted
    /// in whole pieces. Empty until the read answers.
    /// </summary>
    public string DetailQuantityOnHand => QuantityReading?.Figure is { } figure
        ? Math.Ceiling(figure).ToString("0", CultureInfo.CurrentCulture)
        : string.Empty;

    public bool HasSequences => SequencesPresentation == GenericRequestPresentation.Populated;

    public bool IsSequencesUnavailable => SequencesPresentation == GenericRequestPresentation.Unavailable;

    public bool IsSequenceListed => SequencesPresentation == GenericRequestPresentation.Empty && AcceptedPart is not null;

    public bool IsQuantityUnavailable => QuantityPresentation == GenericRequestPresentation.Unavailable;

    public bool IsQuantityAnswered => QuantityPresentation == GenericRequestPresentation.Populated;

    /// <summary>When the sequence list was last attempted, for that list's own unavailable line.</summary>
    public string SequencesAttemptedAtDisplay => FormatAttempt(SequencesAttemptedAtUtc);

    /// <summary>When the quantity was last attempted, for that region's own unavailable line.</summary>
    public string QuantityAttemptedAtDisplay => FormatAttempt(QuantityAttemptedAtUtc);

    /// <summary>
    /// What decides this: an accepted part and a quantity that was actually read. A coil or a flatstock has no
    /// sequence to choose, so only the other parts also require one. A figure of zero and a not-inventoried part
    /// both count as read, because the requester may still raise the request after being warned (FR-014, FR-015,
    /// FR-017).
    /// </summary>
    protected override bool CanAnswer =>
        AcceptedPart is not null
        && (IsCoilOrFlatstock || SelectedSequence is not null)
        && QuantityReading is { Presentation: GenericRequestPresentation.Populated };

    /// <summary>
    /// Read the completions for the current term. Called as the requester types and when they submit.
    /// </summary>
    /// <param name="attempt">
    /// The number of the typing pause that asked for this read, or zero when no pause asked. The answer is applied only
    /// while this attempt is still the newest; a superseded one is discarded by its identity, because a read the plant
    /// has already answered cannot be cancelled (PERF-001).
    /// </param>
    /// <param name="cancellationToken">Stops waiting for an answer once typing resumes.</param>
    /// <remarks>
    /// A completion read is about one half-typed term, not about whether this step has anything to show. So a read
    /// that found nothing leaves the step populated and the box usable, because a number the requester has not
    /// finished typing legitimately matches nothing yet. Only a read that failed takes the body away, and then with
    /// the attempt time and a retry (FR-017). A term nobody knows is refused once it is submitted, by
    /// <see cref="RefuseTypedNumber"/> (FR-002).
    /// </remarks>
    public async Task SearchAsync(long attempt = 0, CancellationToken cancellationToken = default)
    {
        HasNoMatch = false;

        var term = SearchText.Trim();
        if (term.Length == 0)
        {
            Completions.Clear();
            Presentation = GenericRequestPresentation.Populated;
            AttemptedAtUtc = null;
            return;
        }

        // A pause brings its own number; a submit takes the next one, so both are compared against the newest read
        // this step started rather than against each other's arrival.
        var generation = attempt > _searchGeneration ? attempt : ++_searchGeneration;
        _searchGeneration = generation;

        var result = await _data.ReadCompletionsAsync(term, cancellationToken);

        // A read that lands after a later one describes a term the requester has already typed past. Applying it
        // would put the wrong suggestions on screen and, worse, could collapse the box while they are still typing.
        if (generation != _searchGeneration)
        {
            return;
        }

        Completions.Clear();
        foreach (var part in result.Items)
        {
            // The item decides which kinds of part it can be about: a coil or flatstock item offers only those two,
            // and every other item offers neither, so a number that names the wrong kind is never suggested to the
            // requester and can never be accepted (owner decision, 2026-11-23).
            if (GenericRequestPartKind.AllowsPart(Draft.ItemCode, part.PartNumber))
            {
                Completions.Add(part);
            }
        }

        var failed = result.Presentation == GenericRequestPresentation.Unavailable;
        Presentation = failed ? GenericRequestPresentation.Unavailable : GenericRequestPresentation.Populated;
        AttemptedAtUtc = failed ? result.AttemptedAtUtc : null;

        NotifyAnswerChanged();
    }

    /// <summary>
    /// Accept the part the requester picked. A different part clears the sequence and the reading it produced
    /// (FR-008).
    /// </summary>
    public async Task AcceptPartAsync(GenericRequestPartOption? part)
    {
        if (part is null)
        {
            return;
        }

        // The item decides which kinds of part it can be about, so a part the item cannot state is refused here as
        // well as being kept out of the completion list — the acceptance is where the draft would otherwise record it
        // (owner decision, 2026-11-23).
        if (!GenericRequestPartKind.AllowsPart(Draft.ItemCode, part.PartNumber))
        {
            return;
        }

        var changed = !string.Equals(AcceptedPart?.PartNumber, part.PartNumber, StringComparison.OrdinalIgnoreCase);
        AcceptedPart = part;
        SearchText = part.PartNumber;
        HasNoMatch = false;

        // A part other than the one the job carried is the requester's own statement, so the step stops saying the
        // job supplied it (FR-007).
        if (Draft.JobFill is { IsPartFromJob: true } fill
            && !string.Equals(fill.PartNumber, part.PartNumber, StringComparison.OrdinalIgnoreCase))
        {
            ForgetTheJobPart();
        }

        if (changed)
        {
            ClearSequenceAndReading();
        }

        Draft.PartNumber = part.PartNumber;

        // A coil or a flatstock states the part alone: there is no sequence to list, so the step reads what is on
        // hand across the whole part and draws it on the detail card instead (owner decision, 2026-11-23).
        if (IsCoilOrFlatstock)
        {
            await LoadPartDetailAsync();
            return;
        }

        await LoadSequencesAsync();
    }

    private string _pendingJobPart = string.Empty;

    private string _pendingJobSequence = string.Empty;

    /// <summary>
    /// Put the part the running job carries into the search box and remember it as pending, without writing it to the
    /// draft.
    /// </summary>
    /// <remarks>
    /// The candidate is offered, never accepted: the completions read is what proves the part exists, and a part
    /// nobody proved must not reach the request (FR-010). A part the requester has already stated is never replaced
    /// (FR-007), and a candidate the completion read does not return leaves the draft exactly as it was.
    /// </remarks>
    private void SeedFromTheJobFill()
    {
        if (Draft.PartNumber.Length > 0 || Draft.JobFill is not { IsPartFromJob: true } fill)
        {
            return;
        }

        SearchText = fill.PartNumber;
        _pendingJobPart = fill.PartNumber;
        _pendingJobSequence = fill.IsSequenceFromJob ? fill.Sequence : string.Empty;
    }

    /// <summary>Stop claiming the part came from the job, and say so on screen.</summary>
    private void ForgetTheJobPart()
    {
        Draft.JobFill?.ClearPart();
        OnPropertyChanged(nameof(FromJobText));
        OnPropertyChanged(nameof(HasFromJob));
        OnPropertyChanged(nameof(FromJobVisibility));
    }

    /// <summary>Refuse the typed number, naming both sources searched (FR-002).</summary>
    public void RefuseTypedNumber()
    {
        HasNoMatch = true;
        Completions.Clear();
        NotifyAnswerChanged();
    }

    /// <summary>Choose the sequence the requester picked and read its quantity (FR-007).</summary>
    public async Task ChooseSequenceAsync(GenericRequestSequenceOption? sequence)
    {
        SelectedSequence = sequence;
        MarkChosenSequence();

        // A sequence other than the job's is the requester's own choice, so it stops reading as the job's.
        if (Draft.JobFill is { IsSequenceFromJob: true } fill
            && !string.Equals(fill.Sequence, sequence?.Sequence, StringComparison.OrdinalIgnoreCase))
        {
            Draft.JobFill.ClearSequence();
            OnPropertyChanged(nameof(FromJobText));
            OnPropertyChanged(nameof(HasFromJob));
            OnPropertyChanged(nameof(FromJobVisibility));
        }

        if (sequence is null)
        {
            ClearReading();
            NotifyAnswerChanged();
            return;
        }

        await LoadQuantityAsync();
    }

    protected override async Task LoadCoreAsync()
    {
        SeedFromTheJobFill();

        if (Draft.PartNumber.Length > 0)
        {
            SearchText = Draft.PartNumber;
        }

        if (SearchText.Trim().Length == 0)
        {
            // Nothing has been asked of either system yet, and the screen has to be usable so the requester can
            // type. The step is ready for input rather than reporting a read it has not made.
            Presentation = GenericRequestPresentation.Populated;
            AttemptedAtUtc = null;
            return;
        }

        // The same read the typing path makes, so the retry the frame offers after a failed read repeats the term
        // the requester was in the middle of rather than throwing it away.
        await SearchAsync();

        var wanted = _pendingJobPart.Length > 0 ? _pendingJobPart : Draft.PartNumber;
        if (wanted.Length == 0 || !IsPopulated)
        {
            return;
        }

        var restored = Completions.FirstOrDefault(
            part => string.Equals(part.PartNumber, wanted, StringComparison.OrdinalIgnoreCase));

        if (restored is null)
        {
            // A candidate the completion read does not return is not proved, so it is dropped rather than accepted:
            // nothing is written to the draft and the step waits for the requester.
            _pendingJobPart = string.Empty;
            _pendingJobSequence = string.Empty;
            return;
        }

        AcceptedPart = restored;
        Draft.PartNumber = restored.PartNumber;

        if (_pendingJobSequence.Length > 0 && Draft.Sequence.Length == 0)
        {
            Draft.Sequence = _pendingJobSequence;
        }

        _pendingJobPart = string.Empty;
        _pendingJobSequence = string.Empty;

        await LoadSequencesAsync();
    }

    protected override void CommitToDraft()
    {
        if (AcceptedPart is { } part)
        {
            Draft.PartNumber = part.PartNumber;
        }

        if (SelectedSequence is { } sequence)
        {
            Draft.Sequence = sequence.Sequence;
            Draft.SequenceSource = sequence.SourceCodes;
        }

        if (QuantityReading is { } reading)
        {
            Draft.PartStage = reading.Stage;
            Draft.QuantityReading = reading;
        }
    }

    /// <summary>
    /// Editing the term withdraws the previous refusal, and a term that no longer names the accepted part means that
    /// part is not stated any more (FR-002, FR-008).
    /// </summary>
    partial void OnSearchTextChanged(string value)
    {
        HasNoMatch = false;

        if (AcceptedPart is not { } accepted
            || string.Equals(accepted.PartNumber, value.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        AcceptedPart = null;
        Draft.PartNumber = string.Empty;
        ClearSequenceAndReading();

        if (Draft.JobFill is { IsPartFromJob: true })
        {
            ForgetTheJobPart();
        }

        NotifyAnswerChanged();
    }

    partial void OnSequencesPresentationChanged(GenericRequestPresentation value)
    {
        OnPropertyChanged(nameof(HasSequences));
        OnPropertyChanged(nameof(IsSequencesUnavailable));
        OnPropertyChanged(nameof(IsSequenceListed));
        OnPropertyChanged(nameof(SequenceListVisibility));
        OnPropertyChanged(nameof(SequencesUnavailableVisibility));
    }

    partial void OnQuantityPresentationChanged(GenericRequestPresentation value)
    {
        OnPropertyChanged(nameof(IsQuantityUnavailable));
        OnPropertyChanged(nameof(IsQuantityAnswered));
        OnPropertyChanged(nameof(QuantityAnsweredVisibility));
        OnPropertyChanged(nameof(QuantityUnavailableVisibility));
        OnPropertyChanged(nameof(QuantityHeadingVisibility));
    }

    partial void OnAcceptedPartChanged(GenericRequestPartOption? value)
    {
        OnPropertyChanged(nameof(IsCoilOrFlatstock));
        OnPropertyChanged(nameof(SequenceSectionVisibility));
        OnPropertyChanged(nameof(PartDetailVisibility));
        OnPropertyChanged(nameof(QuantityHeadingVisibility));
        OnPropertyChanged(nameof(DetailPartNumber));
        OnPropertyChanged(nameof(DetailDescription));
        OnPropertyChanged(nameof(QuantityAnsweredVisibility));
        OnPropertyChanged(nameof(NothingOnHandVisibility));
        NotifyAnswerChanged();
    }

    partial void OnQuantityReadingChanged(GenericRequestQuantityReading? value) =>
        OnPropertyChanged(nameof(DetailQuantityOnHand));

    partial void OnHasNoMatchChanged(bool value) => OnPropertyChanged(nameof(NoMatchVisibility));

    partial void OnHasNoSequenceChanged(bool value) => OnPropertyChanged(nameof(NoSequenceVisibility));

    partial void OnNextResourceDisplayChanged(string value)
    {
        OnPropertyChanged(nameof(HasNextResource));
        OnPropertyChanged(nameof(NextResourceVisibility));
    }

    partial void OnHasNothingOnHandChanged(bool value) => OnPropertyChanged(nameof(NothingOnHandVisibility));

    partial void OnSequencesAttemptedAtUtcChanged(DateTimeOffset? value) => OnPropertyChanged(nameof(SequencesAttemptedAtDisplay));

    partial void OnQuantityAttemptedAtUtcChanged(DateTimeOffset? value) => OnPropertyChanged(nameof(QuantityAttemptedAtDisplay));

    private async Task LoadSequencesAsync()
    {
        if (AcceptedPart is not { } part)
        {
            return;
        }

        var result = await _data.ReadSequencesAsync(part.PartNumber);

        Sequences.Clear();
        foreach (var sequence in result.Items)
        {
            Sequences.Add(sequence);
        }

        SequencesPresentation = result.Presentation;
        SequencesAttemptedAtUtc = result.AttemptedAtUtc;
        HasNoSequence = result.Presentation == GenericRequestPresentation.Empty;

        SelectedSequence = result.Items.FirstOrDefault(
            sequence => string.Equals(sequence.Sequence, Draft.Sequence, StringComparison.OrdinalIgnoreCase));
        MarkChosenSequence();

        if (SelectedSequence is null)
        {
            ClearReading();
        }
        else
        {
            await LoadQuantityAsync();
        }

        NotifyAnswerChanged();
    }

    /// <summary>
    /// Mark the card the requester picked, and clear every other card, so the accent outline follows the choice even
    /// when the card that was clicked was already the chosen one.
    /// </summary>
    private void MarkChosenSequence()
    {
        foreach (var candidate in Sequences)
        {
            candidate.IsSelected = ReferenceEquals(candidate, SelectedSequence);
        }
    }

    private async Task LoadQuantityAsync()
    {
        if (AcceptedPart is not { } part || SelectedSequence is not { } sequence)
        {
            return;
        }

        var reading = await _data.ReadQuantityAsync(part.PartNumber, sequence.Sequence);

        QuantityReading = reading;
        QuantityPresentation = reading.Presentation;
        QuantityAttemptedAtUtc = reading.AttemptedAtUtc;

        QuantityFigure = reading.FigureDisplay;
        QuantityAnsweredBy = reading.AnsweredByDisplay;
        StageLabel = StageKey(reading.Stage).GetLocalized();
        NextResourceDisplay = sequence.NextResource;
        HasNothingOnHand = reading.IsAnswered
            && reading.Kind == GenericRequestQuantityKind.OnHand
            && reading.Figure is 0m;

        NotifyAnswerChanged();
    }

    /// <summary>
    /// Read what is on hand across the whole part, for a coil or a flatstock, which has no sequence to read it
    /// against.
    /// </summary>
    /// <remarks>
    /// The figure is a whole-part total, so the sequence-derived details — the stage, the next resource — are empty
    /// here rather than borrowed from a sequence that was never chosen.
    /// </remarks>
    private async Task LoadPartDetailAsync()
    {
        if (AcceptedPart is not { } part)
        {
            return;
        }

        var reading = await _data.ReadPartQuantityAsync(part.PartNumber);

        Sequences.Clear();
        SequencesPresentation = GenericRequestPresentation.Empty;
        SequencesAttemptedAtUtc = null;
        HasNoSequence = false;
        SelectedSequence = null;

        QuantityReading = reading;
        QuantityPresentation = reading.Presentation;
        QuantityAttemptedAtUtc = reading.AttemptedAtUtc;

        QuantityFigure = reading.FigureDisplay;
        QuantityAnsweredBy = reading.AnsweredByDisplay;
        StageLabel = string.Empty;
        NextResourceDisplay = string.Empty;
        HasNothingOnHand = reading.IsAnswered
            && reading.Kind == GenericRequestQuantityKind.OnHand
            && reading.Figure is 0m;

        NotifyAnswerChanged();
    }

    /// <summary>
    /// Re-read whichever quantity this step asked for: the part's own total for a coil or a flatstock, and the
    /// chosen sequence's reading for every other part.
    /// </summary>
    private Task RetryQuantityAsync() => IsCoilOrFlatstock ? LoadPartDetailAsync() : LoadQuantityAsync();

    private void ClearSequenceAndReading()
    {
        Sequences.Clear();
        SequencesPresentation = GenericRequestPresentation.Empty;
        SequencesAttemptedAtUtc = null;
        HasNoSequence = false;
        SelectedSequence = null;

        Draft.Sequence = string.Empty;
        Draft.SequenceSource = string.Empty;

        ClearReading();
    }

    private void ClearReading()
    {
        QuantityReading = null;
        QuantityPresentation = GenericRequestPresentation.Empty;
        QuantityAttemptedAtUtc = null;
        QuantityFigure = string.Empty;
        QuantityAnsweredBy = string.Empty;
        StageLabel = string.Empty;
        NextResourceDisplay = string.Empty;
        HasNothingOnHand = false;

        Draft.PartStage = GenericRequestPartStage.Unknown;
        Draft.QuantityReading = null;
    }

    /// <summary>The resource key naming a derived stage, so the label says which of the two types the plant has it is.</summary>
    private static string StageKey(GenericRequestPartStage stage) => stage switch
    {
        GenericRequestPartStage.Wip => "GenericRequest_Part_Stage_Wip.Content",
        GenericRequestPartStage.FinishedGood => "GenericRequest_Part_Stage_FinishedGood.Content",
        _ => "GenericRequest_Part_Stage_Unknown.Content",
    };

    private static string FormatAttempt(DateTimeOffset? attemptedAtUtc) =>
        attemptedAtUtc is { } at ? at.ToLocalTime().ToString("g", CultureInfo.CurrentCulture) : string.Empty;
}
