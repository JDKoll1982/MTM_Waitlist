using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Shared.Helpers;
using MTM_Waitlist.Module_Waitlist.Helpers;

namespace MTM_Waitlist.Module_Waitlist.Models;

public sealed class SampleOrder : INotifyPropertyChanged
{
    public int Id { get; set; }

    /// <summary>
    /// When this row maps to a live waitlist request, the underlying request's Guid. Used to
    /// target cancel-own and other request-scoped actions from the list/detail UI.
    /// </summary>
    public Guid? RequestId { get; set; }

    /// <summary>The underlying request's requester employee number; empty when the row carries none.</summary>
    public string RequesterEmployeeNumber { get; set; } = string.Empty;

    /// <summary>
    /// The request's recorded assignee, or null while it is still available. Drives the Complete/Release
    /// gate, so a row built from the store's own answer is what decides who may finish the work.
    /// </summary>
    public string? AssignedMaterialHandler { get; set; }

    /// <summary>
    /// The assignee's display name, or null when no source names them. <see cref="PersonText"/> prints this
    /// rather than the number beside it, because a person is named by name.
    /// </summary>
    public string? AssignedMaterialHandlerName { get; set; }

    /// <summary>The request's short handler note, or null when it has none.</summary>
    public string? Note { get; set; }

    /// <summary>
    /// Card <b>Line 1</b> — the Item's umbrella phrase: the Category's own word, or the Item's own phrase where
    /// the Item defines one. Read from the Item catalog, never from a stored request type (FR-005).
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Card <b>Line 2</b> — the Item's identifier: a fixed value, a value read from the job, or the value the
    /// flow captured. Resolved from the Item's own template; an unresolvable token renders the Item's display
    /// name and reports the problem through <see cref="Line2Problem"/> (FR-005, FR-026).
    /// </summary>
    public string Subtitle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string RequestedByName { get; set; } = string.Empty;
    public string RequestedPressName { get; set; } = string.Empty;
    /// <summary>
    /// The request's creation instant, kept so the waiting-age text can be recomputed as the clock
    /// advances instead of being frozen at the moment the row was mapped.
    /// </summary>
    public DateTimeOffset? RequestedUtc { get; set; }

    /// <summary>
    /// The request's due instant, kept so the remaining-time countdown can be recomputed on the same tick.
    /// </summary>
    public DateTimeOffset? TargetTimeUtc { get; set; }

    private string _remainingTimeText = string.Empty;

    /// <summary>
    /// Countdown text for the due time ("01:20"), or "Overdue"/"New". Re-evaluated every minute by
    /// <see cref="RefreshTimeDerivedText"/>.
    /// </summary>
    public string RemainingTimeText
    {
        get => _remainingTimeText;
        set
        {
            if (SetField(ref _remainingTimeText, value))
            {
                // The ring overlays this text while the request is live, so a change here is a change there too
                // (FR-015).
                OnPropertyChanged(nameof(RingText));
                OnPropertyChanged(nameof(RemainingTimeAndAllotmentText));
            }
        }
    }

    /// <summary>
    /// The remaining-time row's value: how long is left, against the minutes the Item allotted — "00:09 / 00:15"
    /// (FR-017).
    /// </summary>
    /// <remarks>
    /// The two figures belong together because the countdown alone says nothing about how close it is: nine
    /// minutes left is most of an eleven-minute allotment and half of an eighteen-minute one, and the same
    /// "00:09" reads as comfortable or as nearly out depending on the number it is measured against. The
    /// allotment stated is the same one the ring's fraction uses, so the row and the ring can never disagree
    /// about the scale. A row with no due time has no countdown and no allotment to state, so it shows what
    /// <see cref="RemainingTimeText"/> says and nothing is appended to it (FR-002).
    /// </remarks>
    public string RemainingTimeAndAllotmentText =>
        TargetTimeUtc is null
            ? RemainingTimeText
            : $"{RemainingTimeText} / {FormatMinutes(AllottedMinutes ?? DefaultAllottedMinutes)}";

    /// <summary>Minutes as the same <c>mm:ss</c> shape the countdown and the ring use, so the two figures on the
    /// row are directly comparable.</summary>
    private static string FormatMinutes(int minutes) => $"{minutes / 60:00}:{minutes % 60:00}";

    private bool _isOverdue;

    /// <summary>
    /// Whether the row is displayed as overdue: either the store's flag, or the clock having passed
    /// <see cref="TargetTimeUtc"/>.
    /// </summary>
    public bool IsOverdue
    {
        get => _isOverdue;
        set
        {
            if (SetField(ref _isOverdue, value))
            {
                // The overdue marking is one of the status word's two inputs besides the condition, and it is the
                // whole of the overdue theme, so both move with it (FR-012, FR-014).
                OnPropertyChanged(nameof(StatusWord));
                OnPropertyChanged(nameof(StatusTheme));
            }
        }
    }

    /// <summary>
    /// The overdue flag as supplied by the request store, kept separate from <see cref="IsOverdue"/> so a
    /// later tick cannot clear a flag the store set.
    /// </summary>
    public bool IsOverdueAtSource { get; set; }

    private DateTimeOffset? _closedUtc;

    /// <summary>
    /// The instant the request closed, or null while it is still open (FR-015).
    /// </summary>
    /// <remarks>
    /// It is one of the ring's two extra inputs: once it is set the ring stops measuring the time left and freezes
    /// on the time taken. It is kept as the instant rather than as a second flag so "closed" and "how long it
    /// took" can never come from two different values.
    /// </remarks>
    public DateTimeOffset? ClosedUtc
    {
        get => _closedUtc;
        set
        {
            if (SetField(ref _closedUtc, value))
            {
                OnPropertyChanged(nameof(RingValue));
                OnPropertyChanged(nameof(RingMeasure));
                OnPropertyChanged(nameof(RingText));
                OnPropertyChanged(nameof(RequesterText));
                OnPropertyChanged(nameof(HasRequester));
            }
        }
    }

    /// <summary>
    /// The Item code the request asked for (FR-004). The row's identity: the picture, the request page's
    /// sections and the sort all resolve from it, so no reader has to be told what the request "really" was
    /// through a second, derived pair.
    /// </summary>
    public string ItemCode { get; set; } = string.Empty;

    /// <summary>
    /// The plain-language report of an Item whose identifier could not be resolved (FR-026), or null when the
    /// card's second line resolved cleanly. Never blank in place of a value: a card that cannot show the
    /// identifier says why instead of silently showing nothing.
    /// </summary>
    public string? Line2Problem { get; set; }

    /// <summary>True when the Item's identifier could not be resolved and <see cref="Line2Problem"/> says why.</summary>
    public bool HasLine2Problem => !string.IsNullOrWhiteSpace(Line2Problem);

    public long? WorkCenterCatalogId { get; set; }

    /// <summary>
    /// The part this request is <b>about</b>, or empty when the request names no part at all. It is the material
    /// the card draws a picture of: the coil or flatstock the job holds, the die the request is for, or the
    /// component the operator picked. It is captured where the card's own identifier is resolved, so the picture
    /// and the second line can never disagree about which part the request is about.
    /// </summary>
    public string MaterialPartNumber { get; set; } = string.Empty;

    /// <summary>
    /// The part whose stock the card's identifier opens a lookup for, or empty when the card's identifier is plain
    /// text. Only the Deliver Component card sets it: its identifier is the component the operator picked.
    /// </summary>
    public string StockLookupPartNumber { get; set; } = string.Empty;

    /// <summary>
    /// The store scope the material part belongs to — which of the systems it is a part of. Empty when the
    /// request names no part. A Visual part and a WIP part that share a number are two parts, so the scope
    /// travels beside the number and never instead of it (FR-002).
    /// </summary>
    public string MaterialPartScope { get; set; } = string.Empty;

    /// <summary>
    /// The material part's picture, resolved by the list before this row reached the bound collection — or, for a
    /// dunnage request, the chosen dunnage part's picture, which only the requesting job can answer. Carried
    /// rather than computed: the model is plain data and knows neither a storage root nor a picture rule.
    /// Empty when nothing resolved, which draws the shared placeholder.
    /// </summary>
    public string ResolvedPartImagePath { get; set; } = string.Empty;

    public string WorkCenterImagePath { get; set; } = string.Empty;

    /// <summary>
    /// The picture the card draws: the <b>part the request is about</b> — the material part, or the dunnage part
    /// an operator chose — and the application's one shared no-image placeholder when the request names no part
    /// or its part cannot be pictured.
    /// </summary>
    /// <remarks>
    /// The picture for the kind of request is deliberately not drawn here. A card that showed its Item's artwork
    /// would be showing a picture of a category where the person needs to recognise a part (FR-019), and the
    /// family's artwork standing in for a part that has none is exactly what FR-015 and FR-020 forbid. One
    /// placeholder, and no other stand-in. The converter applies the picture rule as well, so a path that names
    /// a missing, unreadable or unusable file lands on the placeholder too.
    /// </remarks>
    public string EffectiveImagePath =>
        string.IsNullOrWhiteSpace(ResolvedPartImagePath)
            ? ImagePicturePolicy.NoImagePath
            : ResolvedPartImagePath;

    /// <summary>The work centre's picture, or the application's no-image placeholder when none resolved.</summary>
    public string EffectiveWorkCenterImagePath =>
        string.IsNullOrWhiteSpace(WorkCenterImagePath)
            ? ImagePicturePolicy.NoImagePath
            : WorkCenterImagePath;

    /// <summary>The word row 1 reads when the request is overdue (FR-014).</summary>
    private const string OverdueWord = "Overdue";

    /// <summary>
    /// Friendly lifecycle label shown as the card's status badge/pill. Maps the stored status
    /// (Pending/Accepted/Completed/Canceled) to Waiting/In Progress/Done/Cancelled; empty when
    /// the row carries no recognised lifecycle status.
    /// </summary>
    public string StatusBadgeText => LifecycleWordFor(Status);

    public bool HasStatusBadge => !string.IsNullOrWhiteSpace(StatusBadgeText);

    private string? _handlerStatus;

    /// <summary>
    /// The condition this row's own material handler reported, or null when the row carries none (FR-071,
    /// <c>data-model.md</c> §2).
    /// </summary>
    /// <remarks>
    /// The stored value is a condition only when it is one of the four verbatim strings, so a value the vocabulary
    /// does not know — a fifth word, a padded one, a differently cased one — is restored as no condition and then
    /// behaves exactly like a row nobody reported on. Restoring it here rather than at every reader is what keeps
    /// the status word, the theme and the picker agreeing about what was reported, and it is why a row read back
    /// from the store after a restart reads the same as the row that wrote it (FR-073).
    /// </remarks>
    public string? HandlerStatus
    {
        get => _handlerStatus;
        set
        {
            var restored = HandlerStatusCatalog.IsCondition(value) ? value : null;
            if (SetField(ref _handlerStatus, restored))
            {
                OnPropertyChanged(nameof(StatusWord));
                OnPropertyChanged(nameof(StatusTheme));
            }
        }
    }

    /// <summary>
    /// The status zone's first row: the word the card reads out (FR-012, <c>data-model.md</c> §3).
    /// </summary>
    /// <remarks>
    /// Precedence, in order: the handler-set condition when the row carries one, otherwise the <c>Overdue</c>
    /// marking when the request is overdue, otherwise the lifecycle word (FR-012, FR-023). A condition and the
    /// overdue marking are never both shown, and the condition wins for as long as it is set. Empty when the row
    /// has neither a condition, nor an overdue marking, nor a recognised lifecycle status — and the card then
    /// removes the row rather than leaving it blank (FR-018).
    /// </remarks>
    public string StatusWord =>
        HandlerStatus
        ?? (IsOverdue ? OverdueWord : StatusBadgeText);

    /// <summary>
    /// Which of the three themes the whole status column takes (FR-019, FR-072).
    /// </summary>
    /// <remarks>
    /// Derived from the same two inputs <see cref="StatusWord"/> reads, so the word and the theme can never
    /// disagree: a row showing a condition is themed for the condition, a row showing the overdue marking is
    /// themed as overdue, and a row showing a lifecycle word is themed normally. A row that is overdue <em>and</em>
    /// carries a condition reads the condition and is not themed as overdue (FR-023).
    /// </remarks>
    public StatusColumnTheme StatusTheme =>
        HandlerStatus is not null ? StatusColumnTheme.HandlerCondition
        : IsOverdue ? StatusColumnTheme.Overdue
        : StatusColumnTheme.Normal;

    /// <summary>
    /// The minutes an Item that configures no allotment allots (FR-017).
    /// </summary>
    /// <remarks>
    /// Fifteen, pinned by the request, and labelled wherever it is drawn so a person is told where the number came
    /// from rather than left to guess why the ring has that scale.
    /// </remarks>
    public const int DefaultAllottedMinutes = 15;

    private int? _allottedMinutes;

    /// <summary>
    /// The minutes the Item allots to this request — its configured <c>allotted_minutes</c>, or
    /// <see cref="DefaultAllottedMinutes"/> when the Item configures none (FR-017).
    /// </summary>
    /// <remarks>
    /// One of the ring's two extra inputs, and the ring's whole scale. The row resolves the default rather than
    /// leaving it to each caller, so the labelled fifteen minutes cannot be remembered in one place and forgotten
    /// in another. A configured value that cannot be measured — none, or zero or less — reads as the default,
    /// because a ring with no scale has nothing to draw.
    /// </remarks>
    public int? AllottedMinutes
    {
        get => _allottedMinutes is int configured && configured > 0 ? configured : DefaultAllottedMinutes;
        set
        {
            if (SetField(ref _allottedMinutes, value))
            {
                OnPropertyChanged(nameof(RingValue));
                // The allotment is half of what the remaining-time row states, so a row whose Item configuration
                // lands after the first paint has to raise it and not only the ring.
                OnPropertyChanged(nameof(RemainingTimeAndAllotmentText));
            }
        }
    }

    /// <summary>
    /// The ring's fraction: how much of the allotment has been used (FR-013).
    /// </summary>
    /// <remarks>
    /// While the request is live the fraction grows with the clock; once <see cref="ClosedUtc"/> is set it stops at
    /// the moment the request closed and reports the time taken instead (FR-015). A value above 1 means the
    /// allotment was exceeded — an overdue row carries its overrun here, and the ring draws it as its full arc
    /// (FR-014). A row with no creation instant to measure from draws nothing, which reads as zero.
    /// </remarks>
    public double RingValue => RingFraction(RequestedUtc, ClosedUtc, AllottedMinutes ?? DefaultAllottedMinutes);

    /// <summary>
    /// Which of the two things the ring is drawing: the time the request still has left, or — once it has closed —
    /// the time it took (FR-015, FR-016).
    /// </summary>
    /// <remarks>
    /// The ring's accessible name says the same thing, which is why the row states it as a value rather than
    /// leaving the card to infer it from what the ring happens to contain.
    /// </remarks>
    public TimeRingMeasure RingMeasure => IsClosed ? TimeRingMeasure.TimeTaken : TimeRingMeasure.TimeLeft;

    /// <summary>
    /// The text the ring draws inside itself (FR-014, FR-015). While the request is live that is the time it still
    /// has left; once it has closed it is the time the request took; and once it is overdue it is the time it has
    /// run <b>over</b> by.
    /// </summary>
    /// <remarks>
    /// <para>
    /// FR-015 asks the ring to carry one of two different quantities by state, and FR-016 asks it to say which.
    /// <see cref="RingMeasure"/> states which, and this is what the ring actually draws, so a closed request no
    /// longer shows a countdown frozen at the moment it closed.
    /// </para>
    /// <para>
    /// <b>Overdue is the third case, and it used to be wrong.</b> FR-014 requires an overdue request to read
    /// <c>Overdue</c> <i>and</i> to show "the time overrun in its ring", and the settled layout says the same
    /// (D29): row 1 carries the word and the ring carries the overrun, in the same <c>-00:15</c> shape the
    /// countdown uses. The ring was reading <see cref="RemainingTimeText"/>, which is the word itself once the due
    /// time has passed - so an overdue card printed <c>Overdue</c> twice, side by side, and the ring said nothing
    /// about how late the request was. The two are now separate: the word stays where FR-014 puts it and the ring
    /// carries the measurement. <see cref="RemainingTimeText"/> is deliberately left alone, because the request's
    /// own detail page reads it as a word.
    /// </para>
    /// </remarks>
    public string RingText =>
        RingMeasure == TimeRingMeasure.TimeTaken
            ? FormatTimeTaken(ClosedUtc, RequestedUtc)
            : IsOverdue
                ? FormatTimeOverrun(TargetTimeUtc)
                : RemainingTimeText;

    /// <summary>
    /// Whether the request has closed: it carries a close instant, or its stored status is one of the finished
    /// ones. Asked of the lifecycle status rather than of the displayed word, so no reader compares display text
    /// as though it were a stored status.
    /// </summary>
    /// <summary>
    /// Whether the request has closed. A derived value the row's own members read, kept internal rather than
    /// private so the naming guard does not mistake an expression-bodied property for a private field.
    /// </summary>
    internal bool IsClosed => ClosedUtc is not null || RequestActionPolicy.IsDone(Status);

    /// <summary>
    /// The status zone's person row: the material handler once the request has one, and the requester while it has
    /// none (FR-022, <c>data-model.md</c> §5).
    /// </summary>
    /// <remarks>
    /// <para>
    /// One rule covers the three states. An unclaimed request has no handler, so the row reads the requester; a
    /// claimed one reads the handler; and a closed one reads the handler, or — on a request cancelled while it was
    /// still waiting — the requester, who is the only person who could have cancelled an unclaimed request. Empty
    /// when there is nobody to name, and the card then draws no row at all rather than a blank one (FR-018).
    /// </para>
    /// <para>
    /// <b>The handler is named, never numbered.</b> The store records who holds a request by employee number,
    /// because that number is what the handler authorizations compare, and this row used to print it — so a
    /// claimed card read <c>6229</c> where the name of the person holding the work belongs. It now reads the
    /// handler's own name. A handler whose name no source records leaves the row empty, and the card draws no
    /// person row rather than falling back to the number: an employee number is an identifier, not a name, and
    /// printing one where a person belongs is the defect this rule exists to prevent.
    /// </para>
    /// </remarks>
    public string PersonText =>
        string.IsNullOrWhiteSpace(AssignedMaterialHandler)
            ? RequestedByName
            : AssignedMaterialHandlerName ?? string.Empty;

    /// <summary>
    /// Whether the person row carries a value. False means the card removes the row rather than leaving it blank
    /// (FR-018).
    /// </summary>
    public bool HasPerson => !string.IsNullOrWhiteSpace(PersonText);

    /// <summary>
    /// The status zone's requester row: the requester, drawn only once the request has closed (FR-022).
    /// </summary>
    /// <remarks>
    /// While the request is open the requester is already the person row, so a second row naming them would repeat
    /// it. Empty on an open request and on a row that names no requester, and the card then draws no row at all
    /// (FR-018).
    /// </remarks>
    public string RequesterText => IsClosed ? RequestedByName : string.Empty;

    /// <summary>
    /// Whether the requester row carries a value. False means the card removes the row rather than leaving it blank
    /// (FR-018).
    /// </summary>
    public bool HasRequester => !string.IsNullOrWhiteSpace(RequesterText);

    private string _waitingForText = string.Empty;

    /// <summary>
    /// Human-friendly "how long this request has been waiting" text (from its created time), for example
    /// "Waiting 35m". Re-evaluated every minute by <see cref="RefreshTimeDerivedText"/>.
    /// </summary>
    public string WaitingForText
    {
        get => _waitingForText;
        set
        {
            if (SetField(ref _waitingForText, value))
            {
                OnPropertyChanged(nameof(HasWaitingFor));
            }
        }
    }

    public bool HasWaitingFor => !string.IsNullOrWhiteSpace(WaitingForText);

    public ObservableCollection<WaitlistField> Fields { get; } = new();

    /// <summary>
    /// The card's detail cells, in declared order — one per field the Item declares for this row, with every cell
    /// that resolved no value left out entirely (FR-026, <c>data-model.md</c> §7).
    /// </summary>
    /// <remarks>
    /// The cells are filled from the Item's declaration rather than chosen per row, and nothing is padded: the
    /// card's geometry follows the number of cells that remain (FR-006, FR-009). <c>WaitlistLineCardCell</c> owns
    /// the rule that a cell with no value is not one the card draws.
    /// </remarks>
    public ObservableCollection<WaitlistLineCardCell> Cells { get; } = new();

    /// <summary>The cell in the first slot, or <see langword="null"/> when the row filled fewer than one.</summary>
    public WaitlistLineCardCell? Cell0 => CellInSlot(0);

    /// <summary>The cell in the second slot, or <see langword="null"/> when the row filled fewer than two.</summary>
    public WaitlistLineCardCell? Cell1 => CellInSlot(1);

    /// <summary>The cell in the third slot, or <see langword="null"/> when the row filled fewer than three.</summary>
    public WaitlistLineCardCell? Cell2 => CellInSlot(2);

    /// <summary>The cell in the fourth slot, or <see langword="null"/> when the row filled fewer than four.</summary>
    public WaitlistLineCardCell? Cell3 => CellInSlot(3);

    /// <summary>
    /// The cell in one of the panel's four slots, or <see langword="null"/> when the row carries none there.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The per-Item templates place their cells in fixed slots and most rows fill fewer than four of them, so an
    /// absent slot is the ordinary case rather than an error. Binding a slot as <c>Cells[3]</c> made it an error:
    /// the indexer is asked for an entry that does not exist, and the binding engine reports
    /// <i>"Failed to connect to index '3"</i> every time, for every card, on every load - 102 failures in one list
    /// of eleven rows. <c>FallbackValue</c> kept the drawing right, so nothing looked broken, but the failures
    /// buried the debug output that a real binding fault would have to appear in.
    /// </para>
    /// <para>
    /// Asking for a slot by name answers <see langword="null"/> for an empty one, which is an ordinary value and
    /// not a failed lookup, so the same templates bind without a single error.
    /// </para>
    /// </remarks>
    /// <param name="slot">The zero-based slot.</param>
    /// <returns>The cell in that slot, or <see langword="null"/>.</returns>
    private WaitlistLineCardCell? CellInSlot(int slot) =>
        slot >= 0 && slot < Cells.Count ? Cells[slot] : null;

    /// <summary>Initializes a new instance of the <see cref="SampleOrder"/> class.</summary>
    public SampleOrder() =>
        Cells.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(Cell0));
            OnPropertyChanged(nameof(Cell1));
            OnPropertyChanged(nameof(Cell2));
            OnPropertyChanged(nameof(Cell3));
        };

    /// <summary>
    /// The urgency state this row was ordered by on the list, or null when no due value could be derived.
    /// Held on the row so the card's "Remaining time" and the list order can never come from two different
    /// numbers, and so the ordering helper is the only place the ordering rule lives.
    /// </summary>
    public UrgencyState? Urgency { get; set; }

    /// <summary>
    /// Whether the current viewer is offered Accept: a handler-or-above on an available, unfinished request.
    /// A view of <c>RequestActionPolicy.CanViewerAccept</c>, never a wider rule than the service enforces.
    /// </summary>
    public bool CanAccept { get; set; }

    /// <summary>
    /// Whether the current viewer is offered Complete and Release: they are the request's recorded assignee.
    /// A view of <c>RequestActionPolicy.CanViewerCompleteOrRelease</c>.
    /// </summary>
    public bool CanCompleteOrRelease { get; set; }

    /// <summary>
    /// Whether the current viewer is offered Cancel: their own request while it is still waiting, or a request
    /// they have claimed and can no longer work. The card shows it whenever the Complete button is live, so the
    /// two appear together.
    /// </summary>
    public bool CanCancelRequest { get; set; }

    /// <summary>
    /// Whether the signed-in person may set this row's handler condition — that is, whether the status word is
    /// clickable and opens the four-condition picker (FR-070, FR-074).
    /// </summary>
    /// <remarks>
    /// The gate is the claim itself: only the request's own material handler, on a request that is not finished,
    /// can report a condition on it, which is the same answer <see cref="CanCompleteOrRelease"/> carries and the
    /// same rule the write procedure enforces (FR-076). Deriving it rather than carrying a second flag means a card
    /// can never offer the picker to somebody the store would refuse, and nobody can put the two out of step.
    /// </remarks>
    public bool CanSetStatusCondition => CanCompleteOrRelease;

    /// <summary>
    /// Whether the current viewer is offered Give back: they are the request's recorded assignee, which is the same
    /// question <see cref="CanCompleteOrRelease"/> answers (FR-047).
    /// </summary>
    /// <remarks>
    /// Handing a claimed request back is the assignee's other option on their own claim, so the two are offered
    /// together and never to anybody else. Derived rather than carried as a second flag, so the Complete button and
    /// the Give back button can never come apart.
    /// </remarks>
    public bool CanRelease => CanCompleteOrRelease;

    /// <summary>
    /// Whether the row offers Open details, the only way the card opens the request (FR-068).
    /// </summary>
    /// <remarks>
    /// Always available: the card body no longer opens the request (FR-069), so a row that could not open its
    /// details would be a row nobody could act on. It is a gate all the same, so the card draws the button by the
    /// same rule as every other button (FR-062).
    /// </remarks>
    public bool CanOpenDetails => true;

    /// <summary>
    /// Whether the row offers Print. It never does: printing ships disabled, says it is not available yet when
    /// pointed at, and performs no action (FR-067).
    /// </summary>
    public bool CanPrint => false;

    /// <summary>
    /// Whether a message arrived on this request since the viewer last opened its details — the card's
    /// new-message indicator. Derived from the newest human-authored message against the viewer's last-seen time.
    /// </summary>
    public bool HasNewMessages { get; set; }

    /// <summary>Localized tooltip for the new-message indicator; empty when there is nothing to announce.</summary>
    public string NewMessageTooltip { get; set; } = string.Empty;

    /// <summary>The list's Accept command, carried to the card through this row. Null when the gate is false.</summary>
    public ICommand? AcceptCommand { get; set; }

    /// <summary>The list's Complete command, carried to the card through this row. Null when the gate is false.</summary>
    public ICommand? CompleteCommand { get; set; }

    /// <summary>
    /// The list's Give back command, carried to the card through this row. Null when the gate is false. It is the
    /// same gate as Complete: handing a claimed request back is the assignee's other option, so the two are
    /// offered together and never to anybody else (FR-047).
    /// </summary>
    public ICommand? ReleaseCommand { get; set; }

    /// <summary>The list's Cancel command, carried to the card through this row. Null when the gate is false.</summary>
    public ICommand? CancelCommand { get; set; }

    /// <summary>
    /// When the newest message written by a person was added — a note, not a lifecycle change. This is the
    /// cheap "somebody said something" signal behind the new-message indicator, so the list does not need a
    /// history read for every row. Null means no person has written on the request, which never raises the
    /// indicator: job created, accepted, completed and canceled are system events, not messages.
    /// </summary>
    public DateTimeOffset? LastMessageUtc { get; set; }

    /// <summary>Localized accessible name for the Accept icon button; empty when the action is not offered.</summary>
    public string AcceptActionText { get; set; } = string.Empty;

    /// <summary>Localized accessible name for the Complete icon button; empty when the action is not offered.</summary>
    public string CompleteActionText { get; set; } = string.Empty;

    /// <summary>Localized accessible name for the Give back icon button; empty when the action is not offered.</summary>
    public string ReleaseActionText { get; set; } = string.Empty;

    /// <summary>Localized accessible name for the Cancel icon button; empty when the action is not offered.</summary>
    public string CancelActionText { get; set; } = string.Empty;

    /// <summary>
    /// The number of buttons this row offers — the one number the card's action area can be checked against, so
    /// a gate flag with no control and a control with no gate are both visible at once.
    /// </summary>
    /// <remarks>
    /// The card draws [primary][Give back][Cancel]: the primary is Accept while the request is available and
    /// becomes Complete once the viewer has claimed it — so Accept and Complete are never both live — and Complete
    /// and Give back are both live exactly when the viewer is the assignee, which is why the claimed state
    /// contributes two. Cancel accompanies a claim. Open details and Print are not counted: one is always drawn and
    /// the other never is, so neither says anything about this row's state.
    /// </remarks>
    public int OfferedActionCount =>
        (CanAccept ? 1 : 0)
        + (CanCompleteOrRelease ? 1 : 0)
        + (CanRelease ? 1 : 0)
        + (CanCancelRequest ? 1 : 0);

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Recomputes the two time-derived texts (<see cref="WaitingForText"/> and
    /// <see cref="RemainingTimeText"/>) as of <paramref name="now"/>.
    /// </summary>
    /// <remarks>
    /// Called once a minute while the row is on screen. Change notifications are raised only for values
    /// that actually changed, so a tick that does not cross a minute boundary neither repaints the card
    /// nor reports a change. Rows that carry no timestamps are left untouched.
    /// </remarks>
    /// <param name="now">The instant to measure the row against.</param>
    /// <returns><see langword="true"/> when at least one displayed value changed.</returns>
    public bool RefreshTimeDerivedText(DateTimeOffset now)
    {
        var changed = false;

        if (RequestedUtc is DateTimeOffset requested)
        {
            var waiting = FormatWaitingAge(requested, now);
            if (!string.Equals(waiting, WaitingForText, StringComparison.Ordinal))
            {
                WaitingForText = waiting;
                changed = true;
            }
        }

        // The store's flag is sticky, and the clock can additionally make a row overdue while it is open. The
        // rule lives on the row so the list that builds it applies the same one (FR-014).
        var overdue = IsOverdueAt(IsOverdueAtSource, TargetTimeUtc, now);
        if (overdue != IsOverdue)
        {
            IsOverdue = overdue;
            changed = true;
        }

        if (TargetTimeUtc is DateTimeOffset due)
        {
            var remaining = FormatRemainingTime(due, overdue, now);
            if (!string.Equals(remaining, RemainingTimeText, StringComparison.Ordinal))
            {
                RemainingTimeText = remaining;
                changed = true;
            }

            // While a row is overdue its ring carries the time overrun (FR-014), which grows with every minute
            // and is therefore never equal to the value drawn a minute ago. Nothing else on an overdue row
            // changes, so without this the ring's text would be raised once and then freeze at the overrun the
            // row happened to have when something last changed.
            if (overdue)
            {
                changed = true;
            }
        }

        if (changed)
        {
            // The ring rides the same tick as the texts it sits beside, so the two can never disagree about the
            // minute they are describing (FR-017). `RingText` is raised as well as `RingValue`, because the ring's
            // text is time-derived in two of its three states (the countdown, and the overrun once overdue).
            OnPropertyChanged(nameof(RingValue));
            OnPropertyChanged(nameof(RingText));
            OnPropertyChanged(nameof(RemainingTimeAndAllotmentText));
        }

        return changed;
    }

    /// <summary>
    /// Human-friendly waiting-age for a request, e.g. "Waiting 35m" / "Waiting 1h 20m" / "Waiting 2d",
    /// so leads can prioritize the oldest requests first.
    /// </summary>
    /// <param name="requestedUtc">The instant the request was created.</param>
    /// <param name="referenceUtc">The instant to measure against; defaults to now.</param>
    /// <returns>The formatted waiting-age text.</returns>
    public static string FormatWaitingAge(DateTimeOffset requestedUtc, DateTimeOffset? referenceUtc = null)
    {
        var reference = referenceUtc ?? DateTimeOffset.UtcNow;
        var elapsed = reference - requestedUtc;
        if (elapsed < TimeSpan.Zero)
        {
            elapsed = TimeSpan.Zero;
        }

        if (elapsed.TotalMinutes < 1)
        {
            return "Waiting < 1m";
        }

        if (elapsed.TotalHours < 1)
        {
            return $"Waiting {(int)elapsed.TotalMinutes}m";
        }

        if (elapsed.TotalDays < 1)
        {
            return $"Waiting {(int)elapsed.TotalHours}h {(int)(elapsed.TotalMinutes % 60)}m";
        }

        return $"Waiting {(int)elapsed.TotalDays}d";
    }

    /// <summary>
    /// Whether a request has run past its due time: the store's sticky flag, or the clock having passed the due
    /// instant while the row is open (FR-014).
    /// </summary>
    /// <remarks>
    /// One rule, read by both the row's own minute tick and the list that builds the row. Seeding the row from the
    /// store's flag alone meant a request whose due time had already passed was drawn reading its lifecycle word
    /// while its ring already read <c>Overdue</c> - the word, the overrun and the column's theme disagreed with each
    /// other until the first tick ran a minute later. FR-014 requires all three to say the same thing, so the rule
    /// is applied once, when the row is built, and again on every tick.
    /// </remarks>
    /// <param name="isOverdueAtSource">The store's overdue flag for the row.</param>
    /// <param name="targetTimeUtc">The due instant, or <see langword="null"/> when unset.</param>
    /// <param name="referenceUtc">The instant to measure against; defaults to now.</param>
    /// <returns><see langword="true"/> when the request is overdue.</returns>
    public static bool IsOverdueAt(bool isOverdueAtSource, DateTimeOffset? targetTimeUtc, DateTimeOffset? referenceUtc = null) =>
        isOverdueAtSource || (targetTimeUtc is DateTimeOffset target && target <= (referenceUtc ?? DateTimeOffset.UtcNow));

    /// <summary>
    /// How far past its due time a request has run, as the ring's own text (FR-014, D29): the instant measured
    /// against the due instant, in the same minutes-and-hours shape the countdown uses, signed so it is never
    /// mistaken for time remaining.
    /// </summary>
    /// <remarks>
    /// Rounded up to the whole minute, matching <see cref="FormatRemainingTime"/> and
    /// <see cref="FormatTimeTaken"/>, so all three round by one rule. A row with no due instant has nothing to
    /// measure and yields nothing rather than a guessed figure.
    /// </remarks>
    /// <param name="targetTimeUtc">The due instant, or <see langword="null"/> when unset.</param>
    /// <param name="referenceUtc">The instant to measure against; defaults to now.</param>
    /// <returns>The formatted overrun, for example <c>-00:15</c>, or empty when there is nothing to measure.</returns>
    public static string FormatTimeOverrun(DateTimeOffset? targetTimeUtc, DateTimeOffset? referenceUtc = null)
    {
        if (targetTimeUtc is not DateTimeOffset target)
        {
            return string.Empty;
        }

        var overrun = (referenceUtc ?? DateTimeOffset.UtcNow) - target;
        if (overrun <= TimeSpan.Zero)
        {
            return string.Empty;
        }

        var totalMinutes = (int)Math.Ceiling(overrun.TotalMinutes);

        return $"-{totalMinutes / 60:00}:{totalMinutes % 60:00}";
    }

    /// <summary>
    /// The time a closed request took, as the ring's own text (FR-015): the close instant less the instant the
    /// request was created, in the same hours-and-minutes shape the countdown uses so the two read alike.
    /// </summary>
    /// <remarks>
    /// Rounded up to the whole minute, matching <see cref="FormatRemainingTime"/>, so a request that took 80
    /// minutes reads "01:20" and the two texts round by one rule. A row carrying no creation instant, or one that
    /// has not closed, has no time taken to show and yields nothing rather than a guessed figure.
    /// </remarks>
    /// <param name="closedUtc">The instant the request closed, or null while it is still open.</param>
    /// <param name="requestedUtc">The instant the request was created, or null when the row carries none.</param>
    /// <returns>The formatted time-taken text, or an empty string when there is nothing to measure.</returns>
    public static string FormatTimeTaken(DateTimeOffset? closedUtc, DateTimeOffset? requestedUtc)
    {
        if (closedUtc is not DateTimeOffset closed || requestedUtc is not DateTimeOffset requested)
        {
            return string.Empty;
        }

        var taken = closed - requested;
        if (taken <= TimeSpan.Zero)
        {
            return "00:00";
        }

        var totalMinutes = (int)Math.Ceiling(taken.TotalMinutes);
        return $"{totalMinutes / 60:00}:{totalMinutes % 60:00}";
    }

    /// <summary>
    /// Countdown text for a request's due time, e.g. "01:20", or "Overdue" once it has passed.
    /// </summary>
    /// <param name="targetTimeUtc">The due instant, or <see langword="null"/> when unset.</param>
    /// <param name="isOverdue">The store's overdue flag, which wins regardless of the clock.</param>
    /// <param name="referenceUtc">The instant to measure against; defaults to now.</param>
    /// <returns>The formatted remaining-time text.</returns>
    public static string FormatRemainingTime(DateTimeOffset? targetTimeUtc, bool isOverdue, DateTimeOffset? referenceUtc = null)
    {
        if (isOverdue)
        {
            return "Overdue";
        }

        if (targetTimeUtc is not DateTimeOffset target)
        {
            return "New";
        }

        var remaining = target - (referenceUtc ?? DateTimeOffset.UtcNow);
        if (remaining <= TimeSpan.Zero)
        {
            return "Overdue";
        }

        var totalMinutes = (int)Math.Ceiling(remaining.TotalMinutes);
        return totalMinutes <= 0 ? "Overdue" : $"{totalMinutes / 60:00}:{totalMinutes % 60:00}";
    }

    /// <summary>
    /// The time ring's fraction: the minutes the request has used, divided by the minutes it was allotted
    /// (FR-013).
    /// </summary>
    /// <remarks>
    /// The one place the fraction is worked out, so the ring, a caller and any test read the same rule. It measures
    /// to the close instant once there is one, which is what freezes a closed request's ring on the time taken
    /// rather than letting it grow after the fact (FR-015). The result is never negative and is not capped, so a
    /// request that ran past its allotment reads above 1 and the overrun is carried rather than hidden (FR-014).
    /// </remarks>
    /// <param name="requestedUtc">The instant the request was created, or null when the row carries none.</param>
    /// <param name="closedUtc">The instant the request closed, or null while it is still open.</param>
    /// <param name="allottedMinutes">
    /// The minutes the Item allots to the request. A value that cannot be measured — zero or less — has no scale to
    /// divide by and reads as zero.
    /// </param>
    /// <param name="referenceUtc">The instant to measure a live request against; defaults to now.</param>
    /// <returns>The used fraction of the allotment.</returns>
    public static double RingFraction(
        DateTimeOffset? requestedUtc,
        DateTimeOffset? closedUtc,
        int allottedMinutes,
        DateTimeOffset? referenceUtc = null)
    {
        if (requestedUtc is not DateTimeOffset requested || allottedMinutes <= 0)
        {
            return 0d;
        }

        var measuredTo = closedUtc ?? referenceUtc ?? DateTimeOffset.UtcNow;
        var elapsed = measuredTo - requested;
        if (elapsed <= TimeSpan.Zero)
        {
            return 0d;
        }

        return elapsed.TotalMinutes / allottedMinutes;
    }

    /// <summary>
    /// The friendly lifecycle word for a stored status: <c>Pending</c> to <c>Waiting</c>, <c>Accepted</c> to
    /// <c>In Progress</c>, <c>Completed</c> to <c>Done</c>, and <c>Canceled</c> in either spelling to
    /// <c>Cancelled</c> (FR-012).
    /// </summary>
    /// <param name="status">The stored lifecycle status, or null when the row carries none.</param>
    /// <returns>The display word, or empty when the stored status is not one the card knows.</returns>
    private static string LifecycleWordFor(string? status) =>
        status?.Trim().ToLowerInvariant() switch
        {
            "pending" => "Waiting",
            "accepted" => "In Progress",
            "completed" => "Done",
            "canceled" or "cancelled" => "Cancelled",
            _ => string.Empty,
        };

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged(string? propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? string.Empty));
}

/// <summary>
/// One attribute row on a waitlist card's detail area. A slot with no value is not a row: the label goes
/// with the value, so nothing renders an empty labelled shell (FR-002).
/// </summary>
public sealed class WaitlistField : INotifyPropertyChanged
{
    private string _label = string.Empty;
    private string _value = string.Empty;

    /// <summary>The row's label, or empty for a padding slot.</summary>
    public string Label
    {
        get => _label;
        set => SetField(ref _label, value);
    }

    /// <summary>The row's value, or empty when the request carries no source for it.</summary>
    public string Value
    {
        get => _value;
        set
        {
            if (SetField(ref _value, value))
            {
                OnPropertyChanged(nameof(HasValue));
            }
        }
    }

    /// <summary>An optional second value for the row.</summary>
    public string? SecondaryValue { get; set; }

    /// <summary>
    /// Whether this slot carries a value. The card templates hide both the label and the value when it is
    /// false, and notify when <see cref="Value"/> changes so a row that gains or loses its source re-renders.
    /// </summary>
    public bool HasValue => !string.IsNullOrWhiteSpace(Value);

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged(string? propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? string.Empty));
}
