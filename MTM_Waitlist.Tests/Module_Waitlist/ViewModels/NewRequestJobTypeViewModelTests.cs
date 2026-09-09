using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;
using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Tests.Module_Waitlist.ViewModels;

[TestClass]
public sealed class NewRequestJobTypeViewModelTests
{
    // --- Legacy fallback path (DB-down → default types carry no Category/ItemId) ---

    [TestMethod]
    public async Task OnNavigatedTo_LegacyFallback_JobHasNoCoil_CoilTileIsHidden()
    {
        var viewModel = CreateViewModel(coil: new WaitlistCoilInfo { HasCoil = false });

        viewModel.OnNavigatedTo(new NewRequestFlowState { WorkCenter = "100-17" });

        await WaitUntilAsync(() => !viewModel.IsLoading && viewModel.Options.Any(item => item.Name == "Pickup"));

        Assert.IsFalse(viewModel.IsCanonicalPicker);
        Assert.IsFalse(viewModel.Options.Any(item => string.Equals(item.Name, "Coil", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(viewModel.Options.Any(item => string.Equals(item.Name, "Pickup", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public async Task OnNavigatedTo_LegacyFallback_JobHasCoil_CoilTileIsShown()
    {
        var viewModel = CreateViewModel(coil: new WaitlistCoilInfo { HasCoil = true, CoilNumber = "COIL-204" });

        viewModel.OnNavigatedTo(new NewRequestFlowState { WorkCenter = "100-3" });

        await WaitUntilAsync(() => !viewModel.IsLoading && viewModel.Options.Any(item => item.Name == "Pickup"));

        Assert.IsFalse(viewModel.IsCanonicalPicker);
        Assert.IsTrue(viewModel.Options.Any(item => string.Equals(item.Name, "Coil", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public async Task OnNavigatedTo_JobHasCoil_ShowsCoilBanner()
    {
        var viewModel = CreateViewModel(coil: new WaitlistCoilInfo { HasCoil = true, CoilNumber = "COIL-204", QuantityOnHand = "18 coils", Description = "galvanized", AverageWeight = "1,240 lb" });

        viewModel.OnNavigatedTo(new NewRequestFlowState { WorkCenter = "100-3" });

        await WaitUntilAsync(() => !viewModel.IsLoading && viewModel.Options.Any(item => item.Name == "Pickup"));

        Assert.IsTrue(viewModel.IsCoilBannerVisible);
        Assert.IsTrue(viewModel.CoilBannerText.Contains("COIL-204", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task OnNavigatedTo_JobHasNoCoil_ShowsNoCoilBanner()
    {
        var viewModel = CreateViewModel(coil: new WaitlistCoilInfo { HasCoil = false });

        viewModel.OnNavigatedTo(new NewRequestFlowState { WorkCenter = "100-17" });

        await WaitUntilAsync(() => !viewModel.IsLoading && viewModel.Options.Any(item => item.Name == "Pickup"));

        Assert.IsTrue(viewModel.IsCoilBannerVisible);
        Assert.IsTrue(viewModel.CoilBannerText.Contains("No coil", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task OnNavigatedTo_LivePlaceholderNoCoilNumber_HidesBanner()
    {
        var viewModel = CreateViewModel(coil: new WaitlistCoilInfo { HasCoil = true });

        viewModel.OnNavigatedTo(new NewRequestFlowState { WorkCenter = "100-3" });

        await WaitUntilAsync(() => !viewModel.IsLoading && viewModel.Options.Any(item => item.Name == "Pickup"));

        Assert.IsFalse(viewModel.IsCoilBannerVisible);
    }

    // --- Canonical Category→Item picker path (real DB tree carries Category/ItemId) ---

    [TestMethod]
    public async Task OnNavigatedTo_CanonicalTree_ShowsFourCategoryTiles()
    {
        var viewModel = CreateViewModel(
            types: CanonicalTree(),
            coil: new WaitlistCoilInfo { HasCoil = true, CoilNumber = "COIL-204" });

        viewModel.OnNavigatedTo(new NewRequestFlowState { WorkCenter = "100-3" });

        await WaitUntilAsync(() => !viewModel.IsLoading && viewModel.Options.Count > 0);

        Assert.IsTrue(viewModel.IsCanonicalPicker);
        CollectionAssert.AreEqual(
            new[] { "Pickup", "Deliver", "Assist", "Other" },
            viewModel.Options.Select(item => item.Name).ToArray());
        Assert.IsTrue(viewModel.Options.All(item => item.IsCategoryTile));
    }

    [TestMethod]
    public async Task SelectCategory_DrillsIntoCategoryItems()
    {
        var viewModel = CreateViewModel(types: CanonicalTree(), coil: new WaitlistCoilInfo { HasCoil = true });
        viewModel.OnNavigatedTo(new NewRequestFlowState { WorkCenter = "100-3" });

        await WaitUntilAsync(() => !viewModel.IsLoading && viewModel.Options.Count == 4);
        var pickupTile = viewModel.Options.Single(item => item.Category == RequestCategory.Pickup);

        viewModel.SelectOptionCommand.Execute(pickupTile);

        await WaitUntilAsync(() => viewModel.Options.Count == 2 && viewModel.Options.All(item => item.IsItemTile));
        CollectionAssert.AreEqual(
            new[] { "pickup-coil", "pickup-ncm" },
            viewModel.Options.Select(item => item.Item!.ItemId).ToArray());
        Assert.IsTrue(viewModel.PromptText.StartsWith("Pickup", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task SelectCanonicalItem_SetsStateAndNavigatesToSummary()
    {
        var navigation = new RecordingNavigationService();
        var viewModel = CreateViewModel(navigation: navigation, types: CanonicalTree(), coil: new WaitlistCoilInfo { HasCoil = true });
        var state = new NewRequestFlowState { WorkCenter = "100-3" };
        viewModel.OnNavigatedTo(state);

        await WaitUntilAsync(() => !viewModel.IsLoading && viewModel.Options.Count == 4);
        viewModel.SelectOptionCommand.Execute(viewModel.Options.Single(item => item.Category == RequestCategory.Pickup));
        await WaitUntilAsync(() => viewModel.Options.Count == 2 && viewModel.Options.All(item => item.IsItemTile));
        var coilItem = viewModel.Options.Single(item => item.Item!.ItemId == "pickup-coil");

        viewModel.SelectOptionCommand.Execute(coilItem);

        Assert.AreEqual("Pickup", state.RequestType?.RequestType);
        Assert.AreEqual("Pickup Coil", state.Subtype?.Name);
        Assert.AreEqual(typeof(NewRequestSummaryViewModel).FullName, navigation.Navigations.Single().PageKey);
    }

    [TestMethod]
    public async Task SelectOtherItem_RequiresTextInput_NavigatesToDetails()
    {
        var navigation = new RecordingNavigationService();
        var viewModel = CreateViewModel(navigation: navigation, types: CanonicalTree(), coil: new WaitlistCoilInfo { HasCoil = true });
        var state = new NewRequestFlowState { WorkCenter = "100-3" };
        viewModel.OnNavigatedTo(state);

        await WaitUntilAsync(() => !viewModel.IsLoading && viewModel.Options.Count == 4);
        viewModel.SelectOptionCommand.Execute(viewModel.Options.Single(item => item.Category == RequestCategory.Other));
        await WaitUntilAsync(() => viewModel.Options.Count == 1 && viewModel.Options.All(item => item.IsItemTile));
        var otherItem = viewModel.Options.Single(item => item.Item!.ItemId == "other");

        viewModel.SelectOptionCommand.Execute(otherItem);

        Assert.AreEqual("Other", state.RequestType?.RequestType);
        Assert.AreEqual("General Text Entry", state.Subtype?.Name);
        Assert.AreEqual(typeof(NewRequestDetailsViewModel).FullName, navigation.Navigations.Single().PageKey);
    }

    [TestMethod]
    public async Task Back_FromItemStage_ReturnsToCategoryTiles()
    {
        var navigation = new RecordingNavigationService();
        var viewModel = CreateViewModel(navigation: navigation, types: CanonicalTree(), coil: new WaitlistCoilInfo { HasCoil = true });
        viewModel.OnNavigatedTo(new NewRequestFlowState { WorkCenter = "100-3" });

        await WaitUntilAsync(() => !viewModel.IsLoading && viewModel.Options.Count == 4);
        viewModel.SelectOptionCommand.Execute(viewModel.Options.Single(item => item.Category == RequestCategory.Pickup));
        await WaitUntilAsync(() => viewModel.Options.All(item => item.IsItemTile));

        viewModel.BackCommand.Execute(null);

        Assert.AreEqual(0, navigation.GoBackCount);
        Assert.IsTrue(viewModel.Options.All(item => item.IsCategoryTile));
        CollectionAssert.AreEqual(
            new[] { "Pickup", "Deliver", "Assist", "Other" },
            viewModel.Options.Select(item => item.Name).ToArray());
    }

    private static NewRequestJobTypeViewModel CreateViewModel(
        RecordingNavigationService? navigation = null,
        IReadOnlyList<NewRequestTypeDefinition>? types = null,
        WaitlistCoilInfo? coil = null)
        => new(
            navigation ?? new RecordingNavigationService(),
            new FakeNewRequestFlowService(types),
            new FakeCoilService(coil ?? new WaitlistCoilInfo { HasCoil = true, CoilNumber = "COIL-204" }));

    private static IReadOnlyList<NewRequestTypeDefinition> CanonicalTree()
    {
        var pickup = Type("Pickup",
            Sub("Pickup Coil", "Pickup", "pickup-coil"),
            Sub("Pickup NCM", "Pickup", "pickup-ncm"));
        var coil = Type("Coil",
            Sub("Bring", "Deliver", "deliver-coil"),
            Sub("Pickup", "Pickup", "pickup-coil"),
            Sub("Need Coil Turned around", "Assist", "assist-coil-turn"));
        var dieHandling = Type("Die Handling", Sub("Bring Die", "Deliver", "deliver-die"));
        var other = Type("Other", Sub("General Text Entry", "Other", "other", requiresTextInput: true));

        return new List<NewRequestTypeDefinition> { pickup, coil, dieHandling, other };
    }

    private static NewRequestTypeDefinition Type(string name, params NewRequestSubtypeDefinition[] subtypes)
        => new() { RequestType = name, Subtypes = subtypes.ToList() };

    private static NewRequestSubtypeDefinition Sub(string name, string category, string itemId, bool requiresTextInput = false)
        => new()
        {
            Name = name,
            Category = category,
            ItemId = itemId,
            RequiresTextInput = requiresTextInput,
        };

    private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 3000)
    {
        var start = DateTime.UtcNow;
        while (!condition())
        {
            if ((DateTime.UtcNow - start).TotalMilliseconds > timeoutMs)
            {
                Assert.Fail("Timed out waiting for the job type view model to finish loading.");
            }

            await Task.Delay(20);
        }
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
        private readonly IReadOnlyList<NewRequestTypeDefinition> _types;

        public FakeNewRequestFlowService(IReadOnlyList<NewRequestTypeDefinition>? types)
        {
            _types = types ?? NewRequestFlowRules.GetDefaultTypes();
        }

        public Task<IReadOnlyList<NewRequestTypeDefinition>> LoadRequestTypesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_types);

        public Task<string> ResolveRequestTypeImagePathAsync(string requestTypeName, CancellationToken cancellationToken = default) =>
            Task.FromResult(string.Empty);

        public Task<string> ResolveRequestSubtypeImagePathAsync(string requestTypeName, string subtypeName, CancellationToken cancellationToken = default) =>
            Task.FromResult(string.Empty);

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
