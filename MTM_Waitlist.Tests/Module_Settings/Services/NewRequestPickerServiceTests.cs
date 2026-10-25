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
    public void Service_NewRequestPickerServiceTests_GetCategoriesInOrder_IsPickupDeliverAssistOther()
    {
        var service = CreateService();
        CollectionAssert.AreEqual(
            new[] { RequestCategory.Pickup, RequestCategory.Deliver, RequestCategory.Assist, RequestCategory.Other },
            service.GetCategoriesInOrder().ToArray());
    }

    [TestMethod]
    public void Service_NewRequestPickerServiceTests_GetItems_AreOrderedWithinCategory()
    {
        var service = CreateService();
        var pickup = service.GetItems(RequestCategory.Pickup);
        Assert.AreEqual(11, pickup.Count);
        Assert.AreEqual("pickup-coil", pickup[0].Id);
        Assert.AreEqual("pickup-hopper", pickup[^1].Id);
    }

    [TestMethod]
    public void Service_NewRequestPickerServiceTests_GetVisibleItems_FiltersByAvailabilityAndKeepsOrder()
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
    public void Service_NewRequestPickerServiceTests_GetAllVisible_FlattensCategoriesInOrderFilteringHidden()
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

    /// <summary>
    /// The five job-independent Items — the only ones offered when the job has nothing — and none of them
    /// belongs to Assist, which is why an unfiltered Assist Category would open an empty Item step (D20).
    /// </summary>
    private static readonly string[] JobIndependentItemIds =
    [
        "pickup-riser-table", "deliver-riser-table", "pickup-hopper", "deliver-hopper", "other"
    ];

    [TestMethod]
    public void Service_NewRequestPickerServiceTests_GetVisibleItems_IsNeverEmptyForAJobWithNoParts()
    {
        var service = CreateService();
        var noParts = RequestJobPartAvailability.All with
        {
            HasCoil = false,
            HasFlatstock = false,
            HasDie = false,
            HasComponent = false,
            HasDunnage = false,
            HasScrapDecision = false,
        };

        foreach (var category in service.GetCategoriesInOrder())
        {
            var visible = service.GetVisibleItems(category, noParts);
            if (category == RequestCategory.Assist)
            {
                Assert.AreEqual(0, visible.Count, "Assist has no job-independent Item.");
                continue;
            }

            Assert.IsTrue(
                visible.Count > 0,
                $"Every reachable Item step must offer at least one Item; '{category}' offered none.");
        }
    }

    [TestMethod]
    public void Service_NewRequestPickerServiceTests_GetVisibleItems_IsNeverEmptyForAWorkCentreWithNoActiveJob()
    {
        var service = CreateService();

        foreach (var item in JobIndependentItemIds)
        {
            Assert.IsTrue(
                service.GetVisibleItems(RequestItemCatalog.FindById(item)!.Category, RequestJobPartAvailability.None)
                    .Any(i => i.Id == item),
                $"'{item}' is job-independent and must still be offered with no active job.");
        }
    }

    [TestMethod]
    public void Service_NewRequestPickerServiceTests_GetVisibleCategories_DoesNotOfferAssistOnAJobWithNoParts()
    {
        var service = CreateService();
        var noParts = RequestJobPartAvailability.All with
        {
            HasCoil = false,
            HasFlatstock = false,
            HasDie = false,
            HasComponent = false,
            HasDunnage = false,
            HasScrapDecision = false,
        };

        var visible = service.GetVisibleCategories(noParts).ToList();

        CollectionAssert.DoesNotContain(visible, RequestCategory.Assist);
        CollectionAssert.DoesNotContain(service.GetVisibleCategories(RequestJobPartAvailability.None).ToList(), RequestCategory.Assist);

        // Every offered Category would open a non-empty Item step, so the Item step is never empty.
        foreach (var category in visible)
        {
            Assert.IsTrue(service.GetVisibleItems(category, noParts).Count > 0, category.ToString());
        }
    }

    [TestMethod]
    public void Service_NewRequestPickerServiceTests_GetVisibleCategories_OffersAssistWhenTheJobHasAPart()
    {
        var service = CreateService();

        var visible = service.GetVisibleCategories(RequestJobPartAvailability.All).ToList();

        CollectionAssert.Contains(visible, RequestCategory.Assist);
    }

    [TestMethod]
    public void Service_NewRequestPickerServiceTests_GetVisibleItems_AbsentItemsAreNotInTheBuiltCollectionNotMerelyHidden()
    {
        var service = CreateService();
        var onlyFlatstock = RequestJobPartAvailability.All with
        {
            HasCoil = false,
            HasDie = false,
            HasComponent = false,
            HasDunnage = false,
            HasScrapDecision = false,
            // The NCM and outside-service pickups are offered on the signal each one names (FR-094), and this job
            // has neither. The three product pickups answer one question, so the generic one is the sole product
            // choice while nothing was derived.
            HasFinishedGoodsToReturn = false,
            HasNonconformingStockToReturn = false,
            HasWorkInProgressToReturn = false,
            HasOutsideServiceWorkToReturn = false,
        };

        var built = service.GetVisibleItems(RequestCategory.Pickup, onlyFlatstock).Select(i => i.Id).ToList();

        // Filtering happens inside the build: an unsupported Item is never constructed, never bound and never
        // offered, so it cannot be "offered and then refused" (FR-002).
        Assert.IsFalse(built.Contains("pickup-die"));
        Assert.IsFalse(built.Contains("pickup-scrap"));
        Assert.IsFalse(built.Contains("pickup-ncm"));
        Assert.IsFalse(built.Contains("pickup-outside-service"));
        Assert.IsTrue(built.Contains("pickup-coil"));
        CollectionAssert.IsSubsetOf(built, RequestItemCatalog.GetByCategory(RequestCategory.Pickup).Select(i => i.Id).ToList());
        CollectionAssert.AreEquivalent(
            new[] { "pickup-riser-table", "pickup-hopper", "pickup-coil", "pickup-fg" },
            built);
    }
}
