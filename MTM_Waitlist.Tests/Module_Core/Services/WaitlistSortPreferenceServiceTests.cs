using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Tests.Module_Settings;

namespace MTM_Waitlist.Tests.Module_Core.Services;

/// <summary>
/// US3 (FR-010, FR-011, SC-009) and US6 (FR-023, SC-008). The viewer's sort choice is held against the person in
/// the store, and most urgent is what it starts as. A service re-construction stands in for the restart SC-009
/// names, because the store is the only thing that survives between two runs.
/// </summary>
[TestClass]
public sealed class WaitlistSortPreferenceServiceTests
{
    [TestMethod]
    public async Task GetSortOrder_FirstRun_IsMostUrgent()
    {
        var service = new WaitlistSortPreferenceService(new InMemoryScopedPreferenceStore());

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
        var preferences = new InMemoryScopedPreferenceStore();
        await new WaitlistSortPreferenceService(preferences).SetSortOrderAsync(sortOrder).ConfigureAwait(false);

        // A second service over the same store is what a restart looks like from here (SC-009).
        var afterRestart = await new WaitlistSortPreferenceService(preferences).GetSortOrderAsync().ConfigureAwait(false);

        Assert.AreEqual(sortOrder, afterRestart, $"'{sortOrder}' must come back after a restart.");
    }

    [TestMethod]
    public async Task SetSortOrder_StoresOneScalarKey_RatherThanASecondStore()
    {
        var preferences = new InMemoryScopedPreferenceStore();
        var service = new WaitlistSortPreferenceService(preferences);

        await service.SetSortOrderAsync(WaitlistSortOrder.Press).ConfigureAwait(false);

        Assert.IsNotNull(
            preferences.TextFor(WaitlistSortPreferenceService.SettingsKey, PreferenceScope.Person),
            "The choice is one stored key held against the person, not a store of its own.");
        Assert.AreEqual(
            0,
            preferences.Writes.Count(write => write.Scope is PreferenceScope.Plant),
            "Nothing about one viewer's order may be written at plant scope, where it would decide for everybody.");
    }

    [TestMethod]
    public async Task GetSortOrder_UnknownStoredValue_ReportsTheDefault()
    {
        var preferences = new InMemoryScopedPreferenceStore();
        preferences.SeedText(WaitlistSortPreferenceService.SettingsKey, PreferenceScope.Person, "by-vibes");

        Assert.AreEqual(
            WaitlistSortOrder.MostUrgent,
            await new WaitlistSortPreferenceService(preferences).GetSortOrderAsync().ConfigureAwait(false),
            "A preference that outlives its key must not leave the list unordered (FR-010).");
    }

    [TestMethod]
    public async Task SetSortOrder_UnknownKey_RemembersTheDefault()
    {
        var preferences = new InMemoryScopedPreferenceStore();
        var service = new WaitlistSortPreferenceService(preferences);

        await service.SetSortOrderAsync("by-vibes").ConfigureAwait(false);

        Assert.AreEqual(
            WaitlistSortOrder.MostUrgent,
            await new WaitlistSortPreferenceService(preferences).GetSortOrderAsync().ConfigureAwait(false),
            "Nothing may be stored that no order key names.");
    }
}
