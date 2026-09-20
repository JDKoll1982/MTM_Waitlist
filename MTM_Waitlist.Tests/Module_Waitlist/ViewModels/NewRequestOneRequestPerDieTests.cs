using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;
using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Tests.Module_Waitlist.ViewModels;

/// <summary>
/// FR-054, the confirmation half: the operator who chose three dies gets three requests, never one request that
/// carries three dies, and the step says how many were raised.
/// </summary>
/// <remarks>
/// The value the request stores is the one column that distinguishes two entries raised from the same job, so a
/// request per die is what keeps them apart on the queue (D22).
/// </remarks>
[TestClass]
public sealed class NewRequestOneRequestPerDieTests
{
    [TestMethod]
    public void ToDrafts_WithTwoDiesChosen_IsOneDraftPerDie()
    {
        var state = DieState(("FGT0002000", "DIE SHOP"), ("FGT0002001", "PRESS BAY"));

        var drafts = state.ToDrafts();

        Assert.AreEqual(2, drafts.Count, "One request per die chosen (FR-054).");
        CollectionAssert.AreEqual(
            new[] { "FGT0002000-DIE SHOP", "FGT0002001-PRESS BAY" },
            drafts.Select(draft => draft.InputValue).ToArray(),
            "Each request carries the die it is for, in the order the operator chose them.");
        Assert.AreEqual(
            0,
            drafts.Count(draft => string.IsNullOrWhiteSpace(draft.Item)),
            "Every raised request names the Item, so none of them is a bare entry.");
    }

    [TestMethod]
    public void ToDrafts_WithNoDieChosen_IsTheSingleRequestTheWizardAlwaysRaised()
    {
        // An Item that asks nothing, or a request raised before the die step existed, still produces exactly one
        // request — the change adds requests, it does not replace the single-request path.
        var state = new NewRequestFlowState
        {
            WorkCenter = "100-7",
            Category = RequestCategory.Other,
            Item = RequestItemCatalog.FindById("other"),
            InputValue = "Skid 4471 is on the wrong dock",
        };

        var drafts = state.ToDrafts();

        Assert.AreEqual(1, drafts.Count);
        Assert.AreEqual("Skid 4471 is on the wrong dock", drafts[0].InputValue);
    }

    [TestMethod]
    public void ToDraft_IsTheFirstOfTheDrafts()
    {
        // The single-draft entry point is what the rest of the wizard reads; it must not disagree with the list.
        var state = DieState(("FGT0002000", "DIE SHOP"), ("FGT0002001", "PRESS BAY"));

        Assert.AreEqual(state.ToDrafts()[0].InputValue, state.ToDraft().InputValue);
    }

    [TestMethod]
    public async Task ConfirmStep_RaisesOneRequestForEachDieTheOperatorChose()
    {
        var requests = new RecordingRequestService();
        var viewModel = new NewRequestSummaryViewModel(new WaitlistTestNavigationService(), requests, new NoCoilService());

        viewModel.OnNavigatedTo(DieState(("FGT0002000", "DIE SHOP"), ("FGT0002001", "PRESS BAY")));
        await viewModel.SubmitCommand.ExecuteAsync(null);

        Assert.AreEqual(2, requests.Submitted.Count, "Two dies chosen is two requests raised (FR-054).");
        CollectionAssert.AreEqual(
            new[] { "FGT0002000-DIE SHOP", "FGT0002001-PRESS BAY" },
            requests.Submitted.Select(draft => draft.InputValue).ToArray(),
            "Each raised request carries the die it was raised for.");
    }

    [TestMethod]
    public async Task ConfirmStep_WithSeveralDies_ReportsHowManyWereRaised()
    {
        var requests = new RecordingRequestService();
        var viewModel = new NewRequestSummaryViewModel(new WaitlistTestNavigationService(), requests, new NoCoilService());

        viewModel.OnNavigatedTo(DieState(("FGT0002000", "DIE SHOP"), ("FGT0002001", "PRESS BAY")));
        await viewModel.SubmitCommand.ExecuteAsync(null);

        Assert.IsTrue(viewModel.HasMultipleRequests, "The step says how many entries it is about to raise.");
        Assert.IsTrue(
            viewModel.RequestCountText.Contains('2'),
            $"The report names the number of requests: '{viewModel.RequestCountText}'.");
        Assert.IsFalse(
            viewModel.RequestCountText.StartsWith("NewRequest_", StringComparison.Ordinal),
            "The report is plain language, never a bare resource key (FR-022).");
    }

    [TestMethod]
    public async Task ConfirmStep_WithOneDie_SaysNothingAboutSeveral()
    {
        var requests = new RecordingRequestService();
        var viewModel = new NewRequestSummaryViewModel(new WaitlistTestNavigationService(), requests, new NoCoilService());

        viewModel.OnNavigatedTo(DieState(("FGT0002000", "DIE SHOP")));
        await viewModel.SubmitCommand.ExecuteAsync(null);

        Assert.AreEqual(1, requests.Submitted.Count);
        Assert.IsFalse(viewModel.HasMultipleRequests, "One request is the ordinary case and needs no count.");
    }

    private static NewRequestFlowState DieState(params (string PartNumber, string Location)[] dies)
    {
        var parts = dies.Select(die => new RequestDiePart { PartNumber = die.PartNumber, Location = die.Location }).ToArray();

        return new NewRequestFlowState
        {
            WorkCenter = "100-7",
            Category = RequestCategory.Pickup,
            Item = RequestItemCatalog.FindById("pickup-die"),
            Availability = (RequestJobPartAvailability.None with { HasActiveJob = true, HasDie = true }).WithDies(parts),
            SelectedDies = parts,
            InputValue = parts[0].Label,
        };
    }

    private sealed class NoCoilService : ICoilAvailabilityService
    {
        public Task<WaitlistCoilInfo> GetCoilForJobAsync(string? workCenter, CancellationToken cancellationToken = default)
            => Task.FromResult(new WaitlistCoilInfo { HasCoil = false });
    }

    private sealed class RecordingRequestService : IWaitlistRequestService
    {
        public List<WaitlistRequestDraft> Submitted { get; } = new();

        public event EventHandler? RequestsChanged
        {
            add { }
            remove { }
        }

        public IReadOnlyList<WaitlistRequest> GetActiveRequests(string? building = null) => [];

        public WaitlistRequest? GetRequest(Guid requestId) => null;

        public IReadOnlyList<WaitlistRequest> GetMyRequests(string requesterEmployeeNumber, string? building = null) => [];

        public IReadOnlyList<WaitlistRequestAuditEntry> GetAuditTrail(Guid requestId) => [];

        public Task<IReadOnlyList<WaitlistRequestAuditEntry>> LoadAuditTrailAsync(Guid requestId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<WaitlistRequestAuditEntry>>([]);

        public void Reset()
        {
        }

        public Task<int> RefreshFromDatabaseAsync(string? building = null, CancellationToken cancellationToken = default) => Task.FromResult(0);

        public Task<WaitlistRequestSubmitResult> SubmitAsync(WaitlistRequestDraft draft, bool allowDuplicate, CancellationToken cancellationToken = default)
        {
            Submitted.Add(draft);
            return Task.FromResult(WaitlistRequestSubmitResult.Success(new WaitlistRequest
            {
                Id = Guid.NewGuid(),
                Building = draft.Building,
                WorkCenter = draft.WorkCenter,
                Category = draft.Category,
                Item = draft.Item,
                InputValue = draft.InputValue,
            }));
        }

        public Task<bool> TransitionStatusAsync(Guid requestId, string status, string? cancellationReason = null, string? canceledByEmployeeNumber = null, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<WaitlistRequestCancelResult> CancelOwnRequestAsync(Guid requestId, string requesterEmployeeNumber, string? reason = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<WaitlistRequest?> UpdateNoteAsync(Guid requestId, string? note, string? actorEmployeeNumber = null, string? actorEmployeeName = null, CancellationToken cancellationToken = default) => Task.FromResult<WaitlistRequest?>(null);

        public Task<WaitlistRequest?> AcceptAsync(Guid requestId, string handlerEmployeeNumber, string? handlerEmployeeName = null, CancellationToken cancellationToken = default) => Task.FromResult<WaitlistRequest?>(null);

        public Task<WaitlistRequest?> CompleteAsync(Guid requestId, string handlerEmployeeNumber, string? handlerEmployeeName = null, CancellationToken cancellationToken = default) => Task.FromResult<WaitlistRequest?>(null);

        public Task<WaitlistRequest?> ReleaseAsync(Guid requestId, string handlerEmployeeNumber, string? handlerEmployeeName = null, CancellationToken cancellationToken = default) => Task.FromResult<WaitlistRequest?>(null);
    }
}
