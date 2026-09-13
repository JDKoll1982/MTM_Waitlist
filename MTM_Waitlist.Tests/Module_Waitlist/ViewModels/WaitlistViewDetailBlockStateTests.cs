using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;
using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Tests.Module_Waitlist.ViewModels;

/// <summary>
/// US1 check 4 (<c>contracts/verification-gates.md</c> G4 #10's sibling, <c>data-model.md</c> §1).
/// The three block states must stay distinguishable: an empty read hides the block, a failed read renders
/// the error in place with a retry, and neither is ever rendered as the page-level store outage or as
/// absence.
/// </summary>
/// <remarks>
/// The read under test is the section load's request lookup. It is the only read the sections perform that
/// can fail: every other value they render is already in memory on the item.
/// </remarks>
[TestClass]
public sealed class WaitlistViewDetailBlockStateTests
{
    [TestMethod]
    public void FailedSectionRead_ReportsTheFailedStateInPlaceWithARetry()
    {
        var viewModel = BuildViewModel(new ThrowingRequestService());

        viewModel.OnNavigatedTo(4242);

        Assert.IsTrue(viewModel.IsSectionLoadFailed, "A read that threw must report the Failed state.");
        Assert.IsFalse(
            string.IsNullOrWhiteSpace(viewModel.SectionFailureMessage),
            "The Failed state must explain itself rather than render an empty box.");
        Assert.IsNotNull(viewModel.RetryLoadCommand, "The Failed state must offer a retry action.");
        Assert.IsFalse(
            viewModel.IsEmptyStateVisible,
            "Failed must never be rendered as absence: the failure is not the same state as 'nothing to show'.");
        Assert.IsFalse(
            viewModel.IsItemPresent,
            "A failed read must not claim an item resolved, so the Failed state cannot be read as Present.");
        Assert.AreEqual(
            0,
            viewModel.InventoryRows.Count,
            "A failed read must not leave inventory rows behind from a previous load.");
    }

    [TestMethod]
    public void SuccessfulReadWithNoSourcedRows_ReportsTheHiddenStateNotAFailedOrEmptyLabeledShell()
    {
        var requestService = new SparseRequestService();
        var viewModel = BuildViewModel(requestService);

        viewModel.OnNavigatedTo(requestService.Request.Id.GetHashCode());

        Assert.IsFalse(viewModel.IsSectionLoadFailed, "A read that succeeded must not report the Failed state.");
        Assert.IsTrue(
            viewModel.IsItemPresent,
            "A successful read that finds no rows for a block must still present the request: an empty block is not an absent request.");

        foreach (var section in viewModel.TemplateSections)
        {
            Assert.AreNotEqual(
                0,
                section.Fields.Count,
                $"Section '{section.Title}' rendered with no rows: a block with nothing to show must be hidden, not drawn as an empty titled shell.");
        }

        Assert.IsFalse(
            viewModel.TemplateSections.Any(section => string.Equals(section.Title, "Coil material", StringComparison.OrdinalIgnoreCase)),
            "The coil material block has no source on this request, so it must be hidden.");
    }

    [TestMethod]
    public void RetryAfterAFailedRead_ReachesThePresentState()
    {
        var requestService = new FlakyRequestService();
        var viewModel = BuildViewModel(requestService);

        viewModel.OnNavigatedTo(requestService.Request.Id.GetHashCode());
        Assert.IsTrue(viewModel.IsSectionLoadFailed, "The first read was meant to fail.");

        viewModel.RetryLoadCommand!.Execute(null);

        Assert.IsFalse(viewModel.IsSectionLoadFailed, "A successful retry must leave the Failed state.");
        Assert.IsTrue(viewModel.TemplateSections.Count > 0, "A successful retry must reach the Present state.");
        Assert.IsTrue(requestService.AttemptCount >= 2, "The retry must actually re-run the read.");
    }

    [TestMethod]
    public void FailedSectionRead_DoesNotSubstituteSampleRows()
    {
        var viewModel = BuildViewModel(new ThrowingRequestService());

        viewModel.OnNavigatedTo(4242);

        Assert.AreEqual(
            0,
            viewModel.TemplateSections.Sum(section => section.Fields.Count),
            "A failed read must render no rows at all: no sample data, no stand-in values.");
    }

    private static WaitlistViewDetailViewModel BuildViewModel(IWaitlistRequestService requestService)
        => new(new WaitlistTestNavigationService(), new WaitlistTestBuildingSelectionService(), requestService: requestService);

    /// <summary>A request service whose list read always throws.</summary>
    private sealed class ThrowingRequestService : StubRequestServiceBase
    {
        public override IReadOnlyList<WaitlistRequest> GetActiveRequests(string? building = null)
            => throw new InvalidOperationException("mtm_waitlist is unreachable.");
    }

    /// <summary>A request service whose first read throws and whose later reads succeed.</summary>
    private sealed class FlakyRequestService : StubRequestServiceBase
    {
        /// <summary>The request the successful read returns.</summary>
        public WaitlistRequest Request { get; } = Sparse();

        public int AttemptCount { get; private set; }

        public override IReadOnlyList<WaitlistRequest> GetActiveRequests(string? building = null)
        {
            AttemptCount++;
            if (AttemptCount == 1)
            {
                throw new InvalidOperationException("mtm_waitlist is unreachable.");
            }

            return [Request];
        }
    }

    /// <summary>A request service that returns one request carrying no material source at all.</summary>
    private sealed class SparseRequestService : StubRequestServiceBase
    {
        /// <summary>The request read.</summary>
        public WaitlistRequest Request { get; } = Sparse();

        public override IReadOnlyList<WaitlistRequest> GetActiveRequests(string? building = null) => [Request];
    }

    /// <summary>A pickup request with no work order, no work centre and no typed detail, so no block has a source.</summary>
    private static WaitlistRequest Sparse() => new()
    {
        Building = "Expo Drive",
        WorkCenter = string.Empty,
        WorkCenterName = string.Empty,
        Category = "Pickup",
        Item = "pickup-coil",
        InputValue = null,
        ActiveSetupJobId = string.Empty,
        RequesterEmployeeNumber = "6331",
        RequesterEmployeeName = "Dana Whitfield",
        Status = "Pending",
    };

    /// <summary>The members a stub does not care about.</summary>
    private abstract class StubRequestServiceBase : IWaitlistRequestService
    {
        public event EventHandler? RequestsChanged
        {
            add { }
            remove { }
        }

        public abstract IReadOnlyList<WaitlistRequest> GetActiveRequests(string? building = null);

        public virtual WaitlistRequest? GetRequest(Guid requestId) => null;

        public virtual IReadOnlyList<WaitlistRequest> GetMyRequests(string requesterEmployeeNumber, string? building = null) => [];

        public virtual IReadOnlyList<WaitlistRequestAuditEntry> GetAuditTrail(Guid requestId) => [];

        public virtual Task<IReadOnlyList<WaitlistRequestAuditEntry>> LoadAuditTrailAsync(Guid requestId, CancellationToken cancellationToken = default)
            => Task.FromResult(GetAuditTrail(requestId));

        public virtual void Reset()
        {
        }

        public virtual Task<int> RefreshFromDatabaseAsync(string? building = null, CancellationToken cancellationToken = default) => Task.FromResult(0);

        public virtual Task<WaitlistRequestSubmitResult> SubmitAsync(WaitlistRequestDraft draft, bool allowDuplicate, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public virtual Task<bool> TransitionStatusAsync(Guid requestId, string status, string? cancellationReason = null, string? canceledByEmployeeNumber = null, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public virtual Task<WaitlistRequestCancelResult> CancelOwnRequestAsync(Guid requestId, string requesterEmployeeNumber, string? reason = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public virtual Task<WaitlistRequest?> UpdateNoteAsync(Guid requestId, string? note, string? actorEmployeeNumber = null, string? actorEmployeeName = null, CancellationToken cancellationToken = default)
            => Task.FromResult<WaitlistRequest?>(null);

        public virtual Task<WaitlistRequest?> AcceptAsync(Guid requestId, string handlerEmployeeNumber, string? handlerEmployeeName = null, CancellationToken cancellationToken = default)
            => Task.FromResult<WaitlistRequest?>(null);

        public virtual Task<WaitlistRequest?> CompleteAsync(Guid requestId, string handlerEmployeeNumber, string? handlerEmployeeName = null, CancellationToken cancellationToken = default)
            => Task.FromResult<WaitlistRequest?>(null);

        public virtual Task<WaitlistRequest?> ReleaseAsync(Guid requestId, string handlerEmployeeNumber, string? handlerEmployeeName = null, CancellationToken cancellationToken = default)
            => Task.FromResult<WaitlistRequest?>(null);
    }
}
