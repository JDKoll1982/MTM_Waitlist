using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;

namespace MTM_Waitlist.Module_Waitlist.ViewModels;

public partial class WaitlistViewDetailViewModel : ObservableRecipient, INavigationAware
{
    private readonly INavigationService _navigationService;
    private readonly IBuildingSelectionService _buildingSelectionService;
    private readonly IWaitlistRequestService? _requestService;
    private readonly IWaitlistInventoryService? _inventoryService;
    private readonly IImageLocationService? _imageLocationService;
    private readonly IRequestItemConfigurationService? _itemConfigurationService;
    private readonly IRequestJobPartAvailabilityProvider? _jobAvailabilityProvider;
    private readonly Dictionary<string, RequestJobPartAvailability> _jobAvailabilityCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _currentEmployeeNumber;
    private readonly string _currentEmployeeName;
    private readonly string _currentRole;
    private DispatcherQueue? _dispatcherQueue;
    private readonly IWaitlistMessageSeenStore? _messageSeenStore;
    private IDisposable? _imageLocationSubscription;
    private int? _lastOrderId;

    /// <summary>
    /// How often the page re-reads the request and its history while it stays open, so activity another
    /// handler caused — an accept, a completion, a note — shows up without the viewer reloading.
    /// </summary>
    public static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(30);

    private DispatcherQueueTimer? _refreshTimer;

    /// <summary>
    /// The request this page is showing, remembered once it has been resolved so the page stays on the same
    /// request across a transition. Without it, completing or cancelling would resolve nothing on the reload
    /// — the request is no longer on the open list it was found on — and the page would blank instead of
    /// showing the state the action produced.
    /// </summary>
    private Guid? _resolvedRequestId;

    [ObservableProperty]
    public partial SampleOrder? Item
    {
        get; set;
    }

    [ObservableProperty]
    public partial string EmptyStateMessage
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial bool IsEmptyStateVisible
    {
        get; set;
    }

    [ObservableProperty]
    public partial bool IsItemPresent
    {
        get; set;
    }

    /// <summary>
    /// True when a section's read threw or reported an error. The failure is rendered in place, with
    /// <see cref="RetryLoadCommand"/>, and is never rendered as absence and never as the page-level store
    /// outage (<c>data-model.md</c> §1 invariants 1–3).
    /// </summary>
    [ObservableProperty]
    public partial bool IsSectionLoadFailed
    {
        get; set;
    }

    /// <summary>Localized explanation shown in place of the sections whose read failed.</summary>
    public string SectionFailureMessage => "Waitlist_Detail.BlockFailed.Message".GetLocalized();

    /// <summary>Localized label for the section-failure retry action.</summary>
    public string SectionFailureRetryText => "Waitlist_Detail.BlockFailed.Retry".GetLocalized();

    /// <summary>
    /// Re-runs the read that failed and rebuilds the sections. Bound to the retry action on the failure
    /// block, so a failure is recoverable without leaving the page.
    /// </summary>
    [RelayCommand]
    private void RetryLoad()
    {
        LoadItemAndSections();

        if (Item is not null && _inventoryService is not null)
        {
            var inventoryPart = ResolveInventoryPartNumber(Item);
            if (!string.IsNullOrWhiteSpace(inventoryPart))
            {
                _ = LoadInventoryAsync(inventoryPart);
            }
        }
    }

    /// <summary>
    /// True when the location grid has no rows to show (no part resolved, service absent, or
    /// every row was filtered by the on-hand &gt;= 1 / ignored-location rules).
    /// </summary>
    [ObservableProperty]
    public partial bool IsInventoryEmpty
    {
        get; set;
    } = true;

    public ObservableCollection<InventoryLocationRow> InventoryRows { get; } = new();

    public ICommand? SortInventoryCommand
    {
        get;
        private set;
    }

    private string? _inventorySortColumn;
    private bool _inventorySortDescending;

    public ObservableCollection<WaitlistDetailTemplateSection> TemplateSections { get; } = new();

    /// <summary>
    /// The Item's own declared fields, laid out two per row in declared order (FR-007). An Item's own fields
    /// belong to this page and never to the card; the grid is built from the Item's configuration row, so
    /// changing that row changes this page with no code change (FR-013, FR-015). One row per pair, and a field
    /// alone on its row takes the full width — the span comes from the declared field count, never from the
    /// Item's identity (§D9, FR-006).
    /// </summary>
    public ObservableCollection<WaitlistDetailFieldRow> DeclaredFieldRows { get; } = new();

    /// <summary>Whether the declared-field grid has anything to draw. A grid with nothing to show is hidden.</summary>
    public bool HasDeclaredFields => DeclaredFieldRows.Count > 0;

    /// <summary>
    /// Plain-language report of a configuration this page cannot use, or empty when the configuration read
    /// cleanly. A configuration that cannot be read says so here rather than quietly drawing fewer fields
    /// (FR-026).
    /// </summary>
    [ObservableProperty]
    public partial string DeclaredFieldsReport { get; private set; } = string.Empty;

    /// <summary>Whether there is a configuration problem to report.</summary>
    public bool HasDeclaredFieldsReport => !string.IsNullOrWhiteSpace(DeclaredFieldsReport);

    partial void OnDeclaredFieldsReportChanged(string value) => OnPropertyChanged(nameof(HasDeclaredFieldsReport));

    /// <summary>
    /// Whether the declared-field block is drawn at all: it carries either the Item's own fields or the reason
    /// they are not there. An empty block is hidden rather than drawn as a titled shell.
    /// </summary>
    public bool IsDeclaredFieldsSectionVisible => HasDeclaredFields || HasDeclaredFieldsReport;

    /// <summary>Localized heading of the declared-field grid (FR-022).</summary>
    public string DeclaredFieldsTitle => "Waitlist_Detail.DeclaredFields.Title".GetLocalized();

    public WaitlistViewDetailViewModel(
        INavigationService navigationService,
        IBuildingSelectionService buildingSelectionService,
        IImageLocationService? imageLocationService = null,
        IWaitlistRequestService? requestService = null,
        IWaitlistInventoryService? inventoryService = null,
        StartupState? startupState = null,
        DispatcherQueue? dispatcherQueue = null,
        IWaitlistMessageSeenStore? messageSeenStore = null,
        IRequestItemConfigurationService? itemConfigurationService = null,
        IRequestJobPartAvailabilityProvider? jobAvailabilityProvider = null)
    {
        ArgumentNullException.ThrowIfNull(navigationService);
        ArgumentNullException.ThrowIfNull(buildingSelectionService);

        _navigationService = navigationService;
        _buildingSelectionService = buildingSelectionService;
        _imageLocationService = imageLocationService;
        _requestService = requestService;
        _inventoryService = inventoryService;
        _itemConfigurationService = itemConfigurationService;
        _jobAvailabilityProvider = jobAvailabilityProvider;
        _currentEmployeeNumber = startupState?.EmployeeNumber?.Trim() ?? string.Empty;
        _currentEmployeeName = startupState?.EmployeeName?.Trim() ?? string.Empty;
        _currentRole = startupState?.CurrentRole?.Trim() ?? string.Empty;
        _dispatcherQueue = dispatcherQueue;
        _messageSeenStore = messageSeenStore;
        SortInventoryCommand = new RelayCommand<string>(SortInventoryBy);
    }

    /// <summary>
    /// Starts the periodic refresh. The first tick is one full interval away — the page has just been loaded,
    /// so there is nothing to refresh yet. A host to which no timer can be given leaves the page static rather
    /// than throwing out of navigation.
    /// </summary>
    /// <remarks>
    /// The dispatcher is taken from whatever the container handed over and, failing that, from the thread this
    /// runs on. That fallback matters: a factory resolved while navigation was still completing can find
    /// <c>GetForCurrentThread()</c> empty and hand over a null, which used to leave the page silently static
    /// instead of refreshing — the timer simply never started. This runs from <c>OnNavigatedTo</c>, which is the
    /// UI thread, so the fallback is the right queue whenever there is one at all.
    /// <para>
    /// A dispatcher being reachable is not the same as its being usable. A thread can report a queue whose
    /// timer cannot be constructed, and the queue lookup itself can throw where there is no dispatcher at all
    /// (the test host does exactly that). Both cases leave the page static and say so in the log, which is the
    /// difference between a page that does not refresh and a page nobody can tell is not refreshing.
    /// </para>
    /// </remarks>
    private void StartRefreshTimer()
    {
        if (_refreshTimer is null)
        {
            DispatcherQueueTimer timer;

            try
            {
                var dispatcher = _dispatcherQueue ?? DispatcherQueue.GetForCurrentThread();

                if (dispatcher is null)
                {
                    StartupDebugLog.Info("WaitlistDetail", "No dispatcher is available, so the request page will not refresh while it is open.");
                    return;
                }

                _dispatcherQueue = dispatcher;
                timer = dispatcher.CreateTimer();
            }
            catch (Exception ex)
            {
                StartupDebugLog.Error("WaitlistDetail", ex, "The request page could not get a refresh timer, so it will not refresh while it is open.");
                return;
            }

            timer.Interval = RefreshInterval;
            timer.IsRepeating = true;
            timer.Tick += OnRefreshTick;
            _refreshTimer = timer;
        }

        _refreshTimer.Start();
        StartupDebugLog.Info("WaitlistDetail", $"The request page will refresh every {RefreshInterval.TotalSeconds:0} seconds while it is open.");
    }

    private void StopRefreshTimer() => _refreshTimer?.Stop();

    private async void OnRefreshTick(DispatcherQueueTimer sender, object args)
    {
        try
        {
            await RefreshFromStoreAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("WaitlistDetail", ex, "The periodic refresh of the request page failed.");
        }
    }

    /// <summary>
    /// Re-sorts <see cref="InventoryRows"/> by a column (PartNumber / Quantity / Location).
    /// Clicking the same column again toggles ascending/descending; default ascending.
    /// </summary>
    public void SortInventoryBy(string? column)
    {
        var normalized = (column ?? string.Empty).Trim();
        if (normalized.Length == 0 || InventoryRows.Count == 0)
        {
            return;
        }

        var descending = string.Equals(_inventorySortColumn, normalized, StringComparison.OrdinalIgnoreCase)
            ? !_inventorySortDescending
            : false;
        _inventorySortColumn = normalized;
        _inventorySortDescending = descending;

        IEnumerable<InventoryLocationRow> sorted = normalized.ToLowerInvariant() switch
        {
            "partnumber" => descending
                ? InventoryRows.OrderByDescending(row => row.PartNumber, StringComparer.OrdinalIgnoreCase).ThenBy(row => row.Location, StringComparer.OrdinalIgnoreCase)
                : InventoryRows.OrderBy(row => row.PartNumber, StringComparer.OrdinalIgnoreCase).ThenBy(row => row.Location, StringComparer.OrdinalIgnoreCase),
            "quantity" => descending
                ? InventoryRows.OrderByDescending(row => row.OnHandQuantity)
                : InventoryRows.OrderBy(row => row.OnHandQuantity),
            _ => descending
                ? InventoryRows.OrderByDescending(row => row.Location, StringComparer.OrdinalIgnoreCase).ThenBy(row => row.PartNumber, StringComparer.OrdinalIgnoreCase)
                : InventoryRows.OrderBy(row => row.Location, StringComparer.OrdinalIgnoreCase).ThenBy(row => row.PartNumber, StringComparer.OrdinalIgnoreCase),
        };

        // Materialize BEFORE clearing the source collection (OrderBy is deferred).
        var materialized = sorted.ToArray();
        InventoryRows.Clear();
        foreach (var row in materialized)
        {
            InventoryRows.Add(row);
        }

        StartupDebugLog.Info("WaitlistDetail", $"SortInventoryBy '{normalized}' descending={descending}. Count={InventoryRows.Count}.");
    }

    // ── Note and history ──────────────────────────────────────────────────────────────────────────
    //
    // The accept / complete / release / cancel actions live on the list card, not here: the card is where a
    // handler is working through the queue. This page reads the request, lets a handler leave a note, and
    // shows what has happened to it.

    /// <summary>The message being written, seeded from the message the request carries.</summary>
    [ObservableProperty]
    public partial string NoteDraft { get; set; } = string.Empty;

    /// <summary>The stored message the draft was last seeded from, so an in-progress edit is recognisable.</summary>
    private string _storedNote = string.Empty;

    /// <summary>
    /// True while the box holds something the viewer has typed that is not stored yet. The periodic refresh
    /// must not seed over it: this page reloads every 30 seconds, and a tick landing mid-sentence used to erase
    /// what was being written.
    /// </summary>
    private bool _isNoteDirty;

    partial void OnNoteDraftChanged(string value) =>
        _isNoteDirty = !string.Equals((value ?? string.Empty).Trim(), _storedNote, StringComparison.Ordinal);

    /// <summary>Caption above the message box.</summary>
    public string NoteLabel => "Waitlist_Note.Label".GetLocalized();

    /// <summary>Placeholder shown in the note editor.</summary>
    public string NotePlaceholder => "Waitlist_Note.Placeholder".GetLocalized();

    /// <summary>Label for the send action.</summary>
    public string NoteSaveText => "Waitlist_Note.Save".GetLocalized();

    /// <summary>Plain-language outcome of the last note change, or empty when there is nothing to report.</summary>
    [ObservableProperty]
    public partial string NoteMessage { get; private set; } = string.Empty;

    /// <summary>Whether there is a note outcome to show.</summary>
    public bool HasNoteMessage => !string.IsNullOrWhiteSpace(NoteMessage);

    partial void OnNoteMessageChanged(string value) => OnPropertyChanged(nameof(HasNoteMessage));

    /// <summary>
    /// The request's audit trail, newest last, read straight from the service. A request with no readable
    /// history produces no rows, and the history block hides itself rather than drawing an empty shell.
    /// </summary>
    public ObservableCollection<WaitlistRequestAuditEntry> HistoryRows { get; } = new();

    /// <summary>Heading for the history block.</summary>
    public string HistoryTitle => "Waitlist_History.Title".GetLocalized();

    /// <summary>One-line description of the history block.</summary>
    public string HistorySummary => "Waitlist_History.Summary".GetLocalized();

    /// <summary>Whether there is a history to show.</summary>
    public bool HasHistory => HistoryRows.Count > 0;

    /// <summary>
    /// Stores the message and records who sent it in the request's history. Anyone signed in may send one: the
    /// box is how the floor tells whoever picks the request up what is going on, so it is deliberately not
    /// gated on the handler role. Every click answers — an empty box says there is nothing to send rather than
    /// looking broken, and an unchanged message says so rather than reporting a write that did not happen.
    /// </summary>
    [RelayCommand]
    private async Task SaveNoteAsync()
    {
        if (Item is not { RequestId: Guid requestId } || _requestService is null)
        {
            NoteMessage = "Waitlist_Note.Refused".GetLocalized();
            return;
        }

        var note = (NoteDraft ?? string.Empty).Trim();
        var stored = (Item.Note ?? string.Empty).Trim();

        if (note.Length == 0 && stored.Length == 0)
        {
            NoteMessage = "Waitlist_Note.Empty".GetLocalized();
            return;
        }

        if (string.Equals(note, stored, StringComparison.Ordinal))
        {
            // The service treats an identical note as no change and writes nothing, so claiming a save here
            // would promise a history entry that never appears.
            NoteMessage = "Waitlist_Note.Unchanged".GetLocalized();
            return;
        }

        try
        {
            var updated = await _requestService.UpdateNoteAsync(requestId, note, _currentEmployeeNumber, _currentEmployeeName);
            if (updated is null)
            {
                NoteMessage = "Waitlist_Note.Refused".GetLocalized();
                return;
            }

            // The write is done, so the draft is no longer an unsaved edit: settle it before the refresh runs,
            // otherwise the refresh would treat it as dirty and keep the pre-send text on screen.
            _storedNote = (updated.Note ?? string.Empty).Trim();
            _isNoteDirty = false;
            NoteMessage = "Waitlist_Note.Saved".GetLocalized();
            await RefreshFromStoreAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("WaitlistDetail", ex, "Sending the message failed; the request is unchanged.");
            NoteMessage = "Waitlist_Note.Refused".GetLocalized();
        }
    }

    /// <summary>Clears the note's outcome message.</summary>
    [RelayCommand]
    private void DismissNoteMessage() => NoteMessage = string.Empty;

    /// <summary>
    /// Rebuilds the page from the store and reloads the request's history. This is what keeps the page current:
    /// it runs after a note is saved and on the periodic refresh while the page stays open, so activity another
    /// handler caused arrives without the viewer doing anything.
    /// </summary>
    private async Task RefreshFromStoreAsync()
    {
        try
        {
            LoadItemAndSections();
            await LoadDeclaredFieldRowsAsync().ConfigureAwait(true);
            await LoadHistoryAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("WaitlistDetail", ex, "Refreshing the request page failed; the page keeps the previous state.");
        }
    }

    /// <summary>
    /// Seeds the message box from the request on screen — but never over an edit in progress. The page reloads
    /// every 30 seconds, and seeding unconditionally meant a tick could erase what the viewer was typing.
    /// </summary>
    private void ApplyItemState(SampleOrder? item)
    {
        _storedNote = (item?.Note ?? string.Empty).Trim();

        if (_isNoteDirty)
        {
            return;
        }

        NoteDraft = _storedNote;
    }

    /// <summary>
    /// Reads the request's history from the store and renders it. The read merges with whatever this session
    /// already recorded, so an action taken here and an action taken by someone else both appear (FR-013).
    /// </summary>
    /// <remarks>
    /// Opening the request is also what clears its new-message indicator on the list card, and the marker is the
    /// newest entry rather than "now" — so an entry that lands while the page is open re-flags the card instead
    /// of being silently swallowed by a page that was already looking at it.
    /// </remarks>
    public async Task LoadHistoryAsync(CancellationToken cancellationToken = default)
    {
        HistoryRows.Clear();

        if (Item is not { RequestId: Guid requestId } || _requestService is null)
        {
            RaiseHistoryChanged();
            return;
        }

        var entries = await _requestService.LoadAuditTrailAsync(requestId, cancellationToken).ConfigureAwait(true);

        foreach (var entry in entries)
        {
            HistoryRows.Add(entry);
        }

        if (_messageSeenStore is not null)
        {
            var newest = entries.Count > 0 ? entries[^1].OccurredUtc : DateTimeOffset.UtcNow;
            await _messageSeenStore.MarkSeenAsync(requestId, newest, cancellationToken).ConfigureAwait(true);
        }

        RaiseHistoryChanged();
    }

    /// <summary>
    /// Whether the request has no history at all. The block says so rather than disappearing, so "nothing has
    /// happened yet" stays distinguishable from "the history could not be read".
    /// </summary>
    public bool IsHistoryEmpty => HistoryRows.Count == 0;

    /// <summary>Localized line shown when a request genuinely has no history yet.</summary>
    public string HistoryEmptyText => "Waitlist_History.Empty".GetLocalized();

    private void RaiseHistoryChanged()
    {
        OnPropertyChanged(nameof(HasHistory));
        OnPropertyChanged(nameof(IsHistoryEmpty));
    }

    public void OnNavigatedTo(object parameter)
    {
        _lastOrderId = parameter switch
        {
            int intId => intId,
            long longId when longId <= int.MaxValue && longId >= int.MinValue => (int)longId,
            _ => (int?)null
        };

        if (_imageLocationSubscription is null
            && _imageLocationService is not null
            && _imageLocationService.IsInitialized)
        {
            _imageLocationSubscription = _imageLocationService.SubscribeToImageLocationChanges(OnImageLocationChanged);
        }

        // A fresh visit re-reads the requesting job: the die it carries can have moved since this page last showed
        // it, and the card must not outlive the value it was built from.
        _jobAvailabilityCache.Clear();

        LoadItemAndSections();

        // The declared-field grid is a configuration read, so it is loaded alongside the request's own rows.
        _ = LoadDeclaredFieldRowsAsync();

        // The requesting job is a store read of its own — the die and its location come from it — so it is
        // requested here and the page rebuilds when it lands.
        _ = EnsureJobAvailabilityAsync();

        // The history is a store read, so it is awaited separately from the synchronous row/section build.
        _ = LoadHistoryAsync();
        StartRefreshTimer();

        if (_inventoryService is not null && Item is not null)
        {
            var inventoryPart = ResolveInventoryPartNumber(Item);
            if (!string.IsNullOrWhiteSpace(inventoryPart))
            {
                _ = LoadInventoryAsync(inventoryPart);
            }
        }
    }

    /// <summary>
    /// Resolves the request row and rebuilds the sections as one unit, so a read that throws produces the
    /// Failed block state rather than a half-built page or a failure disguised as absence.
    /// </summary>
    private void LoadItemAndSections()
    {
        try
        {
            ResolveItem();
            LoadTemplateSections();
            IsSectionLoadFailed = false;
        }
        catch (Exception ex)
        {
            Item = null;
            TemplateSections.Clear();
            IsSectionLoadFailed = true;
            IsItemPresent = false;
            IsEmptyStateVisible = false;
            EmptyStateMessage = string.Empty;
            StartupDebugLog.Error(
                "WaitlistDetail",
                ex,
                "The request read behind the detail sections failed; the failure is rendered in place with a retry.");
        }
    }

    /// <summary>
    /// Resolves the request on screen. The detail page resolves only real submitted requests (the list
    /// surfaces each request as a <see cref="SampleOrder"/> whose Id is the hash of the request Guid).
    /// </summary>
    private void ResolveItem()
    {
        Item = null;

        if (_requestService is null)
        {
            ApplyItemState(null);
            IsItemPresent = false;
            IsEmptyStateVisible = true;
            EmptyStateMessage = "No waitlist request or coil details are available to show.";
            return;
        }

        // The request this page already resolved wins, so a transition that takes the request off the open
        // list shows that new state rather than blanking the page the user is standing on.
        WaitlistRequest? match = _resolvedRequestId is Guid known
            ? _requestService.GetRequest(known)
            : null;

        if (match is null && _lastOrderId is int orderId)
        {
            var requests = _requestService.GetActiveRequests(_buildingSelectionService.SelectedBuilding);
            match = requests.FirstOrDefault(request => request.Id.GetHashCode() == orderId);
        }

        if (match is not null)
        {
            if (_resolvedRequestId != match.Id)
            {
                // A different request means the box starts clean: an unsaved draft belongs to the request it was
                // written on, not to whatever the viewer opens next.
                _isNoteDirty = false;
            }

            _resolvedRequestId = match.Id;

            // The requesting job travels with the card: the die's number and where the die is are job values the
            // request never stored, so without the job the die rows on this page cannot be drawn (FR-053).
            Item = WaitlistViewViewModel.CreateSessionOrder(match, ResolveJobAvailability(match.WorkCenter));
        }

        ApplyItemState(Item);

        IsItemPresent = Item is not null;
        IsEmptyStateVisible = Item is null;
        EmptyStateMessage = Item is null
            ? "No waitlist request or coil details are available to show."
            : string.Empty;
    }

    public void OnNavigatedFrom()
    {
        // The page is no longer on screen, so it must stop reading the store every 30 seconds.
        StopRefreshTimer();
        _imageLocationSubscription?.Dispose();
        _imageLocationSubscription = null;
    }

    /// <summary>The coil part keyed in the receiving_history seed for the sample coil (MMC0001000).</summary>
    private const string CoilReceivingPartId = "MMC0001000";

    [RelayCommand]
    private void Back()
    {
        _navigationService.GoBack();
    }

    private void LoadTemplateSections()
    {
        TemplateSections.Clear();

        if (Item is null)
        {
            return;
        }

        // One section shape for every Item (FR-006, FR-007). Nothing below branches on *which* Item is on
        // screen: the two lines are the Item's own umbrella phrase and identifier, resolved from the code the
        // request was stored with, and the remaining rows are what the request itself carries. An Item's own
        // declared fields are the configuration-driven grid that lands on this page; until that grid is read,
        // this page states only what it can support and never draws an empty titled block.
        AddSection(
            "Request",
            "What this request asks for, and what the request itself carries.",
            ("Item", Item.Title),
            ("Identifier", Item.Subtitle),
            ("Request details", FieldValue(Item, "Request details")),
            ("Work order", FieldValue(Item, "Work order")));

        AddSection(
            "Request context",
            "Who asked, where it goes, and how urgent it is.",
            ("Work center", Item.RequestedPressName),
            ("Requesting user", Item.RequestedByName),
            ("Employee number", Item.RequesterEmployeeNumber),
            ("Remaining time", Item.RemainingTimeText));
    }

    /// <summary>
    /// Builds the declared-field grid from the Item's stored configuration, in declared order (FR-007, FR-013).
    /// A declared field whose value no source produces is not drawn — the page states only what the request
    /// truthfully carries (contract C2, FR-002). A read that fails leaves the grid empty and says so in the log
    /// rather than inventing rows.
    /// </summary>
    private async Task LoadDeclaredFieldRowsAsync()
    {
        DeclaredFieldRows.Clear();
        DeclaredFieldsReport = string.Empty;
        OnPropertyChanged(nameof(HasDeclaredFields));
        OnPropertyChanged(nameof(IsDeclaredFieldsSectionVisible));

        var item = Item;
        if (item is null || _itemConfigurationService is null || string.IsNullOrWhiteSpace(item.ItemCode))
        {
            return;
        }

        try
        {
            var configuration = await _itemConfigurationService
                .GetConfigurationAsync(item.ItemCode)
                .ConfigureAwait(true);

            if (!configuration.IsAvailable)
            {
                // The Item's configuration cannot be used, so the page states that plainly instead of drawing
                // a grid with fields missing from it (FR-026).
                DeclaredFieldsReport = configuration.UnavailableMessage;
                return;
            }

            var request = ResolveRequest(item);
            var jobAvailability = ResolveJobAvailability(request?.WorkCenter);

            var declared = configuration.DetailFields
                .OrderBy(field => field.Order)
                .Select(field => new WaitlistDetailTemplateField
                {
                    Label = field.Label,
                    ValueType = field.ValueType,
                    Value = ResolveDeclaredFieldValue(field, item, request, jobAvailability) ?? string.Empty,
                })
                .Where(field => !string.IsNullOrWhiteSpace(field.Label) && !string.IsNullOrWhiteSpace(field.Value))
                .ToList();

            foreach (var row in WaitlistDetailFieldRow.RowsFor(declared))
            {
                DeclaredFieldRows.Add(row);
            }
        }
        catch (Exception ex)
        {
            DeclaredFieldRows.Clear();
            DeclaredFieldsReport = SectionFailureMessage;
            StartupDebugLog.Error(
                "WaitlistDetail",
                ex,
                "The declared-field grid could not be read; the page reports the failure rather than drawing a substitute.");
        }
        finally
        {
            OnPropertyChanged(nameof(HasDeclaredFields));
            OnPropertyChanged(nameof(IsDeclaredFieldsSectionVisible));
        }
    }

    /// <summary>
    /// The value a declared field renders, from a source the page actually holds. <c>answer</c> is the one
    /// answer the flow captured; <c>job</c> is a value the <b>requesting job</b> carries and the request never
    /// stored — the die whose number and location a die request is about; every other source is looked up by the
    /// field's own label among the values the request carries. A label that nothing carries yields nothing, so the
    /// row is not drawn — never a substituted value, never a blank standing in for one.
    /// </summary>
    private static string? ResolveDeclaredFieldValue(
        RequestItemFieldDefinition field,
        SampleOrder item,
        WaitlistRequest? request,
        RequestJobPartAvailability? jobAvailability)
    {
        if (string.Equals(field.Source, RequestItemFieldDefinition.Sources.Answer, StringComparison.OrdinalIgnoreCase))
        {
            return request?.InputValue;
        }

        if (string.Equals(field.Source, RequestItemFieldDefinition.Sources.Job, StringComparison.OrdinalIgnoreCase))
        {
            return RequestJobFieldValues.Resolve(field.Label, jobAvailability);
        }

        return FieldValue(item, field.Label);
    }

    /// <summary>
    /// The requesting job as this page last read it. Until the read lands the page draws only the rows the request
    /// itself carries, and it is rebuilt once the job arrives (FR-053).
    /// </summary>
    private RequestJobPartAvailability ResolveJobAvailability(string? workCenter)
    {
        var key = workCenter?.Trim() ?? string.Empty;
        return key.Length > 0 && _jobAvailabilityCache.TryGetValue(key, out var cached)
            ? cached
            : RequestJobPartAvailability.None;
    }

    /// <summary>
    /// Reads the requesting job once per work centre and rebuilds the page when it arrives.
    /// </summary>
    /// <remarks>
    /// The read is deliberately not fatal. The die's number and its location are job values, so a job that cannot
    /// be read leaves the rows the request itself carries rather than replacing the whole page with a failure: the
    /// request on screen is still worth reading, and the failure is logged.
    /// </remarks>
    private async Task EnsureJobAvailabilityAsync()
    {
        if (_jobAvailabilityProvider is null)
        {
            return;
        }

        var workCenter = _resolvedRequestId is Guid known
            ? _requestService?.GetRequest(known)?.WorkCenter
            : null;

        var key = workCenter?.Trim() ?? string.Empty;
        if (key.Length == 0 || _jobAvailabilityCache.ContainsKey(key))
        {
            return;
        }

        RequestJobPartAvailability availability;
        try
        {
            availability = await _jobAvailabilityProvider.GetAvailabilityAsync(key).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error(
                "WaitlistDetail",
                ex,
                $"Reading the job for work center '{key}' failed; the page keeps the rows the request itself carries.");
            return;
        }

        _jobAvailabilityCache[key] = availability;

        // The job's values shape both lines and the declared job rows, so the page is rebuilt now that they are
        // known rather than left showing a die request with no die on it.
        LoadItemAndSections();
        await LoadDeclaredFieldRowsAsync().ConfigureAwait(true);
    }
    private void AddRequestContextSection(SampleOrder item, string title, string summary)
        => AddSection(
            title,
            summary,
            ("Requested by", item.RequestedByName),
            ("Press or resource", item.RequestedPressName),
            ("Remaining time", item.RemainingTimeText));

    /// <summary>
    /// The label-to-value lookup used by every section loader. It has no fallback: a label with no source
    /// returns <see langword="null" /> and its row is not rendered (FR-002, contract C2).
    /// </summary>
    private static string? FieldValue(SampleOrder item, string label)
        => item.Fields.FirstOrDefault(field => string.Equals(field.Label, label, StringComparison.OrdinalIgnoreCase))?.Value;

    /// <summary>
    /// Resolves the underlying <see cref="WaitlistRequest"/> for the row on screen (via its
    /// <see cref="SampleOrder.RequestId"/>), or null when the row carries no request id or the service is absent.
    /// </summary>
    private WaitlistRequest? ResolveRequest(SampleOrder item)
    {
        if (item.RequestId is Guid requestId && _requestService is not null)
        {
            return _requestService.GetRequest(requestId);
        }

        return null;
    }

    /// <summary>
    /// Adds a section, dropping every row whose value has no source and the section itself when no row
    /// survives. A block with nothing to show is hidden rather than drawn as an empty titled shell
    /// (<c>data-model.md</c> §1, invariant 2).
    /// </summary>
    private void AddSection(string title, string summary, params (string Label, string? Value)[] fields)
    {
        var section = new WaitlistDetailTemplateSection
        {
            Title = title,
            Summary = summary
        };

        foreach (var (label, value) in fields)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                section.Fields.Add(new WaitlistDetailTemplateField
                {
                    Label = label,
                    Value = value.Trim()
                });
            }
        }

        if (section.Fields.Count > 0)
        {
            TemplateSections.Add(section);
        }
    }

    /// <summary>
    /// Loads the filtered inventory-location rows for a part into <see cref="InventoryRows"/>.
    /// </summary>
    public async Task LoadInventoryAsync(string? partNumber, CancellationToken cancellationToken = default)
    {
        InventoryRows.Clear();
        var normalizedPart = (partNumber ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedPart) || _inventoryService is null)
        {
            IsInventoryEmpty = true;
            return;
        }

        var rows = await _inventoryService.GetInventoryLocationRowsAsync(normalizedPart, cancellationToken).ConfigureAwait(false);
        foreach (var row in rows)
        {
            InventoryRows.Add(row);
        }

        IsInventoryEmpty = InventoryRows.Count == 0;
        StartupDebugLog.Info("WaitlistDetail", $"LoadInventoryAsync completed. Part='{normalizedPart}', Rows={InventoryRows.Count}.");
    }

    /// <summary>
    /// Best-effort part/inventory token for an order: prefers a real Part number/Part field,
    /// then the coil's "Requested coil" identifier, so the coil detail grid has something to
    /// query (mock inventory is keyed per part token).
    /// </summary>
    public static string? ResolveInventoryPartNumber(SampleOrder? item)
    {
        if (item is null)
        {
            return null;
        }

        foreach (var label in new[] { "Part number", "Part", "Requested coil" })
        {
            var candidate = FieldValue(item, label)?.Trim();
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            return candidate;
        }

        return null;
    }

    private void OnImageLocationChanged(ImageLocationChangedEventArgs args)
    {
        if (Item is null || _imageLocationService is null || !_imageLocationService.IsInitialized)
        {
            return;
        }

        _ = RefreshResolvedPathsAsync(Item);
    }

    private async Task RefreshResolvedPathsAsync(SampleOrder item)
    {
        if (_imageLocationService is null || !_imageLocationService.IsInitialized)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(item.ItemCode))
        {
            item.ResolvedImagePath = await _imageLocationService
                .ResolveRequestItemImagePathAsync(item.ItemCode)
                .ConfigureAwait(false);
        }

        if (item.WorkCenterCatalogId.HasValue)
        {
            item.WorkCenterImagePath = await _imageLocationService
                .ResolveWorkCenterImagePathAsync(item.WorkCenterCatalogId.Value.ToString())
                .ConfigureAwait(false);
        }

        OnPropertyChanged(nameof(Item));
    }
}
