using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Module_Core.Services;

/// <summary>
/// US3 (FR-010, FR-011, SC-009). The viewer's sort choice is remembered per person over the existing local
/// settings, and most urgent is what it starts as. A service re-construction stands in for the restart SC-009
/// names, because the store is the only thing that survives between two runs.
/// </summary>
[TestClass]
public sealed class WaitlistSortPreferenceServiceTests
{
    [TestMethod]
    public async Task GetSortOrder_FirstRun_IsMostUrgent()
    {
        var service = new WaitlistSortPreferenceService(new InMemoryLocalSettings());

        Assert.AreEqual(
            WaitlistSortOrder.MostUrgent,
            await service.GetSortOrderAsync().ConfigureAwait(false),
            "A viewer who has never chosen anything gets the most-urgent order the list ships with (FR-010).");
    }

    [DataTestMethod]
    [DataRow(WaitlistSortOrder.MostUrgent)]
    [DataRow(WaitlistSortOrder.LongestWaiting)]
    [DataRow(WaitlistSortOrder.Press)]
    [DataRow(WaitlistSortOrder.RequestedBy)]
    [DataRow(WaitlistSortOrder.Status)]
    public async Task SetSortOrder_EachOfTheFiveKeys_SurvivesAServiceReconstruction(string sortOrder)
    {
        var settings = new InMemoryLocalSettings();
        await new WaitlistSortPreferenceService(settings).SetSortOrderAsync(sortOrder).ConfigureAwait(false);

        // A second service over the same store is what a restart looks like from here (SC-009).
        var afterRestart = await new WaitlistSortPreferenceService(settings).GetSortOrderAsync().ConfigureAwait(false);

        Assert.AreEqual(sortOrder, afterRestart, $"'{sortOrder}' must come back after a restart.");
    }

    [TestMethod]
    public async Task SetSortOrder_StoresOneScalarKey_RatherThanASecondStore()
    {
        var settings = new InMemoryLocalSettings();
        var service = new WaitlistSortPreferenceService(settings);

        await service.SetSortOrderAsync(WaitlistSortOrder.Press).ConfigureAwait(false);

        Assert.IsTrue(
            settings.Has(WaitlistSortPreferenceService.SettingsKey),
            "The choice is one scalar key through the existing local settings, not a store of its own (§D7).");
    }

    [TestMethod]
    public async Task GetSortOrder_UnknownStoredValue_ReportsTheDefault()
    {
        var settings = new InMemoryLocalSettings();
        await settings.SaveSettingAsync(WaitlistSortPreferenceService.SettingsKey, "by-vibes").ConfigureAwait(false);

        Assert.AreEqual(
            WaitlistSortOrder.MostUrgent,
            await new WaitlistSortPreferenceService(settings).GetSortOrderAsync().ConfigureAwait(false),
            "A preference that outlives its key must not leave the list unordered (FR-010).");
    }

    [TestMethod]
    public async Task SetSortOrder_UnknownKey_RemembersTheDefault()
    {
        var settings = new InMemoryLocalSettings();
        var service = new WaitlistSortPreferenceService(settings);

        await service.SetSortOrderAsync("by-vibes").ConfigureAwait(false);

        Assert.AreEqual(
            WaitlistSortOrder.MostUrgent,
            await new WaitlistSortPreferenceService(settings).GetSortOrderAsync().ConfigureAwait(false),
            "Nothing may be stored that no order key names.");
    }

    /// <summary>The smallest thing that behaves like the settings file, so the test never touches the profile.</summary>
    private sealed class InMemoryLocalSettings : ILocalSettingsService
    {
        private readonly Dictionary<string, object?> _store = new(StringComparer.Ordinal);

        public bool Has(string key) => _store.ContainsKey(key);

        public Task<T?> ReadSettingAsync<T>(string key)
            => Task.FromResult(_store.TryGetValue(key, out var raw) && raw is T typed ? typed : default);

        public Task SaveSettingAsync<T>(string key, T value)
        {
            _store[key] = value;
            return Task.CompletedTask;
        }

        public Task ResetSettingAsync(string key, CancellationToken cancellationToken = default)
        {
            _store.Remove(key);
            return Task.CompletedTask;
        }

        public Task ResetAsync() => Task.CompletedTask;

        public Task CorruptForTestAsync() => Task.CompletedTask;
    }
}
