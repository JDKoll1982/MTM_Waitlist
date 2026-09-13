using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Tests.Module_Settings;

namespace MTM_Waitlist.Tests.Core.Services;

/// <summary>
/// US5 (FR-016, FR-017, SC-007). A request's deadline is derived from its <b>Item's</b> allotted minutes — never
/// from a request type or a subtype — and an Item with no configured allotment is measured by the labelled
/// 15-minute default rather than dropped from the order.
/// </summary>
[TestClass]
public sealed class UrgencyDeadlineServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 6, 12, 0, 0, TimeSpan.Zero);

    private static UrgencyDeadlineService Build(FakeRequestItemAllottedMinutesStore store)
        => new(new UrgencySettingsService(store));

    [TestMethod]
    public async Task Compute_DueIsCreatedPlusTheItemsStoredAllotment()
    {
        var store = new FakeRequestItemAllottedMinutesStore();
        store.Configure("pickup-coil", 60);
        var service = Build(store);

        var created = Now.AddMinutes(-70);
        var state = await service.ComputeAsync(created, "pickup-coil", Now);

        Assert.AreEqual(created + TimeSpan.FromMinutes(60), state.DueUtc);
        Assert.IsTrue(state.IsOverdue);
    }

    [TestMethod]
    public async Task Compute_ForAnItemWithNoConfiguredAllotment_UsesTheLabelledDefault()
    {
        var service = Build(new FakeRequestItemAllottedMinutesStore());

        var created = Now.AddMinutes(-10);
        var state = await service.ComputeAsync(created, "other", Now);

        Assert.AreEqual(created + TimeSpan.FromMinutes(UrgencySettingsService.DefaultMinutes), state.DueUtc);
        Assert.AreEqual(TimeSpan.FromMinutes(5), state.Remaining);
        Assert.IsFalse(state.IsOverdue);
    }

    [TestMethod]
    public async Task GetMaxAllotted_ReturnsTheItemsStoredAllotmentOrDefault()
    {
        var store = new FakeRequestItemAllottedMinutesStore();
        store.Configure("pickup-coil", 45);
        var service = Build(store);

        Assert.AreEqual(TimeSpan.FromMinutes(45), await service.GetMaxAllottedAsync("pickup-coil"));
        Assert.AreEqual(
            TimeSpan.FromMinutes(UrgencySettingsService.DefaultMinutes),
            await service.GetMaxAllottedAsync("deliver-coil"));
        Assert.AreEqual(
            TimeSpan.FromMinutes(UrgencySettingsService.DefaultMinutes),
            await service.GetMaxAllottedAsync(null));
    }
}
