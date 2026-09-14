using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;

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
using MTM_Waitlist.Module_Waitlist.Helpers;
using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Waitlist.ViewModels;

public partial class WaitlistViewViewModel : ObservableRecipient, INavigationAware
{
    private readonly INavigationService _navigationService;
    private readonly MTM_Waitlist.Module_Waitlist.Services.IWaitlistRequestService _waitlistRequestService;
    private readonly IImageLocationService? _imageLocationService;
    private readonly IBuildingSelectionService _buildingSelectionService;
    private DispatcherQueue? _dispatcherQueue;
    private readonly string _currentRequesterEmployeeNumber;
    private readonly string _currentEmployeeName;
    private readonly string _currentRole;
    private readonly MTM_Waitlist.Module_Waitlist.Services.IWaitlistRequestActionPrompt? _actionPrompt;
    private readonly IUrgencyDeadlineService? _urgencyDeadlineService;
    private readonly MTM_Waitlist.Module_Waitlist.Services.IWaitlistMessageSeenStore? _messageSeenStore;
    private readonly IWaitlistSortPreferenceService? _sortPreferenceService;

    /// <summary>
    /// The order this viewer's list is shown in, resolved from their remembered choice the first time the list
    /// loads. Null until then, so <see cref="SortOrder"/> answers with the default rather than an empty string
    /// before anything has been read.
    /// </summary>
    private string? _sortOrder;

    /// <summary>
    /// Allotted time per Item for the load in flight. The urgency of every row needs one, and every row of an
    /// Item needs the same one, so a list of N rows costs one lookup per distinct Item rather than N. Cleared
    /// at the start of each load so a settings change is picked up.
    /// </summary>
    private readonly Dictionary<string, TimeSpan> _maxAllottedCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The documented default allotted time, used when no deadline service is supplied or the Item has no configured allotment.</summary>
    private static readonly TimeSpan DefaultMaxAllotted = TimeSpan.FromMinutes(UrgencySettingsService.DefaultMinutes);

    /// <summary>
    /// The ordering key for a row whose due time cannot be derived. It is not overdue and carries the longest
    /// possible remaining time, so it sorts after every row that does carry an urgency without the ordering
    /// rule pretending it is something it is not.
    /// </summary>
    private static readonly UrgencyState NoUrgency = new() { Remaining = TimeSpan.MaxValue, IsOverdue = false };

    private IDisposable? _imageLocationSubscription;
    private long _refreshVersion;
    private bool _isSubscribed;

    public ObservableCollection<SampleOrder> Source { get; } = new ObservableCollection<SampleOrder>();
    public ObservableCollection<SampleOrder> SearchSuggestions { get; } = new();

    [ObservableProperty]
    public partial bool IsWaitlistEmpty { get; private set; } = true;

    /// <summary>
    /// When true, the list is narrowed to the signed-in user's own submitted requests
    /// (the "My Requests" quick view). Toggling reloads the current building's list.
    /// </summary>
    [ObservableProperty]
    public partial bool ShowMyRequestsOnly { get; set; }

    partial void OnShowMyRequestsOnlyChanged(bool value)
    {
        StartupDebugLog.Info("Waitlist", $"My-Requests filter {(value ? "enabled" : "disabled")} for requester '{_currentRequesterEmployeeNumber}'.");
        _ = LoadOrdersAsync(_buildingSelectionService.SelectedBuilding);
    }

    public string SelectedBuilding => _buildingSelectionService.SelectedBuilding;

    /// <summary>
    /// The order the list is currently shown in — the viewer's remembered choice, or the most-urgent default
    /// when they have never made one (FR-010, FR-011).
    /// </summary>
    public string SortOrder => _sortOrder ?? WaitlistSortOrder.MostUrgent;

    public WaitlistViewViewModel(
        INavigationService navigationService,
        IBuildingSelectionService buildingSelectionService,
        MTM_Waitlist.Module_Waitlist.Services.IWaitlistRequestService waitlistRequestService,
        IImageLocationService? imageLocationService = null,
        DispatcherQueue? dispatcherQueue = null,
        StartupState? startupState = null,
        IStoreAvailabilityTracker? storeAvailabilityTracker = null,
        MTM_Waitlist.Module_Waitlist.Services.IWaitlistRequestActionPrompt? actionPrompt = null,
        IUrgencyDeadlineService? urgencyDeadlineService = null,
        MTM_Waitlist.Module_Waitlist.Services.IWaitlistMessageSeenStore? messageSeenStore = null,
        IWaitlistSortPreferenceService? sortPreferenceService = null)
    {
        ArgumentNullException.ThrowIfNull(navigationService);
        ArgumentNullException.ThrowIfNull(buildingSelectionService);
        ArgumentNullException.ThrowIfNull(waitlistRequestService);

        _navigationService = navigationService;
        _buildingSelectionService = buildingSelectionService;
        _waitlistRequestService = waitlistRequestService;
        _imageLocationService = imageLocationService;
        _dispatcherQueue = dispatcherQueue;
        _currentRequesterEmployeeNumber = startupState?.EmployeeNumber?.Trim() ?? string.Empty;
        _currentEmployeeName = startupState?.EmployeeName?.Trim() ?? string.Empty;
        _currentRole = startupState?.CurrentRole?.Trim() ?? string.Empty;
        _actionPrompt = actionPrompt;
        _urgencyDeadlineService = urgencyDeadlineService;
        _messageSeenStore = messageSeenStore;
        _sortPreferenceService = sortPreferenceService;

        // This screen's own internal-store unavailable state (FR-021): the waitlist list reads
        // mtm_waitlist live, so a failure is reported here, with a retry that re-runs this screen's load.
        // It is never replaced by sample rows and it is not an app-wide banner.
        StoreUnavailable = new InternalStoreUnavailableState(
            MySqlDatabaseTarget.MtmWaitlist,
            _ => LoadOrdersAsync(_buildingSelectionService.SelectedBuilding),
            storeAvailabilityTracker);
        StoreUnavailable.PropertyChanged += OnStoreUnavailablePropertyChanged;

        Source.CollectionChanged += OnSourceCollectionChanged;
        IsWaitlistEmpty = Source.Count == 0;
    }

    /// <summary>This screen's internal-store unavailable state, shown in this screen and nowhere else.</summary>
    public InternalStoreUnavailableState StoreUnavailable { get; }

    /// <summary>Localized heading for <see cref="StoreUnavailable"/>.</summary>
    public string StoreUnavailableTitle => "Store_Unavailable.Title".GetLocalized();

    /// <summary>Localized, detail-bearing sentence for <see cref="StoreUnavailable"/>.</summary>
    public string StoreUnavailableMessage => string.Format(
        CultureInfo.CurrentCulture,
        "Store_Unavailable.Message".GetLocalized(),
        StoreUnavailable.StoreName,
        StoreUnavailable.LastAttemptUtc?.ToLocalTime().ToString("g", CultureInfo.CurrentCulture) ?? "-",
        StoreUnavailable.RetryCount,
        StoreUnavailable.NextRetryUtc?.ToLocalTime().ToString("t", CultureInfo.CurrentCulture) ?? "-");

    /// <summary>Localized label for the manual retry action.</summary>
    public string StoreUnavailableRetryText => "Store_Unavailable.Retry".GetLocalized();

    private void OnStoreUnavailablePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(StoreUnavailableMessage));
    }

    /// <summary>
    /// True when the signed-in viewer's role may handle requests at all (Material Handler or above). The
    /// screen uses it only to decide what to offer; the service re-checks it on every action, so a screen
    /// built from stale data can never widen what the viewer is allowed to do (FR-018).
    /// </summary>
    public bool CanHandleRequests => RequestActionPolicy.CanViewerHandleRequests(_currentRole);

    /// <summary>
    /// Plain-language explanation of the last refused action, or empty when nothing was refused. A refusal
    /// is reported here rather than in a dialog so it never blocks the list, and it is never silent (FR-019).
    /// </summary>
    [ObservableProperty]
    public partial string ActionRefusalMessage { get; private set; } = string.Empty;

    /// <summary>Whether there is a refusal to show.</summary>
    public bool HasActionRefusalMessage => !string.IsNullOrWhiteSpace(ActionRefusalMessage);

    partial void OnActionRefusalMessageChanged(string value) => OnPropertyChanged(nameof(HasActionRefusalMessage));

    /// <summary>Dismisses the refusal message.</summary>
    [RelayCommand]
    private void DismissActionRefusal() => ActionRefusalMessage = string.Empty;

    /// <summary>
    /// Claims an available request for the signed-in handler. Offered only while the row's gate says so.
    /// </summary>
    /// <remarks>
    /// Two handlers may try to claim the same request at the same moment. The store settles it — the first
    /// claim wins and the second gets nothing back — so the loser must be told plainly that someone else got
    /// there first and that they should pick another request. That is a warning the user has to acknowledge,
    /// not a quiet line on a list they have already scrolled past. The winner's claim is never touched here:
    /// this method only ever reports what the store decided.
    /// </remarks>
    [RelayCommand]
    private async Task AcceptRequestAsync(SampleOrder? order)
    {
        if (order is not { RequestId: Guid requestId, CanAccept: true })
        {
            return;
        }

        WaitlistRequest? updated;
        try
        {
            // No ConfigureAwait(false) in any of these action paths: the continuation sets bindable state
            // (the refusal message) and may open a dialog, and both are UI-thread-only. Resuming on a pool
            // thread throws RPC_E_WRONG_THREAD (0x8001010E), which surfaces as the action doing nothing.
            updated = await _waitlistRequestService
                .AcceptAsync(requestId, _currentRequesterEmployeeNumber, _currentEmployeeName);
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("Waitlist", ex, "Claiming the request failed; the row is unchanged.");
            ActionRefusalMessage = "Waitlist_Action.Refused.Unexpected".GetLocalized();
            return;
        }

        if (updated is not null)
        {
            ActionRefusalMessage = string.Empty;
            await MarkActivitySeenAsync(requestId, DateTimeOffset.UtcNow);
            return;
        }

        // The store refused the claim. Either another handler got there first, or the request has left the
        // list; both leave this handler needing to choose something else.
        var message = _waitlistRequestService.GetRequest(requestId) is null
            ? "Waitlist_Action.Refused.AcceptGone".GetLocalized()
            : "Waitlist_Action.Refused.AcceptTaken".GetLocalized();

        ActionRefusalMessage = message;
        await ShowActionWarningAsync("Waitlist_Action.AcceptRefusedTitle".GetLocalized(), message);
    }

    /// <summary>
    /// Shows an action warning the user must acknowledge. A warning that cannot be shown (a headless host, or
    /// a dialog that throws) must not lose the report: the inline refusal is already set by the caller.
    /// </summary>
    private async Task ShowActionWarningAsync(string title, string message)
    {
        if (_actionPrompt is null)
        {
            return;
        }

        try
        {
            await _actionPrompt
                .ShowWarningAsync(title, message, "Waitlist_Action.Dismiss".GetLocalized());
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("Waitlist", ex, "The action warning could not be shown; the refusal is still reported on the list.");
        }
    }

    /// <summary>
    /// Marks the viewer's own claimed request Done. Offered only to the recorded assignee; refused in plain
    /// language when the store disagrees with the screen (FR-005/FR-006).
    /// </summary>
    [RelayCommand]
    private async Task CompleteRequestAsync(SampleOrder? order)
    {
        if (order is not { RequestId: Guid requestId, CanCompleteOrRelease: true })
        {
            return;
        }

        await RunHandlerActionAsync(
            requestId,
            "Waitlist_Action.Refused.CompleteOrRelease".GetLocalized(),
            () => _waitlistRequestService.CompleteAsync(requestId, _currentRequesterEmployeeNumber, _currentEmployeeName));
    }

    /// <summary>
    /// Returns the viewer's own claimed request to the open list so another handler can take it. A release is
    /// not a cancellation: the request comes back as available with no assignee (FR-007).
    /// </summary>
    /// <remarks>
    /// The card draws this as <b>Give back</b>, beside Complete and gated by the same flag (FR-047). That is a
    /// recorded <b>supersession</b>: the earlier decision — that the card's two-button action area left this
    /// command with no surface, and that it was kept on the view model only because nothing else can put a
    /// claimed request back on the open list — did not survive contact with the floor, because a handler who
    /// cannot work a request had no control that returned it to the queue. The reason is kept here rather than
    /// deleted, and `specs/003-waitlist-handler-fulfilment/contracts/action-contracts.md` C1 and C7 carry the
    /// same amendment.
    /// </remarks>
    [RelayCommand]
    private async Task ReleaseRequestAsync(SampleOrder? order)
    {
        if (order is not { RequestId: Guid requestId, CanCompleteOrRelease: true })
        {
            return;
        }

        await RunHandlerActionAsync(
            requestId,
            "Waitlist_Action.Refused.CompleteOrRelease".GetLocalized(),
            () => _waitlistRequestService.ReleaseAsync(requestId, _currentRequesterEmployeeNumber, _currentEmployeeName));
    }

    /// <summary>
    /// Runs a handler action against the store and reports its outcome: a null answer means the store
    /// refused (someone else took the request, or it is no longer yours to act on), which is reported rather
    /// than swallowed. The list itself refreshes through <see cref="IWaitlistRequestService.RequestsChanged"/>,
    /// which every transition already raises, so the new state arrives without a manual reload (FR-023).
    /// </summary>
    private async Task RunHandlerActionAsync(Guid requestId, string refusalMessage, Func<Task<WaitlistRequest?>> action)
    {
        try
        {
            var updated = await action();
            ActionRefusalMessage = updated is null ? refusalMessage : string.Empty;

            if (updated is not null)
            {
                await MarkActivitySeenAsync(requestId, DateTimeOffset.UtcNow);
            }
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("Waitlist", ex, "A waitlist request action failed; the row is unchanged.");
            ActionRefusalMessage = "Waitlist_Action.Refused.Unexpected".GetLocalized();
        }
    }

    /// <summary>
    /// Stops the request in front of the viewer, after confirming and taking a reason. Two viewers may do this,
    /// and the right rule is chosen from the row rather than from the button: the person who raised a request
    /// may withdraw it while it is still waiting, and the handler who accepted one may back out of it. A
    /// dismissed prompt performs no action at all (FR-010/FR-011).
    /// </summary>
    [RelayCommand]
    private async Task CancelRequestAsync(SampleOrder? order)
    {
        if (order is not { RequestId: Guid requestId, CanCancelRequest: true })
        {
            return;
        }

        if (_actionPrompt is null)
        {
            // No prompt to ask with means no confirmation can be obtained, so the action must not run and
            // must not pass silently either.
            ActionRefusalMessage = "Waitlist_Action.Refused.Unexpected".GetLocalized();
            return;
        }

        string? reason;
        try
        {
            reason = await _actionPrompt.RequestCancellationReasonAsync(
                "Waitlist_Action.Cancel.DialogTitle".GetLocalized(),
                "Waitlist_Action.Cancel.DialogMessage".GetLocalized(),
                "Waitlist_Action.Cancel.Confirm".GetLocalized(),
                "Waitlist_Action.Cancel.Dismiss".GetLocalized());
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("Waitlist", ex, "The cancel confirmation could not be shown; nothing was cancelled.");
            ActionRefusalMessage = "Waitlist_Action.Refused.Unexpected".GetLocalized();
            return;
        }

        if (reason is null)
        {
            // The viewer backed out. Nothing happened, so there is nothing to report.
            return;
        }

        var isRequester = IsRequesterOrder(order, _currentRequesterEmployeeNumber);

        try
        {
            bool cancelled;
            if (isRequester && CanRequesterCancel(order))
            {
                var result = await _waitlistRequestService
                    .CancelOwnRequestAsync(requestId, _currentRequesterEmployeeNumber, reason);

                cancelled = result.Status == WaitlistRequestCancelStatus.Success;
            }
            else
            {
                // The assignee backing out of a request they took. The service validates who may do this and
                // from which state; the screen only asks.
                cancelled = await _waitlistRequestService
                    .TransitionStatusAsync(requestId, "Canceled", reason, _currentRequesterEmployeeNumber);
            }

            ActionRefusalMessage = cancelled
                ? string.Empty
                : "Waitlist_Action.Refused.Cancel".GetLocalized();

            if (cancelled)
            {
                await MarkActivitySeenAsync(requestId, DateTimeOffset.UtcNow);
            }
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("Waitlist", ex, "Cancelling the request failed; the request is unchanged.");
            ActionRefusalMessage = "Waitlist_Action.Refused.Unexpected".GetLocalized();
        }
    }

    /// <summary>
    /// Gives every row its gate flags, its carried action commands, its accessible action names and its
    /// new-message indicator. This is the screen's *view* of the rules the service enforces; a flag is never
    /// true where the service would refuse, and a button is drawn only while its flag is true.
    /// </summary>
    /// <remarks>
    /// The card draws a primary button — Accept while the request is available, Complete once the viewer has
    /// claimed it — Give back beside it, and Cancel. Cancel is offered to the requester while their request is
    /// still waiting, and to the assignee of a claimed request, which is what makes it appear alongside Complete.
    /// </remarks>
    private async Task ApplyHandlerActionStateAsync(IReadOnlyList<SampleOrder> orders, DateTimeOffset now)
    {
        var canHandle = CanHandleRequests;

        foreach (var order in orders)
        {
            var status = order.Status;
            var assignee = order.AssignedMaterialHandler;
            var isRequester = IsRequesterOrder(order, _currentRequesterEmployeeNumber);
            var isAssignee = RequestActionPolicy.CanViewerCompleteOrRelease(status, assignee, _currentRequesterEmployeeNumber, canHandle);

            order.CanAccept = RequestActionPolicy.CanViewerAccept(status, canHandle);
            order.CanCompleteOrRelease = isAssignee;
            order.CanCancelRequest = (isRequester && CanRequesterCancel(order)) || isAssignee;

            order.AcceptCommand = order.CanAccept ? AcceptRequestCommand : null;
            order.AcceptActionText = order.CanAccept ? "Waitlist_Action.AcceptRequest".GetLocalized() : string.Empty;

            order.CompleteCommand = order.CanCompleteOrRelease ? CompleteRequestCommand : null;
            order.CompleteActionText = order.CanCompleteOrRelease ? "Waitlist_Action.CompleteRequest".GetLocalized() : string.Empty;

            // Give back rides the same gate as Complete: it is the assignee's other option on their own claim,
            // and it is what puts a request the handler cannot work back on the open list (FR-047).
            order.ReleaseCommand = order.CanCompleteOrRelease ? ReleaseRequestCommand : null;
            order.ReleaseActionText = order.CanCompleteOrRelease ? "Waitlist_Action.ReleaseRequest".GetLocalized() : string.Empty;

            order.CancelCommand = order.CanCancelRequest ? CancelRequestCommand : null;
            order.CancelActionText = order.CanCancelRequest ? "Waitlist_Action.CancelRequest".GetLocalized() : string.Empty;

            order.HasNewMessages = await HasUnseenActivityAsync(order).ConfigureAwait(false);
            order.NewMessageTooltip = order.HasNewMessages ? "Waitlist_NewMessages.Tooltip".GetLocalized() : string.Empty;
        }
    }

    /// <summary>
    /// Whether a message arrived on this request after the viewer last read it. The signal is the newest
    /// message <b>written by a person</b>, and the store remembers when the viewer last looked — so the
    /// indicator clears when they read the request and stays clear across a restart.
    /// </summary>
    /// <remarks>
    /// A lifecycle change deliberately does not raise this. Job created, accepted, completed and canceled are
    /// things the system recorded, not things somebody said, and flagging them turned every routine transition
    /// into an unread message. The row carries <see cref="SampleOrder.LastMessageUtc"/> — the newest note entry
    /// with an author — and a request nobody has written on never raises the indicator at all.
    /// </remarks>
    private async Task<bool> HasUnseenActivityAsync(SampleOrder order)
    {
        if (_messageSeenStore is null || order.RequestId is not Guid requestId || order.LastMessageUtc is not DateTimeOffset written)
        {
            return false;
        }

        var lastSeen = await _messageSeenStore.GetLastSeenUtcAsync(requestId).ConfigureAwait(false);
        return lastSeen is null || written > lastSeen;
    }

    /// <summary>
    /// Records that the viewer has now seen this request's activity, so its indicator clears. Called when they
    /// open the request and after they act on it — their own action is not news to them.
    /// </summary>
    private async Task MarkActivitySeenAsync(Guid requestId, DateTimeOffset upToUtc)
    {
        if (_messageSeenStore is null)
        {
            return;
        }

        await _messageSeenStore.MarkSeenAsync(requestId, upToUtc).ConfigureAwait(false);
    }

    public async void OnNavigatedTo(object parameter)
    {
        if (!_isSubscribed)
        {
            _buildingSelectionService.BuildingChanged += OnBuildingChanged;
            _waitlistRequestService.RequestsChanged += OnRequestsChanged;
            if (_imageLocationService is not null && _imageLocationService.IsInitialized)
            {
                _imageLocationSubscription = _imageLocationService.SubscribeToImageLocationChanges(OnImageLocationChanged);
            }

            _isSubscribed = true;
        }

        await LoadOrdersAsync(_buildingSelectionService.SelectedBuilding);
        StartMinuteTicker();
    }

    public void OnNavigatedFrom()
    {
        StopMinuteTicker();

        if (_isSubscribed)
        {
            _buildingSelectionService.BuildingChanged -= OnBuildingChanged;
            _waitlistRequestService.RequestsChanged -= OnRequestsChanged;
            _imageLocationSubscription?.Dispose();
            _imageLocationSubscription = null;
            _isSubscribed = false;
        }
    }

    /// <summary>One minute: the granularity of both time-derived card texts.</summary>
    private static readonly TimeSpan MinuteTickInterval = TimeSpan.FromMinutes(1);

    private DispatcherQueueTimer? _minuteTicker;

    /// <summary>
    /// Starts the once-a-minute tick that keeps each card's waiting-age and countdown texts current.
    /// </summary>
    /// <remarks>
    /// The first tick is aligned to the next wall-clock minute boundary so the text flips when the minute
    /// changes rather than a full minute after the screen was opened; later ticks settle into a steady
    /// one-minute cadence. Nothing here touches the database: both texts are derived from timestamps the
    /// row already carries. A null dispatcher (headless hosting, unit tests) leaves the texts static
    /// rather than throwing.
    /// </remarks>
    private void StartMinuteTicker()
    {
        if (_minuteTicker is null)
        {
            DispatcherQueueTimer ticker;

            try
            {
                var dispatcher = _dispatcherQueue ?? DispatcherQueue.GetForCurrentThread();

                if (dispatcher is null)
                {
                    return;
                }

                _dispatcherQueue = dispatcher;
                ticker = dispatcher.CreateTimer();
            }
            catch (Exception ex)
            {
                // A dispatcher is not always available — the queue lookup itself throws where a thread has none,
                // and a reachable queue does not always hand out a timer. The countdown text is not worth
                // throwing out of navigation over: it simply stays where it was.
                StartupDebugLog.Error("WaitlistRequest", ex, "The list could not get a countdown timer, so the remaining-time text will not tick.");
                return;
            }

            ticker.IsRepeating = true;
            ticker.Tick += OnMinuteTick;
            _minuteTicker = ticker;
        }

        _minuteTicker.Interval = TimeUntilNextMinute(DateTimeOffset.Now);
        _minuteTicker.Start();
    }

    private void StopMinuteTicker() => _minuteTicker?.Stop();

    private void OnMinuteTick(DispatcherQueueTimer sender, object args)
    {
        // The alignment interval applies to the first tick only.
        if (sender.Interval != MinuteTickInterval)
        {
            sender.Interval = MinuteTickInterval;
        }

        RefreshTimeDerivedText();
    }

    /// <summary>
    /// Time remaining until the next whole minute, used to align the first tick to the minute boundary.
    /// </summary>
    /// <param name="now">The current instant.</param>
    /// <returns>At most one minute; a value on the boundary rolls over to a full minute.</returns>
    public static TimeSpan TimeUntilNextMinute(DateTimeOffset now)
    {
        var elapsedInMinute = TimeSpan.FromSeconds(now.Second) + TimeSpan.FromMilliseconds(now.Millisecond);
        var remaining = MinuteTickInterval - elapsedInMinute;
        return remaining > TimeSpan.Zero ? remaining : MinuteTickInterval;
    }

    /// <summary>
    /// Recomputes every row's waiting-age and remaining-time text against the current clock.
    /// </summary>
    /// <remarks>
    /// Rows are updated in place. Replacing the collection would restart the list's animations and throw
    /// away the user's scroll position and selection; <see cref="SampleOrder.RefreshTimeDerivedText"/> only
    /// notifies for values that actually changed, so an idle minute costs nothing.
    /// </remarks>
    public void RefreshTimeDerivedText()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var order in Source)
        {
            order.RefreshTimeDerivedText(now);
        }
    }

    [RelayCommand]
    private void OnItemClick(SampleOrder? clickedItem)
    {
        if (clickedItem != null)
        {
            _navigationService.SetListDataItemForNextConnectedAnimation(clickedItem);
            _navigationService.NavigateTo(typeof(WaitlistViewDetailViewModel).FullName!, clickedItem.Id);
        }
    }

    private async void OnBuildingChanged(object? sender, EventArgs e)
    {
        await LoadOrdersAsync(_buildingSelectionService.SelectedBuilding);
    }

    private async void OnRequestsChanged(object? sender, EventArgs e)
    {
        await LoadOrdersAsync(_buildingSelectionService.SelectedBuilding);
    }

    private void OnImageLocationChanged(ImageLocationChangedEventArgs args)
    {
        _ = RefreshAsync();
    }

    private async Task LoadOrdersAsync(string building)
    {
        var refreshVersion = Interlocked.Increment(ref _refreshVersion);
        await Task.Yield();

        if (refreshVersion != Volatile.Read(ref _refreshVersion))
        {
            return;
        }

        // The list is built from real requests only: the app's own store is always live (FR-001, FR-014).
        var newItems = new List<SampleOrder>();

        if (refreshVersion != Volatile.Read(ref _refreshVersion))
        {
            return;
        }

        await _waitlistRequestService.RefreshFromDatabaseAsync(building).ConfigureAwait(false);
        var activeRequests = _waitlistRequestService.GetActiveRequests(building);
        var activeRequestCount = activeRequests.Count;
        var workCenterImageLookup = await BuildWorkCenterImageLookupAsync(cancellationToken: default).ConfigureAwait(false);
        var now = DateTimeOffset.UtcNow;
        _maxAllottedCache.Clear();
        foreach (var request in activeRequests)
        {
            var sessionOrder = CreateSessionOrder(request);
            sessionOrder.Urgency = await ResolveUrgencyAsync(request, now).ConfigureAwait(false);
            await ApplyResolvedImagesAsync(sessionOrder, request, workCenterImageLookup).ConfigureAwait(false);
            newItems.Add(sessionOrder);
        }

        if (refreshVersion != Volatile.Read(ref _refreshVersion))
        {
            return;
        }

        // "My Requests": keep only the signed-in user's own submitted request rows.
        if (ShowMyRequestsOnly)
        {
            newItems = newItems.Where(order => IsRequesterOrder(order, _currentRequesterEmployeeNumber)).ToList();
        }

        // The order the viewer left the list in, applied to every build of it: the same rows in the same order
        // when the list reloads, and the same order after a restart (FR-011, SC-009). The ordering itself is
        // UrgencyCalculator's single home for the rule (§D8).
        var sortOrder = await ResolveSortOrderAsync().ConfigureAwait(false);
        newItems = UrgencyCalculator.OrderBy(newItems, sortOrder, SortValuesOf).ToList();

        // What this viewer may do to each row is decided here, once, from the rules the service also uses.
        await ApplyHandlerActionStateAsync(newItems, DateTimeOffset.UtcNow).ConfigureAwait(false);

        await ApplySourceUpdateAsync(newItems, refreshVersion);
        StartupDebugLog.Info("Waitlist", $"Loaded building '{building}'. SessionRequests={activeRequestCount}, TotalRows={Source.Count}, MyRequestsOnly={ShowMyRequestsOnly}, SearchQuery='{SearchQuery}'.");
        UpdateSearchSuggestions(SearchQuery);
    }

    /// <summary>
    /// The order this viewer's list is shown in, read from their remembered choice once per list instance and
    /// held from then on. The read happens on load rather than on every refresh, so a reload cannot change the
    /// order under the viewer.
    /// </summary>
    private async Task<string> ResolveSortOrderAsync()
    {
        if (_sortOrder is not null)
        {
            return _sortOrder;
        }

        var remembered = _sortPreferenceService is null
            ? null
            : await _sortPreferenceService.GetSortOrderAsync().ConfigureAwait(false);

        _sortOrder = WaitlistSortOrder.Normalize(remembered);
        return _sortOrder;
    }

    /// <summary>
    /// Re-orders the rows already on screen when the viewer changes the order, without re-reading the store: the
    /// rows, their action affordances and their overdue marking are the ones the load produced, so switching the
    /// order cannot disturb what the viewer may do or how overdue work is shown (FR-012).
    /// </summary>
    public void ApplySortOrder(string? sortOrder)
    {
        var normalized = WaitlistSortOrder.Normalize(sortOrder);
        if (string.Equals(SortOrder, normalized, StringComparison.Ordinal))
        {
            return;
        }

        _sortOrder = normalized;
        ReorderSourceInPlace();
        StartupDebugLog.Info("Waitlist", $"List re-ordered by '{normalized}' without a reload. Rows={Source.Count}.");
    }

    private void ReorderSourceInPlace()
    {
        void Apply()
        {
            // Materialize before clearing: the ordering is deferred, and clearing the source mid-enumeration
            // would re-order an empty list.
            var ordered = UrgencyCalculator.OrderBy(Source, SortOrder, SortValuesOf).ToList();

            Source.CollectionChanged -= OnSourceCollectionChanged;
            try
            {
                Source.Clear();
                foreach (var item in ordered)
                {
                    Source.Add(item);
                }
            }
            finally
            {
                Source.CollectionChanged += OnSourceCollectionChanged;
            }
        }

        if (_dispatcherQueue is DispatcherQueue dispatcher)
        {
            if (!dispatcher.TryEnqueue(Apply))
            {
                StartupDebugLog.Info("Waitlist", "The list could not be re-ordered because the UI queue is not accepting work.");
            }

            return;
        }

        Apply();
    }

    /// <summary>
    /// The row's already-computed values, as the order keys read them. A row whose urgency could not be derived
    /// carries <see cref="NoUrgency"/>, so it takes its place at the calm end rather than pretending to be urgent.
    /// </summary>
    private static UrgencyCalculator.SortValues SortValuesOf(SampleOrder order) => new(
        order.Urgency ?? NoUrgency,
        order.RequestedUtc,
        order.RequestedPressName,
        order.RequestedByName,
        order.Status);

    private async Task ApplySourceUpdateAsync(List<SampleOrder> newItems, long refreshVersion)
    {
        void Apply()
        {
            if (refreshVersion != Volatile.Read(ref _refreshVersion))
            {
                return;
            }

            Source.CollectionChanged -= OnSourceCollectionChanged;
            try
            {
                Source.Clear();
                foreach (var item in newItems)
                {
                    Source.Add(item);
                }
            }
            finally
            {
                Source.CollectionChanged += OnSourceCollectionChanged;
            }

            IsWaitlistEmpty = Source.Count == 0;
        }

        if (_dispatcherQueue is DispatcherQueue dispatcher)
        {
            var tcs = new TaskCompletionSource();
            dispatcher.TryEnqueue(() =>
            {
                try
                {
                    Apply();
                }
                finally
                {
                    tcs.SetResult();
                }
            });
            await tcs.Task;
        }
        else
        {
            Apply();
        }
    }

    public static SampleOrder CreateSessionOrder(WaitlistRequest request)
    {
        // The request's identity is its Item code. Everything the card shows about *what* was asked for —
        // both lines, the picture, the detail slots — resolves from that one code through the Item catalog,
        // so no reader is handed a second, derived pair it could disagree with (FR-004, FR-005).
        var definition = request.ItemDefinition;
        var line1 = WaitlistRequestTitles.ResolveLine1(definition);
        var line2 = WaitlistRequestTitles.ResolveLine2(definition, BuildLine2Context(request));

        var item = new SampleOrder
        {
            Id = request.Id.GetHashCode(),
            RequestId = request.Id,
            ItemCode = request.Item,
            RequesterEmployeeNumber = request.RequesterEmployeeNumber,
            AssignedMaterialHandler = request.AssignedMaterialHandler,
            Note = request.Note,
            Title = line1,
            Subtitle = line2.Text,
            Line2Problem = line2.IsResolved ? null : line2.Problem,
            Status = request.Status,
            RequestedByName = string.IsNullOrWhiteSpace(request.RequesterEmployeeName) ? "Current user" : request.RequesterEmployeeName,
            RequestedPressName = request.WorkCenter,
            RemainingTimeText = GetRemainingTimeText(request.TargetTimeUtc, request.IsOverdue),
            WaitingForText = GetWaitingForText(request.RequestedUtc),
            RequestedUtc = request.RequestedUtc,
            TargetTimeUtc = request.TargetTimeUtc,
            LastMessageUtc = request.LastMessageUtc,
            ImagePath = ResolveItemCardImagePath(definition),
            IsOverdue = request.IsOverdue,
            IsOverdueAtSource = request.IsOverdue,
        };

        // One card row per request: the row carries the two lines, the picture, the four metadata values and
        // the action affordances, and nothing about it selects a layout (FR-006). Its own declared fields are
        // not padded into template slots any more — the card draws no per-Item field, and the ones the request
        // does carry are read by the request page (FR-007).
        AddRequestFields(item, request);
        return item;
    }

    /// <summary>
    /// The values the Item's second-line template resolves against.
    /// </summary>
    /// <remarks>
    /// The request carries the answer its flow captured, and that is the only identifier source it holds. The
    /// job-derived tokens (<c>{part_number}</c>, <c>{die_number}</c>, <c>{dunnage_part}</c>, …) belong to the
    /// later item-resolution work, so a template that needs one is reported as unresolved and the Item's own
    /// display name is shown — never a substituted value, never a blank
    /// (<c>contracts/card-and-identifier.md</c> §3).
    /// </remarks>
    private static RequestItemLine2Context BuildLine2Context(WaitlistRequest request) => new(
        Answer: request.InputValue,
        Destination: request.InputValue);

    /// <summary>
    /// The card's built-in picture for an Item, before any configured override is applied. An Item the catalog
    /// does not describe keeps the existing placeholder rather than being given a borrowed image.
    /// </summary>
    private static string ResolveItemCardImagePath(RequestItemDefinition? item) => item?.Id switch
    {
        "pickup-ncm" => "pickup_ncm.png",
        "pickup-wip" => "pickup_wip.png",
        "pickup-fg" => "pickup_fg.png",
        "pickup-outside-service" => "pickup_os.png",
        "pickup-scrap" => "scrap.png",
        _ => "pickup_wip.png",
    };

    /// <summary>
    /// Derives a row's urgency from the same due value the card shows: the stored target time when the
    /// request carries one, so the ordering key and the countdown are one number. When the request has no
    /// target the due time is the Item's allotted window, which is what the New Request flow would have
    /// stamped onto it.
    /// </summary>
    private async Task<UrgencyState?> ResolveUrgencyAsync(WaitlistRequest request, DateTimeOffset now)
    {
        if (request.TargetTimeUtc is DateTimeOffset target)
        {
            return UrgencyCalculator.Compute(request.RequestedUtc, target - request.RequestedUtc, now);
        }

        var maxAllotted = await GetMaxAllottedAsync(request.Item).ConfigureAwait(false);
        return UrgencyCalculator.Compute(request.RequestedUtc, maxAllotted, now);
    }

    /// <summary>
    /// Allotted time for an Item, memoised for the load in flight. Falls back to the one documented default
    /// when no deadline service is configured (a headless host), so this fallback cannot disagree with the
    /// service's own 15-minute default (FR-017, SC-007).
    /// </summary>
    private async Task<TimeSpan> GetMaxAllottedAsync(string? itemCode)
    {
        var key = itemCode?.Trim() ?? string.Empty;
        if (_maxAllottedCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var maxAllotted = _urgencyDeadlineService is null
            ? DefaultMaxAllotted
            : await _urgencyDeadlineService.GetMaxAllottedAsync(key).ConfigureAwait(false);

        _maxAllottedCache[key] = maxAllotted;
        return maxAllotted;
    }

    /// <summary>
    /// Human-friendly waiting-age for a request, e.g. "Waiting 35m" / "Waiting 1h 20m" / "Waiting 2d",
    /// so leads can prioritize the oldest requests first.
    /// </summary>
    /// <remarks>
    /// The formatting itself lives on <see cref="SampleOrder"/> because the cards re-render these texts
    /// every minute; this stays as the call site used here and by the tests.
    /// </remarks>
    public static string GetWaitingForText(DateTimeOffset requestedUtc, DateTimeOffset? referenceUtc = null) =>
        SampleOrder.FormatWaitingAge(requestedUtc, referenceUtc);

    private static string GetRemainingTimeText(DateTimeOffset? targetTimeUtc, bool isOverdue) =>
        SampleOrder.FormatRemainingTime(targetTimeUtc, isOverdue);

    private async Task<Dictionary<string, (long WorkCenterId, string ResolvedPath)>> BuildWorkCenterImageLookupAsync(CancellationToken cancellationToken)
    {
        var lookup = new Dictionary<string, (long WorkCenterId, string ResolvedPath)>(StringComparer.OrdinalIgnoreCase);
        if (_imageLocationService is null || !_imageLocationService.IsInitialized)
        {
            return lookup;
        }

        try
        {
            var workCenters = await _imageLocationService.GetActiveWorkCentersAsync(cancellationToken).ConfigureAwait(false);
            if (workCenters is null)
            {
                return lookup;
            }

            foreach (var workCenter in workCenters)
            {
                var resolvedPath = await _imageLocationService.ResolveWorkCenterImagePathAsync(
                    workCenter.WorkCenterId.ToString(),
                    cancellationToken).ConfigureAwait(false);
                lookup[workCenter.DisplayName] = (workCenter.WorkCenterId, resolvedPath);
            }
        }
        catch
        {
            // Keep fallback images when resolver metadata is temporarily unavailable.
        }

        return lookup;
    }

    private async Task ApplyResolvedImagesAsync(
        SampleOrder order,
        WaitlistRequest request,
        IReadOnlyDictionary<string, (long WorkCenterId, string ResolvedPath)> workCenterImageLookup,
        CancellationToken cancellationToken = default)
    {
        if (_imageLocationService is not null && _imageLocationService.IsInitialized)
        {
            try
            {
                var resolvedImagePath = await ResolveRequestImagePathAsync(request, cancellationToken).ConfigureAwait(false);
                if (RequestImagePathPolicy.IsUsableResolvedPath(resolvedImagePath))
                {
                    order.ResolvedImagePath = resolvedImagePath!;
                }
                else if (!string.IsNullOrWhiteSpace(resolvedImagePath))
                {
                    // The service had nothing configured and answered with its own placeholder. Taking it would
                    // replace this row's working image with a "no image available" card, so the row keeps the
                    // image it already has. Logged because the substitution is otherwise invisible.
                    StartupDebugLog.Info(
                        "WaitlistRequest",
                        $"Request '{request.Id}' has no configured image; keeping the row's own image '{order.ImagePath}' instead of the resolver's placeholder.");
                }
            }
            catch
            {
                // Keep legacy fallback image when resolver path cannot be resolved.
            }
        }

        if (workCenterImageLookup.TryGetValue(request.WorkCenter, out var workCenterImage))
        {
            order.WorkCenterCatalogId = workCenterImage.WorkCenterId;
            order.WorkCenterImagePath = workCenterImage.ResolvedPath;
        }
    }

    /// <summary>
    /// Resolves the request's configured picture through the Item, falling back to the Category family and
    /// then to the resolver's own placeholder (FR-009). The legacy request-type/subtype scopes are not
    /// consulted: a request's picture is keyed by the Item it names.
    /// </summary>
    private async Task<string?> ResolveRequestImagePathAsync(WaitlistRequest request, CancellationToken cancellationToken)
    {
        if (_imageLocationService is null || !_imageLocationService.IsInitialized)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(request.Item))
        {
            return null;
        }

        return await _imageLocationService
            .ResolveRequestItemImagePathAsync(request.Item, cancellationToken)
            .ConfigureAwait(false);
    }

    private static void AddRequestFields(SampleOrder item, WaitlistRequest request)
    {
        // Every row below is sourced from the request itself. Nothing is derived from a retired type/subtype
        // string and nothing is invented for a value the request does not carry: an Item's own attributes are
        // restored by the item-resolution work, and until then this surface states only what it can support
        // (FR-001, FR-002). The two-line identity of the request travels on the card, not in these slots.
        void Add(string label, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                item.Fields.Add(new WaitlistField { Label = label, Value = value.Trim() });
            }
        }

        Add("Request details", request.InputValue);
        Add("Work order", request.ActiveSetupJobId);
        Add("Work center", request.WorkCenter);
        Add("Requested by", request.RequesterEmployeeName);
        Add("Request ID", request.Id.ToString("N"));
    }

    /// <summary>
    /// Whether a row was submitted by the given requester (compares the underlying request's
    /// employee number; a row that carries no requester number never matches).
    /// </summary>
    public static bool IsRequesterOrder(SampleOrder order, string employeeNumber)
    {
        if (order is null)
        {
            return false;
        }

        var normalizedRequester = (employeeNumber ?? string.Empty).Trim();
        return !string.IsNullOrWhiteSpace(normalizedRequester)
            && string.Equals(order.RequesterEmployeeNumber, normalizedRequester, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Whether a requester-visible Cancel affordance should be shown: the row maps to a real
    /// request that is still Waiting (Pending). The service still authoritatively gates on the
    /// actual creator identity and state when the action is invoked.
    /// </summary>
    public static bool CanRequesterCancel(SampleOrder order)
    {
        return order is not null
            && order.RequestId.HasValue
            && string.Equals(order.Status, "Pending", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Narrows a set of rows to those submitted by the given requester (used by the "My
    /// Requests" quick view). A row with no requester identity is excluded.
    /// </summary>
    public static IReadOnlyList<SampleOrder> FilterToMyRequests(IEnumerable<SampleOrder> source, string requesterEmployeeNumber)
    {
        if (source is null)
        {
            return Array.Empty<SampleOrder>();
        }

        return source.Where(order => IsRequesterOrder(order, requesterEmployeeNumber)).ToArray();
    }

    public Task RefreshAsync() => LoadOrdersAsync(_buildingSelectionService.SelectedBuilding);
    public string SearchQuery
    {
        get; private set;
    } = string.Empty;

    public void UpdateSearchSuggestions(string? query)
    {
        SearchQuery = query?.Trim() ?? string.Empty;
        SearchSuggestions.Clear();

        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            return;
        }

        foreach (var order in Source.Where(order => MatchesSearch(order, SearchQuery)).Take(8))
        {
            SearchSuggestions.Add(order);
        }
    }

    public void SubmitSearch(string? query, SampleOrder? selectedSuggestion = null)
    {
        var order = selectedSuggestion
            ?? Source.FirstOrDefault(candidate => string.Equals(candidate.Title, query?.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? Source.FirstOrDefault(candidate => MatchesSearch(candidate, query?.Trim() ?? string.Empty));

        if (order is not null)
        {
            OpenOrder(order);
        }
    }
    private void OpenOrder(SampleOrder order)
    {
        _navigationService.SetListDataItemForNextConnectedAnimation(order);
        _navigationService.NavigateTo(typeof(WaitlistViewDetailViewModel).FullName!, order.Id);
    }

    private static bool MatchesSearch(SampleOrder order, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return false;
        }

        return Contains(order.Title, query)
            || Contains(order.Status, query)
            || Contains(order.RequestedByName, query)
            || Contains(order.RequestedPressName, query)
            || order.Fields.Any(field => Contains(field.Label, query) || Contains(field.Value, query));
    }

    private static bool Contains(string value, string query) =>
        value.Contains(query, StringComparison.OrdinalIgnoreCase);

    private void OnSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        var value = Source.Count == 0;
        if (value != IsWaitlistEmpty)
        {
            IsWaitlistEmpty = value;
        }
    }
}
