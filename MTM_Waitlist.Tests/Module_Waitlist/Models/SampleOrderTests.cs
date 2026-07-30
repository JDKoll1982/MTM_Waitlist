using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Tests.Module_Waitlist.Models;

[TestClass]
public sealed class SampleOrderTests
{
    [TestMethod]
    public void SampleOrder_ExposesGenericBindingProperties()
    {
        var order = new SampleOrder
        {
            Title = "Material request",
            Subtitle = "Expo Drive",
            Status = "Ready",
            ImagePath = "coil.png"
        };

        order.Fields.Add(new WaitlistField { Label = "Request type", Value = "Coil" });

        Assert.AreEqual("Material request", order.Title);
        Assert.AreEqual("Expo Drive", order.Subtitle);
        Assert.AreEqual("Ready", order.Status);
        Assert.AreEqual("coil.png", order.ImagePath);
        Assert.AreEqual(1, order.Fields.Count);
        Assert.AreEqual("Request type", order.Fields[0].Label);
        Assert.AreEqual("Coil", order.Fields[0].Value);
    }
}
