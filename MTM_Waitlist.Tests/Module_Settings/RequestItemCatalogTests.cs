using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Tests.Module_Settings;

[TestClass]
public sealed class RequestItemCatalogTests
{
    [TestMethod]
    public void Catalog_HasAllEighteenRows()
    {
        Assert.AreEqual(18, RequestItemCatalog.TotalCount);
        Assert.AreEqual(18, RequestItemCatalog.Items.Count);
    }

    [TestMethod]
    public void Catalog_CountsPerCategory_MatchSpec()
    {
        Assert.AreEqual(9, RequestItemCatalog.GetByCategory(RequestCategory.Pickup).Count);
        Assert.AreEqual(6, RequestItemCatalog.GetByCategory(RequestCategory.Deliver).Count);
        Assert.AreEqual(2, RequestItemCatalog.GetByCategory(RequestCategory.Assist).Count);
        Assert.AreEqual(1, RequestItemCatalog.GetByCategory(RequestCategory.Other).Count);
    }

    [TestMethod]
    public void Catalog_PickupStartsWithPickupCoil_EndsWithDunnage()
    {
        var pickup = RequestItemCatalog.GetByCategory(RequestCategory.Pickup);
        Assert.AreEqual("pickup-coil", pickup[0].Id);
        Assert.AreEqual("pickup-dunnage", pickup[^1].Id);
    }

    [TestMethod]
    public void Catalog_UmbrellaVerb_MapsToCategory()
    {
        Assert.AreEqual("Pickup", RequestItemCatalog.FindById("pickup-die")!.UmbrellaVerb);
        Assert.AreEqual("Deliver", RequestItemCatalog.FindById("deliver-coil")!.UmbrellaVerb);
        Assert.AreEqual("Assist", RequestItemCatalog.FindById("assist-coil-turn")!.UmbrellaVerb);
        Assert.AreEqual("Other", RequestItemCatalog.FindById("other")!.UmbrellaVerb);
    }

    [TestMethod]
    public void Catalog_FindById_IsCaseInsensitive()
    {
        Assert.IsNotNull(RequestItemCatalog.FindById("PICKUP-COIL"));
        Assert.IsNotNull(RequestItemCatalog.FindById("Other"));
        Assert.IsNull(RequestItemCatalog.FindById("does-not-exist"));
    }

    [TestMethod]
    public void Catalog_NeedsUserEntry_FlagsUserEntryItems()
    {
        Assert.IsTrue(RequestItemCatalog.FindById("pickup-die")!.NeedsUserEntry);
        Assert.IsTrue(RequestItemCatalog.FindById("pickup-ncm")!.NeedsUserEntry);
        Assert.IsTrue(RequestItemCatalog.FindById("other")!.NeedsUserEntry);
        Assert.IsFalse(RequestItemCatalog.FindById("deliver-coil")!.NeedsUserEntry);
    }

    [TestMethod]
    public void Catalog_DeliverItems_ExceptDunnage_NeedNoUserEntry()
    {
        foreach (var item in RequestItemCatalog.GetByCategory(RequestCategory.Deliver))
        {
            if (item.Id == "deliver-dunnage")
            {
                // Dunnage is always user-selected via image cards (clarified 2026-09-07).
                Assert.IsTrue(item.NeedsUserEntry, $"Deliver item '{item.Id}' requires dunnage selection.");
            }
            else
            {
                Assert.IsFalse(item.NeedsUserEntry, $"Deliver item '{item.Id}' should need no user entry.");
            }
        }
    }
}
