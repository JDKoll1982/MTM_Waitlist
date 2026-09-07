using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;
using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Tests.Module_Waitlist.ViewModels;

[TestClass]
public sealed class WaitlistViewDetailInventoryTests
{
    [TestMethod]
    public async Task LoadInventoryAsync_PopulatesRowsAndClearsEmptyFlag()
    {
        var inventory = new StubInventoryService(
            new InventoryLocationRow { PartNumber = "P1", Location = "Rack A1", OnHandQuantity = 18m },
            new InventoryLocationRow { PartNumber = "P1", Location = "Rack B2", OnHandQuantity = 6m });
        var viewModel = CreateViewModel(inventory);

        await viewModel.LoadInventoryAsync("P1");

        Assert.IsFalse(viewModel.IsInventoryEmpty);
        Assert.AreEqual(2, viewModel.InventoryRows.Count);
        Assert.AreEqual("Rack A1", viewModel.InventoryRows[0].Location);
    }

    [TestMethod]
    public async Task LoadInventoryAsync_WhenNoRows_SetsEmptyFlag()
    {
        var inventory = new StubInventoryService();
        var viewModel = CreateViewModel(inventory);

        await viewModel.LoadInventoryAsync("P1");

        Assert.IsTrue(viewModel.IsInventoryEmpty);
        Assert.AreEqual(0, viewModel.InventoryRows.Count);
    }

    [TestMethod]
    public async Task LoadInventoryAsync_EmptyPartOrNoService_LeavesEmpty()
    {
        var viewModel = CreateViewModel(new StubInventoryService());
        await viewModel.LoadInventoryAsync("   ");
        Assert.IsTrue(viewModel.IsInventoryEmpty);

        var noService = new WaitlistViewDetailViewModel(
            new NoOpNavigationService(), new EmptySampleData(), new StubBuildingSelectionService());
        await noService.LoadInventoryAsync("P1");
        Assert.IsTrue(noService.IsInventoryEmpty);
    }

    [TestMethod]
    public async Task SortInventoryBy_SortsAndTogglesDirection()
    {
        var inventory = new StubInventoryService(
            new InventoryLocationRow { PartNumber = "P1", Location = "Rack B2", OnHandQuantity = 6m },
            new InventoryLocationRow { PartNumber = "P1", Location = "Rack A1", OnHandQuantity = 18m },
            new InventoryLocationRow { PartNumber = "P1", Location = "Rack C3", OnHandQuantity = 1m });
        var viewModel = CreateViewModel(inventory);
        await viewModel.LoadInventoryAsync("P1");

        // Default (Location) ascending.
        viewModel.SortInventoryBy("Location");
        Assert.AreEqual("Rack A1", viewModel.InventoryRows[0].Location);
        Assert.AreEqual("Rack B2", viewModel.InventoryRows[1].Location);

        // Toggle Location descending.
        viewModel.SortInventoryBy("Location");
        Assert.AreEqual("Rack C3", viewModel.InventoryRows[0].Location);
        Assert.AreEqual("Rack A1", viewModel.InventoryRows[2].Location);

        // Quantity ascending puts the 1-row first.
        viewModel.SortInventoryBy("Quantity");
        Assert.AreEqual("Rack C3", viewModel.InventoryRows[0].Location);
        Assert.AreEqual(1m, viewModel.InventoryRows[0].OnHandQuantity);

        // PartNumber ascending is stable.
        viewModel.SortInventoryBy("PartNumber");
        Assert.IsTrue(viewModel.InventoryRows.All(r => r.PartNumber == "P1"));
    }

    [TestMethod]
    public void ResolveInventoryPartNumber_PrefersPartNumberThenFallsBackToRequestedCoil()
    {
        var withPart = new SampleOrder();
        withPart.Fields.Add(new WaitlistField { Label = "Part number", Value = "MMC0001000" });
        Assert.AreEqual("MMC0001000", WaitlistViewDetailViewModel.ResolveInventoryPartNumber(withPart));

        var coil = new SampleOrder();
        coil.Fields.Add(new WaitlistField { Label = "Requested coil", Value = "COIL-204" });
        Assert.AreEqual("COIL-204", WaitlistViewDetailViewModel.ResolveInventoryPartNumber(coil));

        Assert.IsNull(WaitlistViewDetailViewModel.ResolveInventoryPartNumber(null));
        Assert.IsNull(WaitlistViewDetailViewModel.ResolveInventoryPartNumber(new SampleOrder()));
    }

    private static WaitlistViewDetailViewModel CreateViewModel(StubInventoryService inventory)
    {
        return new WaitlistViewDetailViewModel(
            new NoOpNavigationService(),
            new EmptySampleData(),
            new StubBuildingSelectionService(),
            inventoryService: inventory);
    }

    private sealed class StubInventoryService : IWaitlistInventoryService
    {
        private readonly IReadOnlyList<InventoryLocationRow> _rows;

        public StubInventoryService(params InventoryLocationRow[] rows)
        {
            _rows = rows;
        }

        public Task<IReadOnlyList<InventoryLocationRow>> GetInventoryLocationRowsAsync(string partNumber, CancellationToken cancellationToken = default)
            => Task.FromResult(_rows);
    }

    private sealed class EmptySampleData : ISampleDataService
    {
        public IReadOnlyList<object> GetSampleOrders(string? building = null) => Array.Empty<object>();
    }

    private sealed class StubBuildingSelectionService : IBuildingSelectionService
    {
        public event EventHandler? BuildingChanged
        {
            add { }
            remove { }
        }

        public IReadOnlyList<string> Buildings => new[] { "Expo Drive" };

        public string SelectedBuilding { get; set; } = "Expo Drive";
    }

    private sealed class NoOpNavigationService : INavigationService
    {
        public event Microsoft.UI.Xaml.Navigation.NavigatedEventHandler? Navigated
        {
            add { }
            remove { }
        }

        public bool CanGoBack => false;

        public Microsoft.UI.Xaml.Controls.Frame? Frame { get; set; }

        public bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false) => true;

        public bool GoBack() => true;

        public void SetListDataItemForNextConnectedAnimation(object item)
        {
        }
    }
}
