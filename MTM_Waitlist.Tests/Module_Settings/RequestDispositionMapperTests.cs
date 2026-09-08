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
    public void MapInforRow_MapsRawRow_ToInforDispositionRow()
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
    public void MapInforRow_NullOrEmptyRow_ReturnsNull()
    {
        Assert.IsNull(RequestDispositionMapper.MapInforRow(null));
        Assert.IsNull(RequestDispositionMapper.MapInforRow(new Dictionary<string, object?>()));
    }

    [TestMethod]
    public void MapInforRow_BlankStatus_ReturnsNullStatus()
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
    public void BuildDispositionInput_InforReleasedOpenQuantity_ClassifiesWip()
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
    public void BuildDispositionInput_InforClosedOnHandNoOpen_ClassifiesFinishedGoods()
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
    public void BuildDispositionInput_InforOutsideOperation_ClassifiesOutsideService()
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

    // --- BuildDispositionInput: floor snapshot fallback / confirmation ---

    [TestMethod]
    public void BuildDispositionInput_NoInforRow_FloorWipOnly_ClassifiesWip()
    {
        var floor = new WipFloorQuantitySnapshot
        {
            FinishedGoodsFloorQuantity = 0m,
            OutsideServiceFloorQuantity = 0m,
            NonConformingFloorQuantity = 0m,
            WipFloorQuantity = 40m,
        };

        var input = RequestDispositionMapper.BuildDispositionInput(inforRow: null, floor);

        Assert.AreEqual(RequestDisposition.WorkInProcess, RequestDispositionClassifier.Classify(input));
    }

    [TestMethod]
    public void BuildDispositionInput_NoInforRow_FloorOutsideService_ClassifiesOutsideService()
    {
        var floor = new WipFloorQuantitySnapshot
        {
            FinishedGoodsFloorQuantity = 0m,
            OutsideServiceFloorQuantity = 74m,
            NonConformingFloorQuantity = 0m,
            WipFloorQuantity = 0m,
        };

        var input = RequestDispositionMapper.BuildDispositionInput(inforRow: null, floor);

        Assert.AreEqual(RequestDisposition.OutsideService, RequestDispositionClassifier.Classify(input));
    }

    [TestMethod]
    public void BuildDispositionInput_FloorOutsideService_OverridesClosedStatus()
    {
        // Infor says closed + on hand (would be FG), but the part is physically staged at O/S on the floor.
        var floor = new WipFloorQuantitySnapshot { OutsideServiceFloorQuantity = 10m };
        var input = RequestDispositionMapper.BuildDispositionInput(
            new InforDispositionRow
            {
                WorkOrderStatus = "C",
                OpenWorkOrderQuantity = 0m,
                FinishedGoodsQuantity = 50m,
                HasOutsideVendorOperation = false,
            },
            floor);

        Assert.AreEqual(RequestDisposition.OutsideService, RequestDispositionClassifier.Classify(input));
    }

    [TestMethod]
    public void BuildDispositionInput_InforClosedZeroOnHand_FloorFinishedGoodsFillsGap()
    {
        // Infor has no on-hand recorded but closed, and the floor shows finished-goods stock.
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

        Assert.AreEqual(RequestDisposition.FinishedGoods, RequestDispositionClassifier.Classify(input));
    }

    [TestMethod]
    public void BuildDispositionInput_NoInforRow_FloorFinishedGoodsOnly_ClassifiesUnknown()
    {
        // Floor FG stock alone cannot prove the order is closed -> Unknown (documented boundary).
        var floor = new WipFloorQuantitySnapshot { FinishedGoodsFloorQuantity = 25m };

        var input = RequestDispositionMapper.BuildDispositionInput(inforRow: null, floor);

        Assert.AreEqual(RequestDisposition.Unknown, RequestDispositionClassifier.Classify(input));
    }
}
