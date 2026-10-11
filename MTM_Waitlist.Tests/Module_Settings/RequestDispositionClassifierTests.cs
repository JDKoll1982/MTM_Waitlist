using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;

namespace MTM_Waitlist.Tests.Module_Settings;

[TestClass]
public sealed class RequestDispositionClassifierTests
{
    // --- Outside Service takes precedence ---

    [TestMethod]
    public void Service_RequestDispositionClassifierTests_Classify_OutsideVendorOperationReturnsOutsideService()
    {
        var input = new RequestDispositionClassifier.DispositionInput(
            WorkOrderStatus: "R",
            FinishedGoodsQuantity: 10m,
            OpenWorkOrderQuantity: 5m,
            HasOutsideVendorOperation: true);

        Assert.AreEqual(RequestDisposition.OutsideService, RequestDispositionClassifier.Classify(input));
    }

    [TestMethod]
    public void Service_RequestDispositionClassifierTests_Classify_OutsideVendorOperationEvenWhenNoStockReturnsOutsideService()
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
    public void Service_RequestDispositionClassifierTests_Classify_ClosedWithOnHandReturnsFinishedGoods()
    {
        var input = new RequestDispositionClassifier.DispositionInput(
            WorkOrderStatus: "C",
            FinishedGoodsQuantity: 120m,
            OpenWorkOrderQuantity: 0m,
            HasOutsideVendorOperation: false);

        Assert.AreEqual(RequestDisposition.FinishedGoods, RequestDispositionClassifier.Classify(input));
    }

    [TestMethod]
    public void Service_RequestDispositionClassifierTests_Classify_OnHandButOpenStatusReturnsWorkInProcess()
    {
        // Stock on hand but the order is still open (Released) => still being worked.
        var input = new RequestDispositionClassifier.DispositionInput(
            WorkOrderStatus: "R",
            FinishedGoodsQuantity: 120m,
            OpenWorkOrderQuantity: 0m,
            HasOutsideVendorOperation: false);

        Assert.AreEqual(RequestDisposition.WorkInProcess, RequestDispositionClassifier.Classify(input));
    }

    [TestMethod]
    public void Service_RequestDispositionClassifierTests_Classify_UnreleasedStatusReturnsWorkInProcess()
    {
        // 'U' (Unreleased) is a confirmed open code (42k rows in WORK_ORDER).
        var input = new RequestDispositionClassifier.DispositionInput(
            WorkOrderStatus: "U",
            FinishedGoodsQuantity: 0m,
            OpenWorkOrderQuantity: 0m,
            HasOutsideVendorOperation: false);

        Assert.AreEqual(RequestDisposition.WorkInProcess, RequestDispositionClassifier.Classify(input));
    }

    [TestMethod]
    public void Service_RequestDispositionClassifierTests_Classify_CancelledStatusWithOnHandReturnsUnknownNotFinishedGoods()
    {
        // 'X' (Cancelled) is deliberately in neither set: on-hand stock under a cancelled order must
        // not classify as Finished Goods (and with no open qty it is Unknown).
        var input = new RequestDispositionClassifier.DispositionInput(
            WorkOrderStatus: "X",
            FinishedGoodsQuantity: 80m,
            OpenWorkOrderQuantity: 0m,
            HasOutsideVendorOperation: false);

        Assert.AreEqual(RequestDisposition.Unknown, RequestDispositionClassifier.Classify(input));
    }

    // --- Work In Process ---

    [TestMethod]
    public void Service_RequestDispositionClassifierTests_Classify_OpenQuantityReturnsWorkInProcess()
    {
        var input = new RequestDispositionClassifier.DispositionInput(
            WorkOrderStatus: null,
            FinishedGoodsQuantity: 0m,
            OpenWorkOrderQuantity: 25m,
            HasOutsideVendorOperation: false);

        Assert.AreEqual(RequestDisposition.WorkInProcess, RequestDispositionClassifier.Classify(input));
    }

    [TestMethod]
    public void Service_RequestDispositionClassifierTests_Classify_OpenStatusCodeReturnsWorkInProcess()
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
    public void Service_RequestDispositionClassifierTests_Classify_NoSignalReturnsUnknown()
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
    public void Service_RequestDispositionClassifierTests_Classify_StatusCodeMatchingIsCaseInsensitive()
    {
        var open = new RequestDispositionClassifier.DispositionInput(
            WorkOrderStatus: "r", FinishedGoodsQuantity: 0m, OpenWorkOrderQuantity: 0m, HasOutsideVendorOperation: false);
        Assert.AreEqual(RequestDisposition.WorkInProcess, RequestDispositionClassifier.Classify(open));

        var closed = new RequestDispositionClassifier.DispositionInput(
            WorkOrderStatus: "c", FinishedGoodsQuantity: 50m, OpenWorkOrderQuantity: 0m, HasOutsideVendorOperation: false);
        Assert.AreEqual(RequestDisposition.FinishedGoods, RequestDispositionClassifier.Classify(closed));
    }
}
