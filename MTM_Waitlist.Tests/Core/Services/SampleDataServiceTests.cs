using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Core.Services;

[TestClass]
public sealed class SampleDataServiceTests
{
    [TestMethod]
    public void GetSampleOrders_ReturnsThreeOrdersWithDetailBindings()
    {
        var service = new SampleDataService();

        var rows = service.GetSampleOrders();

        Assert.AreEqual(3, rows.Count);
        var firstOrder = rows[0] as MTM_Waitlist.Module_Waitlist.Models.SampleOrder;
        Assert.IsNotNull(firstOrder);
        Assert.AreEqual("Coil Request", firstOrder!.Title);
        Assert.AreEqual("Jordan Lee", firstOrder.RequestedByName);
        Assert.AreEqual("Press 12", firstOrder.RequestedPressName);
        Assert.AreEqual("00:27", firstOrder.RemainingTimeText);
    }

    [TestMethod]
    public void GetSampleOrders_UsesDifferentDataForEachBuilding()
    {
        var service = new SampleDataService();

        var expoRows = service.GetSampleOrders("Expo Drive");
        var vitsRows = service.GetSampleOrders("Vits Drive");

        var expoOrder = expoRows[0] as MTM_Waitlist.Module_Waitlist.Models.SampleOrder;
        var vitsOrder = vitsRows[0] as MTM_Waitlist.Module_Waitlist.Models.SampleOrder;

        Assert.IsNotNull(expoOrder);
        Assert.IsNotNull(vitsOrder);
        Assert.AreNotEqual(expoOrder!.Title, vitsOrder!.Title);
        Assert.AreNotEqual(expoOrder.RequestedByName, vitsOrder!.RequestedByName);
        Assert.AreEqual(3, expoRows.Count);
        Assert.AreEqual(3, vitsRows.Count);

        var imagePaths = expoRows.Concat(vitsRows)
            .OfType<MTM_Waitlist.Module_Waitlist.Models.SampleOrder>()
            .Select(item => item.ImagePath)
            .ToArray();

        CollectionAssert.AreEquivalent(
            new[]
            {
                "coil.png",
                "pickup_fg.png",
                "pickup_ncm.png",
                "pickup_os.png",
                "pickup_wip.png",
                "scrap.png"
            },
            imagePaths);
    }
}