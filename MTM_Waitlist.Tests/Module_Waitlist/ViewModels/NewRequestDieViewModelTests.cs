using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml.Navigation;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Tests.Module_Waitlist.ViewModels;

/// <summary>
/// FR-054: a job that carries more than one die is a step that lets the operator choose which dies the request is
/// for — one or several — showing where each die lives, refusing to move on with nothing chosen, and offering a
/// single action that takes them all.
/// </summary>
/// <remarks>
/// The step reads the dies the <b>job</b> carries, never the Item the requester chose, so a job whose dies change
/// changes what is offered without any row being edited (FR-013).
/// </remarks>
[TestClass]
public sealed class NewRequestDieViewModelTests
{
    private const string FirstDie = "FGT0002000";
    private const string SecondDie = "FGT0002001";

    [TestMethod]
    public void OnNavigatedTo_BindsEveryDieTheJobCarries_WithItsHomeLocation()
    {
        var viewModel = CreateViewModel();
        viewModel.OnNavigatedTo(TwoDieState());

        CollectionAssert.AreEqual(
            new[] { "FGT0002000", "FGT0002001" },
            viewModel.Options.Select(option => option.Title).ToArray(),
            "Every die the job carries is a card, written as its number and its home location (FR-056, FR-054).");
        Assert.IsFalse(viewModel.IsUnavailableVisible, "A job that carries dies has nothing to report.");
        Assert.AreEqual(
            0,
            viewModel.Options.Count(option => option.IsSelected),
            "Nothing is chosen on the operator's behalf — they are choosing which dies they need.");
    }

    [TestMethod]
    public void SelectDie_TogglesTheCardRatherThanMovingOn()
    {
        // The dunnage step advances on a tap because one part is one answer. Here several dies are one answer
        // each, so a tap marks the card and the operator commits when they have finished choosing.
        var navigation = new RecordingNavigationService();
        var viewModel = CreateViewModel(navigation);
        viewModel.OnNavigatedTo(TwoDieState());

        viewModel.SelectDieCommand.Execute(viewModel.Options[0]);
        viewModel.SelectDieCommand.Execute(viewModel.Options[1]);

        Assert.AreEqual(2, viewModel.Options.Count(option => option.IsSelected), "Both chosen dies stay chosen.");
        Assert.AreEqual(0, navigation.Navigations.Count, "Choosing a die is not the way out of the step.");

        viewModel.SelectDieCommand.Execute(viewModel.Options[0]);

        Assert.IsFalse(viewModel.Options[0].IsSelected, "Tapping a chosen die again un-chooses it.");
        Assert.IsTrue(viewModel.Options[1].IsSelected, "Un-choosing one die leaves the others alone.");
    }

    [TestMethod]
    public void SelectAll_ChoosesEveryDieTheJobCarries()
    {
        var viewModel = CreateViewModel();
        viewModel.OnNavigatedTo(TwoDieState());

        viewModel.SelectAllCommand.Execute(null);

        Assert.AreEqual(2, viewModel.Options.Count(option => option.IsSelected), "Select all takes the whole list (FR-054).");
    }

    [TestMethod]
    public void Continue_WithNothingChosen_RefusesAndStaysOnTheStep()
    {
        var navigation = new RecordingNavigationService();
        var viewModel = CreateViewModel(navigation);
        var state = TwoDieState();
        viewModel.OnNavigatedTo(state);

        viewModel.ContinueCommand.Execute(null);

        Assert.IsNull(state.InputValue, "A request is never raised for no die at all.");
        Assert.AreEqual(0, navigation.Navigations.Count, "Nothing chosen must not advance the flow.");
        Assert.IsTrue(viewModel.IsUnavailableVisible, "The step says why it will not move on (FR-026).");
        Assert.IsFalse(
            viewModel.UnavailableMessage.StartsWith("NewRequest_", StringComparison.Ordinal),
            "The report is plain language, never a bare resource key (FR-022).");
    }

    [TestMethod]
    public void Continue_WithTwoChosen_RecordsBothDiesAndMovesOn()
    {
        var navigation = new RecordingNavigationService();
        var viewModel = CreateViewModel(navigation);
        var state = TwoDieState();
        viewModel.OnNavigatedTo(state);

        viewModel.SelectDieCommand.Execute(viewModel.Options[0]);
        viewModel.SelectDieCommand.Execute(viewModel.Options[1]);
        viewModel.ContinueCommand.Execute(null);

        CollectionAssert.AreEqual(
            new[] { "FGT0002000", "FGT0002001" },
            state.SelectedDies.Select(die => die.Label).ToArray(),
            "The step remembers every die the operator chose, in the job's own order (FR-054).");
        Assert.AreEqual(
            "FGT0002000",
            state.InputValue,
            "The request's stored value is one die, so several dies can never be folded into one entry (FR-054).");
        Assert.AreEqual(
            typeof(NewRequestPreviewViewModel).FullName,
            navigation.Navigations[0].PageKey,
            "With the answer captured the flow continues, rather than asking the same question again.");
    }

    [TestMethod]
    public void OnNavigatedTo_ComingBackWithDiesChosen_ShowsThemMarkedAgain()
    {
        var state = TwoDieState();
        state.SelectedDies = [Die(FirstDie, "DIE SHOP")];
        state.InputValue = "FGT0002000";
        var viewModel = CreateViewModel();

        viewModel.OnNavigatedTo(state);

        Assert.AreEqual(2, viewModel.Options.Count, "The job's cards are still offered.");
        Assert.AreEqual(1, viewModel.Options.Count(option => option.IsSelected), "The remembered choice is marked.");
        Assert.IsTrue(viewModel.Options[0].IsSelected);
    }

    [TestMethod]
    public void OnNavigatedTo_ForAJobWithNoDie_ReportsItAndDropsNoCards()
    {
        var noDie = TwoDieState();
        noDie.Availability = RequestJobPartAvailability.None;

        var viewModel = CreateViewModel();
        viewModel.OnNavigatedTo(noDie);

        Assert.AreEqual(0, viewModel.Options.Count, "There is nothing to offer, so nothing is invented.");
        Assert.IsTrue(viewModel.IsUnavailableVisible, "The step says why it is empty (FR-026).");
    }

    [TestMethod]
    public void OnNavigatedTo_WithoutAState_LeavesTheStep()
    {
        var navigation = new RecordingNavigationService();
        var viewModel = CreateViewModel(navigation);

        viewModel.OnNavigatedTo("not a flow state");

        Assert.AreEqual(1, navigation.GoBackCount, "The step cannot run without the request in progress.");
    }

    private static RequestDiePart Die(string partNumber, string location) =>
        new() { PartNumber = partNumber, Location = location };

    private static NewRequestFlowState TwoDieState() => new()
    {
        WorkCenter = "100-07",
        Category = RequestCategory.Pickup,
        Item = RequestItemCatalog.FindById("pickup-die"),
        Availability = (RequestJobPartAvailability.None with { HasActiveJob = true, HasDie = true })
            .WithDies([Die(FirstDie, "DIE SHOP"), Die(SecondDie, "PRESS BAY")]),
    };

    private static NewRequestDieViewModel CreateViewModel(RecordingNavigationService? navigation = null)
        => new(navigation ?? new RecordingNavigationService());

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

        public Microsoft.UI.Xaml.Controls.Frame? Frame { get; set; }

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
