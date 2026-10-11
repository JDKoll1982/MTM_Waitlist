using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Core.Services;

[TestClass]
public sealed class ResolvedRetentionFilterTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 6, 12, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void Service_ResolvedRetentionFilterTests_WithinRetention_RecentResolvedIsShown()
    {
        Assert.IsTrue(ResolvedRetentionFilter.IsWithinRetention(Now.AddDays(-1), Now));
    }

    [TestMethod]
    public void Service_ResolvedRetentionFilterTests_WithinRetention_ExactlyAtWindowIsStillShown()
    {
        Assert.IsTrue(ResolvedRetentionFilter.IsWithinRetention(Now.AddDays(-90), Now));
    }

    [TestMethod]
    public void Service_ResolvedRetentionFilterTests_AgedOut_ResolvedOlderThanWindowIsHidden()
    {
        Assert.IsTrue(ResolvedRetentionFilter.IsAgedOut(Now.AddDays(-91), Now));
        Assert.IsFalse(ResolvedRetentionFilter.IsWithinRetention(Now.AddDays(-91), Now));
    }

    [TestMethod]
    public void Service_ResolvedRetentionFilterTests_AgedOut_RespectsCustomWindow()
    {
        Assert.IsTrue(ResolvedRetentionFilter.IsAgedOut(Now.AddDays(-30), Now, retentionDays: 7));
        Assert.IsFalse(ResolvedRetentionFilter.IsAgedOut(Now.AddDays(-2), Now, retentionDays: 7));
    }

    [TestMethod]
    public void Service_ResolvedRetentionFilterTests_RetentionCutoff_IsNowMinusWindow()
    {
        Assert.AreEqual(Now.AddDays(-90), ResolvedRetentionFilter.RetentionCutoff(Now));
    }
}
