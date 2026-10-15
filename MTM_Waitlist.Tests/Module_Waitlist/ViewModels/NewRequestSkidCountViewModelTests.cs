using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml.Navigation;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Waitlist.Helpers;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;
using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Tests.Module_Waitlist.ViewModels;

/// <summary>
/// FR-087: the four returning pickups ask how many skids must be picked up, as a numeric answer that defaults to
/// one and can be changed.
/// </summary>
[TestClass]
public sealed class NewRequestSkidCountViewModelTests
{
    [TestMethod]
    public void ViewModel_NewRequestSkidCountViewModelTests_OnNavigatedTo_DefaultsToASingleSkid()
    {
        var viewModel = CreateViewModel();

        viewModel.OnNavigatedTo(SkidCountState());

        Assert.AreEqual("1", viewModel.SkidCountText, "The count defaults to one (FR-087).");
        Assert.IsFalse(viewModel.IsValidationVisible, "The default is a valid answer, so nothing is refused.");
    }

    [TestMethod]
    public void ViewModel_NewRequestSkidCountViewModelTests_Continue_WithTheDefaultRecordsOneAndMovesOn()
    {
        var navigation = new RecordingNavigationService();
        var state = SkidCountState();
        var viewModel = CreateViewModel(navigation);
        viewModel.OnNavigatedTo(state);

        viewModel.ContinueCommand.Execute(null);

        Assert.AreEqual(
            "1",
            state.AnswerFor(NewRequestFlowRules.SkidCountLabel),
            "Accepting the default records one skid under the label the Item declares (FR-087).");
        Assert.AreEqual(
            typeof(NewRequestSummaryViewModel).FullName,
            navigation.Navigations[0].PageKey,
            "The count defaults to one, so the step never blocks on the operator typing it.");
    }

    [TestMethod]
    public void ViewModel_NewRequestSkidCountViewModelTests_Continue_RecordsAChangedCount()
    {
        var navigation = new RecordingNavigationService();
        var state = SkidCountState();
        var viewModel = CreateViewModel(navigation);
        viewModel.OnNavigatedTo(state);
        viewModel.SkidCountText = "3";

        viewModel.ContinueCommand.Execute(null);

        Assert.AreEqual(
            "3",
            state.AnswerFor(NewRequestFlowRules.SkidCountLabel),
            "The count is changeable (FR-087).");
        Assert.AreEqual(1, navigation.Navigations.Count, "With the count captured the flow moves on.");
    }

    [TestMethod]
    public void ViewModel_NewRequestSkidCountViewModelTests_Continue_WithABlankAnswerKeepsTheDefaultOfOne()
    {
        var navigation = new RecordingNavigationService();
        var state = SkidCountState();
        var viewModel = CreateViewModel(navigation);
        viewModel.OnNavigatedTo(state);
        viewModel.SkidCountText = "   ";

        viewModel.ContinueCommand.Execute(null);

        Assert.AreEqual(
            "1",
            state.AnswerFor(NewRequestFlowRules.SkidCountLabel),
            "A blank answer is the default of one rather than a refusal, because the count defaults to one (FR-087).");
    }

    [DataTestMethod]
    [DataRow("two")]
    [DataRow("0")]
    [DataRow("-1")]
    [DataRow("2.5")]
    public void ViewModel_NewRequestSkidCountViewModelTests_Continue_WithAnAnswerThatIsNotAWholeNumberRefuses(string typed)
    {
        var navigation = new RecordingNavigationService();
        var state = SkidCountState();
        var viewModel = CreateViewModel(navigation);
        viewModel.OnNavigatedTo(state);
        viewModel.SkidCountText = typed;

        viewModel.ContinueCommand.Execute(null);

        Assert.IsFalse(state.HasAnswer(NewRequestFlowRules.SkidCountLabel), $"'{typed}' is not a count of skids.");
        Assert.AreEqual(0, navigation.Navigations.Count, "A count that is not a whole number above zero is refused.");
        Assert.IsTrue(viewModel.IsValidationVisible, "The step says why it will not move on.");
        Assert.IsFalse(
            viewModel.ValidationMessage.StartsWith("NewRequest_", StringComparison.Ordinal),
            "The report is plain language, never a bare resource key (FR-022).");
    }

    [TestMethod]
    public void ViewModel_NewRequestSkidCountViewModelTests_OnNavigatedTo_ComingBackWithACountShowsItAgain()
    {
        var state = SkidCountState();
        state.RecordAnswer(NewRequestFlowRules.SkidCountLabel, "4");
        var viewModel = CreateViewModel();

        viewModel.OnNavigatedTo(state);

        Assert.AreEqual("4", viewModel.SkidCountText, "Coming back to the step shows the count already given.");
    }

    [TestMethod]
    public void ViewModel_NewRequestSkidCountViewModelTests_OnNavigatedTo_WithoutAStateLeavesTheStep()
    {
        var navigation = new RecordingNavigationService();
        var viewModel = CreateViewModel(navigation);

        viewModel.OnNavigatedTo("not a flow state");

        Assert.AreEqual(1, navigation.GoBackCount, "The step cannot run without the request in progress.");
    }

    [DataTestMethod]
    [DataRow("pickup-fg")]
    [DataRow("pickup-ncm")]
    [DataRow("pickup-wip")]
    [DataRow("pickup-outside-service")]
    public void ViewModel_NewRequestSkidCountViewModelTests_TheSkidCountLabel_IsACellTheCardDrawsForEveryReturningPickup(
        string itemCode)
    {
        // The four returning pickups draw exactly Part Type and # of Skids, so the label this step captures the
        // count under is one the card draws and the other declared fields are not (FR-036 to FR-039, FR-087).
        Assert.IsTrue(
            WaitlistCardCellRules.IsCell(itemCode, NewRequestFlowRules.SkidCountLabel),
            $"'{itemCode}' must draw its # of Skids cell from the answer this step captures.");
        Assert.IsTrue(
            WaitlistCardCellRules.IsCell(itemCode, "Part Type"),
            $"'{itemCode}' keeps its derived Part Type cell beside the count.");
    }

    private static NewRequestFlowState SkidCountState() => new()
    {
        WorkCenter = "100-07",
        Category = RequestCategory.Pickup,
        Item = RequestItemCatalog.FindById("pickup-fg"),
        ItemConfiguration = new RequestItemConfiguration
        {
            Item = "pickup-fg",
            ControlFlow = RequestItemConfiguration.DirectToConfirmation,
            DetailFields =
            [
                new RequestItemFieldDefinition
                {
                    Label = NewRequestFlowRules.SkidCountLabel,
                    ValueType = RequestItemValueType.String,
                    Source = RequestItemFieldDefinition.Sources.Answer,
                    Order = 2,
                },
            ],
        },
    };

    private static NewRequestSkidCountViewModel CreateViewModel(RecordingNavigationService? navigation = null) =>
        new(navigation ?? new RecordingNavigationService());

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
