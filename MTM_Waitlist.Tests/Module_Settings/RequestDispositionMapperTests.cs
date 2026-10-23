using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;

namespace MTM_Waitlist.Tests.Module_Settings;

[TestClass]
public sealed class RequestDispositionMapperTests
{
    // --- MapInforRow ---

    [TestMethod]
    public void Service_RequestDispositionMapperTests_MapInforRow_MapsRawRowToInforDispositionRow()
    {
        var row = new Dictionary<string, object?>
        {
            ["WorkOrderStatus"] = "R",
            ["OpenWorkOrderQuantity"] = 33m,
            ["FinishedGoodsQuantity"] = 54m,
            ["HasOutsideVendorOperation"] = 1L,
        };

        var mapped = RequestDispositionMapper.MapInforRow(row);

        Assert.IsNotNull(mapped);
        Assert.AreEqual("R", mapped!.WorkOrderStatus);
        Assert.AreEqual(33m, mapped.OpenWorkOrderQuantity);
        Assert.AreEqual(54m, mapped.FinishedGoodsQuantity);
        Assert.IsTrue(mapped.HasOutsideVendorOperation);
    }

    [TestMethod]
    public void Service_RequestDispositionMapperTests_MapInforRow_NullOrEmptyRowReturnsNull()
    {
        Assert.IsNull(RequestDispositionMapper.MapInforRow(null));
        Assert.IsNull(RequestDispositionMapper.MapInforRow(new Dictionary<string, object?>()));
    }

    [TestMethod]
    public void Service_RequestDispositionMapperTests_MapInforRow_BlankStatusReturnsNullStatus()
    {
        var mapped = RequestDispositionMapper.MapInforRow(new Dictionary<string, object?>
        {
            ["WorkOrderStatus"] = "   ",
            ["OpenWorkOrderQuantity"] = 0m,
            ["FinishedGoodsQuantity"] = 0m,
            ["HasOutsideVendorOperation"] = 0,
        });

        Assert.IsNotNull(mapped);
        Assert.IsNull(mapped!.WorkOrderStatus);
    }

    // --- BuildDispositionInput: Infor snapshot drives ---

    [TestMethod]
    public void Service_RequestDispositionMapperTests_BuildDispositionInput_InforReleasedOpenQuantityClassifiesWip()
    {
        var input = RequestDispositionMapper.BuildDispositionInput(
            new InforDispositionRow
            {
                WorkOrderStatus = "R",
                OpenWorkOrderQuantity = 33m,
                FinishedGoodsQuantity = 54m,
                HasOutsideVendorOperation = false,
            },
            floorSnapshot: null);

        Assert.AreEqual(RequestDisposition.WorkInProcess, RequestDispositionClassifier.Classify(input));
    }

    [TestMethod]
    public void Service_RequestDispositionMapperTests_BuildDispositionInput_InforClosedOnHandNoOpenClassifiesFinishedGoods()
    {
        var input = RequestDispositionMapper.BuildDispositionInput(
            new InforDispositionRow
            {
                WorkOrderStatus = "C",
                OpenWorkOrderQuantity = 0m,
                FinishedGoodsQuantity = 120m,
                HasOutsideVendorOperation = false,
            },
            floorSnapshot: null);

        Assert.AreEqual(RequestDisposition.FinishedGoods, RequestDispositionClassifier.Classify(input));
    }

    [TestMethod]
    public void Service_RequestDispositionMapperTests_BuildDispositionInput_InforOutsideOperationClassifiesOutsideService()
    {
        var input = RequestDispositionMapper.BuildDispositionInput(
            new InforDispositionRow
            {
                WorkOrderStatus = "R",
                OpenWorkOrderQuantity = 0m,
                FinishedGoodsQuantity = 0m,
                HasOutsideVendorOperation = true,
            },
            floorSnapshot: null);

        Assert.AreEqual(RequestDisposition.OutsideService, RequestDispositionClassifier.Classify(input));
    }

    [TestMethod]
    public void Service_RequestDispositionMapperTests_MapInforRow_CarriesTheRoutingColumns()
    {
        var mapped = RequestDispositionMapper.MapInforRow(new Dictionary<string, object?>
        {
            ["WorkOrderStatus"] = "R",
            ["OpenWorkOrderQuantity"] = 30_203m,
            ["FinishedGoodsQuantity"] = 0m,
            ["HasOutsideVendorOperation"] = 1L,
            ["NextOperationResourceId"] = "OUTSIDE_SERVICE",
            ["HasRemainingOperation"] = 1L,
        });

        Assert.IsNotNull(mapped);
        Assert.AreEqual("OUTSIDE_SERVICE", mapped!.NextOperationResourceId);
        Assert.AreEqual(true, mapped.HasRemainingOperation);
    }

    [TestMethod]
    public void Service_RequestDispositionMapperTests_MapInforRow_AnAbsentRoutingStaysUnread()
    {
        // NULL is "not read", which is a different answer from "no operation remains" — conflating them would
        // report Finished Goods for every part whose routing was never read.
        var mapped = RequestDispositionMapper.MapInforRow(new Dictionary<string, object?>
        {
            ["WorkOrderStatus"] = "R",
            ["OpenWorkOrderQuantity"] = 10m,
            ["FinishedGoodsQuantity"] = 0m,
            ["HasOutsideVendorOperation"] = 0,
        });

        Assert.IsNotNull(mapped);
        Assert.IsNull(mapped!.NextOperationResourceId);
        Assert.IsNull(mapped.HasRemainingOperation);
    }

    [TestMethod]
    public void Service_RequestDispositionMapperTests_BuildDispositionInput_CarriesTheRoutingThroughToTheClassifier()
    {
        var input = RequestDispositionMapper.BuildDispositionInput(
            new InforDispositionRow
            {
                WorkOrderStatus = "R",
                OpenWorkOrderQuantity = 30_203m,
                FinishedGoodsQuantity = 0m,
                HasOutsideVendorOperation = true,
                NextOperationResourceId = "OUTSIDE_SERVICE",
                HasRemainingOperation = true,
            },
            floorSnapshot: null);

        Assert.AreEqual("OUTSIDE_SERVICE", input.NextOperationResourceId);
        Assert.AreEqual(true, input.HasRemainingOperation);
        Assert.AreEqual(RequestDisposition.OutsideService, RequestDispositionClassifier.Classify(input));
    }

    [TestMethod]
    public void Service_RequestDispositionMapperTests_BuildDispositionInput_NoInforRowLeavesTheRoutingUnread()
    {
        var input = RequestDispositionMapper.BuildDispositionInput(inforRow: null, floorSnapshot: null);

        Assert.IsFalse(input.HasRouting);
    }

    // --- BuildDispositionInput: the WIP application never names the production type ---
    [TestMethod]
    public void Service_RequestDispositionMapperTests_BuildDispositionInput_TheFloorStoresWipQuantityNeverNamesWorkInProcess()
    {
        // A Visual read that returned nothing may not be answered from the WIP application's floor buckets: the
        // production type is Visual's to state, and the floor store is not the system of record for it
        // (operator instruction, 2026-10-23).
        var floor = new WipFloorQuantitySnapshot
        {
            FinishedGoodsFloorQuantity = 25m,
            OutsideServiceFloorQuantity = 74m,
            NonConformingFloorQuantity = 0m,
            WipFloorQuantity = 40m,
        };

        var input = RequestDispositionMapper.BuildDispositionInput(inforRow: null, floor);

        Assert.AreEqual(0m, input.FinishedGoodsQuantity);
        Assert.AreEqual(0m, input.OpenWorkOrderQuantity);
        Assert.IsFalse(input.HasOutsideVendorOperation);
        Assert.AreEqual(RequestDisposition.Unknown, RequestDispositionClassifier.Classify(input));
    }

    [TestMethod]
    public void Service_RequestDispositionMapperTests_BuildDispositionInput_TheVisualRowAloneDecidesAndTheFloorDoesNotOverrideIt()
    {
        // Visual says closed with stock on hand and no outside operation, so the product is a finished good even
        // though the floor store happens to hold outside-service quantity for the same part.
        var floor = new WipFloorQuantitySnapshot
        {
            FinishedGoodsFloorQuantity = 25m,
            OutsideServiceFloorQuantity = 10m,
            WipFloorQuantity = 40m,
        };

        var input = RequestDispositionMapper.BuildDispositionInput(
            new InforDispositionRow
            {
                WorkOrderStatus = "C",
                OpenWorkOrderQuantity = 0m,
                FinishedGoodsQuantity = 50m,
                HasOutsideVendorOperation = false,
            },
            floor);

        Assert.AreEqual(RequestDisposition.FinishedGoods, RequestDispositionClassifier.Classify(input));
    }

    [TestMethod]
    public void Service_RequestDispositionMapperTests_BuildDispositionInput_AnEmptyVisualRowIsNotFilledInFromTheFloor()
    {
        // Visual reports a closed order with no stock. The floor's finished-goods bucket may not fill that gap: the
        // figure the classifier sees is Visual's.
        var floor = new WipFloorQuantitySnapshot { FinishedGoodsFloorQuantity = 25m };
        var input = RequestDispositionMapper.BuildDispositionInput(
            new InforDispositionRow
            {
                WorkOrderStatus = "C",
                OpenWorkOrderQuantity = 0m,
                FinishedGoodsQuantity = 0m,
                HasOutsideVendorOperation = false,
            },
            floor);

        Assert.AreEqual(0m, input.FinishedGoodsQuantity);
        Assert.AreEqual(RequestDisposition.Unknown, RequestDispositionClassifier.Classify(input));
    }

    // --- BuildDispositionInput: the non-conforming (NCM) location signal ---

    [TestMethod]
    public void Service_RequestDispositionMapperTests_BuildDispositionInput_FloorStockAtAnNcmLocationClassifiesNcm()
    {
        // The location the stock occupies is the signal, and it reaches the classifier through the floor
        // buckets, which classify each location (`md_locations.Location LIKE 'NCM%'`). FR-093.
        var floor = new WipFloorQuantitySnapshot
        {
            FinishedGoodsFloorQuantity = 0m,
            OutsideServiceFloorQuantity = 0m,
            NonConformingFloorQuantity = 74m,
            WipFloorQuantity = 0m,
        };

        var input = RequestDispositionMapper.BuildDispositionInput(inforRow: null, floor);

        Assert.IsTrue(input.HasNonConformingStock);
        Assert.AreEqual(RequestDisposition.Ncm, RequestDispositionClassifier.Classify(input));
    }

    [TestMethod]
    public void Service_RequestDispositionMapperTests_BuildDispositionInput_StockAtAnNcmLocationIsNeverReadAsAProductionType()
    {
        // Infor reports a closed order with stock on hand, which on its own would be Finished Goods. The stock is
        // physically in an NCM area, so the location decides and the production state is not reported (FR-093).
        var floor = new WipFloorQuantitySnapshot { NonConformingFloorQuantity = 6m };
        var input = RequestDispositionMapper.BuildDispositionInput(
            new InforDispositionRow
            {
                WorkOrderStatus = "C",
                OpenWorkOrderQuantity = 0m,
                FinishedGoodsQuantity = 50m,
                HasOutsideVendorOperation = false,
            },
            floor);

        Assert.AreEqual(RequestDisposition.Ncm, RequestDispositionClassifier.Classify(input));
    }

    [TestMethod]
    public void Service_RequestDispositionMapperTests_BuildDispositionInput_NoStockAtAnNcmLocationIsNotNcm()
    {
        var floor = new WipFloorQuantitySnapshot { WipFloorQuantity = 40m };

        var input = RequestDispositionMapper.BuildDispositionInput(inforRow: null, floor);

        Assert.IsFalse(input.HasNonConformingStock);
        Assert.AreEqual(
            RequestDisposition.Unknown,
            RequestDispositionClassifier.Classify(input),
            "A part with no Visual row and no NCM stock has nothing that may name its product type.");
    }

    [TestMethod]
    public void Service_RequestDispositionMapperTests_BuildDispositionInput_NoFloorSnapshotCarriesNoNcmSignal()
    {
        var input = RequestDispositionMapper.BuildDispositionInput(
            new InforDispositionRow
            {
                WorkOrderStatus = "R",
                OpenWorkOrderQuantity = 0m,
                FinishedGoodsQuantity = 0m,
                HasOutsideVendorOperation = false,
                HasRemainingOperation = true,
            },
            floorSnapshot: null);

        Assert.IsFalse(input.HasNonConformingStock);
        Assert.AreEqual(RequestDisposition.WorkInProcess, RequestDispositionClassifier.Classify(input));
    }
}
