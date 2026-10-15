using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Tests.Module_Settings.Models;

/// <summary>
/// The job snapshot is what the picker and the card read, so the coil's own facts, the dunnage home location and
/// the four returning-pickup gates have to survive the trip onto it: the four returning Items are offered on the
/// requesting job's real work rather than hidden by a fixed list (FR-088, FR-089, FR-094).
/// </summary>
[TestClass]
public sealed class RequestJobPartAvailabilityTests
{
    [TestMethod]
    public void Model_RequestJobPartAvailabilityTests_ReturningPickupGates_AreEachTheirOwnWorkSignal()
    {
        // Each gate names the work one returning Item needs (finished goods, non-conforming stock, work in
        // progress, subcontracted work). Setting one must say nothing about the other three, or a job with
        // finished goods would also be offered a pickup for work it does not have (FR-094).
        var finishedGoods = RequestJobPartAvailability.None with { HasFinishedGoodsToReturn = true };
        var nonconforming = RequestJobPartAvailability.None with { HasNonconformingStockToReturn = true };
        var workInProgress = RequestJobPartAvailability.None with { HasWorkInProgressToReturn = true };
        var outsideService = RequestJobPartAvailability.None with { HasOutsideServiceWorkToReturn = true };

        Assert.IsTrue(finishedGoods.HasFinishedGoodsToReturn);
        Assert.IsTrue(nonconforming.HasNonconformingStockToReturn);
        Assert.IsTrue(workInProgress.HasWorkInProgressToReturn);
        Assert.IsTrue(outsideService.HasOutsideServiceWorkToReturn);

        Assert.IsFalse(nonconforming.HasFinishedGoodsToReturn);
        Assert.IsFalse(workInProgress.HasFinishedGoodsToReturn);
        Assert.IsFalse(outsideService.HasFinishedGoodsToReturn);
        Assert.IsFalse(finishedGoods.HasNonconformingStockToReturn);
        Assert.IsFalse(finishedGoods.HasWorkInProgressToReturn);
        Assert.IsFalse(finishedGoods.HasOutsideServiceWorkToReturn);
    }

    [TestMethod]
    public void Model_RequestJobPartAvailabilityTests_ReturningPickupGates_AreFalseOnAnEmptyJobAndTrueOnAJobWithEverything()
    {
        Assert.IsFalse(RequestJobPartAvailability.None.HasFinishedGoodsToReturn);
        Assert.IsFalse(RequestJobPartAvailability.None.HasNonconformingStockToReturn);
        Assert.IsFalse(RequestJobPartAvailability.None.HasWorkInProgressToReturn);
        Assert.IsFalse(RequestJobPartAvailability.None.HasOutsideServiceWorkToReturn);

        Assert.IsTrue(RequestJobPartAvailability.All.HasFinishedGoodsToReturn);
        Assert.IsTrue(RequestJobPartAvailability.All.HasNonconformingStockToReturn);
        Assert.IsTrue(RequestJobPartAvailability.All.HasWorkInProgressToReturn);
        Assert.IsTrue(RequestJobPartAvailability.All.HasOutsideServiceWorkToReturn);
    }

    [TestMethod]
    public void Model_RequestJobPartAvailabilityTests_CoilFacts_CarryTheDescriptionAndTheAverageWeight()
    {
        var job = RequestJobPartAvailability.None with
        {
            HasCoil = true,
            CoilPartNumber = "MMC1234-01",
            CoilDescription = "Coil 0.036 x 24.5 CRS",
            CoilAverageWeight = "4820.5000",
        };

        Assert.AreEqual("Coil 0.036 x 24.5 CRS", job.CoilDescription);
        Assert.AreEqual("4820.5000", job.CoilAverageWeight);
    }

    [TestMethod]
    public void Model_RequestJobPartAvailabilityTests_CoilFacts_DefaultToEmptyRatherThanASubstitute()
    {
        // A job with no coil carries no coil description and no average weight, and nothing fills the gap — the
        // card leaves the cell out instead of naming the Item or the coil number in the weight's place (FR-089).
        Assert.AreEqual(string.Empty, RequestJobPartAvailability.None.CoilDescription);
        Assert.AreEqual(string.Empty, RequestJobPartAvailability.None.CoilAverageWeight);
        Assert.AreEqual(
            string.Empty,
            (RequestJobPartAvailability.None with { HasCoil = true, CoilPartNumber = "MMC1234-01" }).CoilAverageWeight);
    }

    [TestMethod]
    public void Model_RequestJobPartAvailabilityTests_DunnageHomeLocation_ReachesTheSnapshotThroughTheDunnagePartList()
    {
        var job = RequestJobPartAvailability.None.WithDunnageParts(new[]
        {
            new RequestDunnagePart { PartNumber = "DN-STL-4", DisplayName = "Steel rack", HomeLocation = "V-A0-01", IsAssignedToJob = true },
        });

        Assert.AreEqual(1, job.DunnageParts.Count);
        Assert.AreEqual("V-A0-01", job.DunnageParts[0].HomeLocation);
    }
}
