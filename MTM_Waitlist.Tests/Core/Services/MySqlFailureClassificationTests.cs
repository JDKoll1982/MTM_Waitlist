using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Core.Services;

/// <summary>
/// A failure the server will give the same answer to however often it is asked is not retried, because the
/// retry budget buys nothing and the delay is paid by the person waiting for the screen.
/// </summary>
[TestClass]
public sealed class MySqlFailureClassificationTests
{
    [TestMethod]
    public void A_Missing_Procedure_Is_Permanent()
    {
        var reason = MySqlFailureClassification.ReasonFor(1305);

        Assert.IsNotNull(reason, "A missing procedure was reported as worth retrying.");
        StringAssert.Contains(reason!, "procedure", "The reason does not name what is missing.");
    }

    [TestMethod]
    public void A_Refused_Connection_Is_Not_Permanent()
    {
        Assert.IsNull(
            MySqlFailureClassification.ReasonFor(0),
            "A refused connection was classified as permanent, so the retry that exists for it would not run.");
        Assert.IsNull(
            MySqlFailureClassification.ReasonFor(1042),
            "A failed connect was classified as permanent, so the retry that exists for it would not run.");
        Assert.IsNull(
            MySqlFailureClassification.ReasonFor(1205),
            "A lock timeout was classified as permanent, so the retry that could clear it would not run.");
    }

    [TestMethod]
    public void A_Failure_With_No_Permanent_Cause_Is_Null()
    {
        Assert.IsNull(MySqlFailureClassification.PermanentReason(null), "No exception was reported as permanent.");
        Assert.IsNull(
            MySqlFailureClassification.PermanentReason(new TimeoutException("the server is starting")),
            "A timeout was reported as a permanent cause.");
        Assert.IsNull(
            MySqlFailureClassification.PermanentReason(
                new InvalidOperationException("outer", new TimeoutException("inner"))),
            "A wrapped timeout was reported as a permanent cause.");
    }
}
