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
            HasOutsideVendorOperation: true,
            NonConformingQuantity: 0m);

        Assert.AreEqual(RequestDisposition.OutsideService, RequestDispositionClassifier.Classify(input));
    }

    [TestMethod]
    public void Service_RequestDispositionClassifierTests_Classify_OutsideVendorOperationEvenWhenNoStockReturnsOutsideService()
    {
        var input = new RequestDispositionClassifier.DispositionInput(
            WorkOrderStatus: null,
            FinishedGoodsQuantity: 0m,
            OpenWorkOrderQuantity: 0m,
            HasOutsideVendorOperation: true,
            NonConformingQuantity: 0m);

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
            HasOutsideVendorOperation: false,
            NonConformingQuantity: 0m);

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
            HasOutsideVendorOperation: false,
            NonConformingQuantity: 0m);

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
            HasOutsideVendorOperation: false,
            NonConformingQuantity: 0m);

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
            HasOutsideVendorOperation: false,
            NonConformingQuantity: 0m);

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
            HasOutsideVendorOperation: false,
            NonConformingQuantity: 0m);

        Assert.AreEqual(RequestDisposition.WorkInProcess, RequestDispositionClassifier.Classify(input));
    }

    [TestMethod]
    public void Service_RequestDispositionClassifierTests_Classify_OpenStatusCodeReturnsWorkInProcess()
    {
        var input = new RequestDispositionClassifier.DispositionInput(
            WorkOrderStatus: "R",
            FinishedGoodsQuantity: 0m,
            OpenWorkOrderQuantity: 0m,
            HasOutsideVendorOperation: false,
            NonConformingQuantity: 0m);

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
            HasOutsideVendorOperation: false,
            NonConformingQuantity: 0m);

        Assert.AreEqual(RequestDisposition.Unknown, RequestDispositionClassifier.Classify(input));
    }

    // --- NCM: a state of the stock's location, never a production type ---

    [TestMethod]
    public void Service_RequestDispositionClassifierTests_Classify_StockAtAnNcmLocationReturnsNcm()
    {
        // The stock is sitting at a non-conforming location: the location is the signal, and there is no
        // production state to report for it (FR-093).
        var input = new RequestDispositionClassifier.DispositionInput(
            WorkOrderStatus: "R",
            FinishedGoodsQuantity: 0m,
            OpenWorkOrderQuantity: 0m,
            HasOutsideVendorOperation: false,
            NonConformingQuantity: 12m);

        Assert.AreEqual(RequestDisposition.Ncm, RequestDispositionClassifier.Classify(input));
    }

    [TestMethod]
    public void Service_RequestDispositionClassifierTests_Classify_StockAtAnNcmLocationIsNeverReadAsAProductionType()
    {
        // On hand at a finished-goods location and a closed order would be Finished Goods on its own, and an open
        // step would be WIP. Stock at an NCM location is neither: NCM is never treated as a production type
        // (FR-093).
        var looksLikeFinishedGoods = new RequestDispositionClassifier.DispositionInput(
            WorkOrderStatus: "C",
            FinishedGoodsQuantity: 120m,
            OpenWorkOrderQuantity: 0m,
            HasOutsideVendorOperation: false,
            NonConformingQuantity: 4m);

        var looksLikeWip = new RequestDispositionClassifier.DispositionInput(
            WorkOrderStatus: "R",
            FinishedGoodsQuantity: 0m,
            OpenWorkOrderQuantity: 33m,
            HasOutsideVendorOperation: false,
            NonConformingQuantity: 4m);

        Assert.AreEqual(RequestDisposition.Ncm, RequestDispositionClassifier.Classify(looksLikeFinishedGoods));
        Assert.AreEqual(RequestDisposition.Ncm, RequestDispositionClassifier.Classify(looksLikeWip));
    }

    [TestMethod]
    public void Service_RequestDispositionClassifierTests_Classify_OutsideServiceStillOutranksNcm()
    {
        // The documented priority is unchanged: a subcontracted operation is still decided first (FR-092).
        var input = new RequestDispositionClassifier.DispositionInput(
            WorkOrderStatus: "R",
            FinishedGoodsQuantity: 0m,
            OpenWorkOrderQuantity: 0m,
            HasOutsideVendorOperation: true,
            NonConformingQuantity: 40m);

        Assert.AreEqual(RequestDisposition.OutsideService, RequestDispositionClassifier.Classify(input));
    }

    [TestMethod]
    public void Service_RequestDispositionClassifierTests_Classify_NoStockAtAnNcmLocationIsNotNcm()
    {
        var input = new RequestDispositionClassifier.DispositionInput(
            WorkOrderStatus: "R",
            FinishedGoodsQuantity: 0m,
            OpenWorkOrderQuantity: 25m,
            HasOutsideVendorOperation: false,
            NonConformingQuantity: 0m);

        Assert.IsFalse(input.HasNonConformingStock);
        Assert.AreEqual(RequestDisposition.WorkInProcess, RequestDispositionClassifier.Classify(input));
    }

    [TestMethod]
    public void Service_RequestDispositionClassifierTests_TheClassifierInput_CarriesNoPartsRecordedItemType()
    {
        // FR-093: NCM is derived from the location the stock occupies and never from the part's recorded item
        // type. The way that is guaranteed is structural — there is no item type for the classifier to read.
        var inputMembers = typeof(RequestDispositionClassifier.DispositionInput)
            .GetProperties()
            .Select(property => property.Name)
            .ToArray();

        CollectionAssert.DoesNotContain(
            inputMembers,
            "ItemType",
            "The classifier must have no way to read a part's recorded item type.");

        var itemTypeShaped = inputMembers
            .Where(name => name.Replace("_", string.Empty).Contains("itemtype", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        CollectionAssert.AreEqual(
            Array.Empty<string>(),
            itemTypeShaped,
            $"No member of the input may be an item type, but found [{string.Join(", ", itemTypeShaped)}].");
    }

    // --- Status code matching is case-insensitive ---

    [TestMethod]
    public void Service_RequestDispositionClassifierTests_Classify_StatusCodeMatchingIsCaseInsensitive()
    {
        var open = new RequestDispositionClassifier.DispositionInput(
            WorkOrderStatus: "r", FinishedGoodsQuantity: 0m, OpenWorkOrderQuantity: 0m, HasOutsideVendorOperation: false, NonConformingQuantity: 0m);
        Assert.AreEqual(RequestDisposition.WorkInProcess, RequestDispositionClassifier.Classify(open));

        var closed = new RequestDispositionClassifier.DispositionInput(
            WorkOrderStatus: "c", FinishedGoodsQuantity: 50m, OpenWorkOrderQuantity: 0m, HasOutsideVendorOperation: false, NonConformingQuantity: 0m);
        Assert.AreEqual(RequestDisposition.FinishedGoods, RequestDispositionClassifier.Classify(closed));
    }
}
