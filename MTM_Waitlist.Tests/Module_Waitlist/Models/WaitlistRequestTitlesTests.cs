using Microsoft.VisualStudio.TestTools.UnitTesting;

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
}
