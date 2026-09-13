using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Tests.Module_Settings;

[TestClass]
public sealed class RequestItemCatalogTests
{
    [TestMethod]
    public void Catalog_HasAllTwentyThreeRows()
    {
        Assert.AreEqual(23, RequestItemCatalog.TotalCount);
        Assert.AreEqual(23, RequestItemCatalog.Items.Count);
    }

    [TestMethod]
    public void Catalog_CountsPerCategory_MatchSpec()
    {
        Assert.AreEqual(11, RequestItemCatalog.GetByCategory(RequestCategory.Pickup).Count);
        Assert.AreEqual(8, RequestItemCatalog.GetByCategory(RequestCategory.Deliver).Count);
        Assert.AreEqual(3, RequestItemCatalog.GetByCategory(RequestCategory.Assist).Count);
        Assert.AreEqual(1, RequestItemCatalog.GetByCategory(RequestCategory.Other).Count);
    }

    [TestMethod]
    public void Catalog_PickupStartsWithPickupCoil_EndsWithHopper()
    {
        var pickup = RequestItemCatalog.GetByCategory(RequestCategory.Pickup);
        Assert.AreEqual("pickup-coil", pickup[0].Id);
        Assert.AreEqual("pickup-hopper", pickup[^1].Id);
    }

    [TestMethod]
    public void Catalog_IncludesLegacyOnlyRowsAdded20260908()
    {
        // Wrong Coil / Wrong Flatstock (Deliver-correct + Pickup-wrong), scrap offal removal,
        // hopper pickup, and table remove were folded into the canonical catalog on 2026-09-08.
        Assert.IsNotNull(RequestItemCatalog.FindById("deliver-wrong-coil"));
        Assert.IsNotNull(RequestItemCatalog.FindById("deliver-wrong-flatstock"));
        Assert.IsNotNull(RequestItemCatalog.FindById("pickup-scrap"));
        Assert.IsNotNull(RequestItemCatalog.FindById("pickup-hopper"));
        Assert.IsNotNull(RequestItemCatalog.FindById("assist-table-remove"));
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
    public void Catalog_UmbrellaVerb_OverridesTheWrongMaterialItems()
    {
        // The two wrong-material Items carry their own first line, pinned verbatim, rather than the
        // plain Category word (contract §2, FR-029).
        Assert.AreEqual("Wrong Coil Bring:", RequestItemCatalog.FindById("deliver-wrong-coil")!.UmbrellaVerb);
        Assert.AreEqual("Wrong Flatstock Bring:", RequestItemCatalog.FindById("deliver-wrong-flatstock")!.UmbrellaVerb);
        Assert.AreEqual("Deliver", RequestItemCatalog.FindById("deliver-coil")!.UmbrellaVerb);
    }

    [TestMethod]
    public void Catalog_EveryItemDeclaresADisplayNameResourceKeyAndALine2Template()
    {
        foreach (var item in RequestItemCatalog.Items)
        {
            Assert.AreEqual(
                RequestItemCatalog.DisplayNameResourceKeyFor(item.Id),
                item.DisplayNameResourceKey,
                $"Item '{item.Id}' must carry its display-name resource key.");
            Assert.IsFalse(
                string.IsNullOrWhiteSpace(item.CardLine2Template),
                $"Item '{item.Id}' must carry a Line 2 template.");
        }
    }

    [TestMethod]
    public void Catalog_CardLine2Template_EncodesTheDiesHomeLocationConditional()
    {
        // The only conditional in the language: the die's location when the captured destination is
        // Home Location, otherwise the die's number (contract §3).
        var die = RequestItemCatalog.FindById("pickup-die")!;
        Assert.AreEqual("{die_number:die_location=Home Location}", die.CardLine2Template);
    }
}
