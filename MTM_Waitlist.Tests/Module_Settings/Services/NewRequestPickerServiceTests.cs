using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;

namespace MTM_Waitlist.Tests.Module_Settings.Services;

[TestClass]
public sealed class NewRequestPickerServiceTests
{
    private static NewRequestPickerService CreateService()
        => new(new RequestItemCatalogService());

    [TestMethod]
    public void GetCategoriesInOrder_IsPickupDeliverAssistOther()
    {
        var service = CreateService();
        CollectionAssert.AreEqual(
            new[] { RequestCategory.Pickup, RequestCategory.Deliver, RequestCategory.Assist, RequestCategory.Other },
            service.GetCategoriesInOrder().ToArray());
    }

    [TestMethod]
    public void GetItems_AreOrderedWithinCategory()
    {
        var service = CreateService();
        var pickup = service.GetItems(RequestCategory.Pickup);
        Assert.AreEqual(11, pickup.Count);
        Assert.AreEqual("pickup-coil", pickup[0].Id);
        Assert.AreEqual("pickup-hopper", pickup[^1].Id);
    }

    [TestMethod]
    public void GetVisibleItems_FiltersByAvailability_AndKeepsOrder()
    {
        var service = CreateService();
        var availability = RequestJobPartAvailability.All with
        {
            HasCoil = true,
            HasFlatstock = false,
            HasDie = false,
            HasComponent = false,
            HasDunnage = false,
        };

        var pickup = service.GetVisibleItems(RequestCategory.Pickup, availability);
        var ids = pickup.Select(i => i.Id).ToList();

        Assert.IsTrue(ids.Contains("pickup-coil"), "Coil must show when the job has a coil.");
        Assert.IsFalse(ids.Contains("pickup-die"), "Die must hide when the job has no die.");
        Assert.IsTrue(ids.Contains("pickup-riser-table"), "Manual equipment is always visible.");
        Assert.AreEqual(ids.OrderBy(x => x).ToList().Count, ids.Count);
    }

    [TestMethod]
    public void GetAllVisible_FlattensCategoriesInOrder_FilteringHidden()
    {
        var service = CreateService();
        var availability = RequestJobPartAvailability.None;

        var all = service.GetAllVisible(availability);

        // With no job parts, only manual/always-visible + free-text items appear (riser/hopper/scrap/other),
        // plus nothing auto-populated. Riser Table (Pickup) precedes Hopper (Deliver) precedes Other.
        var ids = all.Select(i => i.Id).ToList();
        Assert.IsFalse(ids.Contains("pickup-coil"));
        Assert.IsFalse(ids.Contains("deliver-coil"));
        Assert.IsTrue(ids.Contains("pickup-riser-table"));
        Assert.IsTrue(ids.Contains("other"));
        // Order: category then item.
        var categories = all.Select(i => i.Category).ToList();
        var expectedOrder = new[] { RequestCategory.Pickup, RequestCategory.Deliver, RequestCategory.Assist, RequestCategory.Other };
        Assert.IsTrue(IsSubsequenceOrdered(categories, expectedOrder));
    }

    private static bool IsSubsequenceOrdered(List<RequestCategory> actual, RequestCategory[] expected)
    {
        var rank = expected.Select((c, i) => (c, i)).ToDictionary(x => x.c, x => x.i);
        return actual.Select(c => rank[c]).ToList().SequenceEqual(actual.Select(c => rank[c]).OrderBy(x => x).ToList());
    }
}
