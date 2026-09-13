using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Tests.Module_Settings;

namespace MTM_Waitlist.Tests.Core.Services;

/// <summary>
/// US5 (FR-016, FR-017, SC-007; §D4, §D5). The configured allotment is keyed by <b>Item</b> and read from the
/// <b>store</b> — not from a per-Windows-user local key — because it is shown beside an observed average that
/// comes from a stored procedure, and two numbers meant to be compared cannot have one of them keyed to a
/// profile. An Item with no configured minutes uses the <b>15-minute default, labelled as a default</b>, and
/// still takes its place in the urgency order.
/// </summary>
[TestClass]
public sealed class UrgencySettingsServiceTests
{
    private const string Item = "pickup-coil";

    // ── The source of truth is the Item's stored configuration, not a local key ─────────────────────────

    [TestMethod]
    public async Task GetMaxAllotted_ReadsTheItemsStoredConfiguration()
    {
        var store = new FakeRequestItemAllottedMinutesStore();
        store.Configure(Item, 45);
        var service = new UrgencySettingsService(store);

        Assert.AreEqual(TimeSpan.FromMinutes(45), await service.GetMaxAllottedAsync(Item));

        CollectionAssert.Contains((System.Collections.ICollection)store.ItemsRead, Item);
    }

    [TestMethod]
    public void TheService_NoLongerTakesThePerWindowsUserLocalSettingsStore()
    {
        Assert.IsNull(
            typeof(UrgencySettingsService).GetConstructor([typeof(ILocalSettingsService)]),
            "The allotment is no longer written to Urgency.MaxAllottedMinutes.<subtype> in %LOCALAPPDATA% (§D4).");
    }

    [TestMethod]
    public async Task GetAllottedMinutes_ReadsThroughTheExistingConfigurationRead()
    {
        var helper = new FakeMySqlHelperServer();
        helper.EnqueueQueryResult(new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["item"] = Item,
            ["category"] = nameof(RequestCategory.Pickup),
            ["control_flow"] = RequestItemConfiguration.DirectToConfirmation,
            ["requires_answer"] = 0,
            ["min_length"] = 0,
            ["max_length"] = 200,
            ["allotted_minutes"] = 45,
        });

        var store = new RequestItemAllottedMinutesStore(
            new RequestItemConfigurationService(helper, new RequestItemCatalogService()),
            helper);

        Assert.AreEqual(45, await store.GetAllottedMinutesAsync(Item));
        Assert.AreEqual(
            "sp_waitlist_request_item_configs_get",
            helper.ExecutedQueries[0].Sql,
            "The configured value is read through the configuration read, not through a second query path.");
    }

    // ── The write is the one writer of a configured allotment ───────────────────────────────────────────

    [TestMethod]
    public async Task SetMaxAllotted_WritesThroughTheAllottedMinutesProcedure()
    {
        var helper = new FakeMySqlHelperServer();
        helper.EnqueueNonQueryResult(1);

        var store = new RequestItemAllottedMinutesStore(
            new RequestItemConfigurationService(helper, new RequestItemCatalogService()),
            helper);

        await store.SetAllottedMinutesAsync(Item, 45);

        Assert.AreEqual(1, helper.ExecutedNonQueries.Count);
        Assert.AreEqual("sp_waitlist_request_item_allotted_minutes_update", helper.ExecutedNonQueries[0].Sql);
        Assert.AreEqual(Item, helper.ExecutedNonQueries[0].Parameters["p_item"]);
        Assert.AreEqual(45, helper.ExecutedNonQueries[0].Parameters["p_allotted_minutes"]);
    }

    [TestMethod]
    public async Task SetMaxAllotted_IsClampedToThePositiveMinimumAndTheTwentyFourHourMaximum()
    {
        var store = new FakeRequestItemAllottedMinutesStore();
        var service = new UrgencySettingsService(store);

        await service.SetMaxAllottedAsync(Item, 0);
        await service.SetMaxAllottedAsync(Item, 5000);

        Assert.AreEqual(2, store.Writes.Count);
        Assert.AreEqual(1, store.Writes[0].Minutes);
        Assert.AreEqual(24 * 60, store.Writes[1].Minutes);
        Assert.AreEqual(Item, store.Writes[0].Item);
    }

    [TestMethod]
    public async Task SetMaxAllotted_WhenTheWriteFails_ReportsItRatherThanSwallowingIt()
    {
        var store = new FakeRequestItemAllottedMinutesStore
        {
            WriteFailure = new InvalidOperationException("store down")
        };
        var service = new UrgencySettingsService(store);

        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => service.SetMaxAllottedAsync(Item, 45));
    }

    // ── FR-017 / SC-007: the labelled 15-minute default, and the Item keeps its place in the order ──────

    [TestMethod]
    public async Task GetMaxAllotted_ForAnItemWithNoConfiguredMinutes_UsesTheFifteenMinuteDefault()
    {
        var service = new UrgencySettingsService(new FakeRequestItemAllottedMinutesStore());

        Assert.AreEqual(TimeSpan.FromMinutes(15), await service.GetMaxAllottedAsync(Item));
        Assert.AreEqual(15, UrgencySettingsService.DefaultMinutes);
        Assert.AreEqual(TimeSpan.FromMinutes(15), service.DefaultMaxAllotted);
    }

    [TestMethod]
    public async Task GetMaxAllotted_WhenTheStoreCannotBeReached_StillAnswersTheLabelledDefault()
    {
        var store = new FakeRequestItemAllottedMinutesStore
        {
            ReadFailure = new InvalidOperationException("store down")
        };
        var service = new UrgencySettingsService(store);

        Assert.AreEqual(
            TimeSpan.FromMinutes(UrgencySettingsService.DefaultMinutes),
            await service.GetMaxAllottedAsync(Item),
            "An unreachable store produces a sane deadline rather than no deadline (§D5).");
    }

    [TestMethod]
    public async Task AnItemWithNoConfiguredMinutes_KeepsItsPlaceInTheUrgencyOrder()
    {
        var service = new UrgencySettingsService(new FakeRequestItemAllottedMinutesStore());
        var created = new DateTimeOffset(2026, 9, 13, 8, 0, 0, TimeSpan.Zero);

        var maxAllotted = await service.GetMaxAllottedAsync(Item);
        var state = UrgencyCalculator.Compute(created, maxAllotted, created.AddMinutes(5));

        Assert.AreEqual(created.AddMinutes(15), state.DueUtc, "The Item still has a deadline, so it is still ordered.");
        Assert.IsFalse(state.IsOverdue);

        // And a request older than the default is overdue rather than excluded.
        var overdue = UrgencyCalculator.Compute(created, maxAllotted, created.AddMinutes(20));
        Assert.IsTrue(overdue.IsOverdue);
    }

    [TestMethod]
    public async Task AnItemWithNoConfiguredMinutes_IsLabelledAsADefaultOnThePairTheScreenShows()
    {
        var configurations = new FakeRequestItemConfigurationService();
        configurations.Configure(Item, null);

        var service = new RequestItemObservedTimeService(
            new FakeMySqlHelperServer(),
            new RequestItemCatalogService(),
            configurations);

        var pairs = await service.GetObservedTimesAsync();
        var pair = pairs.Single(candidate => candidate.Item == Item);

        Assert.AreEqual(TimeSpan.FromMinutes(15), pair.ConfiguredMinutes);
        Assert.IsTrue(pair.IsConfiguredValueDefault, "A value nobody configured is labelled as a default (FR-017).");
    }
}
