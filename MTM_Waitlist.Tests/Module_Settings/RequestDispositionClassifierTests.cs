using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;

namespace MTM_Waitlist.Tests.Module_Settings;

[TestClass]
public sealed class RequestDispositionClassifierTests
{
    // --- Outside Service takes precedence ---

    [TestMethod]
    public void Classify_OutsideVendorOperation_ReturnsOutsideService()
    {
        var input = new RequestDispositionClassifier.DispositionInput(
            WorkOrderStatus: "O",
            FinishedGoodsQuantity: 10m,
            OpenWorkOrderQuantity: 5m,
            HasOutsideVendorOperation: true);

        Assert.AreEqual(RequestDisposition.OutsideService, RequestDispositionClassifier.Classify(input));
    }

    [TestMethod]
    public void Classify_OutsideVendorOperation_EvenWhenNoStock_ReturnsOutsideService()
    {
        var input = new RequestDispositionClassifier.DispositionInput(
            WorkOrderStatus: null,
            FinishedGoodsQuantity: 0m,
            OpenWorkOrderQuantity: 0m,
            HasOutsideVendorOperation: true);

        Assert.AreEqual(RequestDisposition.OutsideService, RequestDispositionClassifier.Classify(input));
    }

    // --- Finished Goods: closed status + on-hand, no open qty ---

    [TestMethod]
    public void Classify_ClosedWithOnHand_ReturnsFinishedGoods()
    {
        var input = new RequestDispositionClassifier.DispositionInput(
            WorkOrderStatus: "C",
            FinishedGoodsQuantity: 120m,
            OpenWorkOrderQuantity: 0m,
            HasOutsideVendorOperation: false);

        Assert.AreEqual(RequestDisposition.FinishedGoods, RequestDispositionClassifier.Classify(input));
    }

    [TestMethod]
    public void Classify_OnHandButOpenStatus_ReturnsWorkInProcess()
    {
        // Stock on hand but the order is still open => still being worked.
        var input = new RequestDispositionClassifier.DispositionInput(
            WorkOrderStatus: "O",
            FinishedGoodsQuantity: 120m,
            OpenWorkOrderQuantity: 0m,
            HasOutsideVendorOperation: false);

        Assert.AreEqual(RequestDisposition.WorkInProcess, RequestDispositionClassifier.Classify(input));
    }

    // --- Work In Process ---

    [TestMethod]
    public void Classify_OpenQuantity_ReturnsWorkInProcess()
    {
        var input = new RequestDispositionClassifier.DispositionInput(
            WorkOrderStatus: null,
            FinishedGoodsQuantity: 0m,
            OpenWorkOrderQuantity: 25m,
            HasOutsideVendorOperation: false);

        Assert.AreEqual(RequestDisposition.WorkInProcess, RequestDispositionClassifier.Classify(input));
    }

    [TestMethod]
    public void Classify_OpenStatusCode_ReturnsWorkInProcess()
    {
        var input = new RequestDispositionClassifier.DispositionInput(
            WorkOrderStatus: "R",
            FinishedGoodsQuantity: 0m,
            OpenWorkOrderQuantity: 0m,
            HasOutsideVendorOperation: false);

        Assert.AreEqual(RequestDisposition.WorkInProcess, RequestDispositionClassifier.Classify(input));
    }

    // --- Unknown ---

    [TestMethod]
    public void Classify_NoSignal_ReturnsUnknown()
    {
        var input = new RequestDispositionClassifier.DispositionInput(
            WorkOrderStatus: null,
            FinishedGoodsQuantity: 0m,
            OpenWorkOrderQuantity: 0m,
            HasOutsideVendorOperation: false);

        Assert.AreEqual(RequestDisposition.Unknown, RequestDispositionClassifier.Classify(input));
    }

    // --- Status code matching is case-insensitive ---

    [TestMethod]
    public void Classify_StatusCodeMatching_IsCaseInsensitive()
    {
        var open = new RequestDispositionClassifier.DispositionInput(
            WorkOrderStatus: "o", FinishedGoodsQuantity: 0m, OpenWorkOrderQuantity: 0m, HasOutsideVendorOperation: false);
        Assert.AreEqual(RequestDisposition.WorkInProcess, RequestDispositionClassifier.Classify(open));

        var closed = new RequestDispositionClassifier.DispositionInput(
            WorkOrderStatus: "c", FinishedGoodsQuantity: 50m, OpenWorkOrderQuantity: 0m, HasOutsideVendorOperation: false);
        Assert.AreEqual(RequestDisposition.FinishedGoods, RequestDispositionClassifier.Classify(closed));
    }
}
