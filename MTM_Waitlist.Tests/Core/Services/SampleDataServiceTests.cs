using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Core.Services;

namespace MTM_Waitlist.Tests.Core.Services;

[TestClass]
public sealed class SampleDataServiceTests
{
    [TestMethod]
    public async Task GetContentGridDataAsync_ReturnsKnownBuildingRowsAsync()
    {
        var service = new SampleDataService();

        var rows = (await service.GetContentGridDataAsync("Expo Drive")).ToList();

        Assert.AreEqual(8, rows.Count);
        Assert.AreEqual(10001, rows[0].OrderID);
        Assert.AreEqual("Spot Weld", rows[0].Company);
    }

    [TestMethod]
    public async Task GetContentGridDataAsync_ReturnsEmptyForUnknownBuildingAsync()
    {
        var service = new SampleDataService();

        var rows = await service.GetContentGridDataAsync("Unknown Building");

        Assert.AreEqual(0, rows.Count());
    }

    [TestMethod]
    public async Task GetContentGridDataAsync_ThrowsForBlankBuildingAsync()
    {
        var service = new SampleDataService();

        await Assert.ThrowsExceptionAsync<ArgumentException>(() => service.GetContentGridDataAsync("   "));
    }
}