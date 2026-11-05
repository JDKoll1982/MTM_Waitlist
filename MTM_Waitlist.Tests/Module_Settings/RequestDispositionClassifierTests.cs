using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;

namespace MTM_Waitlist.Tests.Module_Settings;

[TestClass]
public sealed class RequestDispositionClassifierTests
{
    // --- Outside service is a next resource, not a type (IT, 2026-11-04) ---

    [TestMethod]
    public void Service_RequestDispositionClassifierTests_Classify_AnOutsideVendorOperationIsWorkInProcess()
    {
        var input = new RequestDispositionClassifier.DispositionInput(
            WorkOrderStatus: "R",
            FinishedGoodsQuantity: 10m,
            OpenWorkOrderQuantity: 5m,
            HasOutsideVendorOperation: true,
            NonConformingQuantity: 0m);

        Assert.AreEqual(RequestDisposition.WorkInProcess, RequestDispositionClassifier.Classify(input));
        Assert.IsTrue(input.IsOutsideServiceWork, "the pickup signal survives even though the type is work in process");
    }

    [TestMethod]
    public void Service_RequestDispositionClassifierTests_Classify_OutsideVendorOperationWithNothingElseReadStaysUnknown()
    {
        // The vendor fields alone say the work is going out; they say nothing about the type, and nothing else was
        // read. The pickup still lights from the signal, and the type cell is left out rather than guessed (FR-089).
        var input = new RequestDispositionClassifier.DispositionInput(
            WorkOrderStatus: null,
            FinishedGoodsQuantity: 0m,
            OpenWorkOrderQuantity: 0m,
            HasOutsideVendorOperation: true,
            NonConformingQuantity: 0m);

        Assert.AreEqual(RequestDisposition.Unknown, RequestDispositionClassifier.Classify(input));
        Assert.IsTrue(input.IsOutsideServiceWork);
    }

    // --- The routing decides: the operation that follows the job's own (research 2026-10-23) ---

    [TestMethod]
    public void Service_RequestDispositionClassifierTests_Classify_NextOperationIsTheOutsideServiceResourceIsWorkInProcess()
    {
        // Outside Service is the plant's OUTSIDE_SERVICE shop resource being the next operation, whether or not
        // the vendor fields were read: the routing is the signal the tag print itself uses, and the tag it produces
        // is the WIP tag with "Outside Service" on its next-resource line.
        var input = new RequestDispositionClassifier.DispositionInput(
            WorkOrderStatus: "R",
            FinishedGoodsQuantity: 0m,
            OpenWorkOrderQuantity: 100m,
            HasOutsideVendorOperation: false,
            NonConformingQuantity: 0m,
            NextOperationResourceId: " outside_service ",
            HasRemainingOperation: true);

        Assert.AreEqual(RequestDisposition.WorkInProcess, RequestDispositionClassifier.Classify(input));
        Assert.IsTrue(input.IsOutsideServiceWork, "the subcontracted pickup is still offered");
    }

    [TestMethod]
    public void Service_RequestDispositionClassifierTests_Classify_TheLastOperationWithSubcontractedWorkBehindItIsFinishedGoods()
    {
        // The routing ends here, so the plant prints the FG tag — the vendor fields left on the order are history
        // and decide nothing. Only the pickup signal still reads them, and only while the routing is unread.
        var finishedInput = new RequestDispositionClassifier.DispositionInput(
            WorkOrderStatus: "R",
            FinishedGoodsQuantity: 0m,
            OpenWorkOrderQuantity: 100m,
            HasOutsideVendorOperation: true,
            NonConformingQuantity: 0m,
            NextOperationResourceId: null,
            HasRemainingOperation: false,
            HasCustomerDemand: true);

        Assert.AreEqual(RequestDisposition.FinishedGoods, RequestDispositionClassifier.Classify(finishedInput));
        Assert.IsFalse(finishedInput.IsOutsideServiceWork, "with the routing read, nothing is subcontracted after this op");
    }

    [TestMethod]
    public void Service_RequestDispositionClassifierTests_Classify_ACompletedSubcontractedOperationNoLongerReadsAsOutsideService()
    {
        // The vendor fields stay on the order for ever, so a part that has already been out and come back would
        // read as subcontracted work on the legacy signal alone. With the routing read, what follows decides.
        var wipInput = new RequestDispositionClassifier.DispositionInput(
            WorkOrderStatus: "R",
            FinishedGoodsQuantity: 0m,
            OpenWorkOrderQuantity: 100m,
            HasOutsideVendorOperation: true,
            NonConformingQuantity: 0m,
            NextOperationResourceId: "135-23",
            HasRemainingOperation: true);

        Assert.AreEqual(RequestDisposition.WorkInProcess, RequestDispositionClassifier.Classify(wipInput));
        Assert.IsFalse(wipInput.IsOutsideServiceWork);

        var finishedInput = wipInput with
        {
            NextOperationResourceId = null,
            HasRemainingOperation = false,
            HasCustomerDemand = true,
        };

        Assert.AreEqual(RequestDisposition.FinishedGoods, RequestDispositionClassifier.Classify(finishedInput));
    }

    [TestMethod]
    public void Service_RequestDispositionClassifierTests_Classify_TheLastOperationOnAPartNothingBuysIsStillWorkInProcess()
    {
        // WO-074654 / 100-17: the press is the last operation and 130 of 131 pieces are done, but the part exists
        // only to feed open WO-073735 — no customer-order line anywhere. That is work in process (2026-10-23).
        var feedsAnotherJob = new RequestDispositionClassifier.DispositionInput(
            WorkOrderStatus: "R",
            FinishedGoodsQuantity: 0m,
            OpenWorkOrderQuantity: 3m,
            HasOutsideVendorOperation: false,
            NonConformingQuantity: 0m,
            NextOperationResourceId: null,
            HasRemainingOperation: false,
            HasCustomerDemand: false);

        Assert.AreEqual(RequestDisposition.WorkInProcess, RequestDispositionClassifier.Classify(feedsAnotherJob));
    }

    [TestMethod]
    public void Service_RequestDispositionClassifierTests_Classify_TheLastOperationOnAPartThePlantSellsIsFinishedGoods()
    {
        // WO-074962 / 100-08: the press is the last operation and still running at 8,409 of 10,000, but the part
        // has 180 customer-order lines — it is the end product, so the job's output is Finished Goods even while
        // the operation is open (2026-10-23).
        var soldPart = new RequestDispositionClassifier.DispositionInput(
            WorkOrderStatus: "R",
            FinishedGoodsQuantity: 0m,
            OpenWorkOrderQuantity: 1_763m,
            HasOutsideVendorOperation: false,
            NonConformingQuantity: 0m,
            NextOperationResourceId: null,
            HasRemainingOperation: false,
            HasCustomerDemand: true);

        Assert.AreEqual(RequestDisposition.FinishedGoods, RequestDispositionClassifier.Classify(soldPart));
    }

    [TestMethod]
    public void Service_RequestDispositionClassifierTests_Classify_ARemainingOperationKeepsASoldPartInProcess()
    {
        // Customer demand does not promote a part that still has work ahead of it: a sold part at an operation
        // with another one after it has not been made yet.
        var soldButUnfinished = new RequestDispositionClassifier.DispositionInput(
            WorkOrderStatus: "R",
            FinishedGoodsQuantity: 0m,
            OpenWorkOrderQuantity: 1_763m,
            HasOutsideVendorOperation: false,
            NonConformingQuantity: 0m,
            NextOperationResourceId: "135-23",
            HasRemainingOperation: true,
            HasCustomerDemand: true);

        Assert.AreEqual(RequestDisposition.WorkInProcess, RequestDispositionClassifier.Classify(soldButUnfinished));
    }

    [TestMethod]
    public void Service_RequestDispositionClassifierTests_Classify_NoOperationAfterASoldPartReturnsFinishedGoodsEvenWhileTheOrderIsOpen()
    {
        // Nothing follows the job's own operation and the part is one the plant sells, so what the job produced is
        // a finished good. The order itself may still be open — a lot can complete before its order does.
        var input = new RequestDispositionClassifier.DispositionInput(
            WorkOrderStatus: "R",
            FinishedGoodsQuantity: 0m,
            OpenWorkOrderQuantity: 12_000m,
            HasOutsideVendorOperation: false,
            NonConformingQuantity: 0m,
            NextOperationResourceId: null,
            HasRemainingOperation: false,
            HasCustomerDemand: true);

        Assert.AreEqual(RequestDisposition.FinishedGoods, RequestDispositionClassifier.Classify(input));
    }

    [TestMethod]
    public void Service_RequestDispositionClassifierTests_Classify_ARemainingInHouseOperationReturnsWorkInProcess()
    {
        var input = new RequestDispositionClassifier.DispositionInput(
            WorkOrderStatus: "R",
            FinishedGoodsQuantity: 0m,
            OpenWorkOrderQuantity: 12_000m,
            HasOutsideVendorOperation: false,
            NonConformingQuantity: 0m,
            NextOperationResourceId: "V880-GRP",
            HasRemainingOperation: true);

        Assert.AreEqual(RequestDisposition.WorkInProcess, RequestDispositionClassifier.Classify(input));
    }

    [TestMethod]
    public void Service_RequestDispositionClassifierTests_Classify_NonConformingStockOutranksTheRoutingProductionTypes()
    {
        // FR-093: quarantined stock is never reported as a production type, so the location still wins over a
        // routing that would otherwise say WIP.
        var input = new RequestDispositionClassifier.DispositionInput(
            WorkOrderStatus: "R",
            FinishedGoodsQuantity: 0m,
            OpenWorkOrderQuantity: 100m,
            HasOutsideVendorOperation: false,
            NonConformingQuantity: 25m,
            NextOperationResourceId: "135-23",
            HasRemainingOperation: true);

        Assert.AreEqual(RequestDisposition.Ncm, RequestDispositionClassifier.Classify(input));
    }

    [TestMethod]
    public void Service_RequestDispositionClassifierTests_Classify_AnUnreadRoutingFallsBackToTheStockAndQuantityArms()
    {
        // The floor-stock half and a cached row written before the routing was read carry no routing at all, and
        // must answer exactly as they did before it existed.
        var open = new RequestDispositionClassifier.DispositionInput(
            WorkOrderStatus: "R",
            FinishedGoodsQuantity: 0m,
            OpenWorkOrderQuantity: 100m,
            HasOutsideVendorOperation: false,
            NonConformingQuantity: 0m);

        Assert.IsFalse(open.HasRouting);
        Assert.AreEqual(RequestDisposition.WorkInProcess, RequestDispositionClassifier.Classify(open));

        var closedWithStock = open with
        {
            WorkOrderStatus = "C",
            OpenWorkOrderQuantity = 0m,
            FinishedGoodsQuantity = 40m,
        };

        Assert.AreEqual(RequestDisposition.FinishedGoods, RequestDispositionClassifier.Classify(closedWithStock));
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
    public void Service_RequestDispositionClassifierTests_Classify_NcmOutranksTheProductionArms()
    {
        // NCM is a location state, never a production type (FR-093), so quarantined stock is never reported as
        // Finished Goods or WIP — and a subcontracted next operation does not change that. Its pickup signal still
        // stands, which is how the subcontracted pickup outranks the NCM one on the picker.
        var input = new RequestDispositionClassifier.DispositionInput(
            WorkOrderStatus: "R",
            FinishedGoodsQuantity: 0m,
            OpenWorkOrderQuantity: 0m,
            HasOutsideVendorOperation: true,
            NonConformingQuantity: 40m);

        Assert.AreEqual(RequestDisposition.Ncm, RequestDispositionClassifier.Classify(input));
        Assert.IsTrue(input.IsOutsideServiceWork);
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
