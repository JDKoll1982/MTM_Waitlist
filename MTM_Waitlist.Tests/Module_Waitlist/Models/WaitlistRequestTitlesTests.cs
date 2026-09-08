using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Tests.Module_Waitlist.Models;

[TestClass]
public sealed class WaitlistRequestTitlesTests
{
    [TestMethod]
    public void For_ReturnsRewordedTitles_ForKnownPairs()
    {
        Assert.AreEqual("Deliver: Coil", WaitlistRequestTitles.For("Coil", "Bring"));
        Assert.AreEqual("Return: Coil", WaitlistRequestTitles.For("Coil", "Pickup"));
        Assert.AreEqual("Pickup: NCM", WaitlistRequestTitles.For("Pickup", "Pickup NCM"));
        Assert.AreEqual("Pickup: FG", WaitlistRequestTitles.For("Pickup", "Pickup FG"));
        Assert.AreEqual("Pickup: WIP", WaitlistRequestTitles.For("Pickup", "Pickup WIP"));
        Assert.AreEqual("Return: Coil", WaitlistRequestTitles.For("Pickup", "Pickup Coil"));
        Assert.AreEqual("Pickup: Outside Service", WaitlistRequestTitles.For("Pickup", "Outside Service"));
        Assert.AreEqual("Scrap: Empty", WaitlistRequestTitles.For("Scrap", "Empty"));
        Assert.AreEqual("Deliver: Flatstock", WaitlistRequestTitles.For("Flatstock", "Bring"));
        Assert.AreEqual("Assist: Place parts on table", WaitlistRequestTitles.For("Table Handling", "Table Place Parts"));
        Assert.AreEqual("General Request", WaitlistRequestTitles.For("Other", "General Text Entry"));
    }

    [TestMethod]
    public void For_NoSubtype_ReturnsRequestType()
    {
        Assert.AreEqual("Forklift Assist", WaitlistRequestTitles.For("Forklift Assist", null));
        Assert.AreEqual("Coil", WaitlistRequestTitles.For("Coil", ""));
    }

    [TestMethod]
    public void For_UnlistedPair_FallsBackToTypeSlashSubtype()
    {
        Assert.AreEqual("Coil / Future Subtype", WaitlistRequestTitles.For("Coil", "Future Subtype"));
    }

    [TestMethod]
    public void ResolveLine1_ReturnsCanonicalUmbrellaVerb_WhenMapped()
    {
        // Phase 1.2 uniform-card Line 1 = umbrella verb (Pickup/Deliver/Assist/Other).
        Assert.AreEqual("Deliver", WaitlistRequestTitles.ResolveLine1("Coil", "Bring"));
        Assert.AreEqual("Pickup", WaitlistRequestTitles.ResolveLine1("Coil", "Pickup"));
        Assert.AreEqual("Deliver", WaitlistRequestTitles.ResolveLine1("Coil", "Wrong Coil @ press"));
        Assert.AreEqual("Assist", WaitlistRequestTitles.ResolveLine1("Coil", "Need Coil Turned around"));
        Assert.AreEqual("Pickup", WaitlistRequestTitles.ResolveLine1("Pickup", "Pickup NCM"));
        Assert.AreEqual("Pickup", WaitlistRequestTitles.ResolveLine1("Scrap", "Empty"));
        Assert.AreEqual("Deliver", WaitlistRequestTitles.ResolveLine1("Scrap", "Bring Hopper"));
        Assert.AreEqual("Assist", WaitlistRequestTitles.ResolveLine1("Table Handling", "Table Remove Parts"));
    }

    [TestMethod]
    public void ResolveLine1_FallsBackToLegacyType_WhenUnmappedOrSubtypeLess()
    {
        // Forklift Assist is a type-level leaf -> Other umbrella.
        Assert.AreEqual("Other", WaitlistRequestTitles.ResolveLine1("Forklift Assist", null));
        // Unlisted pairing keeps the legacy type as Line 1.
        Assert.AreEqual("Coil", WaitlistRequestTitles.ResolveLine1("Coil", "Future Subtype"));
        // Subtype-less grouping types are not leaves -> legacy type fallback.
        Assert.AreEqual("Coil", WaitlistRequestTitles.ResolveLine1("Coil", ""));
    }

    [TestMethod]
    public void ResolveItem_ReturnsCanonicalCatalogItem_ForKnownPairs()
    {
        var item = WaitlistRequestTitles.ResolveItem("Coil", "Bring");
        Assert.IsNotNull(item);
        Assert.AreEqual("deliver-coil", item!.Id);
        Assert.AreEqual(RequestCategory.Deliver, item.Category);

        var pickupNcm = WaitlistRequestTitles.ResolveItem("Pickup", "Pickup NCM");
        Assert.IsNotNull(pickupNcm);
        Assert.AreEqual("pickup-ncm", pickupNcm!.Id);
        Assert.AreEqual(RequestCategory.Pickup, pickupNcm.Category);

        var forklift = WaitlistRequestTitles.ResolveItem("Forklift Assist", null);
        Assert.IsNotNull(forklift);
        Assert.AreEqual("other", forklift!.Id);
    }

    [TestMethod]
    public void ResolveItem_ReturnsNull_ForGroupingTypeOrUnknownPair()
    {
        Assert.IsNull(WaitlistRequestTitles.ResolveItem("Pickup", null));
        Assert.IsNull(WaitlistRequestTitles.ResolveItem("Coil", "Future Subtype"));
        Assert.IsNull(WaitlistRequestTitles.ResolveItem("", "Pickup NCM"));
    }
}
