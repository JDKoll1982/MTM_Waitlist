using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;

namespace MTM_Waitlist.Tests.Module_Settings;

[TestClass]
public sealed class RequestItemCatalogServiceTests
{
    private readonly IRequestItemCatalogService _service = new RequestItemCatalogService();

    [TestMethod]
    public void GetAllItems_ReturnsAll23CanonicalRows()
    {
        var items = _service.GetAllItems();

        Assert.AreEqual(23, items.Count);
        Assert.AreEqual("pickup-coil", items[0].Id);
        Assert.AreEqual("other", items[^1].Id);
    }

    [TestMethod]
    public void GetByCategory_MatchesCsvCounts_Pickup11_Deliver8_Assist3_Other1()
    {
        Assert.AreEqual(11, _service.GetByCategory(RequestCategory.Pickup).Count);
        Assert.AreEqual(8, _service.GetByCategory(RequestCategory.Deliver).Count);
        Assert.AreEqual(3, _service.GetByCategory(RequestCategory.Assist).Count);
        Assert.AreEqual(1, _service.GetByCategory(RequestCategory.Other).Count);
    }

    [TestMethod]
    public void GetByCategory_IsOrderedByCsvOrder()
    {
        var pickup = _service.GetByCategory(RequestCategory.Pickup);

        var orders = pickup.Select(i => i.Order).ToArray();
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 }, orders);
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
