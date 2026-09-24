using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml.Navigation;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Shared.Helpers;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Tests.Module_Waitlist.ViewModels;

/// <summary>
/// The component step: the operator picks the component they need from the ones the requesting job carries, by
/// clicking a box for each — never by picking a number out of a drop-down list — with a temporary picture
/// placeholder on every box until part numbers can be pictured.
/// </summary>
/// <remarks>
/// The step reads the components the <b>job</b> carries and the wording the <b>row</b> declares, never the Item the
/// requester chose, so what is offered and what is asked change without any code being edited (FR-013).
/// </remarks>
[TestClass]
public sealed class NewRequestComponentViewModelTests
{
    private const string FirstComponent = "V-EMB-2";
    private const string SecondComponent = "CMP0004455";

    [TestMethod]
    public void OnNavigatedTo_BindsABoxForEveryComponentTheJobCarries_EachWithThePlaceholderPicture()
    {
        var viewModel = CreateViewModel();
        viewModel.OnNavigatedTo(TwoComponentState());

        CollectionAssert.AreEqual(
            new[] { FirstComponent, SecondComponent },
            viewModel.Options.Select(option => option.Title).ToArray(),
            "Every component the job carries is a box, in the job's own order.");
        CollectionAssert.AreEqual(
            new[] { FirstComponent, SecondComponent },
            viewModel.Options.Select(option => option.PartNumber).ToArray(),
            "The box offers the part number, which is the value the request will store.");
        Assert.IsTrue(
            viewModel.Options.All(option => option.ImagePath == ImagePicturePolicy.NoImagePath),
            "Until part numbers can be pictured, every box draws the application's shared no-image placeholder.");
        Assert.IsFalse(viewModel.IsUnavailableVisible, "A job that carries components has nothing to report.");
        Assert.AreEqual(
            0,
            viewModel.Options.Count(option => option.IsSelected),
            "Nothing is chosen on the operator's behalf.");
    }

    [TestMethod]
    public void EveryBox_NamesItselfAndItsPictureBox_SoTheStandInCanBeFoundByAutomation()
    {
        var viewModel = CreateViewModel();
        viewModel.OnNavigatedTo(TwoComponentState());

        var ids = viewModel.Options.Select(option => option.AutomationId).ToArray();
        var imageIds = viewModel.Options.Select(option => option.ImageAutomationId).ToArray();

        CollectionAssert.AreEqual(
            new[] { $"{NewRequestComponentOption.AutomationIdPrefix}_{FirstComponent}", $"{NewRequestComponentOption.AutomationIdPrefix}_{SecondComponent}" },
            ids,
            "Each box is named after the part it offers.");
        CollectionAssert.AreEqual(
            new[] { $"{NewRequestComponentOption.AutomationIdPrefix}Image_{FirstComponent}", $"{NewRequestComponentOption.AutomationIdPrefix}Image_{SecondComponent}" },
            imageIds,
            "Each picture placeholder is named after the part whose box it sits in.");
        Assert.AreEqual(
            ids.Length,
            ids.Distinct(StringComparer.Ordinal).Count(),
            "Two parts must never share one automation name, or automation cannot tell the boxes apart.");
    }

    [TestMethod]
    public void OnNavigatedTo_AsksTheRowsOwnPrompt()
    {
        // The two component rows ask the question differently — one collects, one brings — and the step asks what
        // the ROW says rather than flattening both into one wording (FR-013).
        var state = TwoComponentState();
        state.ItemConfiguration = RowWithPrompt("Choose the component to bring.");
        var viewModel = CreateViewModel();

        viewModel.OnNavigatedTo(state);

        Assert.AreEqual("Choose the component to bring.", viewModel.PromptText);
    }

    [TestMethod]
    public void OnNavigatedTo_WithNoConfiguredPrompt_AsksThroughTheResourceMechanism()
    {
        var state = TwoComponentState();
        state.ItemConfiguration = RowWithPrompt(null);
        var viewModel = CreateViewModel();

        viewModel.OnNavigatedTo(state);

        Assert.IsFalse(string.IsNullOrWhiteSpace(viewModel.PromptText), "The step always asks something (FR-022).");
        Assert.IsFalse(
            viewModel.PromptText.StartsWith("NewRequest_", StringComparison.Ordinal),
            "The question is plain language, never a bare resource key (FR-022).");
    }

    [TestMethod]
    public void SelectComponent_RecordsThePartAndMovesOn()
    {
        var navigation = new RecordingNavigationService();
        var viewModel = CreateViewModel(navigation);
        var state = TwoComponentState();
        viewModel.OnNavigatedTo(state);

        viewModel.SelectComponentCommand.Execute(viewModel.Options[1]);

        Assert.AreEqual(
            SecondComponent,
            state.InputValue,
            "The clicked box's part number becomes the request's answer, which the card's second line reads back.");
        Assert.AreEqual(
            typeof(NewRequestSummaryViewModel).FullName,
            navigation.Navigations[0].PageKey,
            "One component is one answer, so the click moves the flow on to the confirmation step rather than "
            + "marking a card to commit later.");
        Assert.IsTrue(viewModel.Options[1].IsSelected, "The chosen box is marked.");
        Assert.IsFalse(viewModel.Options[0].IsSelected, "Choosing one component does not mark another.");
    }

    [TestMethod]
    public void OnNavigatedTo_ComingBackWithAComponentChosen_ShowsItMarkedAgain()
    {
        var state = TwoComponentState();
        state.InputValue = FirstComponent;
        var viewModel = CreateViewModel();

        viewModel.OnNavigatedTo(state);

        Assert.AreEqual(2, viewModel.Options.Count, "The job's boxes are still offered.");
        Assert.AreEqual(1, viewModel.Options.Count(option => option.IsSelected), "The remembered choice is marked.");
        Assert.IsTrue(viewModel.Options[0].IsSelected);
    }

    [TestMethod]
    public void OnNavigatedTo_ForAJobWithNoComponent_ReportsItAndDropsNoBoxes()
    {
        var noComponent = TwoComponentState();
        noComponent.Availability = RequestJobPartAvailability.None;
        var viewModel = CreateViewModel();

        viewModel.OnNavigatedTo(noComponent);

        Assert.AreEqual(0, viewModel.Options.Count, "There is nothing to offer, so nothing is invented.");
        Assert.IsTrue(viewModel.IsUnavailableVisible, "The step says why it is empty (FR-026).");
        Assert.IsFalse(
            viewModel.UnavailableMessage.StartsWith("NewRequest_", StringComparison.Ordinal),
            "The report is plain language, never a bare resource key (FR-022).");
    }

    [TestMethod]
    public void OnNavigatedTo_WithoutAState_LeavesTheStep()
    {
        var navigation = new RecordingNavigationService();
        var viewModel = CreateViewModel(navigation);

        viewModel.OnNavigatedTo("not a flow state");

        Assert.AreEqual(1, navigation.GoBackCount, "The step cannot run without the request in progress.");
    }

    private static RequestItemConfiguration RowWithPrompt(string? promptText) => new()
    {
        Item = "deliver-component",
        Category = "Deliver",
        ControlFlow = RequestItemConfiguration.CollectInputThenConfirm,
        RequiresAnswer = true,
        AnswerValueType = RequestItemValueType.Enum,
        PromptText = promptText,
        DetailFields = new[]
        {
            new RequestItemFieldDefinition
            {
                Label = "Component",
                ValueType = RequestItemValueType.Enum,
                Source = RequestItemFieldDefinition.Sources.Answer,
                List = RequestItemFieldDefinition.Lists.Component,
                Order = 1,
                IsRequired = true,
            },
        },
    };

    private static NewRequestFlowState TwoComponentState() => new()
    {
        WorkCenter = "100-03",
        Category = RequestCategory.Deliver,
        Item = RequestItemCatalog.FindById("deliver-component"),
        ItemConfiguration = RowWithPrompt("Choose the component to bring."),
        Availability = (RequestJobPartAvailability.None with { HasActiveJob = true, HasComponent = true })
            .WithComponentPartNumbers([FirstComponent, SecondComponent]),
    };

    private static NewRequestComponentViewModel CreateViewModel(RecordingNavigationService? navigation = null)
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
