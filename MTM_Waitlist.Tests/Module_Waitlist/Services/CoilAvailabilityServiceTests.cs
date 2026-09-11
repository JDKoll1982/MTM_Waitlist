using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Waitlist.Services;

namespace MTM_Waitlist.Tests.Module_Waitlist.Services;

/// <summary>
/// Coil availability no longer consults a sample catalog (FR-014): the mock toggle and
/// <c>the retired job-coil sample catalog</c> are gone. These tests pin the interim behavior until the live source/// lands (FR-019, task T099), and prove no sample coil data is fabricated in the meantime.
/// </summary>
[TestClass]
public sealed class CoilAvailabilityServiceTests
{
    [TestMethod]
    public async Task GetCoilForJobAsync_WithoutALiveSource_AssumesCoilAvailableAndFabricatesNoSampleData()
    {
        var service = new CoilAvailabilityService();

        var coil = await service.GetCoilForJobAsync("100-3");

        Assert.IsTrue(coil.HasCoil, "The coil request type must not be hidden while the live source is unconfigured.");
        Assert.AreEqual(string.Empty, coil.CoilNumber);
        Assert.AreEqual(string.Empty, coil.QuantityOnHand);
        Assert.AreEqual(string.Empty, coil.Description);
        Assert.AreEqual(string.Empty, coil.AverageWeight);
    }

    [TestMethod]
    public async Task GetCoilForJobAsync_AcceptsAMissingWorkCenter()
    {
        var service = new CoilAvailabilityService();

        var coil = await service.GetCoilForJobAsync(null);

        Assert.IsTrue(coil.HasCoil);
    }

    [TestMethod]
    public async Task GetCoilForJobAsync_HonorsCancellation()
    {
        var service = new CoilAvailabilityService();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsExceptionAsync<OperationCanceledException>(
            () => service.GetCoilForJobAsync("100-3", cts.Token));
    }
}
