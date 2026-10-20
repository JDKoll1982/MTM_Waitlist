using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Waitlist.Services;
using MTM_Waitlist.Module_Waitlist.ViewModels;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Waitlist.Services;

[TestClass]
public sealed class PartStockServiceTests
{
    [TestMethod]
    public async Task Service_PartStockServiceTests_GetStockAsync_WhenTheWipFloorHoldsThePartAnswersFromTheWipApp()
    {
        var wip = new StubWipReader([new WipLocationQuantity("B-2", 5m), new WipLocationQuantity("A-1", 7m)]);
        var visualLocations = new FakeVisualReadFallback<VisualInventoryLocationRequest, VisualInventoryLocationRow>([]);

        var snapshot = await Create(wip, visualLocations).GetStockAsync(" FGT0002000 ");

        Assert.AreEqual(PartStockSource.WipApp, snapshot.Source);
        Assert.AreEqual("FGT0002000", snapshot.PartNumber);
        Assert.AreEqual(12m, snapshot.TotalQuantity);
        CollectionAssert.AreEqual(new[] { "A-1", "B-2" }, snapshot.Locations.Select(row => row.Location).ToArray());
        Assert.AreEqual(0, visualLocations.ReadCount, "Visual is read even though the WIP floor answered.");
    }

    [TestMethod]
    public async Task Service_PartStockServiceTests_GetStockAsync_WhenTheWipFloorHoldsNothingAnswersFromVisualWithTheWarehouseTotal()
    {
        var visualLocations = new FakeVisualReadFallback<VisualInventoryLocationRequest, VisualInventoryLocationRow>(
        [
            new VisualInventoryLocationRow { PartNumber = "FGT0002000", Location = "RACK-1", OnHandQuantity = 40m },
            new VisualInventoryLocationRow { PartNumber = "FGT0002000", Location = "HIDDEN", OnHandQuantity = 9m },
        ]);
        var quantities = new FakeVisualReadFallback<VisualPartWarehouseQuantitiesRequest, VisualPartWarehouseQuantitiesRow>(
        [
            new VisualPartWarehouseQuantitiesRow { PartNumber = "FGT0002000", WarehouseQuantity = 49m },
        ]);

        var snapshot = await Create(new StubWipReader([]), visualLocations, quantities, ["HIDDEN"]).GetStockAsync("FGT0002000");

        Assert.AreEqual(PartStockSource.Visual, snapshot.Source);
        Assert.IsFalse(snapshot.IsCachedData);
        Assert.AreEqual(49m, snapshot.TotalQuantity);
        CollectionAssert.AreEqual(new[] { "RACK-1" }, snapshot.Locations.Select(row => row.Location).ToArray());
    }

    [TestMethod]
    public async Task Service_PartStockServiceTests_GetStockAsync_WhenVisualWasServedFromTheMirrorSaysSo()
    {
        var refreshed = new DateTimeOffset(2026, 10, 20, 8, 0, 0, TimeSpan.Zero);
        var visualLocations = new CachedLocationsFallback(refreshed);

        var snapshot = await Create(new StubWipReader([]), visualLocations).GetStockAsync("FGT0002000");

        Assert.AreEqual(PartStockSource.Visual, snapshot.Source);
        Assert.IsTrue(snapshot.IsCachedData);
        Assert.AreEqual(refreshed, snapshot.CachedAsOfUtc);
    }

    [TestMethod]
    public async Task Service_PartStockServiceTests_GetStockAsync_WhenTheLookupFailsAnswersUnreadRatherThanZero()
    {
        var snapshot = await Create(new StubWipReader(new InvalidOperationException("down")), null).GetStockAsync("FGT0002000");

        Assert.IsFalse(snapshot.WasRead);
        Assert.AreEqual(0, snapshot.Locations.Count);
    }

    [TestMethod]
    public async Task Service_PartStockServiceTests_LoadAsync_FillsTheLocationsAndTotalFromTheAnswer()
    {
        var wip = new StubWipReader([new WipLocationQuantity("A-1", 1234.5m)]);
        var viewModel = new PartStockDialogViewModel(Create(wip, null));

        await viewModel.LoadAsync("FGT0002000");

        Assert.IsFalse(viewModel.IsLoading);
        Assert.IsTrue(viewModel.HasLocations);
        Assert.IsTrue(viewModel.HasAnswer);
        Assert.IsFalse(viewModel.HasEmptyText);
        Assert.AreEqual(1, viewModel.Locations.Count);
        Assert.AreEqual("A-1", viewModel.Locations[0].Location);
        StringAssert.Contains(viewModel.TotalText, "1,234.5");
    }

    [TestMethod]
    public async Task Service_PartStockServiceTests_LoadAsync_WhenTheLookupFailsShowsTheUnavailableStateWithNoTotal()
    {
        var viewModel = new PartStockDialogViewModel(Create(new StubWipReader(new InvalidOperationException("down")), null));

        await viewModel.LoadAsync("FGT0002000");

        Assert.IsTrue(viewModel.HasEmptyText);
        Assert.IsFalse(viewModel.HasAnswer);
        Assert.AreEqual(string.Empty, viewModel.TotalText);
        Assert.IsFalse(viewModel.HasLocations);
    }

    private static PartStockService Create(
        IWipPartStockReader wip,
        IVisualReadFallback<VisualInventoryLocationRequest, VisualInventoryLocationRow>? locations,
        IVisualReadFallback<VisualPartWarehouseQuantitiesRequest, VisualPartWarehouseQuantitiesRow>? quantities = null,
        IReadOnlyList<string>? ignored = null) =>
        new(
            wip,
            new StubIgnoredLocations(ignored ?? []),
            locations ?? new FakeVisualReadFallback<VisualInventoryLocationRequest, VisualInventoryLocationRow>([]),
            quantities ?? new FakeVisualReadFallback<VisualPartWarehouseQuantitiesRequest, VisualPartWarehouseQuantitiesRow>([]));

    private sealed class StubWipReader : IWipPartStockReader
    {
        private readonly IReadOnlyList<WipLocationQuantity> _rows;
        private readonly Exception? _failure;

        public StubWipReader(IReadOnlyList<WipLocationQuantity> rows) => _rows = rows;

        public StubWipReader(Exception failure)
        {
            _rows = [];
            _failure = failure;
        }

        public Task<IReadOnlyList<WipLocationQuantity>> GetLocationsAsync(string partNumber, CancellationToken cancellationToken = default) =>
            _failure is null ? Task.FromResult(_rows) : Task.FromException<IReadOnlyList<WipLocationQuantity>>(_failure);
    }

    private sealed class StubIgnoredLocations : IIgnoredLocationsService
    {
        private readonly IReadOnlyList<string> _ignored;

        public StubIgnoredLocations(IReadOnlyList<string> ignored) => _ignored = ignored;

        public Task<IReadOnlyList<string>> GetIgnoredLocationsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_ignored);

        public Task<bool> IsLocationIgnoredAsync(string location, CancellationToken cancellationToken = default) =>
            Task.FromResult(_ignored.Contains(location, StringComparer.OrdinalIgnoreCase));

        public Task<bool> SaveIgnoredLocationsAsync(IReadOnlyList<string> locations, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }

    private sealed class CachedLocationsFallback : IVisualReadFallback<VisualInventoryLocationRequest, VisualInventoryLocationRow>
    {
        private readonly DateTimeOffset _refreshed;

        public CachedLocationsFallback(DateTimeOffset refreshed) => _refreshed = refreshed;

        public Task<IReadOnlyList<VisualInventoryLocationRow>> ReadAsync(
            VisualInventoryLocationRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<VisualInventoryLocationRow>>([]);

        public Task<CachedReadResult<IReadOnlyList<VisualInventoryLocationRow>>> ReadWithProvenanceAsync(
            VisualInventoryLocationRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new CachedReadResult<IReadOnlyList<VisualInventoryLocationRow>>
            {
                Value = [],
                Source = VisualReadSource.ServedFromCache,
                RefreshedUtc = _refreshed,
            });
    }
}
