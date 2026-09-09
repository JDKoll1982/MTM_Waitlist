using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;

namespace MTM_Waitlist.Tests.Module_Settings.Services;

[TestClass]
public sealed class RequestItemPickerRulesTests
{
    [TestMethod]
    public void EveryDeliverItem_IsDeliverDestinationWorkCenter()
    {
        var deliver = RequestItemCatalog.GetByCategory(RequestCategory.Deliver);
        Assert.AreEqual(8, deliver.Count);
        foreach (var item in deliver)
        {
            Assert.IsTrue(RequestItemPickerRules.IsDeliverDestinationWorkCenter(item), item.Id);
        }
    }

    [TestMethod]
    public void NonDeliverItem_IsNotDeliverDestinationWorkCenter()
    {
        var pickupCoil = RequestItemCatalog.FindById("pickup-coil")!;
        Assert.IsFalse(RequestItemPickerRules.IsDeliverDestinationWorkCenter(pickupCoil));
    }

    [TestMethod]
    public void JobPartItem_VisibleOnlyWhenJobHasThatPart()
    {
        var empty = RequestJobPartAvailability.None;
        var withCoil = RequestJobPartAvailability.All with { HasFlatstock = false, HasDie = false, HasComponent = false, HasDunnage = false };

        var coil = RequestItemCatalog.FindById("pickup-coil")!;
        var die = RequestItemCatalog.FindById("deliver-die")!;
        var dunnage = RequestItemCatalog.FindById("deliver-dunnage")!;

        Assert.IsFalse(RequestItemPickerRules.IsVisible(coil, empty));
        Assert.IsTrue(RequestItemPickerRules.IsVisible(coil, withCoil));
        Assert.IsFalse(RequestItemPickerRules.IsVisible(die, withCoil), "Die must not show when the job has no die.");
        Assert.IsFalse(RequestItemPickerRules.IsVisible(dunnage, withCoil), "Dunnage must not show when the job has no dunnage.");
    }

    [TestMethod]
    public void FlatstockAndDieAndDunnage_EvaluateIndependently()
    {
        var onlyFlatstock = RequestJobPartAvailability.All with
        {
            HasCoil = false,
            HasDie = false,
            HasComponent = false,
            HasDunnage = false,
        };

        Assert.IsTrue(RequestItemPickerRules.IsVisible(RequestItemCatalog.FindById("deliver-flatstock")!, onlyFlatstock));
        Assert.IsFalse(RequestItemPickerRules.IsVisible(RequestItemCatalog.FindById("deliver-coil")!, onlyFlatstock));
        Assert.IsFalse(RequestItemPickerRules.IsVisible(RequestItemCatalog.FindById("deliver-die")!, onlyFlatstock));
    }

    [TestMethod]
    public void ManualEquipment_AlwaysVisible_EvenWithNoJobParts()
    {
        var empty = RequestJobPartAvailability.None;
        foreach (var id in new[] { "pickup-riser-table", "deliver-riser-table", "pickup-hopper", "deliver-hopper" })
        {
            var item = RequestItemCatalog.FindById(id)!;
            Assert.IsTrue(RequestItemPickerRules.IsManualEquipment(item), id);
            Assert.IsTrue(RequestItemPickerRules.IsVisible(item, empty), id);
        }
    }

    [TestMethod]
    public void ScrapAndOther_AlwaysVisible()
    {
        var empty = RequestJobPartAvailability.None;
        Assert.IsTrue(RequestItemPickerRules.IsVisible(RequestItemCatalog.FindById("pickup-scrap")!, empty));
        Assert.IsTrue(RequestItemPickerRules.IsVisible(RequestItemCatalog.FindById("other")!, empty));
    }

    [TestMethod]
    public void FinishedProductItems_RequireActiveJob()
    {
        var noJob = RequestJobPartAvailability.None;
        var withJob = RequestJobPartAvailability.All with
        {
            HasCoil = false,
            HasFlatstock = false,
            HasDie = false,
            HasComponent = false,
            HasDunnage = false,
        };

        foreach (var id in new[] { "pickup-fg", "pickup-wip", "pickup-outside-service", "pickup-ncm" })
        {
            var item = RequestItemCatalog.FindById(id)!;
            Assert.IsFalse(RequestItemPickerRules.IsVisible(item, noJob), id);
            Assert.IsTrue(RequestItemPickerRules.IsVisible(item, withJob), id);
        }
    }

    [TestMethod]
    public void AssistTableItems_VisibleWhenAnySubordinatePartPresent()
    {
        var noParts = RequestJobPartAvailability.None;
        var withComponent = RequestJobPartAvailability.All with
        {
            HasCoil = false,
            HasFlatstock = false,
            HasDie = false,
            HasDunnage = false,
        };

        var place = RequestItemCatalog.FindById("assist-table-place")!;
        var remove = RequestItemCatalog.FindById("assist-table-remove")!;
        Assert.IsFalse(RequestItemPickerRules.IsVisible(place, noParts));
        Assert.IsFalse(RequestItemPickerRules.IsVisible(remove, noParts));
        Assert.IsTrue(RequestItemPickerRules.IsVisible(place, withComponent));
        Assert.IsTrue(RequestItemPickerRules.IsVisible(remove, withComponent));
    }

    [TestMethod]
    public void EveryCatalogItem_HasARuleWithoutThrowing()
    {
        // Regression guard: the RequiredJobPart/IsVisible rules must handle all 23 canonical rows.
        foreach (var item in RequestItemCatalog.Items)
        {
            _ = RequestItemPickerRules.RequiredJobPart(item);
            _ = RequestItemPickerRules.IsManualEquipment(item);
            _ = RequestItemPickerRules.IsDeliverDestinationWorkCenter(item);
            _ = RequestItemPickerRules.IsVisible(item, RequestJobPartAvailability.None);
        }
    }
}
