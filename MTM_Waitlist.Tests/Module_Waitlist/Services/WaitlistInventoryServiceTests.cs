using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Waitlist.Services;
using MTM_Waitlist.Mock.Models;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Waitlist.Services;

/// <summary>
/// Verifies the Waitlist inventory-location read: it is served through the shape-4 fallback, and the
/// caller-side filtering (on-hand &gt;= 1 plus the ignored-location set) still applies to whatever
/// source answered.
/// </summary>
[TestClass]
public sealed class WaitlistInventoryServiceTests
{
    [TestMethod]
    public async Task GetInventoryLocationRowsAsync_FiltersQtyZeroAndIgnoredLocations()
    {
        var settings = new InMemorySettings();
        settings.SaveSettingAsync(IgnoredLocationDefaults.SettingKey, new List<string> { "SHIP" }).GetAwaiter().GetResult();
        var service = CreateService(settings);

        var rows = await service.GetInventoryLocationRowsAsync("MMC0001000");

        var locations = rows.Select(r => r.Location).ToArray();
        CollectionAssert.AreEquivalent(new[] { "V-A0-01", "WC" }, locations);
        Assert.IsTrue(rows.All(r => r.OnHandQuantity >= 1m), "qty-0 rows must be filtered.");
    }

    [TestMethod]
    public async Task GetInventoryLocationRowsAsync_EmptyPartNumber_ReturnsEmptyWithoutReading()
    {
        var fallback = new FakeVisualReadFallback<VisualInventoryLocationRequest, VisualInventoryLocationRow>(
            Array.Empty<VisualInventoryLocationRow>());
        var service = CreateService(new InMemorySettings(), fallback);

        var rows = await service.GetInventoryLocationRowsAsync("   ");

        Assert.AreEqual(0, rows.Count);
        Assert.AreEqual(0, fallback.ReadCount, "A blank part number must not reach the fallback.");
    }

    [TestMethod]
    public async Task GetInventoryLocationRowsAsync_WhenFallbackFails_ReturnsEmptyWithoutThrowing()
    {
        // A failed Visual read surfaces from the fallback; the service must degrade rather than crash.
        var fallback = new FakeVisualReadFallback<VisualInventoryLocationRequest, VisualInventoryLocationRow>(
            _ => throw new VisualReadFailedException("inventory_locations", "Unreachable with no cache configured."));
        var service = CreateService(new InMemorySettings(), fallback);

        var rows = await service.GetInventoryLocationRowsAsync("MMC0001000");

        Assert.IsNotNull(rows);
        Assert.AreEqual(0, rows.Count);
    }

    [TestMethod]
    public void MapToInventoryLocationRow_MapsPartLocationAndQuantity()
    {
        var mapped = WaitlistInventoryService.MapToInventoryLocationRow(new VisualInventoryLocationRow
        {
            PartNumber = "MMC0001000",
            Location = "V-A0-01",
            OnHandQuantity = 46000m,
        });

        Assert.AreEqual("MMC0001000", mapped.PartNumber);
        Assert.AreEqual("V-A0-01", mapped.Location);
        Assert.AreEqual(46000m, mapped.OnHandQuantity);
    }

    [TestMethod]
    public void MapToInventoryLocationRow_HandlesEmptyRow()
    {
        var mapped = WaitlistInventoryService.MapToInventoryLocationRow(new VisualInventoryLocationRow());

        Assert.AreEqual(string.Empty, mapped.PartNumber);
        Assert.AreEqual(string.Empty, mapped.Location);
        Assert.AreEqual(0m, mapped.OnHandQuantity);
    }

    private static WaitlistInventoryService CreateService(InMemorySettings settings)
        => CreateService(settings, CreateRowSet());

    private static WaitlistInventoryService CreateService(
        InMemorySettings settings,
        IReadOnlyList<VisualInventoryLocationRow> rows)
        => CreateService(
            settings,
            new FakeVisualReadFallback<VisualInventoryLocationRequest, VisualInventoryLocationRow>(rows));

    private static WaitlistInventoryService CreateService(
        InMemorySettings settings,
        FakeVisualReadFallback<VisualInventoryLocationRequest, VisualInventoryLocationRow> fallback)
        => new(new IgnoredLocationsService(settings), fallback);

    /// <summary>
    /// A row set exercising both filter rules: an ignored location (SHIP) and a zero-quantity location
    /// (V-B2-10) that must both be omitted.
    /// </summary>
    private static IReadOnlyList<VisualInventoryLocationRow> CreateRowSet() =>
    [
        new VisualInventoryLocationRow { PartNumber = "MMC0001000", Location = "V-A0-01", OnHandQuantity = 10m },
        new VisualInventoryLocationRow { PartNumber = "MMC0001000", Location = "WC", OnHandQuantity = 5m },
        new VisualInventoryLocationRow { PartNumber = "MMC0001000", Location = "SHIP", OnHandQuantity = 3m },
        new VisualInventoryLocationRow { PartNumber = "MMC0001000", Location = "V-B2-10", OnHandQuantity = 0m },
    ];

    private sealed class InMemorySettings : ILocalSettingsService
    {
        private readonly Dictionary<string, object> _values = new(StringComparer.Ordinal);

        public Task<T?> ReadSettingAsync<T>(string key)
        {
            if (_values.TryGetValue(key, out var value) && value is T typed)
            {
                return Task.FromResult<T?>(typed);
            }

            return Task.FromResult(default(T));
        }

        public Task SaveSettingAsync<T>(string key, T value)
        {
            _values[key] = value!;
            return Task.CompletedTask;
        }

        public Task ResetSettingAsync(string key, CancellationToken cancellationToken = default)
        {
            _values.Remove(key);
            return Task.CompletedTask;
        }

        public Task ResetAsync()
        {
            _values.Clear();
            return Task.CompletedTask;
        }

        public Task CorruptForTestAsync() => Task.CompletedTask;
    }
}
