using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Core.Models;

namespace MTM_Waitlist.Tests.Core.Models;

[TestClass]
public sealed class SampleModelsTests
{
    [TestMethod]
    public void SampleOrder_ComputedPropertiesAndDefaultsWork()
    {
        var order = new SampleOrder
        {
            OrderID = 42,
            SymbolCode = 65,
            Company = "Acme",
            Status = "Waiting"
        };

        Assert.AreEqual('A', order.Symbol);
        Assert.AreEqual("Order ID: 42", order.ShortDescription);
        Assert.AreEqual("Acme Waiting", order.ToString());
        Assert.IsNotNull(order.Details);
        Assert.AreEqual(0, order.Details.Count);
        Assert.AreEqual(string.Empty, order.ShipperName);
        Assert.AreEqual(string.Empty, order.ImageIconPath);
    }

    [TestMethod]
    public void SampleOrderDetail_ComputedDescriptionAndDefaultsWork()
    {
        var detail = new SampleOrderDetail
        {
            ProductID = 7,
            ProductName = "Bracket"
        };

        Assert.AreEqual("Product ID: 7 - Bracket", detail.ShortDescription);
        Assert.AreEqual(string.Empty, detail.QuantityPerUnit);
        Assert.AreEqual(string.Empty, detail.CategoryName);
        Assert.AreEqual(string.Empty, detail.CategoryDescription);
    }

    [TestMethod]
    public void SampleCompany_DefaultOrdersCollectionIsInitialized()
    {
        var company = new SampleCompany
        {
            CompanyID = "C001",
            CompanyName = "Acme"
        };

        Assert.IsNotNull(company.Orders);
        Assert.AreEqual(0, company.Orders.Count);
        Assert.AreEqual(string.Empty, company.ContactName);
        Assert.AreEqual("Acme", company.CompanyName);
    }
}