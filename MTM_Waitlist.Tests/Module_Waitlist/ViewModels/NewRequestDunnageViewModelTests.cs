using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Tests.Module_Waitlist.ViewModels;

/// <summary>
/// FR-048, FR-049, FR-051. The dunnage step asks <b>which</b> dunnage the operator needs from the parts the job
/// carries, and lets them use a part the job does not carry when they have to substitute.
/// </summary>
/// <remarks>
/// The picker is faked rather than driven: what is asserted here is the step's own behaviour — what it offers,
/// what it captures, what a dismissal does and what a failure does — not the receiving catalogue the reused
/// dialog reads.
/// </remarks>
[TestClass]
public sealed class NewRequestDunnageViewModelTests
{
    private const string FirstAssignedPart = "DN-STL-4";
    private const string SecondAssignedPart = "DN-BOX-2";
    private const string SubstitutePartNumber = "DN-SUB-9";

    [TestMethod]
    public void OnNavigatedTo_BindsEveryPartTheJobCarries()
    {
        var viewModel = CreateViewModel();
        viewModel.OnNavigatedTo(StateWith(FirstAssignedPart, SecondAssignedPart));

        CollectionAssert.AreEqual(
            new[] { FirstAssignedPart, SecondAssignedPart },
            viewModel.Options.Select(option => option.Title).ToArray(),
            "Every dunnage part the job carries is offered as a card, in the job's own order (FR-048).");
        Assert.IsFalse(viewModel.IsUnavailableVisible, "A job that carries dunnage has nothing to report.");
        Assert.AreEqual(0, viewModel.Options.Count(option => option.IsSelected), "Nothing is chosen on the operator's behalf (FR-048).");
    }

    [TestMethod]
    public void SelectPart_CapturesTheChosenPartAndMovesOn()
    {
        var navigation = new RecordingNavigationService();
        var viewModel = CreateViewModel(navigation: navigation);
        var state = StateWith(FirstAssignedPart, SecondAssignedPart);
        viewModel.OnNavigatedTo(state);

        viewModel.SelectPartCommand.Execute(viewModel.Options[1]);

        Assert.AreEqual(SecondAssignedPart, state.InputValue, "The part the operator picked is the request's value (FR-048).");
        Assert.AreEqual(
            typeof(NewRequestSummaryViewModel).FullName,
            navigation.Navigations[0].PageKey,
            "With the answer captured the flow continues to the confirmation step, rather than asking the same "
            + "question again.");
        Assert.IsTrue(viewModel.Options[1].IsSelected, "The card they chose is the marked one.");
    }

    [TestMethod]
    public void SelectPart_WithNoChoice_DoesNothing()
    {
        var navigation = new RecordingNavigationService();
        var viewModel = CreateViewModel(navigation: navigation);
        var state = StateWith(FirstAssignedPart);
        viewModel.OnNavigatedTo(state);

        viewModel.SelectPartCommand.Execute(null);

        Assert.IsNull(state.InputValue);
        Assert.AreEqual(0, navigation.Navigations.Count, "An empty selection must not advance the flow.");
    }

    [TestMethod]
    public async Task UseSubstitute_CapturesAPartTheJobDoesNotCarry()
    {
        var navigation = new RecordingNavigationService();
        var substitute = new RequestDunnagePart
        {
            Id = "p9",
            PartNumber = SubstitutePartNumber,
            DisplayName = "Substitute rack",
            IsAssignedToJob = false,
        };

        var viewModel = CreateViewModel(navigation: navigation, picker: new FakeSubstitutePicker(substitute));
        var state = StateWith(FirstAssignedPart);
        viewModel.OnNavigatedTo(state);

        await viewModel.UseSubstituteCommand.ExecuteAsync(null);

        Assert.AreEqual(SubstitutePartNumber, state.InputValue, "The substituted part becomes the request's value (FR-049).");
        Assert.AreEqual(
            SubstitutePartNumber,
            state.SelectedDunnagePart?.PartNumber,
            "The step remembers the substitute as its current choice, so coming back to it does not hide it.");
        Assert.AreEqual(
            typeof(NewRequestSummaryViewModel).FullName,
            navigation.Navigations[0].PageKey,
            "A substitute advances the flow to the confirmation step exactly as an assigned part does.");
    }

    [TestMethod]
    public async Task UseSubstitute_Dismissed_ChangesNothing()
    {
        var navigation = new RecordingNavigationService();
        var viewModel = CreateViewModel(navigation: navigation, picker: new FakeSubstitutePicker(null));
        var state = StateWith(FirstAssignedPart);
        viewModel.OnNavigatedTo(state);

        await viewModel.UseSubstituteCommand.ExecuteAsync(null);

        Assert.IsNull(state.InputValue, "A dismissed picker captures nothing.");
        Assert.IsNull(state.SelectedDunnagePart, "A dismissed picker leaves no choice behind.");
        Assert.AreEqual(0, navigation.Navigations.Count, "A dismissed picker must not advance the flow.");
        Assert.IsFalse(viewModel.IsUnavailableVisible, "A dismissal is not a failure, so nothing is reported.");
    }

    [TestMethod]
    public async Task UseSubstitute_ThatCannotOpen_IsReportedInPlainLanguageAndLeavesTheStepUsable()
    {
        var navigation = new RecordingNavigationService();
        var viewModel = CreateViewModel(navigation: navigation, picker: new ThrowingSubstitutePicker());
        var state = StateWith(FirstAssignedPart);
        viewModel.OnNavigatedTo(state);

        await viewModel.UseSubstituteCommand.ExecuteAsync(null);

        Assert.IsTrue(viewModel.IsUnavailableVisible, "A picker that cannot open is reported rather than swallowed (FR-026).");
        Assert.IsFalse(string.IsNullOrWhiteSpace(viewModel.UnavailableMessage));
        Assert.IsFalse(
            viewModel.UnavailableMessage.StartsWith("NewRequest_", StringComparison.Ordinal),
            "The report is plain language, never a bare resource key (FR-022).");
        Assert.AreEqual(1, viewModel.Options.Count, "The job's own cards stay usable after a failed picker.");
        Assert.AreEqual(0, navigation.Navigations.Count, "A failed picker must not advance the flow.");
    }

    [TestMethod]
    public void OnNavigatedTo_ForAJobWithNoDunnage_ReportsItAndDropsNoCards()
    {
        var viewModel = CreateViewModel();
        viewModel.OnNavigatedTo(StateWith());

        Assert.AreEqual(0, viewModel.Options.Count, "There is nothing to offer, so nothing is invented.");
        Assert.IsTrue(viewModel.IsUnavailableVisible, "The step says why it is empty (FR-026).");
    }

    [TestMethod]
    public void OnNavigatedTo_ComingBackWithASubstituteChosen_ShowsItAgainAsTheChosenCard()
    {
        var substitute = new RequestDunnagePart
        {
            PartNumber = SubstitutePartNumber,
            DisplayName = "Substitute rack",
            IsAssignedToJob = false,
        };

        var state = StateWith(FirstAssignedPart);
        state.InputValue = SubstitutePartNumber;
        state.SelectedDunnagePart = substitute;
        var viewModel = CreateViewModel();

        viewModel.OnNavigatedTo(state);

        Assert.AreEqual(2, viewModel.Options.Count, "The job's own card is still offered beside the substitute.");
        Assert.AreEqual(
            SubstitutePartNumber,
            viewModel.Options[0].Part?.PartNumber,
            "The substitute is shown first, because it is the current choice.");
        Assert.IsTrue(viewModel.Options[0].IsSelected, "The current choice is the marked card.");
    }

    [TestMethod]
    public void OnNavigatedTo_ComingBackWithAnAssignedPartChosen_DoesNotDuplicateIt()
    {
        var state = StateWith(FirstAssignedPart, SecondAssignedPart);
        state.InputValue = SecondAssignedPart;
        var viewModel = CreateViewModel();

        viewModel.OnNavigatedTo(state);

        Assert.AreEqual(2, viewModel.Options.Count, "An assigned part is one of the job's cards, so it is never added twice.");
        Assert.IsTrue(viewModel.Options[1].IsSelected, "The remembered choice is marked.");
    }

    [TestMethod]
    public void OnNavigatedTo_WithoutAState_LeavesTheStep()
    {
        var navigation = new RecordingNavigationService();
        var viewModel = CreateViewModel(navigation: navigation);

        viewModel.OnNavigatedTo("not a flow state");

        Assert.AreEqual(1, navigation.GoBackCount, "The step cannot run without the request in progress.");
    }

    private static NewRequestFlowState StateWith(params string[] assignedPartNumbers) => new()
    {
        WorkCenter = "100-03",
        Category = RequestCategory.Pickup,
        Item = RequestItemCatalog.FindById("pickup-dunnage"),
        Availability = RequestJobPartAvailability.None.WithDunnageParts(assignedPartNumbers.Select(partNumber =>
            new RequestDunnagePart
            {
                Id = partNumber,
                PartNumber = partNumber,
                DisplayName = partNumber,
                IsAssignedToJob = true,
            })),
    };

    private static NewRequestDunnageViewModel CreateViewModel(
        RecordingNavigationService? navigation = null,
        IDunnageSubstitutePicker? picker = null)
        => new(navigation ?? new RecordingNavigationService(), picker ?? new FakeSubstitutePicker(null));

    private sealed class FakeSubstitutePicker : IDunnageSubstitutePicker
    {
        private readonly RequestDunnagePart? _part;

        public FakeSubstitutePicker(RequestDunnagePart? part) => _part = part;

        public Task<RequestDunnagePart?> PickSubstituteAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_part);
    }

    private sealed class ThrowingSubstitutePicker : IDunnageSubstitutePicker
    {
        public Task<RequestDunnagePart?> PickSubstituteAsync(CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("The receiving catalogue is unreachable.");
    }

    private sealed class RecordingNavigationService : INavigationService
    {
        public List<(string PageKey, object? Parameter)> Navigations { get; } = new();

        public int GoBackCount { get; private set; }

        public event NavigatedEventHandler? Navigated
        {
            add { }
            remove { }
        }

        public bool CanGoBack => true;

        public Frame? Frame { get; set; }

        public bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false)
        {
            Navigations.Add((pageKey, parameter));
            return true;
        }

        public bool GoBack()
        {
            GoBackCount++;
            return true;
        }

        public void SetListDataItemForNextConnectedAnimation(object item)
        {
        }
    }
}
