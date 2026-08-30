using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Helpers;

namespace MTM_Waitlist.Tests.Core.Helpers;

[TestClass]
public sealed class ResourceExtensionsTests
{
    [TestMethod]
    public void GetLocalized_NullOrWhitespace_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, string.Empty.GetLocalized());
        Assert.AreEqual(string.Empty, "   ".GetLocalized());
    }

    [TestMethod]
    public void GetLocalized_MissingKey_FallsBackToKey()
    {
        // In the test host there are no PRI resources, so any key resolves to the key itself.
        var key = "Does.Not.Exist.Key";

        Assert.AreEqual(key, key.GetLocalized());
    }
}
