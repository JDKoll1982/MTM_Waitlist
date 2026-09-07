using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Waitlist.Services;

namespace MTM_Waitlist.Tests.Module_Waitlist.Services;

[TestClass]
public sealed class SampleWaitlistRequestCatalogTests
{
    [TestMethod]
    public void GetRequests_ReturnsAllFourLifecycleStatuses()
    {
        var requests = SampleWaitlistRequestCatalog.GetRequests("Expo Drive");

        Assert.AreEqual(5, requests.Count);
        Assert.IsTrue(requests.Any(item => item.Status == "Pending"));
        Assert.IsTrue(requests.Any(item => item.Status == "Accepted"));
        Assert.IsTrue(requests.Any(item => item.Status == "Completed"));
        Assert.IsTrue(requests.Any(item => item.Status == "Canceled"));
    }

    [TestMethod]
    public void GetRequests_IncludesSignedInAndOtherRequesters()
    {
        var requests = SampleWaitlistRequestCatalog.GetRequests("Expo Drive");

        Assert.IsTrue(requests.Any(item => item.RequesterEmployeeNumber == "6229"));
        Assert.IsTrue(requests.Any(item => item.RequesterEmployeeNumber == "5000"));
        Assert.IsTrue(requests.Count(item => item.RequesterEmployeeNumber == "5000") == 1);
    }

    [TestMethod]
    public void GetRequests_CancelledRowIsRetainedWithMetadata()
    {
        var requests = SampleWaitlistRequestCatalog.GetRequests("Expo Drive");
        var canceled = requests.Single(item => item.Status == "Canceled");

        Assert.IsNotNull(canceled.CanceledUtc);
        Assert.IsNotNull(canceled.CanceledByEmployeeNumber);
        Assert.AreEqual("6229", canceled.CanceledByEmployeeNumber);
        Assert.IsFalse(string.IsNullOrWhiteSpace(canceled.CancellationReason));
    }

    [TestMethod]
    public void GetRequests_AcceptedAndCompletedCarryTimestampsAndHandler()
    {
        var requests = SampleWaitlistRequestCatalog.GetRequests("Expo Drive");
        var accepted = requests.Single(item => item.Status == "Accepted");
        var completed = requests.Single(item => item.Status == "Completed");

        Assert.AreEqual("6229", accepted.AssignedMaterialHandler);
        Assert.IsNotNull(accepted.AcceptedUtc);
        Assert.IsNotNull(accepted.TargetTimeUtc);
        Assert.IsTrue(accepted.IsOverdue);
        Assert.IsFalse(string.IsNullOrWhiteSpace(accepted.Note));
        Assert.IsNotNull(completed.AcceptedUtc);
        Assert.IsNotNull(completed.CompletedUtc);
    }
}
