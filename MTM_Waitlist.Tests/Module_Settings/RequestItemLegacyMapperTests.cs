using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;

namespace MTM_Waitlist.Tests.Module_Settings;

[TestClass]
public sealed class RequestItemLegacyMapperTests
{
    [TestMethod]
    public void Map_EveryLegacyLeafSubtype_ResolvesToCanonicalItem()
    {
        // The Phase 1.2 gate: every current request (legacy type/subtype leaf) maps onto a
        // Category/Item row. These mirror the DB seed mapping in seed_waitlist_request_catalog.
        AssertCanonical("Pickup", "Pickup Other", "other", RequestCategory.Other);
        AssertCanonical("Pickup", "Pickup NCM", "pickup-ncm", RequestCategory.Pickup);
        AssertCanonical("Pickup", "Pickup WIP", "pickup-wip", RequestCategory.Pickup);
        AssertCanonical("Pickup", "Pickup FG", "pickup-fg", RequestCategory.Pickup);
        AssertCanonical("Pickup", "Pickup Coil", "pickup-coil", RequestCategory.Pickup);
        AssertCanonical("Pickup", "Pickup Flatstock", "pickup-coil", RequestCategory.Pickup);
        AssertCanonical("Pickup", "Outside Service", "pickup-outside-service", RequestCategory.Pickup);
        AssertCanonical("Other", "General Text Entry", "other", RequestCategory.Other);
        AssertCanonical("Coil", "Bring", "deliver-coil", RequestCategory.Deliver);
        AssertCanonical("Coil", "Pickup", "pickup-coil", RequestCategory.Pickup);
        AssertCanonical("Coil", "Wrong Coil @ press", "deliver-wrong-coil", RequestCategory.Deliver);
        AssertCanonical("Coil", "Need Riser Table", "deliver-riser-table", RequestCategory.Deliver);
        AssertCanonical("Coil", "Need Coil Turned around", "assist-coil-turn", RequestCategory.Assist);
        AssertCanonical("Scrap", "Empty", "pickup-scrap", RequestCategory.Pickup);
        AssertCanonical("Scrap", "Pickup Hopper, do not return", "pickup-hopper", RequestCategory.Pickup);
        AssertCanonical("Scrap", "Bring Hopper", "deliver-hopper", RequestCategory.Deliver);
        AssertCanonical("Flatstock", "Bring", "deliver-flatstock", RequestCategory.Deliver);
        AssertCanonical("Flatstock", "Pickup", "pickup-coil", RequestCategory.Pickup);
        AssertCanonical("Flatstock", "Wrong Flatstock @ Workcenter", "deliver-wrong-flatstock", RequestCategory.Deliver);
        AssertCanonical("Table Handling", "Table Place Parts", "assist-table-place", RequestCategory.Assist);
        AssertCanonical("Table Handling", "Table Remove Parts", "assist-table-remove", RequestCategory.Assist);
        AssertCanonical("Die Handling", "Bring Die", "deliver-die", RequestCategory.Deliver);
        AssertCanonical("Die Handling", "Pull Die and Put Away", "pickup-die", RequestCategory.Pickup);
        AssertCanonical("Die Handling", "Pull Die and Take to Die Shop", "pickup-die", RequestCategory.Pickup);
        AssertCanonical("Die Handling", "Pull Die and Leave @ press", "pickup-die", RequestCategory.Pickup);
    }

    [TestMethod]
    public void Map_TypeLeaf_ForkliftAssist_ResolvesToOther()
    {
        var item = RequestItemLegacyMapper.Map("Forklift Assist", null);

        Assert.IsNotNull(item);
        Assert.AreEqual("other", item!.Id);
        Assert.AreEqual(RequestCategory.Other, item.Category);
    }

    [TestMethod]
    public void Map_UnknownPairOrGroupingType_ReturnsNull()
    {
        // Grouping types carry subtypes and are not directly selectable leaves.
        Assert.IsNull(RequestItemLegacyMapper.Map("Pickup", null));
        Assert.IsNull(RequestItemLegacyMapper.Map("Coil", null));
        Assert.IsNull(RequestItemLegacyMapper.Map("Coil", "Future Subtype"));
        Assert.IsNull(RequestItemLegacyMapper.Map("", "Pickup NCM"));
        Assert.IsNull(RequestItemLegacyMapper.Map(null, null));
    }

    [TestMethod]
    public void ResolveUmbrellaVerb_ReturnsCanonicalVerb_OrLegacyTypeFallback()
    {
        Assert.AreEqual("Deliver", RequestItemLegacyMapper.ResolveUmbrellaVerb("Coil", "Bring"));
        Assert.AreEqual("Pickup", RequestItemLegacyMapper.ResolveUmbrellaVerb("Pickup", "Pickup NCM"));
        // Pull Die variants map to pickup-die (Pickup umbrella); the destination is captured at submit.
        Assert.AreEqual("Pickup", RequestItemLegacyMapper.ResolveUmbrellaVerb("Die Handling", "Pull Die and Leave @ press"));
        Assert.AreEqual("Other", RequestItemLegacyMapper.ResolveUmbrellaVerb("Forklift Assist", null));
        // Unlisted pairing falls back to the legacy type verb.
        Assert.AreEqual("Coil", RequestItemLegacyMapper.ResolveUmbrellaVerb("Coil", "Future Subtype"));
    }

    private static void AssertCanonical(string type, string subtype, string expectedId, RequestCategory expectedCategory)
    {
        var item = RequestItemLegacyMapper.Map(type, subtype);
        Assert.IsNotNull(item, $"{type}/{subtype} should map to a canonical row.");
        Assert.AreEqual(expectedId, item!.Id, $"{type}/{subtype} item id.");
        Assert.AreEqual(expectedCategory, item.Category, $"{type}/{subtype} category.");
    }
}
