using System.Collections.ObjectModel;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;
using MTM_Waitlist.Module_Waitlist.ViewModels;
using MTM_Waitlist.Tests.Fixtures;

namespace MTM_Waitlist.Tests.Module_Waitlist.ViewModels;

/// <summary>
/// The list's notice surface: the strip is offered only to a person who may handle requests (FR-038), it holds one
/// bar per undismissed notice, the dismissal removes that bar and only that bar (FR-036), and a change recorded
/// before the list opened is never shown as though it had just happened (FR-037).
/// </summary>
[TestClass]
public sealed class WaitlistViewViewModelNoticeTests
{
    private const string HandlerEmployeeNumber = "9001";

    [TestMethod]
    public async Task ViewModel_WaitlistViewViewModelNoticeTests_Poll_APersonWithoutTheHandlerPermissionNeverAsksForNotices()
    {
        var notices = new FakeNoticeService();
        notices.Notices.Add(Notice(1));
        var viewModel = BuildViewModel(holdsHandlerPermission: false, notices);

        await viewModel.OpenLuggerChangeNoticesAsync().ConfigureAwait(false);
        await viewModel.PollLuggerChangeNoticesAsync().ConfigureAwait(false);

        Assert.AreEqual(0, notices.OpenCalls, "Opening the list on behalf of a non-handler asks for nothing.");
        Assert.AreEqual(0, notices.PollCalls, "A person who does not hold the handler permission is never polled for notices.");
        Assert.AreEqual(0, viewModel.LuggerChangeNotices.Count, "And is not interrupted by a bar.");
    }

    [TestMethod]
    public async Task ViewModel_WaitlistViewViewModelNoticeTests_Poll_APersonHoldingThePermissionDrawsOneBarPerUndismissedNotice()
    {
        var notices = new FakeNoticeService();
        notices.Notices.Add(Notice(1));
        notices.Notices.Add(Notice(2));
        var viewModel = BuildViewModel(holdsHandlerPermission: true, notices);

        await viewModel.PollLuggerChangeNoticesAsync().ConfigureAwait(false);

        Assert.AreEqual(1, notices.PollCalls, "The holder's tick asks the store.");
        CollectionAssert.AreEqual(
            new long[] { 1, 2 },
            viewModel.LuggerChangeNotices.Select(notice => notice.Id).ToArray(),
            "Two undismissed notices draw two bars, in id order.");
    }

    [TestMethod]
    public async Task ViewModel_WaitlistViewViewModelNoticeTests_DismissLuggerChangeNotice_RemovesThatBarAndOnlyThatBar()
    {
        var notices = new FakeNoticeService();
        notices.Notices.Add(Notice(1));
        notices.Notices.Add(Notice(2));
        var viewModel = BuildViewModel(holdsHandlerPermission: true, notices);
        await viewModel.PollLuggerChangeNoticesAsync().ConfigureAwait(false);

        viewModel.DismissLuggerChangeNoticeCommand.Execute(viewModel.LuggerChangeNotices.First());

        CollectionAssert.AreEqual(
            new long[] { 2 },
            viewModel.LuggerChangeNotices.Select(notice => notice.Id).ToArray(),
            "Closing one bar leaves the other in place.");
        CollectionAssert.AreEqual(
            new long[] { 1 },
            notices.Dismissals.Select(dismissal => dismissal.ChangeId).ToArray(),
            "The closed bar's dismissal is recorded against the session.");
        Assert.IsTrue(notices.Dismissals.Single().RaisedByCloseButton, "The notice's close button is a dismissal.");
    }

    [TestMethod]
    public async Task ViewModel_WaitlistViewViewModelNoticeTests_Poll_ADismissedBarStaysGoneOnALaterTick()
    {
        var notices = new FakeNoticeService();
        notices.Notices.Add(Notice(1));
        notices.Notices.Add(Notice(2));
        var viewModel = BuildViewModel(holdsHandlerPermission: true, notices);
        await viewModel.PollLuggerChangeNoticesAsync().ConfigureAwait(false);
        viewModel.DismissLuggerChangeNoticeCommand.Execute(viewModel.LuggerChangeNotices.First());

        await viewModel.PollLuggerChangeNoticesAsync().ConfigureAwait(false);

        CollectionAssert.AreEqual(
            new long[] { 2 },
            viewModel.LuggerChangeNotices.Select(notice => notice.Id).ToArray(),
            "A dismissed notice does not return on a later tick.");
    }

    [TestMethod]
    public async Task ViewModel_WaitlistViewViewModelNoticeTests_OpenLuggerChangeNotices_ReportsNoNoticeForAChangeRecordedBeforeTheListOpened()
    {
        var arrangement = new FakeArrangementService { Watermark = 5 };
        arrangement.Changes.AddRange(
        [
            new ArrangementChange { Id = 4, ChangeKind = ArrangementChange.Kinds.LuggerAdded, LuggerName = "Lugger A" },
            new ArrangementChange { Id = 5, ChangeKind = ArrangementChange.Kinds.LuggerRenamed, LuggerName = "Lugger B", PreviousValue = "Lugger A" },
        ]);
        var viewModel = BuildViewModel(
            holdsHandlerPermission: true,
            notices: new LuggerChangeNoticeService(arrangement));

        await viewModel.OpenLuggerChangeNoticesAsync().ConfigureAwait(false);
        await viewModel.PollLuggerChangeNoticesAsync().ConfigureAwait(false);

        Assert.AreEqual(0, viewModel.LuggerChangeNotices.Count, "A change recorded before the list opened is never shown.");

        arrangement.Changes.Add(new ArrangementChange
        {
            Id = 6,
            ChangeKind = ArrangementChange.Kinds.StatusChanged,
            LuggerName = "Lugger A",
            ScrapType = "PET",
            PreviousValue = "Full",
            NewValue = "Available",
        });

        await viewModel.PollLuggerChangeNoticesAsync().ConfigureAwait(false);

        CollectionAssert.AreEqual(
            new long[] { 6 },
            viewModel.LuggerChangeNotices.Select(notice => notice.Id).ToArray(),
            "Only the change made while the list was open is shown.");
    }

    private static LuggerChangeNotice Notice(long id) => new()
    {
        Id = id,
        ChangeKind = ArrangementChange.Kinds.LuggerAdded,
        LuggerName = "Lugger A",
        MessageTemplate = "Lugger_Notice.LuggerAdded",
        MessageArguments = ["Lugger A"],
    };

    private static WaitlistViewViewModel BuildViewModel(bool holdsHandlerPermission, ILuggerChangeNoticeService notices) => new(
        new WaitlistTestNavigationService(),
        new WaitlistTestBuildingSelectionService(),
        new QuietRequestService(),
        imageLocationService: null,
        dispatcherQueue: null,
        personIdentity: new FakePersonIdentity
        {
            EmployeeNumber = HandlerEmployeeNumber,
            DisplayName = "Morgan Reyes",
        },
        storeAvailabilityTracker: null,
        actionPrompt: new NoOpWaitlistRequestActionPrompt(),
        permissionService: new StubPermissionService(holdsHandlerPermission),
        luggerChangeNoticeService: notices);

    /// <summary>One answer for the one permission the list asks about.</summary>
    private sealed class StubPermissionService : IPermissionService
    {
        private readonly bool _holds;

        public StubPermissionService(bool holds) => _holds = holds;

        public Task<bool> HasPermissionAsync(string permissionKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(_holds);

        public Task<IReadOnlyDictionary<string, bool>> HasPermissionsAsync(
            IEnumerable<string> permissionKeys,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<string, bool>>(
                permissionKeys.ToDictionary(key => key, _ => _holds, StringComparer.Ordinal));

        public void Invalidate()
        {
        }
    }

    /// <summary>The notice seam as the list meets it: canned notices, and a record of what was asked and dismissed.</summary>
    private sealed class FakeNoticeService : ILuggerChangeNoticeService
    {
        public ObservableCollection<LuggerChangeNotice> Notices { get; } = new();

        public List<(long ChangeId, bool RaisedByCloseButton)> Dismissals { get; } = [];

        public int OpenCalls { get; private set; }

        public int PollCalls { get; private set; }

        public Task<IReadOnlyList<LuggerChangeNotice>> OpenSessionAsync(CancellationToken cancellationToken = default)
        {
            OpenCalls++;
            return Task.FromResult<IReadOnlyList<LuggerChangeNotice>>(Notices.ToArray());
        }

        public Task<IReadOnlyList<LuggerChangeNotice>> PollAsync(CancellationToken cancellationToken = default)
        {
            PollCalls++;
            return Task.FromResult<IReadOnlyList<LuggerChangeNotice>>(Notices.ToArray());
        }

        public Task<IReadOnlyList<LuggerChangeNotice>> DismissAsync(
            long changeId,
            bool raisedByCloseButton,
            CancellationToken cancellationToken = default)
        {
            Dismissals.Add((changeId, raisedByCloseButton));

            if (raisedByCloseButton)
            {
                var dismissed = Notices.FirstOrDefault(notice => notice.Id == changeId);

                if (dismissed is not null)
                {
                    Notices.Remove(dismissed);
                }
            }

            return Task.FromResult<IReadOnlyList<LuggerChangeNotice>>(Notices.ToArray());
        }
    }

    /// <summary>The arrangement seam as the real notice service meets it: the watermark and the rows above it.</summary>
    private sealed class FakeArrangementService : ILuggerArrangementService
    {
        public long Watermark { get; set; }

        public List<ArrangementChange> Changes { get; } = [];

        public Task<long> GetChangeWatermarkAsync(CancellationToken cancellationToken = default) => Task.FromResult(Watermark);

        public Task<IReadOnlyList<ArrangementChange>> GetChangesAfterAsync(
            long afterId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ArrangementChange>>(Changes.Where(change => change.Id > afterId).ToArray());

        public Task<LuggerArrangementSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The notice seam reads changes, not the snapshot.");

        public Task<IReadOnlyList<Lugger>> GetLuggersAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The notice seam reads changes, not the lugger list.");

        public Task<IReadOnlyList<ScrapAssignment>> GetAssignmentsAsync(
            string scrapTypeKey,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The notice seam reads changes, not the assignment list.");

        public Task<int> AddLuggerAsync(string luggerName, int displayPosition, string actorEmployeeNumber, string actorEmployeeName, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The notice seam never writes the arrangement.");

        public Task<int> RenameLuggerAsync(string luggerPublicId, string luggerName, string actorEmployeeNumber, string actorEmployeeName, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The notice seam never writes the arrangement.");

        public Task<int> SetLuggerPositionAsync(string luggerPublicId, int displayPosition, string actorEmployeeNumber, string actorEmployeeName, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The notice seam never writes the arrangement.");

        public Task<int> RemoveLuggerAsync(string luggerPublicId, string actorEmployeeNumber, string actorEmployeeName, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The notice seam never writes the arrangement.");

        public Task<int> SetStatusAsync(string luggerPublicId, string status, string? reason, string actorEmployeeNumber, string actorEmployeeName, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The notice seam never writes the arrangement.");

        public Task<int> PromoteLuggerAsync(string luggerPublicId, string scrapTypeKey, string actorEmployeeNumber, string actorEmployeeName, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The notice seam never writes the arrangement.");

        public Task<int> AssignScrapTypeAsync(string scrapType, string luggerPublicId, int sortPosition, string actorEmployeeNumber, string actorEmployeeName, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The notice seam never writes the arrangement.");

        public Task<int> UnassignScrapTypeAsync(string scrapTypeKey, string luggerPublicId, string actorEmployeeNumber, string actorEmployeeName, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The notice seam never writes the arrangement.");
    }

    /// <summary>The list's own store read, quiet: these cases never load rows.</summary>
    private sealed class QuietRequestService : IWaitlistRequestService
    {
        public event EventHandler? RequestsChanged
        {
            add { }
            remove { }
        }

        public IReadOnlyList<WaitlistRequest> GetActiveRequests(string? building = null) => [];

        public WaitlistRequest? GetRequest(Guid requestId) => null;

        public IReadOnlyList<WaitlistRequest> GetMyRequests(string requesterEmployeeNumber, string? building = null) => [];

        public IReadOnlyList<WaitlistRequestAuditEntry> GetAuditTrail(Guid requestId) => [];

        public Task<IReadOnlyList<WaitlistRequestAuditEntry>> LoadAuditTrailAsync(Guid requestId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WaitlistRequestAuditEntry>>([]);

        public void Reset()
        {
        }

        public Task<int> RefreshFromDatabaseAsync(string? building = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<WaitlistRequestSubmitResult> SubmitAsync(WaitlistRequestDraft draft, bool allowDuplicate, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("These cases never raise a request.");

        public Task<bool> TransitionStatusAsync(Guid requestId, string status, string? cancellationReason = null, string? canceledByEmployeeNumber = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("These cases never change a request.");

        public Task<WaitlistRequestCancelResult> CancelOwnRequestAsync(Guid requestId, string requesterEmployeeNumber, string? reason = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("These cases never change a request.");

        public Task<WaitlistRequest?> UpdateNoteAsync(Guid requestId, string? note, string? actorEmployeeNumber = null, string? actorEmployeeName = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("These cases never change a request.");

        public Task<WaitlistRequest?> AcceptAsync(Guid requestId, string handlerEmployeeNumber, string? handlerEmployeeName = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("These cases never change a request.");

        public Task<WaitlistRequest?> CompleteAsync(Guid requestId, string handlerEmployeeNumber, string? handlerEmployeeName = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("These cases never change a request.");

        public Task<WaitlistRequest?> ReleaseAsync(Guid requestId, string handlerEmployeeNumber, string? handlerEmployeeName = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("These cases never change a request.");

        public Task<int> SetHandlerStatusAsync(Guid requestId, string? handlerStatus, string expectedMaterialHandler, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("These cases never change a request.");
    }
}
