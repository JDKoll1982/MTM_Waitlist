using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Setup.Models;
using MTM_Waitlist.Module_Setup.Services;
using MTM_Waitlist.Module_Setup.ViewModels;

namespace MTM_Waitlist.Tests.Module_Setup.ViewModels;

/// <summary>
/// The work-order box canonicalises what the operator typed, and does it before the lookup runs.
/// </summary>
/// <remarks>
/// The operator may enter a partial order — <c>55691</c>, <c>076951</c> and <c>WO-076951</c> all name the same
/// order — and the box is expected to show the canonical <c>WO-######</c> form, which is the first visible
/// confirmation that the number was understood. That has to happen before the lookup is awaited: on a machine
/// that cannot reach Infor Visual the lookup spends seconds failing over to the <c>mtm_mock</c> mirror, so
/// canonicalising afterwards leaves the operator staring at their raw digits for the whole search.
/// </remarks>
[TestClass]
public sealed class SetupWorkOrderViewModelTests
{
    [TestMethod]
    public async Task SearchAsync_PartialWorkOrderNumber_HandsTheLookupTheCanonicalKey()
    {
        var (viewModel, workflow) = CreateViewModel();
        viewModel.WorkOrderInput = "55691";

        await viewModel.SearchCommand.ExecuteAsync(null);

        Assert.AreEqual(
            "WO-055691",
            workflow.InputReceived,
            "The operator's digits are canonicalised before the lookup runs, not after it returns.");
        Assert.AreEqual("WO-055691", viewModel.WorkOrderInput);
    }

    [TestMethod]
    public async Task SearchAsync_InputThatCannotBeCanonicalised_LeavesWhatTheOperatorTyped()
    {
        var (viewModel, workflow) = CreateViewModel();
        viewModel.WorkOrderInput = "1234567";

        await viewModel.SearchCommand.ExecuteAsync(null);

        Assert.AreEqual(
            "1234567",
            viewModel.WorkOrderInput,
            "Input the rule rejects is left as typed so the operator can correct it; the workflow owns the message.");
        Assert.AreEqual("1234567", workflow.InputReceived);
    }

    [TestMethod]
    public async Task SearchAsync_WhileTheLookupIsInFlight_HoldsTheScreenSoASecondSearchCannotBeIssued()
    {
        var (viewModel, workflow) = CreateViewModel();
        bool? isIdleDuringSearch = null;
        workflow.DuringSearch = () => isIdleDuringSearch = viewModel.IsIdle;
        viewModel.WorkOrderInput = "55691";

        await viewModel.SearchCommand.ExecuteAsync(null);

        Assert.AreEqual(
            false,
            isIdleDuringSearch,
            "The Search action is held for the whole lookup, not just while it is being issued.");
        Assert.IsTrue(viewModel.IsIdle, "The screen is released once the lookup returns.");
    }

    [TestMethod]
    public async Task ACommandThatOutlivesItsView_DoesNotNotifyBoundMembers()
    {
        var workflow = new RecordingSetupWorkflowService();
        var release = new TaskCompletionSource();
        workflow.BeforeSearchReturns = () => release.Task;
        var viewModel = new SetupWorkOrderViewModel(
            new NoOpNavigationService(),
            workflow,
            new WorkOrderValidationService());
        var notified = new List<string?>();
        viewModel.PropertyChanged += (_, e) => notified.Add(e.PropertyName);
        viewModel.WorkOrderInput = "55691";

        var running = viewModel.SearchCommand.ExecuteAsync(null);

        // The window closes while the read is still in flight; the continuation then runs against a XAML tree
        // that has been torn down, where writing a bound member throws E_UNEXPECTED out of the notification.
        // Only what is raised from here on is the subject: going busy was announced while the view was alive.
        viewModel.NotifyViewClosing();
        notified.Clear();
        release.SetResult();
        await running;

        CollectionAssert.DoesNotContain(
            notified,
            nameof(SetupWorkOrderViewModel.IsBusy),
            "IsBusy drives a bound Control, so notifying a closed view writes into a torn-down tree.");
        CollectionAssert.DoesNotContain(
            notified,
            "IsIdle",
            "IsIdle is what the Search action binds, so it must not reach a closed view either.");
    }

    private static (SetupWorkOrderViewModel ViewModel, RecordingSetupWorkflowService Workflow) CreateViewModel()
    {
        var workflow = new RecordingSetupWorkflowService();
        var viewModel = new SetupWorkOrderViewModel(
            new NoOpNavigationService(),
            workflow,
            new WorkOrderValidationService());
        return (viewModel, workflow);
    }

    /// <summary>Records the work-order string the lookup was actually handed.</summary>
    private sealed class RecordingSetupWorkflowService : ISetupWorkflowService
    {
        public SetupWorkflowState State { get; } = new();

        public string? InputReceived { get; private set; }

        /// <summary>Runs while the lookup is in flight, so a test can read the screen's state at that moment.</summary>
        public Action? DuringSearch { get; set; }

        /// <summary>Lets a test hold the lookup open, so a command can be observed outliving its view.</summary>
        public Func<Task>? BeforeSearchReturns { get; set; }

        public bool HasUnsavedChanges => State.HasUnsavedChanges;

        public Task ResetAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public async Task<SetupLookupResult> SearchWorkOrderAsync(string workOrderInput, CancellationToken cancellationToken = default)
        {
            InputReceived = workOrderInput;
            DuringSearch?.Invoke();

            if (BeforeSearchReturns is not null)
            {
                await BeforeSearchReturns().ConfigureAwait(false);
            }

            return new SetupLookupResult { Success = true };
        }

        public Task<SetupSelectionResult> SelectPartAsync(string partNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SetupSelectionResult { Success = true });

        public Task<SetupSelectionResult> SelectSequenceAsync(string sequenceNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SetupSelectionResult { Success = true });

        public Task<SetupSelectionResult> SelectDunnageTypeAsync(string dunnageTypeId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SetupSelectionResult { Success = true });

        public Task<SetupSelectionResult> SelectDunnagePartAsync(string dunnagePartId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SetupSelectionResult { Success = true });

        public Task<SetupSelectionResult> AddDunnagePartToPairAsync(SetupDunnagePart part, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SetupSelectionResult { Success = true });

        public Task<SetupSelectionResult> RemoveDunnagePartAsync(string dunnagePartId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SetupSelectionResult { Success = true });

        public Task<SetupSelectionResult> RemoveAllDunnageForTypeAsync(string dunnageTypeId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SetupSelectionResult { Success = true });

        public Task<SetupSelectionResult> ClearAllDunnageForPairAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new SetupSelectionResult { Success = true });

        public Task<SetupSaveResult> SaveAsync(bool forceReplace = false, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SetupSaveResult { Success = true });
    }

    private sealed class NoOpNavigationService : INavigationService
    {
        public event Microsoft.UI.Xaml.Navigation.NavigatedEventHandler? Navigated
        {
            add { }
            remove { }
        }

        public Microsoft.UI.Xaml.Controls.Frame? Frame
        {
            get => null;
            set { }
        }

        public bool CanGoBack => false;

        public bool GoBack() => false;

        public bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false) => true;

        public void SetListDataItemForNextConnectedAnimation(object item)
        {
        }
    }
}
