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
/// FR-086 with FR-054: an assist coil turn asks for the weight of the coil as a <b>required numeric</b> answer, and
/// a blank answer prevents confirmation. It is deliberately <b>not</b> the job's average coil weight, which is a
/// snapshot value about the coil's part and is never what the operator supplied.
/// </summary>
[TestClass]
public sealed class NewRequestCoilTurnViewModelTests
{
    /// <summary>The coil's own average weight the request's job snapshot carries — never the answer (FR-054).</summary>
    private const string JobsAverageCoilWeight = "1200";

    [TestMethod]
    public void ViewModel_NewRequestCoilTurnViewModelTests_OnNavigatedTo_AsksForTheWeightAndStartsBlank()
    {
        var viewModel = CreateViewModel();

        viewModel.OnNavigatedTo(CoilTurnState());

        Assert.AreEqual(string.Empty, viewModel.WeightText, "The operator supplies the weight rather than inheriting one.");
        Assert.IsFalse(
            viewModel.PromptText.StartsWith("NewRequest_", StringComparison.Ordinal),
            "The question is plain language, never a bare resource key (FR-022).");
        Assert.IsFalse(viewModel.IsValidationVisible, "Nothing is refused before the operator has answered.");
    }

    [TestMethod]
    public void ViewModel_NewRequestCoilTurnViewModelTests_Continue_WithABlankAnswerRefusesAndStaysOnTheStep()
    {
        var navigation = new RecordingNavigationService();
        var state = CoilTurnState();
        var viewModel = CreateViewModel(navigation);
        viewModel.OnNavigatedTo(state);

        viewModel.ContinueCommand.Execute(null);

        Assert.IsFalse(
            state.HasAnswer(NewRequestFlowRules.CoilWeightLabel),
            "A blank weight is not an answer, so nothing is recorded for it (FR-086).");
        Assert.AreEqual(0, navigation.Navigations.Count, "A blank answer prevents confirmation (FR-086).");
        Assert.IsTrue(viewModel.IsValidationVisible, "The step says why it will not move on.");
        Assert.IsFalse(
            viewModel.ValidationMessage.StartsWith("NewRequest_", StringComparison.Ordinal),
            "The report is plain language, never a bare resource key (FR-022).");
    }

    [DataTestMethod]
    [DataRow("not a weight")]
    [DataRow("1840 lb")]
    [DataRow("0")]
    [DataRow("-12")]
    public void ViewModel_NewRequestCoilTurnViewModelTests_Continue_WithAnAnswerThatIsNotAPositiveNumberRefuses(
        string typed)
    {
        var navigation = new RecordingNavigationService();
        var state = CoilTurnState();
        var viewModel = CreateViewModel(navigation);
        viewModel.OnNavigatedTo(state);
        viewModel.WeightText = typed;

        viewModel.ContinueCommand.Execute(null);

        Assert.IsFalse(state.HasAnswer(NewRequestFlowRules.CoilWeightLabel), $"'{typed}' is not a weight.");
        Assert.AreEqual(0, navigation.Navigations.Count, "Only a positive number confirms the step.");
    }

    [TestMethod]
    public void ViewModel_NewRequestCoilTurnViewModelTests_Continue_RecordsTheWeightTheOperatorSuppliedAndNotTheJobsAverage()
    {
        var navigation = new RecordingNavigationService();
        var state = CoilTurnState();
        var viewModel = CreateViewModel(navigation);
        viewModel.OnNavigatedTo(state);
        viewModel.WeightText = "1840.5";

        viewModel.ContinueCommand.Execute(null);

        Assert.AreEqual(
            "1840.5",
            state.AnswerFor(NewRequestFlowRules.CoilWeightLabel),
            "The answer is the weight the operator supplied, under the label the Item declares (FR-086).");
        Assert.AreNotEqual(
            JobsAverageCoilWeight,
            state.AnswerFor(NewRequestFlowRules.CoilWeightLabel),
            "Weight of Coil must not be conflated with the job's average coil weight (FR-054).");
        Assert.AreEqual(
            typeof(NewRequestSummaryViewModel).FullName,
            navigation.Navigations[0].PageKey,
            "With the weight captured the flow moves on.");
    }

    [TestMethod]
    public void ViewModel_NewRequestCoilTurnViewModelTests_OnNavigatedTo_ComingBackWithAWeightShowsItAgain()
    {
        var state = CoilTurnState();
        state.RecordAnswer(NewRequestFlowRules.CoilWeightLabel, "1840");
        var viewModel = CreateViewModel();

        viewModel.OnNavigatedTo(state);

        Assert.AreEqual("1840", viewModel.WeightText, "Coming back to the step shows the weight already supplied.");
    }

    [TestMethod]
    public void ViewModel_NewRequestCoilTurnViewModelTests_OnNavigatedTo_WithoutAStateLeavesTheStep()
    {
        var navigation = new RecordingNavigationService();
        var viewModel = CreateViewModel(navigation);

        viewModel.OnNavigatedTo("not a flow state");

        Assert.AreEqual(1, navigation.GoBackCount, "The step cannot run without the request in progress.");
    }

    [TestMethod]
    public void ViewModel_NewRequestCoilTurnViewModelTests_TheWeightLabel_IsACellTheCardDrawsAndNotTheQuantityInHouse()
    {
        // assist-coil-turn draws Coil Number and Weight of Coil; Quantity in house is deliberately excluded from
        // the card, so the label this step captures the weight under is the one drawn (FR-050, FR-086).
        Assert.IsTrue(
            WaitlistCardCellRules.IsCell("assist-coil-turn", NewRequestFlowRules.CoilWeightLabel),
            "The coil turn card draws its Weight of Coil cell from the answer this step captures.");
        Assert.IsFalse(
            WaitlistCardCellRules.IsCell("assist-coil-turn", "Quantity in house"),
            "The coil's quantity in house is not one of the card's two cells.");
    }

    private static NewRequestFlowState CoilTurnState() => new()
    {
        WorkCenter = "100-07",
        Category = RequestCategory.Assist,
        Item = RequestItemCatalog.FindById("assist-coil-turn"),
        Availability = RequestJobPartAvailability.None with
        {
            HasActiveJob = true,
            HasCoil = true,
            CoilPartNumber = "MMC0001000",
            CoilAverageWeight = JobsAverageCoilWeight,
        },
        ItemConfiguration = new RequestItemConfiguration
        {
            Item = "assist-coil-turn",
            ControlFlow = RequestItemConfiguration.DirectToConfirmation,
            DetailFields =
            [
                new RequestItemFieldDefinition
                {
                    Label = NewRequestFlowRules.CoilWeightLabel,
                    ValueType = RequestItemValueType.String,
                    Source = RequestItemFieldDefinition.Sources.Answer,
                    Order = 2,
                    IsRequired = true,
                },
            ],
        },
    };

    private static NewRequestCoilTurnViewModel CreateViewModel(RecordingNavigationService? navigation = null) =>
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
