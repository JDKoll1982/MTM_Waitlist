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
using MTM_Waitlist.Module_Shared.Helpers;
using MTM_Waitlist.Module_Shared.Services;
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
    private readonly MTM_Waitlist.Module_Waitlist.Services.IWaitlistRequestActionPrompt? _actionPrompt;
    private readonly IUrgencyDeadlineService? _urgencyDeadlineService;
    private readonly MTM_Waitlist.Module_Waitlist.Services.IWaitlistMessageSeenStore? _messageSeenStore;
    private readonly IWaitlistSortPreferenceService? _sortPreferenceService;
    private readonly MTM_Waitlist.Module_Waitlist.Services.ILuggerChangeNoticeService? _luggerChangeNoticeService;

    /// <summary>
    /// The reader of this computer's Local Work Centers. The list's own filter narrows to them, so a null seam
    /// leaves the filter matching nothing rather than guessing which work centers this machine serves.
    /// </summary>
    private readonly IWorkCenterCatalogService? _workCenterCatalogService;

    /// <summary>
    /// The Local Work Centers (hot work centers) assigned to this computer, read on each cold load. Empty until the
    /// first read completes, and empty on a host whose machine configuration assigns none.
    /// </summary>
    private HashSet<string> _localWorkCenterNames = new(StringComparer.OrdinalIgnoreCase);

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

    /// <summary>
    /// The requesting job per work centre for the load in flight. Building a card's two lines can need the job's
    /// own part number and the die assigned to it, and every row of a work centre needs the same job, so a list of
    /// N rows costs one read per distinct work centre rather than N. Cleared at the start of each cold load, so a
    /// job that has moved on since the last one is picked up, and kept across a reload raised by a change in the
    /// store, which cannot have moved it.
    /// </summary>
    private readonly Dictionary<string, RequestJobPartAvailability> _jobAvailabilityCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The pictures this list has already resolved, keyed by the scope and part that were asked about, so a
    /// reload raised by a change in the store does not resolve them all over again.
    /// </summary>
    /// <remarks>
    /// A resolved path is the expensive half of building a card: it is a file probe, and the picture root is a
    /// network share in the plant's configuration, so a share that is not answering makes every probe a failed
    /// lookup that measured hundreds of milliseconds to seconds (2026-10-11). A status change alters a request's
    /// status and its handler and cannot alter any picture, so a reload raised by one reuses what the cold load
    /// before it resolved. The map is emptied by every cold load - the page opening, the building changing, the
    /// person's own refresh - so a genuinely new picture is picked up on the next of those rather than being
    /// remembered away. The resolver does the same thing for itself one level down: it copies a picture onto this
    /// computer the first time it draws it, and reads the copy from then on.
    /// </remarks>
    private readonly Dictionary<string, string> _resolvedPictureCache = new(StringComparer.OrdinalIgnoreCase);

    private readonly IRequestJobPartAvailabilityProvider? _jobAvailabilityProvider;

    /// <summary>
    /// The reader every surface that draws a part calls. This screen resolves each row's material picture from it
    /// before the row reaches the list, so the card draws a value it was handed rather than asking for one
    /// (FR-019). Null on a headless host, which draws the one shared placeholder and nothing invented.
    /// </summary>
    private readonly IPartPictureResolver? _partPictureResolver;

    /// <summary>
    /// The one place this screen asks what a person may do. Access is a named permission resolved from the one
    /// declaration, never a role name held or compared here (FR-054).
    /// </summary>
    private readonly IPermissionService? _permissionService;

    /// <summary>
    /// The reader every card's declared fields come from. A row's cells are the fields its Item declares
    /// (FR-025), and the declaration is read live from the internal store once per list load rather than once
    /// per row, which is what <c>GetConfigurationsAsync</c> exists for. Null on a headless host, which draws no
    /// detail cell at all rather than inventing one.
    /// </summary>
    private readonly IRequestItemConfigurationService? _itemConfigurationService;

    /// <summary>The documented default allotted time, used when no deadline service is supplied or the Item has no configured allotment.</summary>
    private static readonly TimeSpan s_defaultMaxAllotted = TimeSpan.FromMinutes(UrgencySettingsService.DefaultMinutes);

    /// <summary>
    /// The ordering key for a row whose due time cannot be derived. It is not overdue and carries the longest
    /// possible remaining time, so it sorts after every row that does carry an urgency without the ordering
    /// rule pretending it is something it is not.
    /// </summary>
    private static readonly UrgencyState s_noUrgency = new() { Remaining = TimeSpan.MaxValue, IsOverdue = false };

    private IDisposable? _imageLocationSubscription;
    private long _refreshVersion;
    private bool _isSubscribed;

    public ObservableCollection<SampleOrder> Source { get; } = new ObservableCollection<SampleOrder>();
    public ObservableCollection<SampleOrder> SearchSuggestions { get; } = new();

    /// <summary>
    /// The arrangement changes this session has been told about and has not dismissed, oldest first. The strip
    /// draws one bar per entry, so two changes draw two bars (FR-033, FR-036).
    /// </summary>
    public ObservableCollection<LuggerChangeNotice> LuggerChangeNotices { get; } = new();

    [ObservableProperty]
    public partial bool IsWaitlistEmpty { get; private set; } = true;

    /// <summary>
    /// When true, the list is narrowed to the requests whose work center is one of this computer's Local Work
    /// Centers (its hot work centers). Toggling reloads the current building's list.
    /// </summary>
    /// <remarks>
    /// This replaced a "My Requests" filter that narrowed to the signed-in person's own submissions. The floor's
    /// question was never "what did I raise" but "what is waiting for the work centers this machine runs", which
    /// is the one thing the computer's own configuration can answer.
    /// </remarks>
    [ObservableProperty]
    public partial bool ShowLocalWorkCentersOnly { get; set; }

    partial void OnShowLocalWorkCentersOnlyChanged(bool value)
    {
        AppLog.Info("Waitlist", $"Local-Work-Centers filter {(value ? "enabled" : "disabled")} for {_localWorkCenterNames.Count} configured Local Work Center(s).");
        OnPropertyChanged(nameof(EmptyStateMessage));
        _ = LoadOrdersAsync(_buildingSelectionService.SelectedBuilding);
    }

    /// <summary>
    /// The sentence under the empty state's title. A filtered list says what the filter left out, so an empty
    /// screen is never read as "nothing is waiting anywhere".
    /// </summary>
    public string EmptyStateMessage => ShowLocalWorkCentersOnly
        ? "Waitlist_EmptyState_Filtered.Message".GetLocalized()
        : "Waitlist_EmptyState.Message".GetLocalized();

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
        IPersonIdentity? personIdentity = null,
        IStoreAvailabilityTracker? storeAvailabilityTracker = null,
        MTM_Waitlist.Module_Waitlist.Services.IWaitlistRequestActionPrompt? actionPrompt = null,
        IUrgencyDeadlineService? urgencyDeadlineService = null,
        MTM_Waitlist.Module_Waitlist.Services.IWaitlistMessageSeenStore? messageSeenStore = null,
        IWaitlistSortPreferenceService? sortPreferenceService = null,
        IRequestJobPartAvailabilityProvider? jobAvailabilityProvider = null,
        IPermissionService? permissionService = null,
        IPartPictureResolver? partPictureResolver = null,
        IRequestItemConfigurationService? itemConfigurationService = null,
        MTM_Waitlist.Module_Waitlist.Services.ILuggerChangeNoticeService? luggerChangeNoticeService = null,
        IWorkCenterCatalogService? workCenterCatalogService = null)
    {
        ArgumentNullException.ThrowIfNull(navigationService);
        ArgumentNullException.ThrowIfNull(buildingSelectionService);
        ArgumentNullException.ThrowIfNull(waitlistRequestService);

        _navigationService = navigationService;
        _buildingSelectionService = buildingSelectionService;
        _waitlistRequestService = waitlistRequestService;
        _imageLocationService = imageLocationService;
        _dispatcherQueue = dispatcherQueue;
        _currentRequesterEmployeeNumber = personIdentity?.EmployeeNumber?.Trim() ?? string.Empty;
        _currentEmployeeName = personIdentity?.DisplayName?.Trim() ?? string.Empty;
        _permissionService = permissionService;
        _actionPrompt = actionPrompt;
        _urgencyDeadlineService = urgencyDeadlineService;
        _messageSeenStore = messageSeenStore;
        _sortPreferenceService = sortPreferenceService;
        _jobAvailabilityProvider = jobAvailabilityProvider;
        _partPictureResolver = partPictureResolver;
        _itemConfigurationService = itemConfigurationService;
        _luggerChangeNoticeService = luggerChangeNoticeService;
        _workCenterCatalogService = workCenterCatalogService;

        // This screen's own internal-store unavailable state (FR-021): the waitlist list reads
        // mtm_waitlist live, so a failure is reported here, with a retry that re-runs this screen's load.
        // It is never replaced by sample rows and it is not an app-wide banner.
        StoreUnavailable = new InternalStoreUnavailableState(
            MySqlDatabaseTarget.MtmWaitlist,
            _ => LoadOrdersAsync(_buildingSelectionService.SelectedBuilding),
            storeAvailabilityTracker,
            dispatcherQueue);
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
    /// True when the signed-in viewer holds <c>permission.requests.handle</c>, which is the gate for being offered
    /// any handler action at all. The screen uses it only to decide what to offer; the service re-checks it on
    /// every action, so a screen built from stale data can never widen what the viewer is allowed to do (FR-018,
    /// FR-056). No role name is compared here: the answer is the named permission, resolved from the one
    /// declaration, and an unreachable store answers from the shipped fallback rather than refusing (FR-050).
    /// </summary>
    private Task<bool> CanHandleRequestsAsync(CancellationToken cancellationToken = default) =>
        RequestActionPolicy.CanViewerHandleRequestsAsync(_permissionService, cancellationToken);

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
            AppLog.Error("Waitlist", ex, "Claiming the request failed; the row is unchanged.");
            ActionRefusalMessage = "Waitlist_Action_Refused.Unexpected".GetLocalized();
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
            ? "Waitlist_Action_Refused.AcceptGone".GetLocalized()
            : "Waitlist_Action_Refused.AcceptTaken".GetLocalized();

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
            AppLog.Error("Waitlist", ex, "The action warning could not be shown; the refusal is still reported on the list.");
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
            "Waitlist_Action_Refused.CompleteOrRelease".GetLocalized(),
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
            "Waitlist_Action_Refused.CompleteOrRelease".GetLocalized(),
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
            AppLog.Error("Waitlist", ex, "A waitlist request action failed; the row is unchanged.");
            ActionRefusalMessage = "Waitlist_Action_Refused.Unexpected".GetLocalized();
        }
    }

    /// <summary>
    /// Sets the condition the row's own material handler reported from its card (FR-070, FR-072, FR-076).
    /// </summary>
    /// <remarks>
    /// This is the command the status word's flyout invokes with the row and the condition chosen from the four;
    /// the flyout's own body and behaviour are Phase 6's work, so nothing more than the command and its gate is
    /// exposed here. The write goes through the seam that reports the affected rows, so a lost race — a handler
    /// who no longer holds the request, or a value that is not one of the four conditions — is told rather than
    /// shown as a success that did not happen (FR-073). The gate is the row's own: only its recorded material
    /// handler is offered the write at all, which is the same rule the store enforces, so nobody is asked and
    /// then refused.
    /// </remarks>
    [RelayCommand]
    private async Task SetStatusConditionAsync((SampleOrder? Row, string? Condition) selection)
    {
        if (selection.Row is not { RequestId: Guid requestId, CanSetStatusCondition: true } row)
        {
            return;
        }

        try
        {
            // No ConfigureAwait(false): a continuation may set the bindable refusal message, and that is
            // UI-thread-only, exactly as the other action paths here are.
            var affected = await _waitlistRequestService.SetHandlerStatusAsync(
                requestId,
                selection.Condition,
                row.AssignedMaterialHandler?.Trim() ?? string.Empty);

            ActionRefusalMessage = affected > 0
                ? string.Empty
                : "Waitlist_Action_Refused.CompleteOrRelease".GetLocalized();
        }
        catch (Exception ex)
        {
            AppLog.Error("Waitlist", ex, "Setting the row's condition failed; the row is unchanged.");
            ActionRefusalMessage = "Waitlist_Action_Refused.Unexpected".GetLocalized();
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
            ActionRefusalMessage = "Waitlist_Action_Refused.Unexpected".GetLocalized();
            return;
        }

        string? reason;
        try
        {
            reason = await _actionPrompt.RequestCancellationReasonAsync(
                "Waitlist_Action_Cancel.DialogTitle".GetLocalized(),
                "Waitlist_Action_Cancel.DialogMessage".GetLocalized(),
                "Waitlist_Action_Cancel.Confirm".GetLocalized(),
                "Waitlist_Action_Cancel.Dismiss".GetLocalized());
        }
        catch (Exception ex)
        {
            AppLog.Error("Waitlist", ex, "The cancel confirmation could not be shown; nothing was cancelled.");
            ActionRefusalMessage = "Waitlist_Action_Refused.Unexpected".GetLocalized();
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
                : "Waitlist_Action_Refused.Cancel".GetLocalized();

            if (cancelled)
            {
                await MarkActivitySeenAsync(requestId, DateTimeOffset.UtcNow);
            }
        }
        catch (Exception ex)
        {
            AppLog.Error("Waitlist", ex, "Cancelling the request failed; the request is unchanged.");
            ActionRefusalMessage = "Waitlist_Action_Refused.Unexpected".GetLocalized();
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
        var canHandle = await CanHandleRequestsAsync().ConfigureAwait(true);

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
        // Made ready before anything subscribes or resolves. Nothing initializes this service at startup, and
        // every guard below used to bail out while it was not ready yet — so on a machine that had just been
        // started the first list load drew the no-image placeholder for every row, including Items whose picture
        // somebody had configured on another machine. Awaiting it here makes the first render deterministic.
        if (_imageLocationService is not null
            && !await _imageLocationService.EnsureInitializedAsync())
        {
            AppLog.Info(
                "Waitlist",
                "The image location service could not be initialized; the list will draw the no-image placeholder.");
        }

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
        await OpenLuggerChangeNoticesAsync();
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
    private static readonly TimeSpan s_minuteTickInterval = TimeSpan.FromMinutes(1);

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
                AppLog.Error("WaitlistRequest", ex, "The list could not get a countdown timer, so the remaining-time text will not tick.");
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
        if (sender.Interval != s_minuteTickInterval)
        {
            sender.Interval = s_minuteTickInterval;
        }

        RefreshTimeDerivedText();
        _ = PollLuggerChangeNoticesAsync();
    }

    /// <summary>
    /// Takes this session's notice watermark as the list opens, so a change recorded before the person was using
    /// the list is never shown to them as though it had just happened (FR-037). A person who may not handle
    /// requests is not asked for anything and is shown no bar (FR-038).
    /// </summary>
    public async Task OpenLuggerChangeNoticesAsync()
    {
        if (_luggerChangeNoticeService is null)
        {
            return;
        }

        try
        {
            if (!await CanHandleRequestsAsync().ConfigureAwait(false))
            {
                await ApplyLuggerChangeNoticesAsync(Array.Empty<LuggerChangeNotice>()).ConfigureAwait(false);
                return;
            }

            var notices = await _luggerChangeNoticeService.OpenSessionAsync().ConfigureAwait(false);
            await ApplyLuggerChangeNoticesAsync(notices).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // A strip that cannot be opened costs the notice, not the list.
            AppLog.Error("Waitlist", ex, "Opening the lugger change notices failed; the strip stays empty.");
        }
    }

    /// <summary>
    /// Asks the notice seam for the changes made since the list opened, once per minute tick, and shows every
    /// notice the session still holds (FR-033). Only a person who holds <c>permission.requests.handle</c> is
    /// asked; an unreachable store answers no notice rather than an invented one.
    /// </summary>
    public async Task PollLuggerChangeNoticesAsync()
    {
        if (_luggerChangeNoticeService is null)
        {
            return;
        }

        try
        {
            if (!await CanHandleRequestsAsync().ConfigureAwait(false))
            {
                await ApplyLuggerChangeNoticesAsync(Array.Empty<LuggerChangeNotice>()).ConfigureAwait(false);
                return;
            }

            var notices = await _luggerChangeNoticeService.PollAsync().ConfigureAwait(false);
            await ApplyLuggerChangeNoticesAsync(notices).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            AppLog.Error("Waitlist", ex, "Polling the lugger change notices failed; this tick shows nothing new.");
        }
    }

    /// <summary>
    /// Records that the person closed one notice's bar, so it is not offered again for the rest of the session
    /// (FR-036). The page calls this only when the bar's own close button raised the close.
    /// </summary>
    [RelayCommand]
    private async Task DismissLuggerChangeNoticeAsync(LuggerChangeNotice? notice)
    {
        if (notice is null)
        {
            return;
        }

        if (_luggerChangeNoticeService is not null)
        {
            try
            {
                var remaining = await _luggerChangeNoticeService
                    .DismissAsync(notice.Id, raisedByCloseButton: true)
                    .ConfigureAwait(false);
                await ApplyLuggerChangeNoticesAsync(remaining).ConfigureAwait(false);
                return;
            }
            catch (Exception ex)
            {
                AppLog.Error("Waitlist", ex, "Recording the dismissed lugger change notice failed; the bar is closed for this list.");
            }
        }

        LuggerChangeNotices.Remove(notice);
    }

    /// <summary>
    /// Makes the strip match the notices the session still holds. Bars that are already up are left alone, so a
    /// poll adds the new ones and removes only the dismissed ones rather than rebuilding the strip every minute.
    /// </summary>
    private async Task ApplyLuggerChangeNoticesAsync(IReadOnlyList<LuggerChangeNotice> notices)
    {
        void Apply()
        {
            for (var index = LuggerChangeNotices.Count - 1; index >= 0; index--)
            {
                if (!notices.Any(notice => notice.Id == LuggerChangeNotices[index].Id))
                {
                    LuggerChangeNotices.RemoveAt(index);
                }
            }

            foreach (var notice in notices.OrderBy(notice => notice.Id))
            {
                if (!LuggerChangeNotices.Any(held => held.Id == notice.Id))
                {
                    LuggerChangeNotices.Add(notice);
                }
            }
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

    /// <summary>
    /// Time remaining until the next whole minute, used to align the first tick to the minute boundary.
    /// </summary>
    /// <param name="now">The current instant.</param>
    /// <returns>At most one minute; a value on the boundary rolls over to a full minute.</returns>
    public static TimeSpan TimeUntilNextMinute(DateTimeOffset now)
    {
        var elapsedInMinute = TimeSpan.FromSeconds(now.Second) + TimeSpan.FromMilliseconds(now.Millisecond);
        var remaining = s_minuteTickInterval - elapsedInMinute;
        return remaining > TimeSpan.Zero ? remaining : s_minuteTickInterval;
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
        // A change in the store - an action taken on a card, or a request just raised - reloads the list, and the
        // job each work centre is running is not what changed. Re-reading it costs one live read per work centre
        // per row, which is what left a person waiting several seconds to see a card they had just acted on, so
        // this reload reuses what the cold load before it read.
        await LoadOrdersCoreAsync(_buildingSelectionService.SelectedBuilding, reuseJobData: true)
            .ConfigureAwait(false);
    }

    private void OnImageLocationChanged(ImageLocationChangedEventArgs args)
    {
        _ = RefreshAsync();
    }

    private async Task LoadOrdersAsync(string building)
        => await LoadOrdersCoreAsync(building, reuseJobData: false).ConfigureAwait(false);

    /// <summary>
    /// Rebuilds the list from the store.
    /// </summary>
    /// <param name="building">The building whose rows are wanted.</param>
    /// <param name="reuseJobData">
    /// Whether the job data and the resolved pictures the cold load before this one read may be reused instead of
    /// read again. Passing <see langword="true" /> is only correct for a reload raised by a change in the store —
    /// an action taken on a card, or a request just raised — because such a change moves a request's status and its
    /// handler and touches neither the job its work centre is running, nor that job's allotted window, nor any
    /// picture. A cold load reads all of it again, and is therefore the refresh point.
    /// </param>
    private async Task LoadOrdersCoreAsync(string building, bool reuseJobData)
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
        var itemDeclarations = await ReadItemDeclarationsAsync(activeRequests).ConfigureAwait(false);
        var workCenterImageLookup = await BuildWorkCenterImageLookupAsync(cancellationToken: default).ConfigureAwait(false);

        // A load raised by a change in the store - an action taken on a card, or a request just raised - reuses
        // what the cold load before it read, because such a change moves a request's status and its handler and
        // cannot move the job its work centre is running, that job's allotted window, or any picture. A cold load
        // - the page opening, the building changing, the person's own refresh - reads all of it again, and is
        // therefore both the refresh point and the only thing that pays for a live read.
        //
        // This was a two-minute timer to begin with, and the timer was the defect: an action taken more than two
        // minutes after the page was opened re-read every work centre's job, which measured up to three seconds
        // from the click to the card changing, and only some of the time - which is exactly how the hang was
        // reported. The window is now the cold load, which the person controls.
        if (!reuseJobData)
        {
            _maxAllottedCache.Clear();
            _jobAvailabilityCache.Clear();
            _resolvedPictureCache.Clear();

            // Its own read, and only while the filter is on: a list that is not narrowing to this computer's work
            // centers has no use for the assignment, and every live read this screen pays for is felt on a card.
            if (ShowLocalWorkCentersOnly)
            {
                await LoadLocalWorkCenterNamesAsync().ConfigureAwait(false);
            }
        }

        var now = DateTimeOffset.UtcNow;

        foreach (var request in activeRequests)
        {
            var jobAvailability = await ResolveJobAvailabilityAsync(request.WorkCenter).ConfigureAwait(false);
            var sessionOrder = CreateSessionOrder(request, jobAvailability, itemDeclarations?.Get(request.Item));
            sessionOrder.Urgency = await ResolveUrgencyAsync(request, now).ConfigureAwait(false);
            await ApplyResolvedImagesAsync(sessionOrder, request, workCenterImageLookup).ConfigureAwait(false);
            newItems.Add(sessionOrder);
        }

        if (refreshVersion != Volatile.Read(ref _refreshVersion))
        {
            return;
        }

        // "Local Work Centers": keep only the rows whose work center is one this computer serves.
        if (ShowLocalWorkCentersOnly)
        {
            newItems = newItems.Where(order => IsLocalWorkCenterOrder(order, _localWorkCenterNames)).ToList();
        }

        // The order the viewer left the list in, applied to every build of it: the same rows in the same order
        // when the list reloads, and the same order after a restart (FR-011, SC-009). The ordering itself is
        // UrgencyCalculator's single home for the rule (§D8).
        var sortOrder = await ResolveSortOrderAsync().ConfigureAwait(false);
        newItems = UrgencyCalculator.OrderBy(newItems, sortOrder, SortValuesOf).ToList();

        // What this viewer may do to each row is decided here, once, from the rules the service also uses.
        await ApplyHandlerActionStateAsync(newItems, DateTimeOffset.UtcNow).ConfigureAwait(false);

        await ApplySourceUpdateAsync(newItems, refreshVersion);
        AppLog.Info("Waitlist", $"Loaded building '{building}'. SessionRequests={activeRequestCount}, TotalRows={Source.Count}, LocalWorkCentersOnly={ShowLocalWorkCentersOnly}, LocalWorkCenterCount={_localWorkCenterNames.Count}, SearchQuery='{SearchQuery}'.");
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
        AppLog.Info("Waitlist", $"List re-ordered by '{normalized}' without a reload. Rows={Source.Count}.");
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
                AppLog.Info("Waitlist", "The list could not be re-ordered because the UI queue is not accepting work.");
            }

            return;
        }

        Apply();
    }

    /// <summary>
    /// The row's already-computed values, as the order keys read them. A row whose urgency could not be derived
    /// carries <see cref="s_noUrgency"/>, so it takes its place at the calm end rather than pretending to be urgent.
    /// </summary>
    private static UrgencyCalculator.SortValues SortValuesOf(SampleOrder order) => new(
        order.Urgency ?? s_noUrgency,
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

    public static SampleOrder CreateSessionOrder(
        WaitlistRequest request,
        RequestJobPartAvailability? jobAvailability = null,
        RequestItemConfiguration? declaration = null)
    {
        // The request's identity is its Item code. Everything the card shows about *what* was asked for —
        // both lines, the picture, the detail slots — resolves from that one code through the Item catalog,
        // so no reader is handed a second, derived pair it could disagree with (FR-004, FR-005).
        var definition = request.ItemDefinition;
        var context = WaitlistRequestTitles.ResolveContext(request, jobAvailability);
        var line1 = WaitlistRequestTitles.ResolveLine1(definition, context);
        var line2 = WaitlistRequestTitles.ResolveLine2(definition, context);

        // The part this request is about — its material — captured on the row here because this is where the
        // requesting job is in hand. It is the same rule the card's own identifier resolves {part_number} from,
        // so the picture the card draws and the part the card names can never be two different parts (FR-019).
        // A request that names no material carries none, and its card draws the one shared placeholder (FR-020).
        var materialPartNumber = WaitlistRequestTitles.ResolveMaterialPartNumber(request, jobAvailability);

        var item = new SampleOrder
        {
            Id = request.Id.GetHashCode(),
            RequestId = request.Id,
            ItemCode = request.Item,
            MaterialPartNumber = materialPartNumber ?? string.Empty,
            StockLookupPartNumber = string.Equals(request.Item, "deliver-component", StringComparison.OrdinalIgnoreCase)
                ? context.Component?.Trim() ?? string.Empty
                : string.Empty,
            MaterialPartScope = materialPartNumber is null ? string.Empty : PartPictureLayout.VisualPartScope,
            RequesterEmployeeNumber = request.RequesterEmployeeNumber,
            AssignedMaterialHandler = request.AssignedMaterialHandler,
            AssignedMaterialHandlerName = request.AssignedMaterialHandlerName,
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
            // The store's overdue flag is sticky, and a row whose due time has already passed is overdue too, so
            // the setter is never given an answer the row's own first paint would contradict: without this the word
            // on row 1 read `Waiting` while the ring beside it already read `Overdue`, until the first tick ran.
            IsOverdue = SampleOrder.IsOverdueAt(request.IsOverdue, request.TargetTimeUtc),
            IsOverdueAtSource = request.IsOverdue,

            // The condition the row's own material handler reported, read back with the request so it is restored
            // onto the same card on the next list read and survives a restart (FR-073, SC-016). It is copied as
            // the store holds it: the row's own setter is what decides whether the value is one of the four
            // conditions, so a fifth word reads as no condition rather than as a report nobody made.
            HandlerStatus = request.HandlerStatus,

            // The ring's two extra inputs (data-model.md §7, FR-015, FR-017). The allotment is the Item's
            // declared one — the number the urgency deadline service reads from the same configuration row — so
            // the ring's scale and the countdown come from one source, and an Item that declares none keeps the
            // row's own labelled fifteen-minute fallback. The close instant freezes the ring for a Done or
            // Cancelled request; from then on the fraction is the time taken rather than the time left, and the
            // minute tick recomputes the fraction without a second clock.
            AllottedMinutes = declaration?.AllottedMinutes,
            ClosedUtc = request.CompletedUtc ?? request.CanceledUtc,
        };

        // One card row per request: the row carries the two lines, the picture, the four metadata values and
        // the action affordances, and nothing about it selects a layout (FR-006). Its own declared fields are
        // not padded into template slots any more — the card draws no per-Item field, and the ones the request
        // does carry are read by the request page (FR-007).
        AddRequestFields(item, request);

        // The card's own cells are the fields the Item declares, resolved here where the request and the
        // requesting job are both in hand (FR-025, FR-026, §7).
        AddDeclaredCells(item, request, jobAvailability, declaration);
        return item;
    }

    /// <summary>
    /// Reads every Item's declaration once for a load, so a list of N rows costs one read rather than N, and
    /// reports the Items whose declaration cannot be read: their cards draw no cells at all and the gap is
    /// recorded, rather than a cell being invented or a reason being drawn on the card (FR-058, FR-101, D41).
    /// </summary>
    /// <param name="requests">The requests the load is about to build rows for.</param>
    /// <returns>The declarations, or null when there is nothing to read them from or the read failed.</returns>
    private async Task<RequestItemConfigurationSet?> ReadItemDeclarationsAsync(IReadOnlyList<WaitlistRequest> requests)
    {
        if (_itemConfigurationService is null)
        {
            return null;
        }

        RequestItemConfigurationSet declarations;
        try
        {
            declarations = await _itemConfigurationService.GetConfigurationsAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // A declaration the screen cannot read is a source that exists but cannot be read, so no card on
            // this load draws a detail cell and the failure is recorded rather than papered over.
            AppLog.Error(
                "Waitlist",
                ex,
                "The Items' declared fields could not be read, so no card on this load draws a detail cell.");
            return null;
        }

        foreach (var itemCode in requests
            .Select(request => request.Item)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!declarations.Get(itemCode).IsAvailable)
            {
                AppLog.Warn(
                    "Waitlist",
                    $"Item '{itemCode}' has no usable declaration, so its card draws no detail cells. The gap needs chasing up (FR-058).");
            }
        }

        return declarations;
    }

    /// <summary>
    /// Fills a row's cells from its Item's declaration: one cell per declared field, in declared order, with
    /// every field that resolved nothing left out entirely (FR-006, FR-026).
    /// </summary>
    private static void AddDeclaredCells(
        SampleOrder item,
        WaitlistRequest request,
        RequestJobPartAvailability? jobAvailability,
        RequestItemConfiguration? declaration)
    {
        foreach (var cell in ResolveDeclaredCells(declaration, request, jobAvailability))
        {
            item.Cells.Add(cell);
        }
    }

    /// <summary>
    /// The cells one row draws: one per field the Item's declaration carries <b>that is one of the Item's
    /// cells</b>, resolved by that field's own source and laid out in declared order, with the hide rule's two
    /// halves applied — a cell the Item never declares is never produced, and a cell whose source exists but
    /// cannot be read is not produced either and is recorded in the store's log instead (FR-025, FR-026,
    /// FR-056, FR-058, FR-101, D41).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>WaitlistLineCardCell.CellsToDraw</c> is the one place the "a cell that resolved nothing is not
    /// drawn" rule lives, so this method hands it every declared field's cell and draws what comes back.
    /// </para>
    /// <para>
    /// <c>WaitlistCardCellRules</c> is the one place the other half lives: an Item declares fields its card
    /// does not show, and those are excluded here rather than removed from configuration, which is what keeps
    /// the exclusion a card rule and lets the two disagree without either being quietly edited (FR-056,
    /// <c>data-model.md</c> §9). An excluded field is working as designed, so nothing is logged for it.
    /// </para>
    /// </remarks>
    /// <param name="declaration">The Item's configuration row, or null when it could not be read.</param>
    /// <param name="request">The request the row was built from.</param>
    /// <param name="jobAvailability">The requesting job's snapshot, or null before one has arrived.</param>
    /// <returns>The cells to draw, in declared order; nothing at all when the declaration cannot be read.</returns>
    public static IReadOnlyList<WaitlistLineCardCell> ResolveDeclaredCells(
        RequestItemConfiguration? declaration,
        WaitlistRequest request,
        RequestJobPartAvailability? jobAvailability)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (declaration is null || !declaration.IsAvailable || declaration.DetailFields.Count == 0)
        {
            return Array.Empty<WaitlistLineCardCell>();
        }

        var candidates = new List<WaitlistLineCardCell?>(declaration.DetailFields.Count);
        foreach (var field in declaration.DetailFields)
        {
            if (field is null || !WaitlistCardCellRules.IsCell(request.Item, field.Label))
            {
                continue;
            }

            candidates.Add(ResolveDeclaredCell(field, request, jobAvailability));
        }

        return WaitlistCardCellRules.OrderForPanel(request.Item, WaitlistLineCardCell.CellsToDraw(candidates));
    }

    /// <summary>
    /// One declared field as a cell, carrying the declared label as the resource key it is and whatever its
    /// source resolved to. A field whose source exists but cannot be read resolves to nothing — and is recorded
    /// — so it is hidden rather than explained on the card or filled with a substitute (FR-058, FR-101, D41).
    /// </summary>
    private static WaitlistLineCardCell ResolveDeclaredCell(
        RequestItemFieldDefinition? field,
        WaitlistRequest request,
        RequestJobPartAvailability? jobAvailability)
    {
        if (field is null)
        {
            return new WaitlistLineCardCell();
        }

        var label = field.Label ?? string.Empty;
        var source = field.Source?.Trim() ?? string.Empty;

        if (WaitlistCardCellRules.IsDieShopField(request.Item, label))
        {            // The die Items' die-shop cell is the one cell with no label: its phrase IS its value, so what the
            // requester ticked is read here and the phrase — chosen by the Item's direction — is what lands in
            // the cell. An unticked checkbox is no answer, so the third cell stays absent rather than drawing
            // the other half of the choice (FR-055, FR-081). The answer is read as a labelled one, never as
            // the request's single answer, so the die the flow asked for can never be mistaken for this flag.
            var given = CapturedAnswer(label, request, required: false);

            return new WaitlistLineCardCell
            {
                Value = WaitlistCardCellRules.IsDieShopAnswerGiven(given)
                    ? WaitlistCardCellRules.DieShopPhrase(request.Item) ?? string.Empty
                    : string.Empty,
                Order = field.Order,
            };
        }

        if (string.Equals(source, RequestItemFieldDefinition.Sources.Answer, StringComparison.OrdinalIgnoreCase))
        {
            var answer = CapturedAnswer(label, request, field.IsRequired);

            if (answer is null && field.IsRequired)
            {
                WarnUnreadableDeclaredField(
                    request,
                    label,
                    "the answer it declares it needs was not captured");
            }

            return new WaitlistLineCardCell
            {
                Label = label,
                Value = answer ?? string.Empty,
                Order = field.Order,

                // The one cell on a hopper card that pictures the answer it shows: the size word is also the key
                // that size's reference picture is filed under, so the card opens the same picture the New
                // Request hopper step drew (FR-077, FR-078, D22). Any other answer-sourced cell opens none.
                ReferencePartNumber = WaitlistCardCellRules.ReferencePicturePart(request.Item, label, answer) ?? string.Empty,
                ReferencePartScope = WaitlistCardCellRules.ReferencePictureScope,
            };
        }

        if (string.Equals(source, RequestItemFieldDefinition.Sources.Job, StringComparison.OrdinalIgnoreCase))
        {
            // A label the requesting job does not carry is the designed absence, not a fault: a job with no die
            // draws no Die cell and nothing is logged for it (FR-057, RequestJobFieldValues). The label that
            // resolves is the declared one, and an answer captured under it outranks the job's own (FR-082).
            var jobValue = RequestJobFieldValues.Resolve(label, jobAvailability, AnswersWithComponent(request));
            return new WaitlistLineCardCell { Label = label, Value = jobValue ?? string.Empty, Order = field.Order };
        }

        if (string.Equals(source, RequestItemFieldDefinition.Sources.Fixed, StringComparison.OrdinalIgnoreCase))
        {
            // A fixed field is one the request itself decides, and `Destination` is the one such field that is
            // a cell: every Deliver Item is delivered to the work centre that asked for it, so the cell reads
            // the request's own requesting work centre and nothing asks the requester where it goes (FR-042,
            // FR-057, research R1). Any other fixed label resolves nothing, so its cell is hidden without a
            // warning — the value does not exist yet rather than existing and failing to be read.
            var fixedValue = WaitlistCardCellRules.RequestWorkCentreValue(label, request);
            return new WaitlistLineCardCell { Label = label, Value = fixedValue ?? string.Empty, Order = field.Order };
        }

        WarnUnreadableDeclaredField(
            request,
            label,
            $"its declared source '{source}' is not one this build can read");

        return new WaitlistLineCardCell { Label = label, Order = field.Order };
    }

    /// <summary>
    /// The request's captured answers with the one answer the flow asked for added under <c>Component</c>, for an
    /// Item that names a component. The request stores that answer on itself rather than among the labelled ones,
    /// and the component's description and quantity are looked up by it.
    /// </summary>
    private static IReadOnlyDictionary<string, string>? AnswersWithComponent(WaitlistRequest request)
    {
        var item = request.Item ?? string.Empty;
        if (!item.EndsWith("-component", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(request.InputValue)
            || (request.Answers is not null && request.Answers.Keys.Any(key => string.Equals(key?.Trim(), "Component", StringComparison.OrdinalIgnoreCase))))
        {
            return request.Answers;
        }

        var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (request.Answers is not null)
        {
            foreach (var answer in request.Answers)
            {
                merged[answer.Key] = answer.Value;
            }
        }

        merged["Component"] = request.InputValue.Trim();
        return merged;
    }

    /// <summary>
    /// The answer a declared answer-sourced field reads.
    /// </summary>
    /// <remarks>
    /// A labelled entry the request captured under the declared label wins, because that is the request's own
    /// answer for that field. Failing that, a <b>required</b> answer-sourced field reads the one answer the flow
    /// asked for and the request stores on itself — <see cref="WaitlistRequest.Answers"/> holds only what was
    /// captured <i>beyond</i> it — which is how a one-answer Item's cell resolves. An <b>optional</b>
    /// answer-sourced field reads only its own labelled entry, so the die-shop checkbox the requester never
    /// ticked stays absent instead of reading an answer the flow asked for under another label (D40, FR-055).
    /// A required answer the request does not carry resolves to nothing, and the caller records that gap.
    /// </remarks>
    private static string? CapturedAnswer(string label, WaitlistRequest request, bool required)
    {
        if (!string.IsNullOrWhiteSpace(label) && request.Answers is { Count: > 0 } answers)
        {
            foreach (var answer in answers)
            {
                if (string.Equals(answer.Key?.Trim(), label.Trim(), StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(answer.Value))
                {
                    return answer.Value.Trim();
                }
            }
        }

        return required && !string.IsNullOrWhiteSpace(request.InputValue)
            ? request.InputValue.Trim()
            : null;
    }

    /// <summary>
    /// Records a declared field whose source exists but cannot be read, so the gap can be chased up later
    /// (FR-058, FR-101, D41). It is never drawn as a reason on the card and never filled with a substitute.
    /// </summary>
    private static void WarnUnreadableDeclaredField(WaitlistRequest request, string label, string reason) =>
        AppLog.Warn(
            "Waitlist",
            $"The card for request {request.Id:D} (item '{request.Item}') cannot read its declared field '{label}': {reason}. The cell is hidden.");

    /// <summary>
    /// The requesting job for one work centre, read once per load and held for that work centre, and reused by a
    /// reload raised by a change in the store, and re-read by the next cold load.
    /// Where a card's two lines need a value the request never stored — the job's own part number, the die it is
    /// assigned, the die's location — this is where it comes from (FR-053).
    /// </summary>
    /// <remarks>
    /// A job that cannot be read is not worth failing the whole list for: the row falls back to what the request
    /// itself carries, the failure is logged, and the card stays readable rather than the list going empty.
    /// </remarks>
    private async Task<RequestJobPartAvailability> ResolveJobAvailabilityAsync(string? workCenter)
    {
        var key = workCenter?.Trim() ?? string.Empty;
        if (key.Length == 0 || _jobAvailabilityProvider is null)
        {
            return RequestJobPartAvailability.None;
        }

        if (_jobAvailabilityCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var availability = RequestJobPartAvailability.None;
        try
        {
            availability = await _jobAvailabilityProvider.GetAvailabilityAsync(key).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            AppLog.Error("Waitlist", ex, $"Reading the job for work center '{key}' failed while building the cards' lines.");
        }

        _jobAvailabilityCache[key] = availability;
        return availability;
    }

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
            ? s_defaultMaxAllotted
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

                // Checked once per work centre rather than once per row, because every row of a work centre draws
                // the same picture. A resolved path that is not a picture the application will draw is not an
                // answer: it is stored as empty so the card draws the application's no-image placeholder rather
                // than a hole. The share is asked about first, because this probe is against the picture share
                // and a share that is not answering costs a failed lookup per work centre on every rebuild
                // (PictureShareReachability); the answer it gives when it is silent is the same one the probe
                // would have produced.
                var shareRoot = PictureShareReachability.ShareRootOf(resolvedPath);

                if ((shareRoot is not null && PictureShareReachability.IsReachable(shareRoot) is false)
                    || ImageFileProbe.IsUsablePicture(resolvedPath) is false)
                {
                    AppLog.Info(
                        "WaitlistRequest",
                        $"Work center '{workCenter.DisplayName}' has no picture to resolve to '{resolvedPath}'; the request page will draw the work centre placeholder instead.");
                    resolvedPath = string.Empty;
                }

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
        await ApplyResolvedPartPictureAsync(order, cancellationToken).ConfigureAwait(false);
        await ApplyResolvedCellReferencePicturesAsync(order, cancellationToken).ConfigureAwait(false);

        if (workCenterImageLookup.TryGetValue(request.WorkCenter, out var workCenterImage))
        {
            order.WorkCenterCatalogId = workCenterImage.WorkCenterId;

            // Empty when the work centre's file carries no picture, which draws the work centre's own placeholder
            // rather than a hole where a picture should be. See the check that emptied it.
            order.WorkCenterImagePath = workCenterImage.ResolvedPath;
        }
    }

    /// <summary>
    /// Resolves the row's <b>material part's</b> picture and carries it on the row, so the card draws a value it
    /// was handed. A request that names no material, and a material whose picture cannot be resolved, both leave
    /// the row empty — and the card then draws the one shared placeholder rather than a picture of the kind of
    /// request or the part's family (FR-014, FR-019, FR-020).
    /// </summary>
    /// <remarks>
    /// A resolver that cannot answer is not worth failing the whole list for: the row keeps its placeholder, the
    /// reason is logged, and the list stays readable.
    /// </remarks>
    private async Task ApplyResolvedPartPictureAsync(SampleOrder order, CancellationToken cancellationToken)
    {
        if (_partPictureResolver is null
            || string.IsNullOrWhiteSpace(order.MaterialPartNumber)
            || string.IsNullOrWhiteSpace(order.MaterialPartScope))
        {
            return;
        }

        try
        {
            var key = PictureCacheKey(order.MaterialPartScope, order.MaterialPartNumber);

            if (_resolvedPictureCache.TryGetValue(key, out var remembered))
            {
                order.ResolvedPartImagePath = remembered;
                return;
            }

            var resolved = await _partPictureResolver
                .ResolvePartPicturePathAsync(order.MaterialPartScope, order.MaterialPartNumber, cancellationToken)
                .ConfigureAwait(false);

            _resolvedPictureCache[key] = resolved ?? string.Empty;

            if (string.IsNullOrWhiteSpace(resolved))
            {
                AppLog.Info(
                    "WaitlistRequest",
                    $"Request '{order.RequestId}' has no picture for its part '{order.MaterialPartNumber}'; the card will draw the no-image placeholder.");
                return;
            }

            order.ResolvedPartImagePath = resolved;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            AppLog.Error(
                "WaitlistRequest",
                ex,
                $"Resolving the picture for part '{order.MaterialPartNumber}' failed; the card will draw the no-image placeholder.");
        }
    }

    /// <summary>
    /// Resolves every cell's own reference picture and carries it on the cell, so the card draws a value it was
    /// handed rather than resolving a storage root of its own (FR-078).
    /// </summary>
    /// <remarks>
    /// Only the cells that open a picture are asked about, so an ordinary card costs no read at all. The hopper
    /// size's picture follows the application's one picture rule through the resolver, and a size whose picture
    /// is missing or unusable leaves the cell's path empty so the card draws the shared placeholder rather than a
    /// blank (FR-080). A resolver that cannot answer is not worth failing the list for: the reason is logged and
    /// the cell keeps its placeholder.
    /// </remarks>
    private async Task ApplyResolvedCellReferencePicturesAsync(SampleOrder order, CancellationToken cancellationToken)
    {
        if (_partPictureResolver is null)
        {
            return;
        }

        foreach (var cell in order.Cells)
        {
            if (!cell.OpensReferencePicture)
            {
                continue;
            }

            try
            {
                var key = PictureCacheKey(cell.ReferencePartScope, cell.ReferencePartNumber);

                if (_resolvedPictureCache.TryGetValue(key, out var remembered))
                {
                    cell.ResolvedReferenceImagePath = remembered;
                    continue;
                }

                var resolved = await _partPictureResolver
                    .ResolvePartPicturePathAsync(cell.ReferencePartScope, cell.ReferencePartNumber, cancellationToken)
                    .ConfigureAwait(false);

                _resolvedPictureCache[key] = resolved ?? string.Empty;

                if (string.IsNullOrWhiteSpace(resolved))
                {
                    AppLog.Info(
                        "WaitlistRequest",
                        $"Request '{order.RequestId}' has no picture for '{cell.ReferencePartNumber}'; the cell will draw the no-image placeholder.");
                    continue;
                }

                cell.ResolvedReferenceImagePath = resolved;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                AppLog.Error(
                    "WaitlistRequest",
                    ex,
                    $"Resolving the reference picture for '{cell.ReferencePartNumber}' failed; the cell will draw the no-image placeholder.");
            }
        }
    }

    /// <summary>
    /// The key a resolved picture is remembered under: the scope and the part, because a picture is resolved for
    /// a part in a scope and for nothing else. The two are joined with a separator neither can contain, so a part
    /// whose name happens to end in another scope's name cannot be answered with the wrong picture.
    /// </summary>
    /// <param name="scope">The store scope the part's picture lives in.</param>
    /// <param name="partNumber">The part the picture is of.</param>
    private static string PictureCacheKey(string? scope, string? partNumber) =>
        $"{scope?.Trim()}\u001F{partNumber?.Trim()}";
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
    /// Reads the Local Work Centers (hot work centers) assigned to this computer. Called on each cold load, which is
    /// this screen's refresh point, so a machine whose assignment was changed in Settings is picked up on the next
    /// load rather than needing a restart.
    /// </summary>
    /// <remarks>
    /// A failed read keeps the set the last successful read produced rather than emptying it: an empty set would
    /// turn the filter into a screen that always says nothing is waiting, which is the one answer a failed read
    /// cannot support. On a host with no reader at all (a unit test) the set stays empty, and the filter is only
    /// reachable through the toggle, which a test sets deliberately.
    /// </remarks>
    private async Task LoadLocalWorkCenterNamesAsync()
    {
        if (_workCenterCatalogService is null)
        {
            return;
        }

        try
        {
            var workstationName = _workCenterCatalogService.GetCurrentComputerName();
            var hotWorkCenters = await _workCenterCatalogService
                .GetHotWorkCenterNamesAsync(workstationName)
                .ConfigureAwait(false);

            _localWorkCenterNames = new HashSet<string>(
                hotWorkCenters
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Select(name => name.Trim()),
                StringComparer.OrdinalIgnoreCase);

            AppLog.Info("Waitlist", $"Local Work Centers loaded. Workstation='{workstationName}', Count={_localWorkCenterNames.Count}.");
        }
        catch (Exception ex)
        {
            AppLog.Error("Waitlist", ex, "Reading this computer's Local Work Centers failed; the list is keeping the assignment it last read.");
        }
    }

    /// <summary>
    /// Whether a row is for one of the given Local Work Centers. A row that names no work center is not one of
    /// them: the filter may only include a work center it can actually match.
    /// </summary>
    public static bool IsLocalWorkCenterOrder(SampleOrder order, IEnumerable<string> localWorkCenterNames)
    {
        if (order is null || localWorkCenterNames is null)
        {
            return false;
        }

        var workCenter = order.RequestedPressName?.Trim() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(workCenter)
            && localWorkCenterNames.Any(name => string.Equals(name?.Trim(), workCenter, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Narrows a set of rows to those whose work center is one of the given Local Work Centers (used by the
    /// screen's own filter).
    /// </summary>
    public static IReadOnlyList<SampleOrder> FilterToLocalWorkCenters(IEnumerable<SampleOrder> source, IEnumerable<string> localWorkCenterNames)
    {
        if (source is null)
        {
            return Array.Empty<SampleOrder>();
        }

        return source.Where(order => IsLocalWorkCenterOrder(order, localWorkCenterNames)).ToArray();
    }

    public Task RefreshAsync() => LoadOrdersAsync(_buildingSelectionService.SelectedBuilding);
    public string SearchQuery
    {
        get; private set;
    } = string.Empty;

    public void UpdateSearchSuggestions(string? query)
    {
        // The suggestion list is the search box's ItemsSource, so it may only be changed on the thread that owns
        // that box. This is called from the load's own continuation as well as from the box: every await in the
        // load resumes off the UI thread, so an unmarshalled change would rewrite a bound collection from a pool
        // thread — and the box, or the list it is bound to, is enumerating it at that moment. That is the fault
        // that ends the process as a stowed exception with no account of itself, so the change is handed to the
        // UI thread instead. A headless host (a unit test) has no queue and applies it where it is.
        if (_dispatcherQueue is DispatcherQueue dispatcher && !dispatcher.HasThreadAccess)
        {
            if (!dispatcher.TryEnqueue(() => UpdateSearchSuggestions(query)))
            {
                AppLog.Info("Waitlist", "The search suggestions were not refreshed because the UI queue is not accepting work.");
            }

            return;
        }

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
