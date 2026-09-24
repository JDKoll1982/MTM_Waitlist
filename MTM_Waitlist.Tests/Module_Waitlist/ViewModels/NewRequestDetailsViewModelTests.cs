using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Tests.Module_Waitlist.ViewModels;

/// <summary>
/// The additional-details step. The step has no Continue button — the answer is the way out — so a listed answer
/// that has been picked, or a typed answer Enter has handed over, must move the flow on, and an answer that does
/// not satisfy the Item's stored configuration row must be reported on the step without moving it (FR-014,
/// FR-013, FR-026).
/// </summary>
[TestClass]
public sealed class NewRequestDetailsViewModelTests
{
    [TestMethod]
    public void Continue_WithNoListedAnswerChosen_ReportsItAndStaysOnTheStep()
    {
        var navigation = new RecordingNavigationService();
        var viewModel = new NewRequestDetailsViewModel(navigation);
        var state = CreateState(EnumConfiguration(), RequestCategory.Assist);

        viewModel.OnNavigatedTo(state);
        viewModel.ContinueCommand.Execute(null);

        Assert.IsTrue(viewModel.IsValidationVisible, "An unchosen answer must be reported rather than silently accepted.");
        Assert.AreEqual(0, navigation.Navigations.Count, "The step must not advance without an answer.");
        Assert.IsNull(state.InputValue);
    }

    [TestMethod]
    public void Continue_WithAListedAnswerChosen_RecordsItAndAdvancesToTheConfirmStep()
    {
        var navigation = new RecordingNavigationService();
        var viewModel = new NewRequestDetailsViewModel(navigation);
        var state = CreateState(EnumConfiguration(), RequestCategory.Assist);

        viewModel.OnNavigatedTo(state);
        var chosen = viewModel.Options[1];
        viewModel.SelectedOption = chosen;

        viewModel.ContinueCommand.Execute(null);

        Assert.IsFalse(viewModel.IsValidationVisible);
        Assert.AreEqual(chosen, state.InputValue);
        Assert.AreEqual(typeof(NewRequestSummaryViewModel).FullName, navigation.Navigations[0].PageKey);
    }

    [TestMethod]
    public void Continue_WithATypedAnswerShorterThanTheConfiguredMinimum_ReportsItAndStaysOnTheStep()
    {
        var navigation = new RecordingNavigationService();
        var viewModel = new NewRequestDetailsViewModel(navigation);
        var state = CreateState(TextConfiguration(), RequestCategory.Pickup);

        viewModel.OnNavigatedTo(state);
        viewModel.InputValue = "ok";

        viewModel.ContinueCommand.Execute(null);

        Assert.IsTrue(viewModel.IsValidationVisible);
        Assert.AreEqual(0, navigation.Navigations.Count);
        Assert.IsNull(state.InputValue);
    }

    [TestMethod]
    public void Continue_WithATypedAnswerWithinTheConfiguredLength_RecordsItAndAdvancesToTheConfirmStep()
    {
        var navigation = new RecordingNavigationService();
        var viewModel = new NewRequestDetailsViewModel(navigation);
        var state = CreateState(TextConfiguration(), RequestCategory.Pickup);

        viewModel.OnNavigatedTo(state);
        viewModel.InputValue = "Coil is loaded backwards";

        viewModel.ContinueCommand.Execute(null);

        Assert.IsFalse(viewModel.IsValidationVisible);
        Assert.AreEqual("Coil is loaded backwards", state.InputValue);
        Assert.AreEqual(typeof(NewRequestSummaryViewModel).FullName, navigation.Navigations[0].PageKey);
    }

    [TestMethod]
    public void OnNavigatedTo_WithAPreviousTypedAnswer_RestoresItForCorrection()
    {
        // Arriving back from the confirm step must show the answer that was given, so the step can be corrected
        // rather than silently re-asked. The page refuses to treat that restore as a fresh choice.
        var viewModel = new NewRequestDetailsViewModel(new RecordingNavigationService());
        var state = CreateState(TextConfiguration(), RequestCategory.Pickup);
        state.InputValue = "Coil is loaded backwards";

        viewModel.OnNavigatedTo(state);

        Assert.AreEqual("Coil is loaded backwards", viewModel.InputValue);
    }

    [TestMethod]
    public void OnNavigatedTo_WithAPreviousListedAnswer_RecordsItAsTheRestoredAnswer()
    {
        // The view compares each selection change against this value to tell the restore apart from a pick, so a
        // restored answer that were not recorded here would make the step leave the moment it appeared.
        var viewModel = new NewRequestDetailsViewModel(new RecordingNavigationService());
        var state = CreateState(EnumConfiguration(), RequestCategory.Assist);
        state.InputValue = "MMC0001000";

        viewModel.OnNavigatedTo(state);

        Assert.AreEqual("MMC0001000", viewModel.SelectedOption);
        Assert.AreEqual("MMC0001000", viewModel.RestoredAnswer);
    }

    [TestMethod]
    public void OnNavigatedTo_WithNoPreviousAnswer_RecordsNoRestoredAnswer()
    {
        var viewModel = new NewRequestDetailsViewModel(new RecordingNavigationService());

        viewModel.OnNavigatedTo(CreateState(EnumConfiguration(), RequestCategory.Assist));

        Assert.IsNull(viewModel.SelectedOption);
        Assert.IsNull(viewModel.RestoredAnswer, "A step entered with no answer must not hold a value that swallows the first pick.");
    }

    private static RequestItemConfiguration EnumConfiguration() => new()
    {
        Item = "assist-table-remove",
        Category = "Assist",
        ControlFlow = RequestItemConfiguration.CollectInputThenConfirm,
        RequiresAnswer = true,
        AnswerValueType = RequestItemValueType.Enum,
        PromptText = "Which part comes off the table?",
        Options = ["MMF0001154", "MMC0001000"],
        AllottedMinutes = 15,
    };

    private static RequestItemConfiguration TextConfiguration() => new()
    {
        Item = "pickup-coil",
        Category = "Pickup",
        ControlFlow = RequestItemConfiguration.CollectInputThenConfirm,
        RequiresAnswer = true,
        AnswerValueType = RequestItemValueType.Text,
        PromptText = "Describe the problem with the coil",
        MinLength = 5,
        MaxLength = 60,
        AllottedMinutes = 15,
    };

    private static NewRequestFlowState CreateState(RequestItemConfiguration configuration, RequestCategory category) => new()
    {
        WorkCenter = "100-03",
        Category = category,
        Item = RequestItemCatalog.FindById(configuration.Item),
        ItemConfiguration = configuration,
        Availability = RequestJobPartAvailability.All,
    };

    private sealed class RecordingNavigationService : INavigationService
    {
        public List<(string PageKey, object? Parameter)> Navigations { get; } = new();

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

        public bool GoBack() => true;

        public void SetListDataItemForNextConnectedAnimation(object item)
        {
        }
    }
}
