using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Core.Services;

[TestClass]
public sealed class WaitlistRequestLinkTests
{
    [TestMethod]
    public void Build_ProducesOpenRequestQueryWithGuid()
    {
        var id = Guid.NewGuid();
        var arguments = WaitlistRequestLink.Build(id);

        StringAssert.Contains(arguments, "action=openrequest");
        StringAssert.Contains(arguments, "request=" + id.ToString("D"));
    }

    [TestMethod]
    public void TryParse_RoundTripsBuiltArguments()
    {
        var id = Guid.NewGuid();
        var arguments = WaitlistRequestLink.Build(id);

        Assert.IsTrue(WaitlistRequestLink.TryParse(arguments, out var parsed));
        Assert.AreEqual(id, parsed);
    }

    [TestMethod]
    public void TryParse_RejectsWrongAction()
    {
        Assert.IsFalse(WaitlistRequestLink.TryParse("action=settings", out _));
        Assert.IsFalse(WaitlistRequestLink.TryParse("action=openrequestx&request=" + Guid.NewGuid(), out _));
    }

    [TestMethod]
    public void TryParse_RejectsMissingOrInvalidRequest()
    {
        Assert.IsFalse(WaitlistRequestLink.TryParse(null, out _));
        Assert.IsFalse(WaitlistRequestLink.TryParse(string.Empty, out _));
        Assert.IsFalse(WaitlistRequestLink.TryParse("action=openrequest", out _));
        Assert.IsFalse(WaitlistRequestLink.TryParse("action=openrequest&request=not-a-guid", out _));
    }
}
