using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Core.Services;

[TestClass]
public sealed class AppModuleClockTests
{
    [TestMethod]
    public void UtcNow_IsCloseToCurrentUtcTime()
    {
        var clock = new AppModuleClock();

        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        var now = clock.UtcNow;
        var after = DateTimeOffset.UtcNow.AddSeconds(1);

        Assert.IsTrue(now >= before, "UtcNow should not be in the past.");
        Assert.IsTrue(now <= after, "UtcNow should not be in the future.");
    }

    [TestMethod]
    public void UtcNow_IsUtc()
    {
        var clock = new AppModuleClock();

        Assert.AreEqual(TimeSpan.Zero, clock.UtcNow.Offset);
    }
}
