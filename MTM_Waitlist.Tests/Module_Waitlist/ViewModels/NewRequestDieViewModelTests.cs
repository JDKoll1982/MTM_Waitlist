using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml.Navigation;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Waitlist.Helpers;
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
    public void ViewModel_NewRequestDieViewModelTests_OnNavigatedTo_BindsEveryDieTheJobCarriesWithItsHomeLocation()
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
    public void ViewModel_NewRequestDieViewModelTests_SelectDie_TogglesTheCardRatherThanMovingOn()
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
    public void ViewModel_NewRequestDieViewModelTests_SelectAll_ChoosesEveryDieTheJobCarries()
    {
        var viewModel = CreateViewModel();
        viewModel.OnNavigatedTo(TwoDieState());

        viewModel.SelectAllCommand.Execute(null);

        Assert.AreEqual(2, viewModel.Options.Count(option => option.IsSelected), "Select all takes the whole list (FR-054).");
    }

    [TestMethod]
    public void ViewModel_NewRequestDieViewModelTests_Continue_WithNothingChosenRefusesAndStaysOnTheStep()
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
    public void ViewModel_NewRequestDieViewModelTests_Continue_WithTwoChosenRecordsBothDiesAndMovesOn()
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
            typeof(NewRequestSummaryViewModel).FullName,
            navigation.Navigations[0].PageKey,
            "With the answer captured the flow continues to the confirmation step, rather than asking the same "
            + "question again.");
    }

    [TestMethod]
    public void ViewModel_NewRequestDieViewModelTests_OnNavigatedTo_ComingBackWithDiesChosenShowsThemMarkedAgain()
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
    public void ViewModel_NewRequestDieViewModelTests_OnNavigatedTo_ForAJobWithNoDieReportsItAndDropsNoCards()
    {
        var noDie = TwoDieState();
        noDie.Availability = RequestJobPartAvailability.None;

        var viewModel = CreateViewModel();
        viewModel.OnNavigatedTo(noDie);

        Assert.AreEqual(0, viewModel.Options.Count, "There is nothing to offer, so nothing is invented.");
        Assert.IsTrue(viewModel.IsUnavailableVisible, "The step says why it is empty (FR-026).");
    }

    [TestMethod]
    public void ViewModel_NewRequestDieViewModelTests_OnNavigatedTo_WithoutAStateLeavesTheStep()
    {
        var navigation = new RecordingNavigationService();
        var viewModel = CreateViewModel(navigation);

        viewModel.OnNavigatedTo("not a flow state");

        Assert.AreEqual(1, navigation.GoBackCount, "The step cannot run without the request in progress.");
    }

    [TestMethod]
    public void ViewModel_NewRequestDieViewModelTests_Continue_WithTheDieShopCheckboxTickedRecordsTheAnswerTheCardReads()
    {
        var state = TwoDieState();
        var viewModel = CreateViewModel();
        viewModel.OnNavigatedTo(state);

        viewModel.IsDieShopSelected = true;
        viewModel.SelectDieCommand.Execute(viewModel.Options[0]);
        viewModel.ContinueCommand.Execute(null);

        Assert.IsTrue(
            WaitlistCardCellRules.IsDieShopAnswerGiven(state.AnswerFor(WaitlistCardCellRules.DieShopAnswerLabel)),
            "A ticked checkbox is an answer the card draws its third, label-less cell for (FR-081).");
        Assert.AreEqual(
            WaitlistCardCellRules.DieShopAnswerLabel,
            state.Answers.Keys.Single(),
            "The answer is captured under the label the die Items declare, which is the label the card reads (FR-081).");
    }

    [TestMethod]
    public void ViewModel_NewRequestDieViewModelTests_Continue_WithTheCheckboxUntickedWritesNoAnswerSoTheCardDrawsTwoCells()
    {
        var state = TwoDieState();
        var viewModel = CreateViewModel();
        viewModel.OnNavigatedTo(state);

        viewModel.SelectDieCommand.Execute(viewModel.Options[0]);
        viewModel.ContinueCommand.Execute(null);

        Assert.IsFalse(
            state.HasAnswer(WaitlistCardCellRules.DieShopAnswerLabel),
            "An unticked checkbox is written as no answer at all, so the card draws two cells rather than a third one (FR-055, FR-081).");
        Assert.IsFalse(
            WaitlistCardCellRules.IsDieShopAnswerGiven(state.AnswerFor(WaitlistCardCellRules.DieShopAnswerLabel)),
            "The card's own rule agrees that nothing was answered.");
    }

    [TestMethod]
    public void ViewModel_NewRequestDieViewModelTests_OnNavigatedTo_ComingBackWithTheDieShopAnswerTickedShowsItTicked()
    {
        var state = TwoDieState();
        state.RecordAnswer(WaitlistCardCellRules.DieShopAnswerLabel, "true");
        var viewModel = CreateViewModel();

        viewModel.OnNavigatedTo(state);

        Assert.IsTrue(viewModel.IsDieShopSelected, "Coming back to the step shows the checkbox already ticked (FR-081).");
    }

    [TestMethod]
    public void ViewModel_NewRequestDieViewModelTests_OnNavigatedTo_AsksTheDieShopQuestionInPlainLanguage()
    {
        var viewModel = CreateViewModel();

        viewModel.OnNavigatedTo(TwoDieState());

        Assert.IsFalse(
            viewModel.DieShopPromptText.StartsWith("NewRequest_", StringComparison.Ordinal),
            "The checkbox's question is plain language, never a bare resource key (FR-022).");
    }

    [TestMethod]
    public void ViewModel_NewRequestDieViewModelTests_OnNavigatedTo_WithoutAResolvedWordingKeepsTheStepsOwnDefault()
    {
        // The direction's wording is resolved from the resource file, and a wording that is not there must leave
        // the step with the question it already had rather than showing a resource key to the operator (FR-022).
        var viewModel = CreateViewModel();
        var before = viewModel.DieShopPromptText;

        viewModel.OnNavigatedTo(new NewRequestFlowState
        {
            WorkCenter = "100-07",
            Category = RequestCategory.Pickup,
            Item = RequestItemCatalog.FindById("pickup-coil"),
        });

        Assert.AreEqual(
            before,
            viewModel.DieShopPromptText,
            "An Item with no die-shop direction replaced the step's own question (FR-022, FR-081).");
    }

    [TestMethod]
    public void ViewModel_NewRequestDieViewModelTests_TheDieShopLabel_IsTheLabelLessCellTheCardDrawsOnlyWhenGiven()
    {
        // The label the checkbox's answer is captured under is the die Items' die-shop field, and the phrase the
        // card draws is chosen by the Item's direction — never by the answer's own text (FR-055, D40).
        foreach (var itemCode in new[] { "pickup-die", "deliver-die" })
        {
            Assert.IsTrue(
                WaitlistCardCellRules.IsDieShopField(itemCode, WaitlistCardCellRules.DieShopAnswerLabel),
                $"'{itemCode}' declares the die-shop field the step answers under.");
            Assert.IsTrue(
                WaitlistCardCellRules.IsCell(itemCode, WaitlistCardCellRules.DieShopAnswerLabel),
                $"'{itemCode}' must draw a cell for the answer the checkbox captures.");
            Assert.IsTrue(
                WaitlistCardCellRules.DieShopPhrase(itemCode)?.Length > 0,
                $"'{itemCode}' must resolve its die-shop phrase from the resource file (FR-104).");
        }

        Assert.IsTrue(
            WaitlistCardCellRules.IsDieShopAnswerGiven("true"),
            "A ticked checkbox is an answer the card draws the third cell for.");
    }

    [TestMethod]
    public void ViewModel_NewRequestDieViewModelTests_TheDirections_AskDifferentDieShopQuestions()
    {
        // The one tick means "take it there" on a pickup and "it is already there" on a deliver, so the two
        // directions cannot ask the same question (FR-081). The wording itself is resolved from the resource
        // file, which a host with no resources cannot read, so what is pinned here is the rule: the two
        // directions name different questions, and an Item with no die-shop direction is given none.
        Assert.AreNotEqual(
            WaitlistCardCellRules.TakeToDieShopPromptKey,
            WaitlistCardCellRules.LocatedInDieShopPromptKey,
            "Both die directions ask one question, so the same tick answers two different things (FR-081).");
        Assert.IsNull(
            WaitlistCardCellRules.DieShopPrompt("pickup-coil"),
            "An Item that has no die-shop direction asks no die-shop question (FR-081).");
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

    private static NewRequestFlowState DeliverDieState() => new()
    {
        WorkCenter = "100-07",
        Category = RequestCategory.Deliver,
        Item = RequestItemCatalog.FindById("deliver-die"),
        Availability = (RequestJobPartAvailability.None with { HasActiveJob = true, HasDie = true })
            .WithDies([Die(FirstDie, "DIE SHOP")]),
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
