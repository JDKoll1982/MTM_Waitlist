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
    /// typing, and only the term currently on screen describes what they want to see.
    /// </summary>
    private int _searchGeneration;

    public GenericRequestPartViewModel(IGenericRequestPartData data, IGenericRequestNavigator navigator)
        : base(navigator)
    {
        ArgumentNullException.ThrowIfNull(data);

        _data = data;
        RetrySequencesCommand = new AsyncRelayCommand(LoadSequencesAsync);
        RetryQuantityCommand = new AsyncRelayCommand(LoadQuantityAsync);
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

    // The three reads draw independently, so each region's presence is its own projection rather than one state
    // standing for the whole body (plan OD-1).
    public Visibility NoMatchVisibility => HasNoMatch ? Visibility.Visible : Visibility.Collapsed;

    public Visibility NoSequenceVisibility => HasNoSequence ? Visibility.Visible : Visibility.Collapsed;

    public Visibility SequenceListVisibility => HasSequences ? Visibility.Visible : Visibility.Collapsed;

    public Visibility SequencesUnavailableVisibility => IsSequencesUnavailable ? Visibility.Visible : Visibility.Collapsed;

    public Visibility QuantityAnsweredVisibility => IsQuantityAnswered ? Visibility.Visible : Visibility.Collapsed;

    public Visibility NextResourceVisibility => HasNextResource ? Visibility.Visible : Visibility.Collapsed;

    public Visibility NothingOnHandVisibility => HasNothingOnHand ? Visibility.Visible : Visibility.Collapsed;

    public Visibility QuantityUnavailableVisibility => IsQuantityUnavailable ? Visibility.Visible : Visibility.Collapsed;

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
    /// Three answers decide this: an accepted part, a chosen sequence, and a quantity that was actually read. A
    /// figure of zero and a not-inventoried part both count as read, because the requester may still raise the
    /// request after being warned (FR-014, FR-015, FR-017).
    /// </summary>
    protected override bool CanAnswer =>
        AcceptedPart is not null
        && SelectedSequence is not null
        && QuantityReading is { Presentation: GenericRequestPresentation.Populated };

    /// <summary>
    /// Read the completions for the current term. Called as the requester types and when they submit.
    /// </summary>
    /// <remarks>
    /// A completion read is about one half-typed term, not about whether this step has anything to show. So a read
    /// that found nothing leaves the step populated and the box usable, because a number the requester has not
    /// finished typing legitimately matches nothing yet. Only a read that failed takes the body away, and then with
    /// the attempt time and a retry (FR-017). A term nobody knows is refused once it is submitted, by
    /// <see cref="RefuseTypedNumber"/> (FR-002).
    /// </remarks>
    public async Task SearchAsync()
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

        var generation = ++_searchGeneration;
        var result = await _data.ReadCompletionsAsync(term);

        // A read that lands after a later one describes a term the requester has already typed past. Applying it
        // would put the wrong suggestions on screen and, worse, could collapse the box while they are still typing.
        if (generation != _searchGeneration)
        {
            return;
        }

        Completions.Clear();
        foreach (var part in result.Items)
        {
            Completions.Add(part);
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

        var changed = !string.Equals(AcceptedPart?.PartNumber, part.PartNumber, StringComparison.OrdinalIgnoreCase);
        AcceptedPart = part;
        SearchText = part.PartNumber;
        HasNoMatch = false;

        if (changed)
        {
            ClearSequenceAndReading();
        }

        Draft.PartNumber = part.PartNumber;

        await LoadSequencesAsync();
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

        if (Draft.PartNumber.Length == 0 || !IsPopulated)
        {
            return;
        }

        var restored = Completions.FirstOrDefault(
            part => string.Equals(part.PartNumber, Draft.PartNumber, StringComparison.OrdinalIgnoreCase));

        if (restored is not null)
        {
            AcceptedPart = restored;
            await LoadSequencesAsync();
        }
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
    }

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
