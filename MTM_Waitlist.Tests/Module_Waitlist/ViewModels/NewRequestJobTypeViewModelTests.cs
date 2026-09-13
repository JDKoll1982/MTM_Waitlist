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
/// The Category step (T055): it binds exactly the Categories a job can actually reach, and choosing one moves on
/// to that Category's Item step. No type or subtype concept is reachable from this step any more (FR-003).
/// </summary>
[TestClass]
public sealed class NewRequestJobTypeViewModelTests
{
    [TestMethod]
    public async Task OnNavigatedTo_BindsTheVisibleCategoriesInCanonicalOrder()
    {
        var viewModel = CreateViewModel(availability: RequestJobPartAvailability.All);

        viewModel.OnNavigatedTo(new NewRequestFlowState { WorkCenter = "100-3" });

        await WaitUntilAsync(() => !viewModel.IsLoading && viewModel.Options.Count > 0);

        var expected = new NewRequestPickerService(TestCatalog.Instance)
            .GetVisibleCategories(RequestJobPartAvailability.All)
            .ToArray();

        CollectionAssert.AreEqual(
            expected.Select(NewRequestItemViewModel.ResolveCategoryName).ToArray(),
            viewModel.Options.Select(item => item.Name).ToArray());
        Assert.IsTrue(viewModel.Options.All(item => item.Category is not null));
    }

    [TestMethod]
    public async Task OnNavigatedTo_JobWithNoParts_OffersNoCategoryThatWouldOpenAnEmptyStep()
    {
        var viewModel = CreateViewModel(availability: RequestJobPartAvailability.None);

        viewModel.OnNavigatedTo(new NewRequestFlowState { WorkCenter = "100-17" });

        await WaitUntilAsync(() => !viewModel.IsLoading);

        var picker = new NewRequestPickerService(TestCatalog.Instance);
        var expected = picker.GetVisibleCategories(RequestJobPartAvailability.None).ToArray();

        CollectionAssert.AreEqual(
            expected.Select(NewRequestItemViewModel.ResolveCategoryName).ToArray(),
            viewModel.Options.Select(item => item.Name).ToArray());

        foreach (var tile in viewModel.Options)
        {
            Assert.IsTrue(
                picker.GetVisibleItems(tile.Category!.Value, RequestJobPartAvailability.None).Count > 0,
                $"Category '{tile.Name}' was offered but would open an empty Item step.");
        }
    }

    [TestMethod]
    public async Task OnNavigatedTo_ResolvesTheAvailabilitySnapshotAndKeepsItOnTheState()
    {
        var viewModel = CreateViewModel(availability: RequestJobPartAvailability.All);
        var state = new NewRequestFlowState { WorkCenter = "100-3" };

        viewModel.OnNavigatedTo(state);

        await WaitUntilAsync(() => !viewModel.IsLoading && viewModel.Options.Count > 0);

        Assert.IsTrue(state.Availability.HasActiveJob);
        Assert.IsTrue(state.Availability.HasCoil);
    }

    [TestMethod]
    public async Task OnNavigatedTo_JobHasCoil_ShowsCoilBanner()
    {
        var viewModel = CreateViewModel(coil: new WaitlistCoilInfo { HasCoil = true, CoilNumber = "COIL-204", QuantityOnHand = "18 coils", Description = "galvanized", AverageWeight = "1,240 lb" });

        viewModel.OnNavigatedTo(new NewRequestFlowState { WorkCenter = "100-3" });

        await WaitUntilAsync(() => !viewModel.IsLoading && viewModel.Options.Count > 0);

        Assert.IsTrue(viewModel.IsCoilBannerVisible);
        Assert.IsTrue(viewModel.CoilBannerText.Contains("COIL-204", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task OnNavigatedTo_JobHasNoCoil_ShowsNoCoilBanner()
    {
        var viewModel = CreateViewModel(coil: new WaitlistCoilInfo { HasCoil = false });

        viewModel.OnNavigatedTo(new NewRequestFlowState { WorkCenter = "100-17" });

        await WaitUntilAsync(() => !viewModel.IsLoading);

        Assert.IsTrue(viewModel.IsCoilBannerVisible);
        Assert.IsTrue(viewModel.CoilBannerText.Contains("No coil", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task SelectCategory_RecordsTheCategoryAndOpensTheItemStep()
    {
        var navigation = new RecordingNavigationService();
        var viewModel = CreateViewModel(navigation: navigation, availability: RequestJobPartAvailability.All);
        var state = new NewRequestFlowState { WorkCenter = "100-3" };
        viewModel.OnNavigatedTo(state);

        await WaitUntilAsync(() => !viewModel.IsLoading && viewModel.Options.Count > 0);
        var pickup = viewModel.Options.Single(item => item.Category == RequestCategory.Pickup);

        viewModel.SelectOptionCommand.Execute(pickup);

        Assert.AreEqual(RequestCategory.Pickup, state.Category);
        Assert.IsNull(state.Item, "The Item is its own step; the Category step must not choose one.");
        Assert.AreEqual(typeof(NewRequestItemViewModel).FullName, navigation.Navigations.Single().PageKey);
        Assert.AreSame(state, navigation.Navigations.Single().Parameter);
    }

    [TestMethod]
    public async Task SelectCategory_LeavesNoTypeOrSubtypeBehind()
    {
        var viewModel = CreateViewModel(availability: RequestJobPartAvailability.All);
        var state = new NewRequestFlowState { WorkCenter = "100-3" };
        viewModel.OnNavigatedTo(state);

        await WaitUntilAsync(() => !viewModel.IsLoading && viewModel.Options.Count > 0);
        viewModel.SelectOptionCommand.Execute(viewModel.Options.First());

        var draft = state.ToDraft();
        Assert.IsFalse(string.IsNullOrWhiteSpace(draft.Category));

        // FR-003: the wizard carries no request type and no subtype — the retired pair has no member left to hold.
        var retiredPair = typeof(WaitlistRequestDraft)
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Select(property => property.Name)
            .Where(name => name is "RequestType" or "Subtype")
            .ToArray();

        Assert.AreEqual(
            0,
            retiredPair.Length,
            $"The draft carries the retired pair again ({string.Join(", ", retiredPair)}).");
    }

    [TestMethod]
    public async Task Back_LeavesTheStep()
    {
        var navigation = new RecordingNavigationService();
        var viewModel = CreateViewModel(navigation: navigation, availability: RequestJobPartAvailability.All);
        viewModel.OnNavigatedTo(new NewRequestFlowState { WorkCenter = "100-3" });

        await WaitUntilAsync(() => !viewModel.IsLoading && viewModel.Options.Count > 0);
        viewModel.BackCommand.Execute(null);

        Assert.AreEqual(1, navigation.GoBackCount);
    }

    private static NewRequestJobTypeViewModel CreateViewModel(
        RecordingNavigationService? navigation = null,
        RequestJobPartAvailability? availability = null,
        WaitlistCoilInfo? coil = null)
        => new(
            navigation ?? new RecordingNavigationService(),
            new FakeNewRequestFlowService(availability ?? RequestJobPartAvailability.All),
            new FakeCoilService(coil ?? new WaitlistCoilInfo { HasCoil = true, CoilNumber = "COIL-204" }));

    private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 3000)
    {
        var start = DateTime.UtcNow;
        while (!condition())
        {
            if ((DateTime.UtcNow - start).TotalMilliseconds > timeoutMs)
            {
                Assert.Fail("Timed out waiting for the category step to finish loading.");
            }

            await Task.Delay(20);
        }
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

    private sealed class FakeCoilService : ICoilAvailabilityService
    {
        private readonly WaitlistCoilInfo _coil;

        public FakeCoilService(WaitlistCoilInfo coil)
        {
            _coil = coil;
        }

        public Task<WaitlistCoilInfo> GetCoilForJobAsync(string? workCenter, CancellationToken cancellationToken = default) =>
            Task.FromResult(_coil);
    }
}
