using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Waitlist.Services;

namespace MTM_Waitlist.Tests.Module_Waitlist.Services;

[TestClass]
public sealed class SampleJobCoilCatalogTests
{
    [TestMethod]
    public void GetCoilForJob_ReturnsCoilForCoilBearingJobs()
    {
        var coil = SampleJobCoilCatalog.GetCoilForJob("WO-076951");

        Assert.IsTrue(coil.HasCoil);
        Assert.AreEqual("COIL-204", coil.CoilNumber);
        Assert.IsFalse(string.IsNullOrWhiteSpace(coil.QuantityOnHand));
        Assert.IsFalse(string.IsNullOrWhiteSpace(coil.Description));
        Assert.IsFalse(string.IsNullOrWhiteSpace(coil.AverageWeight));
    }

    [TestMethod]
    public void GetCoilForJob_ReturnsNoCoilForJobsWithoutCoil()
    {
        var noCoil = SampleJobCoilCatalog.GetCoilForJob("100-17");

        Assert.IsFalse(noCoil.HasCoil);
        Assert.AreEqual(string.Empty, noCoil.CoilNumber);
    }
}
