using CommunityToolkit.Mvvm.Input;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;
using MTM_Waitlist.Module_Waitlist.ViewModels;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Waitlist.ViewModels;

/// <summary>
/// The waitlist handler actions (contract <c>action-contracts.md</c> C1–C5, gates G4/G5/G7). Two things are
/// proved here: the screen offers exactly what the policy allows and nothing more, and every command reaches
/// the service with the signed-in identity rather than the row's — a screen is never the authority.
/// </summary>
/// <remarks>
/// The expected values are written out as a table rather than computed from the policy, so a change to the
/// policy cannot quietly redefine the test's answer.
/// </remarks>
[TestClass]
public sealed class WaitlistViewViewModelActionTests
{
    private const string HandlerEmployeeNumber = "9001";
    private const string OtherHandlerEmployeeNumber = "9002";
    private const string RequesterEmployeeNumber = "6331";
    private const string HandlerRole = "Material Handler";

    /// <summary>A role that is not at handler level, so the handler gate must refuse it.</summary>
    private const string NonHandlerRole = "Contractor";

    private static readonly DateTimeOffset Now = new(2026, 9, 12, 12, 0, 0, TimeSpan.Zero);

    private static readonly Guid PendingRequestId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TakenByViewerRequestId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TakenByOtherRequestId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid CompletedRequestId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid CanceledRequestId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    // ── G4: the gate matrix ──────────────────────────────────────────────────────────────────────────

    [DataTestMethod]
    [DataRow(HandlerRole, "Pending", "none", true, false, false, DisplayName = "handler sees Accept on an available request")]
    [DataRow(HandlerRole, "Accepted", "self", false, true, true, DisplayName = "the assignee sees Complete, and Cancel comes with it")]
    [DataRow(HandlerRole, "Accepted", "other", false, false, false, DisplayName = "another handler sees nothing on a taken request")]
    [DataRow(HandlerRole, "Completed", "self", false, false, false, DisplayName = "nothing is offered on a finished request")]
    [DataRow(HandlerRole, "Canceled", "self", false, false, false, DisplayName = "nothing is offered on a cancelled request")]
    [DataRow(NonHandlerRole, "Pending", "none", false, false, false, DisplayName = "a non-handler is offered no handler action")]
    [DataRow(NonHandlerRole, "Accepted", "self", false, false, false, DisplayName = "a non-handler is offered nothing on a taken request")]
    [DataRow(HandlerRole, "Pending", "requester", true, false, true, DisplayName = "the requester who is also a handler may accept and cancel")]
    [DataRow(NonHandlerRole, "Pending", "requester", false, false, true, DisplayName = "a non-handler requester may still withdraw their own waiting request")]
    [DataRow(NonHandlerRole, "Accepted", "requester", false, false, false, DisplayName = "a requester may not cancel once the request is taken by someone else")]
    public async Task Gates_MatchThePolicyForEveryViewerAndState(
        string role,
        string status,
        string assignee,
        bool expectAccept,
        bool expectCompleteOrRelease,
        bool expectCancel)
    {
        var viewerEmployeeNumber = assignee == "requester" ? RequesterEmployeeNumber : HandlerEmployeeNumber;
        var assigneeNumber = assignee switch
        {
            "self" => viewerEmployeeNumber,
            "other" => OtherHandlerEmployeeNumber,
            _ => null,
        };
        var request = BuildRequest(RequestIdFor(status), status, assigneeNumber, RequesterEmployeeNumber);
        var viewModel = BuildViewModel(role, viewerEmployeeNumber, new RecordingRequestService(request));

        await viewModel.RefreshAsync().ConfigureAwait(false);

        var row = SingleRow(viewModel);

        Assert.AreEqual(expectAccept, row.CanAccept, "Accept gate");
        Assert.AreEqual(expectCompleteOrRelease, row.CanCompleteOrRelease, "Complete/Release gate");
        Assert.AreEqual(expectCancel, row.CanCancelRequest, "Cancel gate");

        // The policy is the same answer, stated independently — a drift here is the defect FR-018 forbids.
        Assert.AreEqual(
            RequestActionPolicy.CanViewerAccept(status, RequestActionPolicy.CanViewerHandleRequests(role)),
            row.CanAccept,
            "The screen's Accept gate disagrees with RequestActionPolicy.");
        Assert.AreEqual(
            RequestActionPolicy.CanViewerCompleteOrRelease(status, assigneeNumber, viewerEmployeeNumber, RequestActionPolicy.CanViewerHandleRequests(role)),
            row.CanCompleteOrRelease,
            "The screen's Complete/Release gate disagrees with RequestActionPolicy.");
    }

    [DataTestMethod]
    [DataRow(HandlerRole, "Pending", "none", DisplayName = "available request")]
    [DataRow(HandlerRole, "Accepted", "self", DisplayName = "request taken by the viewer")]
    [DataRow(HandlerRole, "Accepted", "other", DisplayName = "request taken by someone else")]
    [DataRow(NonHandlerRole, "Pending", "requester", DisplayName = "own waiting request as a non-handler")]
    public async Task Gates_EveryTrueFlagCarriesACommandAndNoFlagCarriesNone(string role, string status, string assignee)
    {
        var viewerEmployeeNumber = assignee == "requester" ? RequesterEmployeeNumber : HandlerEmployeeNumber;
        var assigneeNumber = assignee switch
        {
            "self" => viewerEmployeeNumber,
            "other" => OtherHandlerEmployeeNumber,
            _ => null,
        };
        var request = BuildRequest(RequestIdFor(status), status, assigneeNumber, RequesterEmployeeNumber);
        var viewModel = BuildViewModel(role, viewerEmployeeNumber, new RecordingRequestService(request));

        await viewModel.RefreshAsync().ConfigureAwait(false);

        var row = SingleRow(viewModel);
        var offeredCommands = new[] { row.AcceptCommand, row.CompleteCommand, row.CancelCommand }
            .Count(command => command is not null);

        Assert.AreEqual(
            row.OfferedActionCount,
            offeredCommands,
            "A gate flag with no command, or a command with no flag, is a control that cannot act (FR-020).");

        Assert.AreEqual(row.CanAccept, row.AcceptCommand is not null, "Accept flag and command disagree.");
        Assert.AreEqual(row.CanCompleteOrRelease, row.CompleteCommand is not null, "Complete flag and command disagree.");
        Assert.AreEqual(row.CanCancelRequest, row.CancelCommand is not null, "Cancel flag and command disagree.");

        // The card draws [primary][Cancel], where the primary is Accept while available and Complete once
        // claimed — so exactly one of the two is ever live.
        Assert.IsFalse(
            row.CanAccept && row.CanCompleteOrRelease,
            "Accept and Complete must never be offered at the same time; one replaces the other.");
    }

    [TestMethod]
    public async Task Card_ShowsCancelWheneverCompleteIsOffered()
    {
        // The pairing the card is designed around: once the viewer has claimed a request, refusing it is as
        // available as finishing it.
        var request = BuildRequest(TakenByViewerRequestId, "Accepted", assignee: HandlerEmployeeNumber, RequesterEmployeeNumber);
        var viewModel = BuildViewModel(HandlerRole, HandlerEmployeeNumber, new RecordingRequestService(request));

        await viewModel.RefreshAsync().ConfigureAwait(false);

        var row = SingleRow(viewModel);

        Assert.IsTrue(row.CanCompleteOrRelease, "The assignee must be offered Complete.");
        Assert.IsTrue(row.CanCancelRequest, "Cancel must be offered whenever Complete is.");
        Assert.IsNotNull(row.CancelCommand, "The Cancel button must carry a command when it is shown.");
    }

    // ── G5: the commands reach the service with the signed-in identity ───────────────────────────────

    [TestMethod]
    public async Task Complete_CallsTheServiceWithTheSignedInAssignee()
    {
        var request = BuildRequest(TakenByViewerRequestId, "Accepted", assignee: HandlerEmployeeNumber, RequesterEmployeeNumber);
        var service = new RecordingRequestService(request);
        var viewModel = BuildViewModel(HandlerRole, HandlerEmployeeNumber, service);

        await viewModel.RefreshAsync().ConfigureAwait(false);
        var row = SingleRow(viewModel);

        await viewModel.CompleteRequestCommand.ExecuteAsync(row).ConfigureAwait(false);

        Assert.AreEqual(1, service.CompleteCalls.Count, "Complete must reach the service exactly once.");
        Assert.AreEqual(TakenByViewerRequestId, service.CompleteCalls[0].RequestId, "Complete targeted the wrong request.");
        Assert.AreEqual(HandlerEmployeeNumber, service.CompleteCalls[0].EmployeeNumber, "Complete must act as the signed-in handler.");

        Assert.AreEqual(0, service.ReleaseCalls.Count, "The card offers no Release button, so nothing may reach the release path.");
    }

    [TestMethod]
    public async Task CompleteAndRelease_AreNotOfferedToAHandlerTheRequestIsNotAssignedTo()
    {
        var request = BuildRequest(TakenByOtherRequestId, "Accepted", assignee: OtherHandlerEmployeeNumber, RequesterEmployeeNumber);
        var service = new RecordingRequestService(request);
        var viewModel = BuildViewModel(HandlerRole, HandlerEmployeeNumber, service);

        await viewModel.RefreshAsync().ConfigureAwait(false);
        var row = SingleRow(viewModel);

        Assert.IsFalse(row.CanCompleteOrRelease, "A request assigned to someone else must offer no handler action.");
        Assert.IsFalse(row.CanCancelRequest, "A request assigned to someone else must offer no Cancel either.");
        Assert.IsNull(row.CompleteCommand, "Complete was carried on a request the viewer does not own.");
        Assert.IsNull(row.CancelCommand, "Cancel was carried on a request the viewer does not own.");
    }

    // ── G7: the list leads with the most urgent work ──────────────────────────────────────────────────

    [TestMethod]
    public async Task List_LeadsWithTheMostOverdueRowThenTheLeastRemainingTime()
    {
        var overdueId = Guid.Parse("a0000000-0000-0000-0000-000000000001");
        var soonId = Guid.Parse("a0000000-0000-0000-0000-000000000002");
        var noTargetId = Guid.Parse("a0000000-0000-0000-0000-000000000003");
        var laterId = Guid.Parse("a0000000-0000-0000-0000-000000000004");

        var overdue = BuildRequest(overdueId, "Pending", null, RequesterEmployeeNumber, createdUtc: Now.AddMinutes(-40), targetUtc: Now.AddMinutes(-10));
        var soon = BuildRequest(soonId, "Pending", null, RequesterEmployeeNumber, createdUtc: Now.AddMinutes(-5), targetUtc: Now.AddMinutes(5));
        var noTarget = BuildRequest(noTargetId, "Pending", null, RequesterEmployeeNumber, createdUtc: Now.AddMinutes(-20), targetUtc: null);
        var later = BuildRequest(laterId, "Pending", null, RequesterEmployeeNumber, createdUtc: Now.AddMinutes(-5), targetUtc: Now.AddMinutes(60));

        // Served in a deliberately unhelpful order, so the ordering under test is the list's own.
        var service = new RecordingRequestService(later, noTarget, soon, overdue);
        var viewModel = BuildViewModel(HandlerRole, HandlerEmployeeNumber, service);

        await viewModel.RefreshAsync().ConfigureAwait(false);

        var actual = viewModel.Source.Select(row => row.RequestId).ToArray();

        CollectionAssert.AreEqual(
            new Guid?[] { overdueId, soonId, noTargetId, laterId },
            actual,
            "The list must lead with overdue work and then the least remaining time; the request with no stored target is ordered by its derived due time.");
    }

    [TestMethod]
    public async Task List_OrderNeverContradictsTheCountdownOnEachCard()
    {
        var a = Guid.Parse("b0000000-0000-0000-0000-000000000001");
        var b = Guid.Parse("b0000000-0000-0000-0000-000000000002");
        var c = Guid.Parse("b0000000-0000-0000-0000-000000000003");
        var overdue = Guid.Parse("b0000000-0000-0000-0000-000000000004");

        var service = new RecordingRequestService(
            BuildRequest(a, "Pending", null, RequesterEmployeeNumber, createdUtc: Now.AddMinutes(-5), targetUtc: Now.AddMinutes(90)),
            BuildRequest(b, "Pending", null, RequesterEmployeeNumber, createdUtc: Now.AddMinutes(-5), targetUtc: Now.AddMinutes(3)),
            BuildRequest(c, "Pending", null, RequesterEmployeeNumber, createdUtc: Now.AddMinutes(-5), targetUtc: Now.AddMinutes(30)),
            BuildRequest(overdue, "Pending", null, RequesterEmployeeNumber, createdUtc: Now.AddMinutes(-90), targetUtc: Now.AddMinutes(-30)));

        var viewModel = BuildViewModel(HandlerRole, HandlerEmployeeNumber, service);
        await viewModel.RefreshAsync().ConfigureAwait(false);

        // The card's own countdown text, read back as an urgency rank: overdue is most urgent, then the
        // fewest minutes remaining. A row that says it is more urgent must never sit below a calmer one.
        var ranks = viewModel.Source.Select(row => UrgencyRank(row.RemainingTimeText)).ToArray();

        for (var i = 1; i < ranks.Length; i++)
        {
            Assert.IsTrue(
                ranks[i] <= ranks[i - 1],
                $"Row {i} reads '{viewModel.Source[i].RemainingTimeText}', which is more urgent than the row above it ('{viewModel.Source[i - 1].RemainingTimeText}'), so the order contradicts the cards.");
        }
    }

    // ── US3: the order the viewer remembers (FR-010, FR-011, FR-012) ─────────────────────────────

    [TestMethod]
    public async Task List_NothingRemembered_LeadsWithTheMostUrgentRow()
    {
        var overdueId = Guid.Parse("c0000000-0000-0000-0000-000000000001");
        var calmId = Guid.Parse("c0000000-0000-0000-0000-000000000002");

        // Served alpha-first, so a list that merely kept the store's order would look sorted and be wrong.
        var service = new RecordingRequestService(
            BuildRequest(calmId, "Pending", null, RequesterEmployeeNumber, createdUtc: Now.AddMinutes(-5), targetUtc: Now.AddMinutes(55), workCenter: "Alpha Line"),
            BuildRequest(overdueId, "Pending", null, RequesterEmployeeNumber, createdUtc: Now.AddMinutes(-30), targetUtc: Now.AddMinutes(-10), workCenter: "Bravo Line"));
        var viewModel = BuildViewModel(HandlerRole, HandlerEmployeeNumber, service, sortPreferenceService: new StubSortPreferenceService(null));

        await viewModel.RefreshAsync().ConfigureAwait(false);

        Assert.AreEqual(WaitlistSortOrder.MostUrgent, viewModel.SortOrder, "A viewer who has chosen nothing gets the most-urgent order.");
        CollectionAssert.AreEqual(
            new Guid?[] { overdueId, calmId },
            viewModel.Source.Select(row => row.RequestId).ToArray(),
            "The list must default to most-urgent-first (FR-010).");
    }

    [TestMethod]
    public async Task List_AppliesTheRememberedOrder_OnLoad()
    {
        var overdueId = Guid.Parse("d0000000-0000-0000-0000-000000000001");
        var calmId = Guid.Parse("d0000000-0000-0000-0000-000000000002");

        var service = new RecordingRequestService(
            BuildRequest(calmId, "Pending", null, RequesterEmployeeNumber, createdUtc: Now.AddMinutes(-5), targetUtc: Now.AddMinutes(55), workCenter: "Alpha Line"),
            BuildRequest(overdueId, "Pending", null, RequesterEmployeeNumber, createdUtc: Now.AddMinutes(-30), targetUtc: Now.AddMinutes(-10), workCenter: "Bravo Line"));
        var viewModel = BuildViewModel(HandlerRole, HandlerEmployeeNumber, service, sortPreferenceService: new StubSortPreferenceService(WaitlistSortOrder.Press));

        await viewModel.RefreshAsync().ConfigureAwait(false);

        Assert.AreEqual(WaitlistSortOrder.Press, viewModel.SortOrder, "The remembered order is the one the list loads with (FR-011).");
        CollectionAssert.AreEqual(
            new Guid?[] { calmId, overdueId },
            viewModel.Source.Select(row => row.RequestId).ToArray(),
            "The rows must follow the remembered order, not the order the store happened to return (SC-009).");
    }

    [TestMethod]
    public async Task ApplySortOrder_SwitchingTheOrder_KeepsEveryAffordanceAndTheOverdueMarking()
    {
        var overdueId = Guid.Parse("e0000000-0000-0000-0000-000000000001");
        var claimedId = Guid.Parse("e0000000-0000-0000-0000-000000000002");

        var service = new RecordingRequestService(
            BuildRequest(overdueId, "Pending", null, RequesterEmployeeNumber, createdUtc: Now.AddMinutes(-30), targetUtc: Now.AddMinutes(-10), workCenter: "Bravo Line"),
            BuildRequest(claimedId, "Accepted", HandlerEmployeeNumber, RequesterEmployeeNumber, createdUtc: Now.AddMinutes(-5), targetUtc: Now.AddMinutes(55), workCenter: "Alpha Line"));
        var viewModel = BuildViewModel(HandlerRole, HandlerEmployeeNumber, service);

        await viewModel.RefreshAsync().ConfigureAwait(false);

        var before = viewModel.Source.ToDictionary(row => row.RequestId!.Value);
        Assert.AreEqual(2, before.Count, "The fixture must surface both rows, or the check proves nothing.");

        viewModel.ApplySortOrder(WaitlistSortOrder.Press);

        CollectionAssert.AreEqual(
            new Guid?[] { claimedId, overdueId },
            viewModel.Source.Select(row => row.RequestId).ToArray(),
            "Switching the order must re-order the rows the viewer is looking at.");

        foreach (var row in viewModel.Source)
        {
            var was = before[row.RequestId!.Value];
            Assert.AreEqual(was.CanAccept, row.CanAccept, "Re-ordering must not change what the viewer may do to a row (FR-012).");
            Assert.AreEqual(was.CanCompleteOrRelease, row.CanCompleteOrRelease, "Re-ordering must not change the claimed own work's actions.");
            Assert.AreEqual(was.CanCancelRequest, row.CanCancelRequest, "Re-ordering must not change the cancel affordance either.");
            Assert.AreEqual(was.IsOverdue, row.IsOverdue, "Overdue work stays visibly overdue whatever the sort (FR-012).");
            Assert.AreEqual(was.IsOverdueAtSource, row.IsOverdueAtSource, "The overdue mark the card is drawn from must survive a re-order.");
            Assert.AreEqual(was.RemainingTimeText, row.RemainingTimeText, "The countdown must survive a re-order unchanged.");
        }

        Assert.IsTrue(viewModel.Source.Single(row => row.RequestId == overdueId).IsOverdue, "The overdue row must still say so after the sort changed.");
    }

    [TestMethod]
    public async Task ApplySortOrder_ChoosingTheOrderAlreadyInForce_LeavesTheRowsInPlace()
    {
        var request = BuildRequest(PendingRequestId, "Pending", null, RequesterEmployeeNumber);
        var viewModel = BuildViewModel(HandlerRole, HandlerEmployeeNumber, new RecordingRequestService(request));

        await viewModel.RefreshAsync().ConfigureAwait(false);
        var loaded = viewModel.Source.ToArray();

        viewModel.ApplySortOrder(WaitlistSortOrder.MostUrgent);

        CollectionAssert.AreEqual(loaded, viewModel.Source.ToArray(), "Re-applying the order already in force must not rebuild the list.");
    }

    [TestMethod]
    public async Task Accept_CallsTheServiceWithTheSignedInHandlerAndTheRowsRequestId()
    {
        var request = BuildRequest(PendingRequestId, "Pending", assignee: null, RequesterEmployeeNumber);
        var service = new RecordingRequestService(request);
        var viewModel = BuildViewModel(HandlerRole, HandlerEmployeeNumber, service);

        await viewModel.RefreshAsync().ConfigureAwait(false);
        await viewModel.AcceptRequestCommand.ExecuteAsync(SingleRow(viewModel)).ConfigureAwait(false);

        Assert.AreEqual(1, service.AcceptCalls.Count, "Accept must reach the service exactly once.");
        Assert.AreEqual(PendingRequestId, service.AcceptCalls[0].RequestId, "Accept targeted the wrong request.");
        Assert.AreEqual(HandlerEmployeeNumber, service.AcceptCalls[0].EmployeeNumber, "Accept must claim the request for the signed-in handler.");
        Assert.AreEqual(string.Empty, viewModel.ActionRefusalMessage, "A successful accept must not report a refusal.");
    }

    [TestMethod]
    public async Task Accept_WhenTheStoreRefuses_ReportsItAndWarnsTheHandler()
    {
        var request = BuildRequest(PendingRequestId, "Pending", assignee: null, RequesterEmployeeNumber);
        var service = new RecordingRequestService(request) { AcceptResult = null };
        var prompt = new NoOpWaitlistRequestActionPrompt();
        var viewModel = BuildViewModel(HandlerRole, HandlerEmployeeNumber, service, prompt);

        await viewModel.RefreshAsync().ConfigureAwait(false);
        await viewModel.AcceptRequestCommand.ExecuteAsync(SingleRow(viewModel)).ConfigureAwait(false);

        Assert.IsTrue(viewModel.HasActionRefusalMessage, "A refused accept that says nothing is the failure mode this feature removes.");
        Assert.AreEqual(
            "Waitlist_Action.Refused.AcceptTaken".GetLocalized(),
            viewModel.ActionRefusalMessage,
            "The refusal must come from the resource mechanism rather than being a literal written into the view model.");
        Assert.AreEqual(1, prompt.WarningCallCount, "A refused claim must warn the handler, not just set a message they may never see.");
        Assert.AreEqual("Waitlist_Action.AcceptRefusedTitle".GetLocalized(), prompt.LastWarningTitle, "The warning carried the wrong title.");
    }

    [TestMethod]
    public async Task Accept_WhenTheRequestLeftTheList_TellsTheHandlerToChooseSomethingElse()
    {
        // The store has no such request at all, so "another handler took it" would be the wrong thing to say.
        var request = BuildRequest(PendingRequestId, "Pending", assignee: null, RequesterEmployeeNumber);
        var service = new RecordingRequestService(request) { AcceptResult = null };
        var prompt = new NoOpWaitlistRequestActionPrompt();
        var viewModel = BuildViewModel(HandlerRole, HandlerEmployeeNumber, service, prompt);

        await viewModel.RefreshAsync().ConfigureAwait(false);

        // The request has left the store between this screen's last read and the claim.
        service.ForgetRequests = true;

        await viewModel.AcceptRequestCommand.ExecuteAsync(SingleRow(viewModel)).ConfigureAwait(false);

        Assert.AreEqual(
            "Waitlist_Action.Refused.AcceptGone".GetLocalized(),
            viewModel.ActionRefusalMessage,
            "A request that is gone must be reported as gone, not as taken by someone else.");
        Assert.AreEqual(1, prompt.WarningCallCount, "The handler must be warned either way.");
    }

    [TestMethod]
    public async Task Accept_TwoHandlersRaceForTheSameRequest_FirstWinsAndSecondIsWarned()
    {
        // One shared store, two handlers, both looking at the same available request. This is the only test
        // where the store arbitrates, so the winner is decided by the store and not by either screen.
        var request = BuildRequest(PendingRequestId, "Pending", assignee: null, RequesterEmployeeNumber);
        var store = new FirstClaimWinsRequestService(request);

        var winnerPrompt = new NoOpWaitlistRequestActionPrompt();
        var loserPrompt = new NoOpWaitlistRequestActionPrompt();
        var winner = BuildViewModel(HandlerRole, HandlerEmployeeNumber, store, winnerPrompt);
        var loser = BuildViewModel(HandlerRole, OtherHandlerEmployeeNumber, store, loserPrompt);

        await winner.RefreshAsync().ConfigureAwait(false);
        await loser.RefreshAsync().ConfigureAwait(false);

        Assert.IsTrue(SingleRow(winner).CanAccept, "The request should be available to both handlers before either claims it.");
        Assert.IsTrue(SingleRow(loser).CanAccept, "The request should be available to both handlers before either claims it.");

        await winner.AcceptRequestCommand.ExecuteAsync(SingleRow(winner)).ConfigureAwait(false);
        await loser.AcceptRequestCommand.ExecuteAsync(SingleRow(loser)).ConfigureAwait(false);

        // The store's answer is the authority: the first claim stands.
        Assert.AreEqual(
            HandlerEmployeeNumber,
            store.Current.AssignedMaterialHandler,
            "The first handler to claim the request must keep it.");
        Assert.IsTrue(
            store.GetActiveRequests("Expo Drive").Any(item => item.Id == PendingRequestId),
            "A claimed request stays on the shared list.");

        Assert.AreEqual(string.Empty, winner.ActionRefusalMessage, "The winner must not be told anything went wrong.");
        Assert.AreEqual(0, winnerPrompt.WarningCallCount, "The winner must not be warned.");

        Assert.AreEqual(
            "Waitlist_Action.Refused.AcceptTaken".GetLocalized(),
            loser.ActionRefusalMessage,
            "The handler who lost the race must be told the request was already taken.");
        Assert.AreEqual(1, loserPrompt.WarningCallCount, "The handler who lost the race must be warned in a dialog they have to acknowledge.");
        Assert.AreEqual(
            "Waitlist_Action.AcceptRefusedTitle".GetLocalized(),
            loserPrompt.LastWarningTitle,
            "The lost-claim warning carried the wrong title.");
    }

    [TestMethod]
    public void LostClaimWarning_SaysAnotherHandlerTookItAndToChooseSomethingElse()
    {
        // The wording is a product requirement, not an implementation detail: it is pinned here so a later
        // edit to the copy has to be deliberate. The dialog and the inline refusal share this one string.
        var resourcePath = Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "Strings",
            "en-us",
            "Resources.resw");

        Assert.IsTrue(File.Exists(resourcePath), $"The resource file was not found at '{resourcePath}'.");

        var document = System.Xml.Linq.XDocument.Load(resourcePath);
        var value = document
            .Descendants("data")
            .Where(data => (string?)data.Attribute("name") == "Waitlist_Action.Refused.AcceptTaken")
            .Select(data => (string?)data.Element("value") ?? string.Empty)
            .FirstOrDefault();

        Assert.IsFalse(string.IsNullOrWhiteSpace(value), "Waitlist_Action.Refused.AcceptTaken has no copy.");
        Assert.IsTrue(
            value!.Contains("another handler", StringComparison.OrdinalIgnoreCase),
            "The lost-claim warning must say another handler took the request: '" + value + "'.");
        Assert.IsTrue(
            value.Contains("different", StringComparison.OrdinalIgnoreCase),
            "The lost-claim warning must tell the handler to choose something else: '" + value + "'.");
    }

    [TestMethod]
    public async Task Accept_WhenTheViewerIsNotAHandler_IsNotOfferedAndNeverCallsTheService()
    {
        var request = BuildRequest(PendingRequestId, "Pending", assignee: null, RequesterEmployeeNumber);
        var service = new RecordingRequestService(request);
        var viewModel = BuildViewModel(NonHandlerRole, HandlerEmployeeNumber, service);

        await viewModel.RefreshAsync().ConfigureAwait(false);

        Assert.IsFalse(SingleRow(viewModel).CanAccept, "The screen offered a non-handler an action they cannot take.");
        Assert.AreEqual(0, service.AcceptCalls.Count, "A non-handler must never reach the accept path.");
    }

    // ── Requester cancel: confirmation and reason come first ─────────────────────────────────────────

    [TestMethod]
    public async Task Cancel_CallsTheServiceWithTheRequesterIdentityAndThePromptedReason()
    {
        var request = BuildRequest(PendingRequestId, "Pending", assignee: null, RequesterEmployeeNumber);
        var service = new RecordingRequestService(request);
        var prompt = new NoOpWaitlistRequestActionPrompt { CancellationReasonResult = "Raised by mistake" };
        var viewModel = BuildViewModel(NonHandlerRole, RequesterEmployeeNumber, service, prompt);

        await viewModel.RefreshAsync().ConfigureAwait(false);
        await viewModel.CancelRequestCommand.ExecuteAsync(SingleRow(viewModel)).ConfigureAwait(false);

        Assert.AreEqual(1, prompt.CancellationPromptCallCount, "Cancel must ask before it acts.");
        Assert.AreEqual(1, service.CancelCalls.Count, "Cancel must reach the store exactly once.");
        Assert.AreEqual(PendingRequestId, service.CancelCalls[0].RequestId, "Cancel targeted the wrong request.");
        Assert.AreEqual(RequesterEmployeeNumber, service.CancelCalls[0].EmployeeNumber, "Cancel must act as the requester.");
        Assert.AreEqual("Raised by mistake", service.CancelCalls[0].Reason, "The reason the requester gave must be the reason recorded.");
        Assert.AreEqual(string.Empty, viewModel.ActionRefusalMessage, "A successful cancellation must not report a refusal.");
    }

    [TestMethod]
    public async Task Cancel_WhenThePromptIsDismissed_PerformsNoActionAndSaysNothing()
    {
        var request = BuildRequest(PendingRequestId, "Pending", assignee: null, RequesterEmployeeNumber);
        var service = new RecordingRequestService(request);
        var prompt = new NoOpWaitlistRequestActionPrompt { CancellationReasonResult = null };
        var viewModel = BuildViewModel(NonHandlerRole, RequesterEmployeeNumber, service, prompt);

        await viewModel.RefreshAsync().ConfigureAwait(false);
        await viewModel.CancelRequestCommand.ExecuteAsync(SingleRow(viewModel)).ConfigureAwait(false);

        Assert.AreEqual(1, prompt.CancellationPromptCallCount, "The prompt must have been shown.");
        Assert.AreEqual(0, service.CancelCalls.Count, "A dismissed confirmation must not cancel anything.");
        Assert.AreEqual(string.Empty, viewModel.ActionRefusalMessage, "Backing out is not a refusal and must not be reported as one.");
    }

    [TestMethod]
    public async Task Cancel_WithAnEmptyReason_IsStillAConfirmedCancellation()
    {
        var request = BuildRequest(PendingRequestId, "Pending", assignee: null, RequesterEmployeeNumber);
        var service = new RecordingRequestService(request);
        var prompt = new NoOpWaitlistRequestActionPrompt { CancellationReasonResult = string.Empty };
        var viewModel = BuildViewModel(NonHandlerRole, RequesterEmployeeNumber, service, prompt);

        await viewModel.RefreshAsync().ConfigureAwait(false);
        await viewModel.CancelRequestCommand.ExecuteAsync(SingleRow(viewModel)).ConfigureAwait(false);

        Assert.AreEqual(1, service.CancelCalls.Count, "An empty reason is still a confirmation, not a dismissal.");
        Assert.AreEqual(string.Empty, service.CancelCalls[0].Reason, "The empty reason must be passed through unchanged.");
    }

    [TestMethod]
    public async Task Cancel_WhenTheStoreRefuses_ReportsItInPlainLanguage()
    {
        var request = BuildRequest(PendingRequestId, "Pending", assignee: null, RequesterEmployeeNumber);
        var service = new RecordingRequestService(request)
        {
            CancelResult = WaitlistRequestCancelResult.NotInCancelableState(),
        };
        var prompt = new NoOpWaitlistRequestActionPrompt { CancellationReasonResult = "Changed my mind" };
        var viewModel = BuildViewModel(NonHandlerRole, RequesterEmployeeNumber, service, prompt);

        await viewModel.RefreshAsync().ConfigureAwait(false);
        await viewModel.CancelRequestCommand.ExecuteAsync(SingleRow(viewModel)).ConfigureAwait(false);

        Assert.AreEqual(
            "Waitlist_Action.Refused.Cancel".GetLocalized(),
            viewModel.ActionRefusalMessage,
            "A refused cancellation must say why rather than appearing to work.");
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────────────────────────

    private static Guid RequestIdFor(string status) => status switch
    {
        "Accepted" => TakenByViewerRequestId,
        "Completed" => CompletedRequestId,
        "Canceled" => CanceledRequestId,
        _ => PendingRequestId,
    };

    private static SampleOrder SingleRow(WaitlistViewViewModel viewModel)
    {
        Assert.AreEqual(1, viewModel.Source.Count, "The fixture must surface exactly one row, or the check proved nothing.");
        return viewModel.Source[0];
    }

    /// <summary>
    /// The urgency the card's own countdown text reports, as a sortable number: an overdue row is the most
    /// urgent, then the row with the fewest minutes left. Negative for overdue so it always sorts first.
    /// </summary>
    private static int UrgencyRank(string remainingTimeText)
    {
        if (string.Equals(remainingTimeText, "Overdue", StringComparison.OrdinalIgnoreCase))
        {
            return -1;
        }

        var parts = remainingTimeText.Split(':');
        return parts.Length == 2 && int.TryParse(parts[0], out var minutes) && int.TryParse(parts[1], out var seconds)
            ? (minutes * 60) + seconds
            : int.MaxValue;
    }

    private static WaitlistRequest BuildRequest(
        Guid id,
        string status,
        string? assignee,
        string requesterEmployeeNumber,
        DateTimeOffset? createdUtc = null,
        DateTimeOffset? targetUtc = null,
        DateTimeOffset? lastMessageUtc = null,
        string workCenter = "Expo Line 7",
        string requesterName = "Dana Whitfield") => new()
    {
        Id = id,
        Building = "Expo Drive",
        WorkCenter = workCenter,
        WorkCenterName = workCenter,
        Category = "Pickup",
        Item = "pickup-ncm",
        InputValue = "Skid 4471 is on the wrong dock",
        ActiveSetupJobId = "JOB-9001",
        RequesterEmployeeNumber = requesterEmployeeNumber,
        RequesterEmployeeName = requesterName,
        Status = status,
        AssignedMaterialHandler = assignee,
        RequestedUtc = createdUtc ?? Now.AddMinutes(-20),
        TargetTimeUtc = targetUtc is null && createdUtc is null ? Now.AddMinutes(40) : targetUtc,
        IsOverdue = targetUtc is not null && targetUtc < Now,
        LastMessageUtc = lastMessageUtc,
    };

    private static WaitlistViewViewModel BuildViewModel(
        string role,
        string employeeNumber,
        IWaitlistRequestService requestService,
        IWaitlistRequestActionPrompt? prompt = null,
        IWaitlistMessageSeenStore? messageSeenStore = null,
        IWaitlistSortPreferenceService? sortPreferenceService = null) => new(
        new WaitlistTestNavigationService(),
        new WaitlistTestBuildingSelectionService(),
        requestService,
        imageLocationService: null,
        dispatcherQueue: null,
        startupState: new StartupState
        {
            EmployeeNumber = employeeNumber,
            EmployeeName = "Morgan Reyes",
            CurrentRole = role,
        },
        storeAvailabilityTracker: null,
        actionPrompt: prompt ?? new NoOpWaitlistRequestActionPrompt(),
        messageSeenStore: messageSeenStore,
        sortPreferenceService: sortPreferenceService);

    /// <summary>The viewer's remembered order, without a settings file anywhere near the test.</summary>
    private sealed class StubSortPreferenceService : IWaitlistSortPreferenceService
    {
        public StubSortPreferenceService(string? sortOrder) => Current = WaitlistSortOrder.Normalize(sortOrder);

        public string Current { get; private set; }

        public Task<string> GetSortOrderAsync(CancellationToken cancellationToken = default) => Task.FromResult(Current);

        public Task SetSortOrderAsync(string? sortOrder, CancellationToken cancellationToken = default)
        {
            Current = WaitlistSortOrder.Normalize(sortOrder);
            return Task.CompletedTask;
        }
    }

    // ── The new-message marker answers to messages, not to lifecycle changes ──────────────────────────

    [TestMethod]
    public async Task Card_LifecycleChangeOnly_ShowsNoMessageMarker()
    {
        // Job created, accepted, completed and canceled are things the system recorded. Flagging them turned
        // every routine transition into an unread message, which is exactly what the floor complained about.
        var request = BuildRequest(PendingRequestId, "Accepted", HandlerEmployeeNumber, RequesterEmployeeNumber);
        var viewModel = BuildViewModel(HandlerRole, HandlerEmployeeNumber, new RecordingRequestService(request), messageSeenStore: new RecordingMessageSeenStore());

        await viewModel.RefreshAsync().ConfigureAwait(false);

        var row = SingleRow(viewModel);

        Assert.IsFalse(
            row.HasNewMessages,
            "A request nobody has written on must not carry a message marker, however many lifecycle changes it has had.");
        Assert.AreEqual(string.Empty, row.NewMessageTooltip, "A marker that is not shown must not carry a tooltip.");
    }

    [TestMethod]
    public async Task Card_MessageTheViewerHasNotRead_ShowsTheMarker()
    {
        var written = Now.AddMinutes(-5);
        var request = BuildRequest(PendingRequestId, "Pending", null, RequesterEmployeeNumber, lastMessageUtc: written);
        var viewModel = BuildViewModel(HandlerRole, HandlerEmployeeNumber, new RecordingRequestService(request), messageSeenStore: new RecordingMessageSeenStore());

        await viewModel.RefreshAsync().ConfigureAwait(false);

        var row = SingleRow(viewModel);

        Assert.IsTrue(row.HasNewMessages, "A message written since the viewer last looked must be flagged.");
        Assert.AreEqual(
            "Waitlist_NewMessages.Tooltip".GetLocalized(),
            row.NewMessageTooltip,
            "The marker must explain itself through the resource mechanism.");
    }

    [TestMethod]
    public async Task Card_MessageTheViewerHasAlreadyRead_ShowsNoMarker()
    {
        var written = Now.AddMinutes(-5);
        var request = BuildRequest(PendingRequestId, "Pending", null, RequesterEmployeeNumber, lastMessageUtc: written);
        var seen = new RecordingMessageSeenStore();
        seen.MarkSeenAsync(request.Id, written).GetAwaiter().GetResult();

        var viewModel = BuildViewModel(HandlerRole, HandlerEmployeeNumber, new RecordingRequestService(request), messageSeenStore: seen);

        await viewModel.RefreshAsync().ConfigureAwait(false);

        Assert.IsFalse(
            SingleRow(viewModel).HasNewMessages,
            "Reading a request clears its marker, and a refresh must not raise it again.");
    }

    /// <summary>Remembers what the list recorded as seen, without writing to a settings file.</summary>
    private sealed class RecordingMessageSeenStore : IWaitlistMessageSeenStore
    {
        private readonly Dictionary<Guid, DateTimeOffset> _seen = [];

        public Task<DateTimeOffset?> GetLastSeenUtcAsync(Guid requestId, CancellationToken cancellationToken = default)
            => Task.FromResult(_seen.TryGetValue(requestId, out var seen) ? seen : (DateTimeOffset?)null);

        public Task MarkSeenAsync(Guid requestId, DateTimeOffset seenUtc, CancellationToken cancellationToken = default)
        {
            _seen[requestId] = seenUtc;
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// A request service that serves a fixed set of requests and records every handler action it is asked to
    /// perform, so a test can prove which identity, which request id and which reason reached the store.
    /// </summary>
    private sealed class RecordingRequestService : IWaitlistRequestService
    {
        private readonly WaitlistRequest[] _requests;

        public RecordingRequestService(params WaitlistRequest[] requests)
        {
            _requests = requests;
            AcceptResult = requests[0];
            CompleteResult = requests[0];
            ReleaseResult = requests[0];
            CancelResult = WaitlistRequestCancelResult.Success(requests[0]);
        }

        public event EventHandler? RequestsChanged
        {
            add { }
            remove { }
        }

        /// <summary>What <see cref="AcceptAsync"/> answers; null models the store refusing the claim.</summary>
        public WaitlistRequest? AcceptResult { get; set; }

        /// <summary>What <see cref="CompleteAsync"/> answers; null models the store refusing.</summary>
        public WaitlistRequest? CompleteResult { get; set; }

        /// <summary>What <see cref="ReleaseAsync"/> answers; null models the store refusing.</summary>
        public WaitlistRequest? ReleaseResult { get; set; }

        /// <summary>What <see cref="CancelOwnRequestAsync"/> answers.</summary>
        public WaitlistRequestCancelResult CancelResult { get; set; }

        /// <summary>Every accept the store was asked to perform.</summary>
        public List<(Guid RequestId, string EmployeeNumber)> AcceptCalls { get; } = [];

        /// <summary>Every complete the store was asked to perform.</summary>
        public List<(Guid RequestId, string EmployeeNumber)> CompleteCalls { get; } = [];

        /// <summary>Every release the store was asked to perform.</summary>
        public List<(Guid RequestId, string EmployeeNumber)> ReleaseCalls { get; } = [];

        /// <summary>Every cancel-own the store was asked to perform, with the reason it was given.</summary>
        public List<(Guid RequestId, string EmployeeNumber, string? Reason)> CancelCalls { get; } = [];

        /// <summary>When true, the store no longer knows about the request — it has left the list entirely.</summary>
        public bool ForgetRequests { get; set; }

        public IReadOnlyList<WaitlistRequest> GetActiveRequests(string? building = null) => ForgetRequests ? [] : _requests;

        public WaitlistRequest? GetRequest(Guid requestId)
            => ForgetRequests ? null : _requests.FirstOrDefault(request => request.Id == requestId);

        public IReadOnlyList<WaitlistRequest> GetMyRequests(string requesterEmployeeNumber, string? building = null) => _requests;

        public IReadOnlyList<WaitlistRequestAuditEntry> GetAuditTrail(Guid requestId) => [];

        public Task<IReadOnlyList<WaitlistRequestAuditEntry>> LoadAuditTrailAsync(Guid requestId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<WaitlistRequestAuditEntry>>([]);

        public void Reset()
        {
        }

        public Task<int> RefreshFromDatabaseAsync(string? building = null, CancellationToken cancellationToken = default) => Task.FromResult(_requests.Length);

        public Task<WaitlistRequestSubmitResult> SubmitAsync(WaitlistRequestDraft draft, bool allowDuplicate, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> TransitionStatusAsync(Guid requestId, string status, string? cancellationReason = null, string? canceledByEmployeeNumber = null, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<WaitlistRequestCancelResult> CancelOwnRequestAsync(Guid requestId, string requesterEmployeeNumber, string? reason = null, CancellationToken cancellationToken = default)
        {
            CancelCalls.Add((requestId, requesterEmployeeNumber, reason));
            return Task.FromResult(CancelResult);
        }

        public Task<WaitlistRequest?> UpdateNoteAsync(Guid requestId, string? note, string? actorEmployeeNumber = null, string? actorEmployeeName = null, CancellationToken cancellationToken = default)
            => Task.FromResult<WaitlistRequest?>(null);

        public Task<WaitlistRequest?> AcceptAsync(Guid requestId, string handlerEmployeeNumber, string? handlerEmployeeName = null, CancellationToken cancellationToken = default)
        {
            AcceptCalls.Add((requestId, handlerEmployeeNumber));
            return Task.FromResult(AcceptResult);
        }

        public Task<WaitlistRequest?> CompleteAsync(Guid requestId, string handlerEmployeeNumber, string? handlerEmployeeName = null, CancellationToken cancellationToken = default)
        {
            CompleteCalls.Add((requestId, handlerEmployeeNumber));
            return Task.FromResult(CompleteResult);
        }

        public Task<WaitlistRequest?> ReleaseAsync(Guid requestId, string handlerEmployeeNumber, string? handlerEmployeeName = null, CancellationToken cancellationToken = default)
        {
            ReleaseCalls.Add((requestId, handlerEmployeeNumber));
            return Task.FromResult(ReleaseResult);
        }
    }

    /// <summary>
    /// A store that arbitrates the accept race the way the real one does: the first claim wins and every later
    /// claim gets nothing back. It exists to prove the losing screen is warned rather than quietly doing
    /// nothing, and that the winner's claim is decided by the store rather than by either screen.
    /// </summary>
    private sealed class FirstClaimWinsRequestService : IWaitlistRequestService
    {
        private WaitlistRequest _current;

        public FirstClaimWinsRequestService(WaitlistRequest request) => _current = request;

        public event EventHandler? RequestsChanged
        {
            add { }
            remove { }
        }

        /// <summary>The request's stored state, which is what decides who holds the claim.</summary>
        public WaitlistRequest Current => _current;

        public IReadOnlyList<WaitlistRequest> GetActiveRequests(string? building = null) => [_current];

        public WaitlistRequest? GetRequest(Guid requestId) => requestId == _current.Id ? _current : null;

        public IReadOnlyList<WaitlistRequest> GetMyRequests(string requesterEmployeeNumber, string? building = null) => [_current];

        public IReadOnlyList<WaitlistRequestAuditEntry> GetAuditTrail(Guid requestId) => [];

        public Task<IReadOnlyList<WaitlistRequestAuditEntry>> LoadAuditTrailAsync(Guid requestId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<WaitlistRequestAuditEntry>>([]);

        public void Reset()
        {
        }

        public Task<int> RefreshFromDatabaseAsync(string? building = null, CancellationToken cancellationToken = default) => Task.FromResult(1);

        public Task<WaitlistRequestSubmitResult> SubmitAsync(WaitlistRequestDraft draft, bool allowDuplicate, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> TransitionStatusAsync(Guid requestId, string status, string? cancellationReason = null, string? canceledByEmployeeNumber = null, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<WaitlistRequestCancelResult> CancelOwnRequestAsync(Guid requestId, string requesterEmployeeNumber, string? reason = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<WaitlistRequest?> UpdateNoteAsync(Guid requestId, string? note, string? actorEmployeeNumber = null, string? actorEmployeeName = null, CancellationToken cancellationToken = default)
            => Task.FromResult<WaitlistRequest?>(null);

        public Task<WaitlistRequest?> AcceptAsync(Guid requestId, string handlerEmployeeNumber, string? handlerEmployeeName = null, CancellationToken cancellationToken = default)
        {
            // Read the availability and write the claim as one step. The real store does this atomically; in a
            // single-threaded test one step at a time is the same guarantee, and it is what makes the first
            // caller the winner and every later caller a loser.
            if (!RequestActionPolicy.IsAvailable(_current.Status))
            {
                return Task.FromResult<WaitlistRequest?>(null);
            }

            _current = new WaitlistRequest
            {
                Id = _current.Id,
                Building = _current.Building,
                WorkCenter = _current.WorkCenter,
                WorkCenterName = _current.WorkCenterName,
                Category = _current.Category,
                Item = _current.Item,
                InputValue = _current.InputValue,
                ActiveSetupJobId = _current.ActiveSetupJobId,
                RequesterEmployeeNumber = _current.RequesterEmployeeNumber,
                RequesterEmployeeName = _current.RequesterEmployeeName,
                Status = "Accepted",
                RequestedUtc = _current.RequestedUtc,
                TargetTimeUtc = _current.TargetTimeUtc,
                IsOverdue = _current.IsOverdue,
                AssignedMaterialHandler = handlerEmployeeNumber.Trim(),
                AcceptedUtc = Now,
            };

            return Task.FromResult<WaitlistRequest?>(_current);
        }

        public Task<WaitlistRequest?> CompleteAsync(Guid requestId, string handlerEmployeeNumber, string? handlerEmployeeName = null, CancellationToken cancellationToken = default)
            => Task.FromResult<WaitlistRequest?>(null);

        public Task<WaitlistRequest?> ReleaseAsync(Guid requestId, string handlerEmployeeNumber, string? handlerEmployeeName = null, CancellationToken cancellationToken = default)
            => Task.FromResult<WaitlistRequest?>(null);
    }
}
