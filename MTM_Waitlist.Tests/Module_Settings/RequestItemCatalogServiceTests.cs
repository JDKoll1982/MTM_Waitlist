using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;

namespace MTM_Waitlist.Tests.Module_Settings;

[TestClass]
public sealed class RequestItemCatalogServiceTests
{
    private readonly IRequestItemCatalogService _service = new RequestItemCatalogService();

    [TestMethod]
    public void GetAllItems_ReturnsAll18CanonicalRows()
    {
        var items = _service.GetAllItems();

        Assert.AreEqual(18, items.Count);
        Assert.AreEqual("pickup-coil", items[0].Id);
        Assert.AreEqual("other", items[^1].Id);
    }

    [TestMethod]
    public void GetByCategory_MatchesCsvCounts_Pickup9_Deliver6_Assist2_Other1()
    {
        Assert.AreEqual(9, _service.GetByCategory(RequestCategory.Pickup).Count);
        Assert.AreEqual(6, _service.GetByCategory(RequestCategory.Deliver).Count);
        Assert.AreEqual(2, _service.GetByCategory(RequestCategory.Assist).Count);
        Assert.AreEqual(1, _service.GetByCategory(RequestCategory.Other).Count);
    }

    [TestMethod]
    public void GetByCategory_IsOrderedByCsvOrder()
    {
        var pickup = _service.GetByCategory(RequestCategory.Pickup);

        var orders = pickup.Select(i => i.Order).ToArray();
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, orders);
    }

    [TestMethod]
    public void FindById_ReturnsItem_OrNull()
    {
        Assert.AreEqual(RequestCategory.Pickup, _service.FindById("pickup-coil")!.Category);
        Assert.AreEqual("Other", _service.FindById("other")!.UmbrellaVerb);
        Assert.IsNull(_service.FindById("pickup-unknown"));
        Assert.IsNull(_service.FindById(null));
    }

    [TestMethod]
    public void GetCategoriesInOrder_ReturnsPickupDeliverAssistOther()
    {
        var categories = _service.GetCategoriesInOrder();

        CollectionAssert.AreEqual(
            new[] { RequestCategory.Pickup, RequestCategory.Deliver, RequestCategory.Assist, RequestCategory.Other },
            categories.ToArray());
    }
}
