using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Tests.Module_Settings;

[TestClass]
public sealed class RequestItemCatalogTests
{
    [TestMethod]
    public void Catalog_HasAllTwentyFourRows()
    {
        Assert.AreEqual(24, RequestItemCatalog.TotalCount);
        Assert.AreEqual(24, RequestItemCatalog.Items.Count);
    }

    [TestMethod]
    public void Catalog_CountsPerCategory_MatchSpec()
    {
        Assert.AreEqual(11, RequestItemCatalog.GetByCategory(RequestCategory.Pickup).Count);
        Assert.AreEqual(9, RequestItemCatalog.GetByCategory(RequestCategory.Deliver).Count);
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
    public void Catalog_DeliverComponent_MirrorsThePickupComponentChoice()
    {
        // Added 2026-09-22: a job carrying a component is offered a Deliver component wherever Pickup offers
        // one, so the two families cannot drift apart again.
        var deliver = RequestItemCatalog.FindById("deliver-component");
        var pickup = RequestItemCatalog.FindById("pickup-component")!;

        Assert.IsNotNull(deliver);
        Assert.AreEqual(RequestCategory.Deliver, deliver!.Category);
        Assert.AreEqual(pickup.NormalizedName, deliver.NormalizedName);
        Assert.AreEqual(pickup.CardLine2Template, deliver.CardLine2Template);
        Assert.AreEqual("deliver-component", deliver.ProducedValue);
        Assert.AreEqual("Deliver", deliver.UmbrellaVerb);
    }

    [TestMethod]
    public void Catalog_UmbrellaVerb_MapsToCategory()
    {
        // The die Items are the exception the catalog declares for themselves: their first line names the thing
        // being moved rather than the bare Category word (FR-053).
        Assert.AreEqual("Pickup", RequestItemCatalog.FindById("pickup-coil")!.UmbrellaVerb);
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
        // plain Category word (contract §2, FR-029); the die Items do the same (FR-053).
        Assert.AreEqual("Wrong Coil Bring:", RequestItemCatalog.FindById("deliver-wrong-coil")!.UmbrellaVerb);
        Assert.AreEqual("Wrong Flatstock Bring:", RequestItemCatalog.FindById("deliver-wrong-flatstock")!.UmbrellaVerb);
        Assert.AreEqual("Pickup Die:", RequestItemCatalog.FindById("pickup-die")!.UmbrellaVerb);
        Assert.AreEqual("Deliver Die:", RequestItemCatalog.FindById("deliver-die")!.UmbrellaVerb);
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
    public void Catalog_DieItems_DeclareTheirOwnTwoLines()
    {
        // Superseded 2026-09-14 (FR-053). The identifier used to be the language's one conditional — the die's
        // location at Home Location, otherwise its number. Both lines now carry what the handler needs in one
        // read: which job part the die is for, then the die's own number and where the die is. Nothing in the
        // shipped catalog uses the conditional syntax any more; the resolver still supports it.
        foreach (var itemId in new[] { "pickup-die", "deliver-die" })
        {
            var die = RequestItemCatalog.FindById(itemId)!;

            Assert.AreEqual("{die}", die.CardLine2Template, $"{itemId}'s identifier is the die and its location, composed as one value.");
            Assert.AreEqual("{umbrella} {job_part_number}", die.CardLine1Template, $"{itemId}'s first line names the part the die is assigned to.");
        }
    }
}
