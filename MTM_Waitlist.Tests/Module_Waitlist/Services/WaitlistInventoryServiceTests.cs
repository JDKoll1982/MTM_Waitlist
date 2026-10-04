using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Waitlist.Services;
using MTM_Waitlist.Mock.Models;
using MTM_Waitlist.Tests.Module_Mock;
using MTM_Waitlist.Tests.Module_Settings;

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
        var preferences = new InMemoryScopedPreferenceStore();
        preferences.SeedText(IgnoredLocationDefaults.SettingKey, PreferenceScope.Plant, "SHIP");
        var service = CreateService(preferences);

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
        var service = CreateService(new InMemoryScopedPreferenceStore(), fallback);

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
        var service = CreateService(new InMemoryScopedPreferenceStore(), fallback);

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

    private static WaitlistInventoryService CreateService(InMemoryScopedPreferenceStore preferences)
        => CreateService(preferences, CreateRowSet());

    private static WaitlistInventoryService CreateService(
        InMemoryScopedPreferenceStore preferences,
        IReadOnlyList<VisualInventoryLocationRow> rows)
        => CreateService(
            preferences,
            new FakeVisualReadFallback<VisualInventoryLocationRequest, VisualInventoryLocationRow>(rows));

    private static WaitlistInventoryService CreateService(
        InMemoryScopedPreferenceStore preferences,
        FakeVisualReadFallback<VisualInventoryLocationRequest, VisualInventoryLocationRow> fallback)
        => new(new IgnoredLocationsService(preferences, new AlwaysPermittingPermissionService()), fallback);

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

    /// <summary>
    /// Holds every permission asked of it, which is what these read cases need: reading the ignored-locations list
    /// is open to everybody and only the write is gated (FR-024).
    /// </summary>
    private sealed class AlwaysPermittingPermissionService : IPermissionService
    {
        public Task<bool> HasPermissionAsync(string permissionKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<IReadOnlyDictionary<string, bool>> HasPermissionsAsync(
            IEnumerable<string> permissionKeys,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<string, bool>>(
                permissionKeys.ToDictionary(key => key, _ => true, StringComparer.Ordinal));

        public void Invalidate()
        {
        }
    }
}
