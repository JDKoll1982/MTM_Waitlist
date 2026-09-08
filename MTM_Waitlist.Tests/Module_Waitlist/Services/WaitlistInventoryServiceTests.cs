using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Waitlist.Services;

namespace MTM_Waitlist.Tests.Module_Waitlist.Services;

[TestClass]
public sealed class WaitlistInventoryServiceTests
{
    [TestMethod]
    public async Task GetInventoryLocationRowsAsync_MockOn_ReturnsFilteredNonIgnoredRows()
    {
        var service = CreateService(mockOn: true);

        var rows = await service.GetInventoryLocationRowsAsync("MMC0001000");

        var locations = rows.Select(r => r.Location).ToArray();
        CollectionAssert.AreEquivalent(new[] { "V-A0-01", "V-A0-04", "V-B2-10" }, locations);
        Assert.IsTrue(rows.All(r => r.OnHandQuantity >= 1m));
    }

    [TestMethod]
    public async Task GetInventoryLocationRowsAsync_MockOn_WhenNoIgnoredConfigured_IncludesAllOnHandRows()
    {
        var settings = new InMemorySettings(mockOn: true);
        // Only SHIP is ignored (defaults are not applied once a non-empty set is stored),
        // so WC/NCM rows with qty >= 1 must remain.
        settings.SaveSettingAsync(IgnoredLocationDefaults.SettingKey, new List<string> { "SHIP" }).GetAwaiter().GetResult();
        var service = CreateService(settings);

        var rows = await service.GetInventoryLocationRowsAsync("MMC0001000");

        Assert.IsFalse(rows.Any(r => r.OnHandQuantity < 1m), "qty-0 rows must be filtered.");
        Assert.IsTrue(rows.Any(r => r.Location == "WC"), "WC is not ignored here, so it should remain.");
        Assert.IsFalse(rows.Any(r => r.Location == "SHIP"), "SHIP is ignored, so it must be omitted.");
    }

    [TestMethod]
    public async Task GetInventoryLocationRowsAsync_EmptyPartNumber_ReturnsEmpty()
    {
        var service = CreateService(mockOn: true);

        var rows = await service.GetInventoryLocationRowsAsync("   ");

        Assert.AreEqual(0, rows.Count);
    }

    [TestMethod]
    public async Task GetInventoryLocationRowsAsync_MockOff_NoConnection_ReturnsEmptyWithoutThrowing()
    {
        // With mock OFF the service routes to the shared Infor Visual executor. No connection
        // string is configured in the test environment (and the script is not on the test output
        // path), so the executor returns an empty set — zero rows handled, no throw.
        var service = CreateService(mockOn: false);

        var rows = await service.GetInventoryLocationRowsAsync("MMC0001000");

        Assert.IsNotNull(rows);
        Assert.AreEqual(0, rows.Count);
    }

    [TestMethod]
    public void MapToInventoryLocationRow_MapsPartLocationAndQuantity()
    {
        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["PartNumber"] = "MMC0001000",
            ["Location"] = "  V-A0-01  ",
            ["OnHandQuantity"] = 46000m,
        };

        var mapped = WaitlistInventoryService.MapToInventoryLocationRow(row);

        Assert.AreEqual("MMC0001000", mapped.PartNumber);
        Assert.AreEqual("V-A0-01", mapped.Location);
        Assert.AreEqual(46000m, mapped.OnHandQuantity);
    }

    [TestMethod]
    public void MapToInventoryLocationRow_HandlesMissingOrNullColumns()
    {
        var mapped = WaitlistInventoryService.MapToInventoryLocationRow(new Dictionary<string, object?>());

        Assert.AreEqual(string.Empty, mapped.PartNumber);
        Assert.AreEqual(string.Empty, mapped.Location);
        Assert.AreEqual(0m, mapped.OnHandQuantity);
    }

    private static WaitlistInventoryService CreateService(bool mockOn)
        => CreateService(new InMemorySettings(mockOn));

    private static WaitlistInventoryService CreateService(InMemorySettings settings)
    {
        var sqlHelper = new SqlHelperServer(settings, new EmptySampleDataService());
        return new WaitlistInventoryService(
            sqlHelper,
            new IgnoredLocationsService(settings),
            CreateExecutor());
    }

    private static InforVisualSqlQueryService CreateExecutor()
        => new InforVisualSqlQueryService(new ConfigurationBuilder().Build());

    private sealed class InMemorySettings : ILocalSettingsService
    {
        private readonly Dictionary<string, object> _values = new(StringComparer.Ordinal)
        {
            ["Feature.InforVisualMockData"] = false,
        };

        public InMemorySettings(bool mockOn)
        {
            _values["Feature.InforVisualMockData"] = mockOn;
        }

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

    private sealed class EmptySampleDataService : ISampleDataService
    {
        public IReadOnlyList<object> GetSampleOrders(string? building = null) => Array.Empty<object>();
    }
}
