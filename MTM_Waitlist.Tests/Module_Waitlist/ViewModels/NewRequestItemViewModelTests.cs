using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;
using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Tests.Module_Waitlist.ViewModels;

/// <summary>
/// The Item step (US1, T035): it binds the filtered Item list in the catalog's Order, stops the flow with a
/// plain-language unavailable report when a chosen Item has no configuration row (FR-014), and advances on the
/// card click alone — the step has no Continue button, so the click <b>is</b> the way out.
/// </summary>
[TestClass]
public sealed class NewRequestItemViewModelTests
{
    [TestMethod]
    public void OnNavigatedTo_BindsVisibleItemsInCatalogOrder()
    {
        var viewModel = CreateViewModel(availability: RequestJobPartAvailability.All);
        var state = CreateState(RequestCategory.Pickup, RequestJobPartAvailability.All);

        viewModel.OnNavigatedTo(state);

        var expected = new NewRequestPickerService(TestCatalog.Instance)
            .GetVisibleItems(RequestCategory.Pickup, RequestJobPartAvailability.All)
            .Select(item => item.Id)
            .ToArray();

        Assert.IsTrue(expected.Length > 1, "The pickup Category must offer more than one Item for this test to mean anything.");
        CollectionAssert.AreEqual(expected, viewModel.Items.Select(option => option.Item!.Id).ToArray());
    }

    [TestMethod]
    public void OnNavigatedTo_BindsTheCategoryItWasGiven()
    {
        var viewModel = CreateViewModel(availability: RequestJobPartAvailability.All);

        viewModel.OnNavigatedTo(CreateState(RequestCategory.Deliver, RequestJobPartAvailability.All));

        var expected = new NewRequestPickerService(TestCatalog.Instance)
            .GetVisibleItems(RequestCategory.Deliver, RequestJobPartAvailability.All)
            .Select(item => item.Id)
            .ToArray();

        CollectionAssert.AreEqual(expected, viewModel.Items.Select(option => option.Item!.Id).ToArray());
    }

    [TestMethod]
    public void SelectItem_WithNoConfigurationRow_StopsTheFlowWithAPlainLanguageReport()
    {
        var navigation = new RecordingNavigationService();
        var configuredOnly = RequestItemCatalog.Items
            .Where(item => item.Id != "pickup-dunnage")
            .Select(Configured);
        var viewModel = CreateViewModel(navigation: navigation, availability: RequestJobPartAvailability.All, configurations: configuredOnly);
        var state = CreateState(RequestCategory.Pickup, RequestJobPartAvailability.All);
        viewModel.OnNavigatedTo(state);

        var unconfigured = viewModel.Items.Single(option => option.Item!.Id == "pickup-dunnage");
        Assert.IsFalse(unconfigured.IsConfigured, "The Item with no configuration row must be reported as unconfigured.");

        viewModel.SelectItemCommand.Execute(unconfigured);

        Assert.IsTrue(viewModel.IsUnavailableVisible);
        Assert.IsFalse(string.IsNullOrWhiteSpace(viewModel.UnavailableMessage));
        Assert.IsNull(state.Item, "An unconfigured Item must not be written onto the flow state.");
        Assert.AreEqual(0, navigation.Navigations.Count, "An unconfigured Item must not advance the flow.");

        // FR-026: the report is plain language, never a bare resource key.
        Assert.IsFalse(viewModel.UnavailableMessage.Contains('.', StringComparison.Ordinal)
            && viewModel.UnavailableMessage.StartsWith("RequestItem", StringComparison.Ordinal));
    }

    [TestMethod]
    public void SelectItem_WithAConfigurationRow_CapturesTheItemAndItsAnswerRequirement()
    {
        var viewModel = CreateViewModel(availability: RequestJobPartAvailability.All);
        var state = CreateState(RequestCategory.Other, RequestJobPartAvailability.All);
        viewModel.OnNavigatedTo(state);

        var other = viewModel.Items.Single(option => option.Item!.Id == "other");
        Assert.IsTrue(other.IsConfigured);

        viewModel.SelectItemCommand.Execute(other);

        Assert.IsFalse(viewModel.IsUnavailableVisible);
        Assert.IsNotNull(state.Item);
        Assert.AreEqual("other", state.Item!.Id);
        Assert.IsNotNull(state.ItemConfiguration);
        Assert.IsTrue(state.ItemConfiguration!.RequiresAnswer);
    }

    [TestMethod]
    public void SelectItem_WithAUsableConfiguration_AdvancesTheFlowImmediately()
    {
        var navigation = new RecordingNavigationService();
        var viewModel = CreateViewModel(navigation: navigation, availability: RequestJobPartAvailability.All);
        var state = CreateState(RequestCategory.Other, RequestJobPartAvailability.All);
        viewModel.OnNavigatedTo(state);

        viewModel.SelectItemCommand.Execute(viewModel.Items.Single(option => option.Item!.Id == "other"));

        Assert.AreEqual(1, navigation.Navigations.Count, "The card click is this step's advance; no second action may be needed.");
        Assert.AreEqual(NewRequestFlowRules.GetNextStepType(state).FullName, navigation.Navigations[0].PageKey);
    }

    [TestMethod]
    public void ItemStep_DeclaresNoContinueCommand()
    {
        // The step has exactly one way out. A Continue path reappearing on the view model would put the redundant
        // action back in front of the person the moment something binds it again.
        Assert.IsNull(
            typeof(NewRequestItemViewModel).GetProperty("ContinueCommand"),
            "The Item step must declare no Continue command; its card click is the advance.");
    }

    private static NewRequestFlowState CreateState(RequestCategory category, RequestJobPartAvailability availability) => new()
    {
        WorkCenter = "100-3",
        Category = category,
        Availability = availability,
    };

    private static RequestItemConfiguration Configured(RequestItemDefinition item) => new()
    {
        Item = item.Id,
        Category = item.Category.ToString(),
        ControlFlow = item.Id == "other" ? RequestItemConfiguration.CollectInputThenConfirm : RequestItemConfiguration.DirectToConfirmation,
        RequiresAnswer = item.Id == "other",
        AnswerValueType = item.Id == "other" ? RequestItemValueType.Text : null,
        PromptText = item.Id == "other" ? "Enter a short description" : null,
        MinLength = 5,
        MaxLength = 200,
        AllottedMinutes = 15,
    };

    private static NewRequestItemViewModel CreateViewModel(
        RecordingNavigationService? navigation = null,
        RequestJobPartAvailability? availability = null,
        IEnumerable<RequestItemConfiguration>? configurations = null)
    {
        var rows = configurations ?? RequestItemCatalog.Items.Select(Configured);
        return new NewRequestItemViewModel(
            navigation ?? new RecordingNavigationService(),
            new FakeNewRequestFlowService(availability ?? RequestJobPartAvailability.All),
            new FakeConfigurationService(rows));
    }

    private sealed class TestCatalog : IRequestItemCatalogService
    {
        public static TestCatalog Instance { get; } = new();

        public IReadOnlyList<RequestItemDefinition> GetAllItems() => RequestItemCatalog.Items;

        public IReadOnlyList<RequestItemDefinition> GetByCategory(RequestCategory category) => RequestItemCatalog.GetByCategory(category);

        public RequestItemDefinition? FindById(string? id) => RequestItemCatalog.FindById(id);

        public IReadOnlyList<RequestCategory> GetCategoriesInOrder() =>
        [
            RequestCategory.Pickup,
            RequestCategory.Deliver,
            RequestCategory.Assist,
            RequestCategory.Other,
        ];
    }

    private sealed class FakeConfigurationService : IRequestItemConfigurationService
    {
        private readonly RequestItemConfigurationSet _set;

        public FakeConfigurationService(IEnumerable<RequestItemConfiguration> rows)
        {
            _set = RequestItemConfigurationSet.From(rows);
        }

        public Task<RequestItemConfigurationSet> GetConfigurationsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_set);

        public Task<RequestItemConfiguration> GetConfigurationAsync(string itemCode, CancellationToken cancellationToken = default) =>
            Task.FromResult(_set.Get(itemCode));
    }

    private sealed class FakeNewRequestFlowService : INewRequestFlowService
    {
        private readonly RequestJobPartAvailability _availability;
        private readonly INewRequestPickerService _picker = new NewRequestPickerService(TestCatalog.Instance);

        public FakeNewRequestFlowService(RequestJobPartAvailability availability)
        {
            _availability = availability;
        }

        public Task<RequestJobPartAvailability> ResolveAvailabilityAsync(string workCenter, CancellationToken cancellationToken = default) =>
            Task.FromResult(_availability);

        public IReadOnlyList<RequestCategory> GetVisibleCategories(RequestJobPartAvailability availability) =>
            _picker.GetVisibleCategories(availability);

        public IReadOnlyList<RequestItemDefinition> GetVisibleItems(RequestCategory category, RequestJobPartAvailability availability) =>
            _picker.GetVisibleItems(category, availability);

        public Task<Dictionary<string, string>> BuildWorkCenterImageLookupAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new Dictionary<string, string>());
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
