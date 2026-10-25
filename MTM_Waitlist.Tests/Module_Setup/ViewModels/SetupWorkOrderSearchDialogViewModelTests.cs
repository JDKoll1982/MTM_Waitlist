using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Setup.Models;
using MTM_Waitlist.Module_Setup.ViewModels;

namespace MTM_Waitlist.Tests.Module_Setup.ViewModels;

/// <summary>
/// The work-order search window: it asks the read seam for the operator's three fragments, shows what came
/// back, and records the line the operator accepted.
/// </summary>
[TestClass]
public sealed class SetupWorkOrderSearchDialogViewModelTests
{
    [TestMethod]
    public async Task ViewModel_SetupWorkOrderSearchDialogViewModelTests_Search_HandsTheThreeFragmentsToTheReadAndSelectsTheFirstRow()
    {
        var search = new StubWorkOrderSearchService(
        [
            Row("WO-075201", "105803-08", "Volvo - Mack Trucks (45)"),
            Row("WO-075200", "105798-04", "DETROIT DIESEL"),
        ]);
        var viewModel = new SetupWorkOrderSearchDialogViewModel(search)
        {
            WorkOrderText = "0752",
            PartFilter = "1058",
            CustomerFilter = "Volvo",
        };

        await viewModel.SearchCommand.ExecuteAsync(null);

        Assert.AreEqual(("0752", "1058", "Volvo"), search.LastRequest, "the read gets exactly what was typed");
        Assert.AreEqual(2, viewModel.Results.Count);
        Assert.AreEqual("WO-075201", viewModel.SelectedResult?.WorkOrder, "the first row is ready to accept");
        Assert.IsTrue(viewModel.HasSelection);
        Assert.IsFalse(viewModel.HasNoResults);
    }

    [TestMethod]
    public async Task ViewModel_SetupWorkOrderSearchDialogViewModelTests_Search_WithNoMatchesSaysSoRatherThanLeavingAnEmptyWindow()
    {
        var viewModel = new SetupWorkOrderSearchDialogViewModel(new StubWorkOrderSearchService([]));

        await viewModel.SearchCommand.ExecuteAsync(null);

        Assert.AreEqual(0, viewModel.Results.Count);
        Assert.IsNull(viewModel.SelectedResult);
        Assert.IsFalse(viewModel.HasSelection, "nothing is chosen, so Accept must stay unavailable");
        Assert.IsTrue(viewModel.HasNoResults);
        StringAssert.Contains(viewModel.EmptyStateMessage, "match", "the window says why it is empty");
    }

    [TestMethod]
    public async Task ViewModel_SetupWorkOrderSearchDialogViewModelTests_Search_ThatFailsSaysTheReadFailedRatherThanClaimingNoMatches()
    {
        var search = new StubWorkOrderSearchService([]) { Failure = new InvalidOperationException("live and mirror down") };
        var viewModel = new SetupWorkOrderSearchDialogViewModel(search);

        await viewModel.SearchCommand.ExecuteAsync(null);

        Assert.AreEqual(0, viewModel.Results.Count);
        Assert.IsTrue(viewModel.HasNoResults);
        Assert.AreNotEqual(
            "No open work orders match those filters.",
            viewModel.EmptyStateMessage,
            "a failed read must not be presented as a real empty answer");
        Assert.IsFalse(viewModel.IsBusy, "the window is released after a failure");
    }

    [TestMethod]
    public async Task ViewModel_SetupWorkOrderSearchDialogViewModelTests_Accept_RecordsTheChosenOrderForTheCaller()
    {
        var search = new StubWorkOrderSearchService([Row("WO-075201", "105803-08", "Volvo")]);
        var viewModel = new SetupWorkOrderSearchDialogViewModel(search);
        await viewModel.SearchCommand.ExecuteAsync(null);

        var chosen = viewModel.Results[0];
        viewModel.Accept(chosen);

        Assert.AreSame(chosen, viewModel.AcceptedResult);
        Assert.AreSame(chosen, viewModel.SelectedResult);
    }

    [TestMethod]
    public async Task ViewModel_SetupWorkOrderSearchDialogViewModelTests_Accept_WithNothingChosenRecordsNothing()
    {
        var viewModel = new SetupWorkOrderSearchDialogViewModel(new StubWorkOrderSearchService([]));

        viewModel.Accept(null);

        Assert.IsNull(viewModel.AcceptedResult, "a dismissed window must not hand an order back");
    }

    [TestMethod]
    public async Task ViewModel_SetupWorkOrderSearchDialogViewModelTests_Search_ThatFinishesAfterTheWindowClosedLeavesTheDeadWindowAlone()
    {
        var release = new TaskCompletionSource();
        var search = new StubWorkOrderSearchService([Row("WO-075201", "105803-08", "Volvo")]) { BeforeReturning = () => release.Task };
        var viewModel = new SetupWorkOrderSearchDialogViewModel(search);

        var running = viewModel.SearchCommand.ExecuteAsync(null);
        viewModel.MarkClosed();
        release.SetResult();
        await running;

        Assert.AreEqual(
            0,
            viewModel.Results.Count,
            "the window is gone, so a read that lands afterwards must not write into its bound collections");
    }

    private static SetupWorkOrderSearchResult Row(string workOrder, string part, string customer) => new()
    {
        WorkOrder = workOrder,
        PartNumber = part,
        PartDescription = "Assy-Burner Inshot",
        CustomerName = customer,
        WorkCenter = "Press 12",
        Status = "R",
        WantDate = new DateTimeOffset(2026, 11, 3, 0, 0, 0, TimeSpan.Zero),
    };

    private sealed class StubWorkOrderSearchService : IWorkOrderSearchService
    {
        private readonly IReadOnlyList<SetupWorkOrderSearchResult> _rows;

        public StubWorkOrderSearchService(IReadOnlyList<SetupWorkOrderSearchResult> rows) => _rows = rows;

        public (string WorkOrder, string Part, string Customer)? LastRequest { get; private set; }

        public Exception? Failure { get; init; }

        public Func<Task>? BeforeReturning { get; init; }

        public async Task<IReadOnlyList<SetupWorkOrderSearchResult>> SearchWorkOrdersAsync(
            string workOrderText,
            string partFilter,
            string customerFilter,
            CancellationToken cancellationToken = default)
        {
            LastRequest = (workOrderText, partFilter, customerFilter);

            if (BeforeReturning is not null)
            {
                await BeforeReturning().ConfigureAwait(true);
            }

            if (Failure is not null)
            {
                throw Failure;
            }

            return _rows;
        }
    }
}
