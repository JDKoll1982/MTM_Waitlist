using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Core.Services;

[TestClass]
public sealed class SampleDataServiceTests
{
    [TestMethod]
    public void GetSampleOrders_ReturnsTwoOrdersWithDetailBindings()
    {
        var service = new SampleDataService();

        var rows = service.GetSampleOrders();

        Assert.AreEqual(2, rows.Count);
        var firstOrder = rows[0] as MTM_Waitlist.Module_Waitlist.Models.SampleOrder;
        Assert.IsNotNull(firstOrder);
        Assert.AreEqual("Material request", firstOrder!.Title);
        Assert.AreEqual("Expo Drive", firstOrder.Subtitle);
        Assert.IsTrue(firstOrder.Fields.Count > 0);
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
        Assert.AreNotEqual(expoOrder.Subtitle, vitsOrder.Subtitle);
    }
}