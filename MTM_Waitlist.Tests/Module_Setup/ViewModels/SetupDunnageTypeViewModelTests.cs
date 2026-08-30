using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Setup.Models;
using MTM_Waitlist.Module_Setup.ViewModels;

namespace MTM_Waitlist.Tests.Module_Setup.ViewModels;

[TestClass]
public sealed class SetupDunnageTypeViewModelTests
{
    [TestMethod]
    public void CanContinue_WhenNoScrapSelected_IsFalse()
    {
        var (viewModel, _) = CreateViewModel();

        viewModel.SelectedScrapType = string.Empty;

        Assert.IsFalse(viewModel.HasScrapDecision);
        Assert.IsTrue(viewModel.IsScrapSelectionMissing);
        Assert.IsFalse(viewModel.CanContinue);
    }

    [TestMethod]
    public void CanContinue_WhenPlaceholderSelected_IsFalse()
    {
        var (viewModel, _) = CreateViewModel();

        viewModel.SelectedScrapType = "Scrap Type Required";

        Assert.IsFalse(viewModel.HasScrapDecision);
        Assert.IsTrue(viewModel.IsScrapSelectionMissing);
        Assert.IsFalse(viewModel.CanContinue);
    }

    [TestMethod]
    public void CanContinue_WhenNoScrapExplicitlyChosen_IsTrue()
    {
        var (viewModel, _) = CreateViewModel();

        viewModel.SelectedScrapType = "No Scrap";

        Assert.IsTrue(viewModel.HasScrapDecision);
        Assert.IsFalse(viewModel.IsScrapSelectionMissing);
        Assert.IsTrue(viewModel.CanContinue);
    }

    [TestMethod]
    public void CanContinue_WhenRealScrapTypeSelected_IsTrue()
    {
        var (viewModel, _) = CreateViewModel();

        viewModel.SelectedScrapType = "3003 Aluminum";

        Assert.IsTrue(viewModel.HasScrapDecision);
        Assert.IsFalse(viewModel.IsScrapSelectionMissing);
        Assert.IsTrue(viewModel.CanContinue);
    }

    [TestMethod]
    public void DisplayScrapTypes_ExcludesPlaceholderAndIncludesNoScrap()
    {
        var state = new SetupWorkflowState();
        state.ScrapTypes.Add("Scrap Type Required");
        state.ScrapTypes.Add("No Scrap");
        state.ScrapTypes.Add("3003 Aluminum");

        var (viewModel, _) = CreateViewModel(state);

        CollectionAssert.AreEqual(
            new[] { "No Scrap", "3003 Aluminum" },
            viewModel.DisplayScrapTypes.ToArray());
    }

    [TestMethod]
    public async Task ContinueToReviewAsync_WhenNoDunnageAndConfirmed_NavigatesToReview()
    {
        var state = new SetupWorkflowState();
        var navigation = new RecordingNavigationService();
        var viewModel = new TestableSetupDunnageTypeViewModel(
            confirmNoDunnageResult: true,
            imageSearchPart: null,
            navigation,
            new StubSetupWorkflowService(state),
            dunnageWorkflowService: null!,
            dialogService: new NoOpSetupDialogService());

        await viewModel.ContinueToReviewCommand.ExecuteAsync(null);

        Assert.AreEqual(1, navigation.NavigateCount);
        Assert.AreEqual(typeof(SetupReviewViewModel).FullName, navigation.LastPageKey);
    }

    [TestMethod]
    public async Task ContinueToReviewAsync_WhenNoDunnageAndCanceled_DoesNotNavigate()
    {
        var state = new SetupWorkflowState();
        var navigation = new RecordingNavigationService();
        var viewModel = new TestableSetupDunnageTypeViewModel(
            confirmNoDunnageResult: false,
            imageSearchPart: null,
            navigation,
            new StubSetupWorkflowService(state),
            dunnageWorkflowService: null!,
            dialogService: new NoOpSetupDialogService());

        await viewModel.ContinueToReviewCommand.ExecuteAsync(null);

        Assert.AreEqual(0, navigation.NavigateCount);
    }

    [TestMethod]
    public async Task ContinueToReviewAsync_WhenDunnageSelected_NavigatesWithoutConfirmation()
    {
        var state = new SetupWorkflowState();
        state.SelectedDunnageParts.Add(new SetupDunnagePart
        {
            Id = "coil-a",
            PartNumber = "1",
            DisplayName = "Dunnage Coil A",
        });

        var navigation = new RecordingNavigationService();
        var viewModel = new TestableSetupDunnageTypeViewModel(
            confirmNoDunnageResult: false,
            imageSearchPart: null,
            navigation,
            new StubSetupWorkflowService(state),
            dunnageWorkflowService: null!,
            dialogService: new NoOpSetupDialogService());

        await viewModel.ContinueToReviewCommand.ExecuteAsync(null);

        Assert.AreEqual(1, navigation.NavigateCount);
        Assert.AreEqual(typeof(SetupReviewViewModel).FullName, navigation.LastPageKey);
    }

    [TestMethod]
    public async Task AddDunnageAsync_WhenPartSelected_AddsToPairAndShowsMessage()
    {
        var state = new SetupWorkflowState();
        var navigation = new RecordingNavigationService();
        var part = new SetupDunnagePart
        {
            Id = "coil-a",
            TypeId = "Coils",
            PartNumber = "DUN-COIL-A",
            DisplayName = "Dunnage Coil A",
        };
        var viewModel = new TestableSetupDunnageTypeViewModel(
            confirmNoDunnageResult: false,
            imageSearchPart: part,
            navigation,
            new StubSetupWorkflowService(state),
            dunnageWorkflowService: null!,
            dialogService: new NoOpSetupDialogService());

        await viewModel.AddDunnageCommand.ExecuteAsync(null);

        Assert.AreEqual(1, state.SelectedDunnageParts.Count);
        Assert.AreEqual("coil-a", state.SelectedDunnageParts[0].Id);
        Assert.IsFalse(string.IsNullOrWhiteSpace(viewModel.StatusMessage));
    }

    [TestMethod]
    public async Task AddDunnageAsync_WhenDialogDismissed_DoesNotAdd()
    {
        var state = new SetupWorkflowState();
        var navigation = new RecordingNavigationService();
        var viewModel = new TestableSetupDunnageTypeViewModel(
            confirmNoDunnageResult: false,
            imageSearchPart: null,
            navigation,
            new StubSetupWorkflowService(state),
            dunnageWorkflowService: null!,
            dialogService: new NoOpSetupDialogService());

        await viewModel.AddDunnageCommand.ExecuteAsync(null);

        Assert.AreEqual(0, state.SelectedDunnageParts.Count);
    }

    private static (TestableSetupDunnageTypeViewModel ViewModel, RecordingNavigationService Navigation) CreateViewModel(SetupWorkflowState? state = null)
    {
        var setupState = state ?? new SetupWorkflowState();
        var navigation = new RecordingNavigationService();
        var viewModel = new TestableSetupDunnageTypeViewModel(
            confirmNoDunnageResult: false,
            imageSearchPart: null,
            navigation,
            new StubSetupWorkflowService(setupState),
            dunnageWorkflowService: null!,
            dialogService: new NoOpSetupDialogService());
        return (viewModel, navigation);
    }

    private sealed class TestableSetupDunnageTypeViewModel : SetupDunnageTypeViewModel
    {
        private readonly bool _confirmNoDunnageResult;
        private readonly SetupDunnagePart? _imageSearchPart;

        public TestableSetupDunnageTypeViewModel(
            bool confirmNoDunnageResult,
            SetupDunnagePart? imageSearchPart,
            INavigationService navigationService,
            ISetupWorkflowService workflowService,
            IDunnageWorkflowService dunnageWorkflowService,
            ISetupDialogService dialogService)
            : base(navigationService, workflowService, dunnageWorkflowService, dialogService)
        {
            _confirmNoDunnageResult = confirmNoDunnageResult;
            _imageSearchPart = imageSearchPart;
        }

        protected override Task<bool> ConfirmNoDunnageAsync() => Task.FromResult(_confirmNoDunnageResult);

        protected override Task<SetupDunnagePart?> ShowImageSearchDialogAsync() => Task.FromResult(_imageSearchPart);
    }

    private sealed class StubSetupWorkflowService : ISetupWorkflowService
    {
        public StubSetupWorkflowService(SetupWorkflowState state)
        {
            State = state;
        }

        public SetupWorkflowState State { get; }

        public bool HasUnsavedChanges => State.HasUnsavedChanges;

        public Task ResetAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<SetupLookupResult> SearchWorkOrderAsync(string workOrderInput, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SetupLookupResult { Success = true });

        public Task<SetupSelectionResult> SelectPartAsync(string partNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SetupSelectionResult { Success = true });

        public Task<SetupSelectionResult> SelectSequenceAsync(string sequenceNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SetupSelectionResult { Success = true });

        public Task<SetupSelectionResult> SelectDunnageTypeAsync(string dunnageTypeId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SetupSelectionResult { Success = true });

        public Task<SetupSelectionResult> SelectDunnagePartAsync(string dunnagePartId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SetupSelectionResult { Success = true });

        public Task<SetupSelectionResult> AddDunnagePartToPairAsync(SetupDunnagePart part, CancellationToken cancellationToken = default)
        {
            if (part is not null && !State.SelectedDunnageParts.Any(existing => existing.Id == part.Id))
            {
                State.SelectedDunnageParts.Add(part);
            }

            return Task.FromResult(new SetupSelectionResult { Success = true, Message = $"Dunnage part '{part?.DisplayName}' added to the job." });
        }

        public Task<SetupSelectionResult> RemoveDunnagePartAsync(string dunnagePartId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SetupSelectionResult { Success = true });

        public Task<SetupSelectionResult> RemoveAllDunnageForTypeAsync(string dunnageTypeId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SetupSelectionResult { Success = true });

        public Task<SetupSelectionResult> ClearAllDunnageForPairAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new SetupSelectionResult { Success = true });

        public Task<SetupSaveResult> SaveAsync(bool forceReplace = false, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SetupSaveResult { Success = true });
    }

    private sealed class RecordingNavigationService : INavigationService
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

        public int NavigateCount { get; private set; }

        public string? LastPageKey { get; private set; }

        public bool GoBack() => false;

        public bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false)
        {
            NavigateCount++;
            LastPageKey = pageKey;
            return true;
        }

        public void SetListDataItemForNextConnectedAnimation(object item)
        {
        }
    }
}
