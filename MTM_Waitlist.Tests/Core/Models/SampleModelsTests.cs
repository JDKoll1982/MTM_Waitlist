using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Tests.Core.Models;

[TestClass]
public sealed class SampleModelsTests
{
    [TestMethod]
    public void SampleOrder_DefaultsAndDetailBindingsWork()
    {
        var order = new SampleOrder();

        Assert.AreEqual(0, order.Id);
        Assert.AreEqual(string.Empty, order.Title);
        Assert.AreEqual(string.Empty, order.Subtitle);
        Assert.AreEqual(string.Empty, order.Status);
        Assert.AreEqual(string.Empty, order.ImagePath);
        Assert.AreEqual(0, order.Fields.Count);
    }

    [TestMethod]
    public void SampleOrder_CanBePopulatedForDetailBinding()
    {
        var order = new SampleOrder
        {
            Id = 42,
            Title = "Order 42",
            Subtitle = "Acme",
            Status = "Waiting",
            ImagePath = "coil.png"
        };

        order.Fields.Add(new WaitlistField { Label = "Request type", Value = "Coil" });

        Assert.AreEqual(42, order.Id);
        Assert.AreEqual("Order 42", order.Title);
        Assert.AreEqual("Acme", order.Subtitle);
        Assert.AreEqual("Waiting", order.Status);
        Assert.AreEqual("coil.png", order.ImagePath);
        Assert.AreEqual(1, order.Fields.Count);
        Assert.AreEqual("Request type", order.Fields[0].Label);
        Assert.AreEqual("Coil", order.Fields[0].Value);
    }
}