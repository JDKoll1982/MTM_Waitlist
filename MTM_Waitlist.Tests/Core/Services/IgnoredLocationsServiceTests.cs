using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Permissions;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Tests.Module_Settings;

namespace MTM_Waitlist.Tests.Core.Services;

[TestClass]
public sealed class IgnoredLocationsServiceTests
{
    [TestMethod]
    public async Task GetIgnoredLocationsAsync_WhenUnset_ReturnsDefaults()
    {
        var service = new IgnoredLocationsService(new InMemoryScopedPreferenceStore(), new AlwaysPermittingPermissionService());

        var result = await service.GetIgnoredLocationsAsync();

        CollectionAssert.AreEquivalent(new[] { "NCM", "NCM-VITS", "SHIP", "V-WC", "WC" }, result.ToArray());
    }

    [TestMethod]
    public async Task GetIgnoredLocationsAsync_ReturnsStoredAndNormalized()
    {
        var preferences = new InMemoryScopedPreferenceStore();
        preferences.SeedText(IgnoredLocationDefaults.SettingKey, PreferenceScope.Plant, "wc,SHIP ,scrap-1,wc");
        var service = new IgnoredLocationsService(preferences, new AlwaysPermittingPermissionService());

        var result = await service.GetIgnoredLocationsAsync();

        CollectionAssert.AreEquivalent(new[] { "SHIP", "SCRAP-1", "WC" }, result.ToArray());
    }

    [TestMethod]
    public async Task IsLocationIgnoredAsync_IsCaseInsensitiveAgainstDefaults()
    {
        var service = new IgnoredLocationsService(new InMemoryScopedPreferenceStore(), new AlwaysPermittingPermissionService());

        Assert.IsTrue(await service.IsLocationIgnoredAsync("wc"));
        Assert.IsTrue(await service.IsLocationIgnoredAsync("NCM"));
        Assert.IsTrue(await service.IsLocationIgnoredAsync("v-wc"));
        Assert.IsFalse(await service.IsLocationIgnoredAsync("Rack A1"));
        Assert.IsFalse(await service.IsLocationIgnoredAsync(string.Empty));
    }

    [TestMethod]
    public async Task IsLocationIgnoredAsync_HonorsStoredSet()
    {
        var preferences = new InMemoryScopedPreferenceStore();
        preferences.SeedText(IgnoredLocationDefaults.SettingKey, PreferenceScope.Plant, "SCRAP-1");
        var service = new IgnoredLocationsService(preferences, new AlwaysPermittingPermissionService());

        Assert.IsTrue(await service.IsLocationIgnoredAsync("scrap-1"));
        Assert.IsFalse(await service.IsLocationIgnoredAsync("WC"), "Defaults no longer apply when a stored set exists.");
    }

    /// <summary>
    /// Holds every permission asked of it, which is what the four read cases need: reading the list is open to
    /// everybody and the gate only matters to the write.
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
