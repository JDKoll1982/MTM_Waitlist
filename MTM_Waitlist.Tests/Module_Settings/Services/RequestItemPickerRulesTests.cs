using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Services;
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
        Assert.AreEqual(9, deliver.Count);
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
    public void DieItems_AreNotOfferedWhenTheJobHasNoDie()
    {
        // The picker reads HasDie (FR-055). A job whose only die row is the query's 'No Die' placeholder has no
        // die: the composition root derives HasDie from SetupActiveJobSnapshot.RealDies, whose rule is pinned by
        // DieDecisionRulesTests and SetupActiveJobSnapshotDieTests. This is the picker half of that chain — no die
        // means neither Die Item is offered, in either Category, and the Category itself disappears when nothing
        // else under it is visible either.
        var noDie = RequestJobPartAvailability.All with { HasDie = false };

        Assert.IsFalse(RequestItemPickerRules.IsVisible(RequestItemCatalog.FindById("pickup-die")!, noDie));
        Assert.IsFalse(RequestItemPickerRules.IsVisible(RequestItemCatalog.FindById("deliver-die")!, noDie));
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
    public void ScrapItem_IsGatedOnARealScrapDecision_ThreeWaysPlusUnset()
    {
        var scrap = RequestItemCatalog.FindById("pickup-scrap")!;

        Assert.IsTrue(
            RequestItemPickerRules.IsVisible(scrap, WithScrap("Steel Offal")),
            "A real scrap type means a decision was made and something has to be collected.");
        Assert.IsFalse(
            RequestItemPickerRules.IsVisible(scrap, WithScrap(ScrapDecisionRules.NoScrap)),
            "'No Scrap' is a real answer meaning there is nothing to collect.");
        Assert.IsFalse(
            RequestItemPickerRules.IsVisible(scrap, WithScrap(ScrapDecisionRules.RequiredPlaceholder)),
            "The placeholder means no decision was made, so it is never presented as a scrap type.");
        Assert.IsFalse(
            RequestItemPickerRules.IsVisible(scrap, WithScrap(null)),
            "An unset scrap value is no decision either.");
        Assert.IsFalse(
            RequestItemPickerRules.IsVisible(scrap, WithScrap(string.Empty)));
    }

    [TestMethod]
    public void OutOfScopeItems_AreNeverOffered_ButStayInTheCatalog()
    {
        string[] outOfScope = ["pickup-fg", "pickup-ncm", "pickup-wip", "pickup-outside-service"];

        // Present in the catalog: a hidden Item and a missing row are different states (FR-028).
        Assert.AreEqual(24, RequestItemCatalog.TotalCount);
        foreach (var id in outOfScope)
        {
            Assert.IsNotNull(RequestItemCatalog.FindById(id), id);
        }

        // Never offered — under any job, including the job that has everything.
        foreach (var availability in EightConfigurations())
        {
            foreach (var id in outOfScope)
            {
                Assert.IsFalse(
                    RequestItemPickerRules.IsVisible(RequestItemCatalog.FindById(id)!, availability),
                    $"'{id}' must never be offered, including for {Describe(availability)}.");
            }
        }

        // And the count is exact: no out-of-scope Item leaks into any configuration's visible set.
        foreach (var availability in EightConfigurations())
        {
            foreach (var item in RequestItemCatalog.Items)
            {
                if (RequestItemPickerRules.IsVisible(item, availability))
                {
                    CollectionAssert.DoesNotContain(outOfScope, item.Id);
                }
            }
        }
    }

    /// <summary>
    /// The SC-004 matrix: for each of the eight job configurations §D18 fixes, the exact set of visible Items,
    /// driven through the rules rather than through a snapshot of expected strings.
    /// </summary>
    [TestMethod]
    public void EightJobConfigurations_OfferTheExactVisibleItemSet()
    {
        var jobIndependent = new[]
        {
            "pickup-riser-table", "deliver-riser-table", "pickup-hopper", "deliver-hopper", "other"
        };

        AssertVisible(
            "coil only, a real scrap type",
            WithScrap("Steel Offal") with { HasCoil = true, HasFlatstock = false, HasDie = false, HasComponent = false, HasDunnage = false },
            [
                .. jobIndependent,
                "pickup-coil", "pickup-scrap", "deliver-coil", "deliver-wrong-coil",
                "assist-coil-turn", "assist-table-place", "assist-table-remove"
            ]);

        AssertVisible(
            "flatstock only",
            WithScrap(null) with { HasCoil = false, HasFlatstock = true, HasDie = false, HasComponent = false, HasDunnage = false },
            [
                .. jobIndependent,
                "pickup-coil", "deliver-flatstock", "deliver-wrong-flatstock",
                "assist-table-place", "assist-table-remove"
            ]);

        AssertVisible(
            "die only, the scrap placeholder",
            WithScrap(ScrapDecisionRules.RequiredPlaceholder) with { HasCoil = false, HasFlatstock = false, HasDie = true, HasComponent = false, HasDunnage = false },
            [
                .. jobIndependent,
                "pickup-die", "deliver-die",
                "assist-table-place", "assist-table-remove"
            ]);

        AssertVisible(
            "component only, 'No Scrap'",
            WithScrap(ScrapDecisionRules.NoScrap) with { HasCoil = false, HasFlatstock = false, HasDie = false, HasComponent = true, HasDunnage = false },
            [
                .. jobIndependent,
                "pickup-component", "deliver-component",
                "assist-table-place", "assist-table-remove"
            ]);

        AssertVisible(
            "dunnage only, no subordinate part",
            WithScrap(null) with { HasCoil = false, HasFlatstock = false, HasDie = false, HasComponent = false, HasDunnage = true },
            [
                .. jobIndependent,
                "pickup-dunnage", "deliver-dunnage",
                "assist-table-place", "assist-table-remove"
            ]);

        AssertVisible(
            "everything at once, a real scrap type",
            WithScrap("Steel Offal"),
            [
                .. jobIndependent,
                "pickup-coil", "pickup-die", "pickup-component", "pickup-dunnage", "pickup-scrap",
                "deliver-coil", "deliver-flatstock", "deliver-component", "deliver-die", "deliver-dunnage",
                "deliver-wrong-coil", "deliver-wrong-flatstock",
                "assist-coil-turn", "assist-table-place", "assist-table-remove"
            ]);

        AssertVisible(
            "an active job with no subordinate part",
            WithScrap(null) with { HasActiveJob = true, HasCoil = false, HasFlatstock = false, HasDie = false, HasComponent = false, HasDunnage = false },
            jobIndependent);

        AssertVisible(
            "a work centre with no active job",
            RequestJobPartAvailability.None,
            jobIndependent);
    }

    [TestMethod]
    public void MergedPickupCoil_IsOfferedForAFlatstockOnlyJob()
    {
        // The second half of FR-002, and what SC-004 measures: a flatstock-only job must still be offered the
        // merged Pickup Item.
        var flatstockOnly = WithScrap(null) with
        {
            HasCoil = false,
            HasFlatstock = true,
            HasDie = false,
            HasComponent = false,
            HasDunnage = false,
        };

        Assert.IsTrue(RequestItemPickerRules.IsVisible(RequestItemCatalog.FindById("pickup-coil")!, flatstockOnly));
    }

    [TestMethod]
    public void DeliverComponent_IsGatedOnAJobComponent_LikeItsPickupTwin()
    {
        // Added 2026-09-22 with the Deliver counterpart: the choice is asked for on the same jobs as the Pickup
        // choice, so a job with no component is never offered either of them.
        var deliver = RequestItemCatalog.FindById("deliver-component")!;
        var pickup = RequestItemCatalog.FindById("pickup-component")!;

        Assert.AreEqual(
            RequestItemPickerRules.RequiredJobPart(pickup),
            RequestItemPickerRules.RequiredJobPart(deliver),
            "The two component Items must be gated on the same job part.");
        Assert.IsFalse(RequestItemPickerRules.IsOutOfScope(deliver));

        var noComponent = RequestJobPartAvailability.All with { HasComponent = false };
        Assert.IsFalse(RequestItemPickerRules.IsVisible(deliver, noComponent));
        Assert.IsTrue(RequestItemPickerRules.IsVisible(deliver, RequestJobPartAvailability.All));
    }

    [TestMethod]
    public void DeadPickupFlatstockArm_IsGone()
    {
        // 'pickup-flatstock' is not one of the twenty-four pinned codes and is not in the catalog, so the
        // rule that advertised it must not come back.
        Assert.IsNull(RequestItemCatalog.FindById("pickup-flatstock"));
        Assert.AreNotEqual(
            RequestJobPartKind.Flatstock,
            RequestItemPickerRules.RequiredJobPart(RequestItemCatalog.FindById("pickup-coil")!),
            "The merged Pickup Item is a coil-OR-flatstock Item, not a flatstock-only one.");
    }

    private static RequestJobPartAvailability WithScrap(string? scrapType)
    {
        // The scrap value is stored per subordinate part; the snapshot only carries the derived decision.
        return RequestJobPartAvailability.All with
        {
            HasScrapDecision = ScrapDecisionRules.HasRealScrapDecision(scrapType)
        };
    }

    private static IEnumerable<RequestJobPartAvailability> EightConfigurations()
    {
        yield return WithScrap("Steel Offal") with { HasCoil = true, HasFlatstock = false, HasDie = false, HasComponent = false, HasDunnage = false };
        yield return WithScrap(null) with { HasCoil = false, HasFlatstock = true, HasDie = false, HasComponent = false, HasDunnage = false };
        yield return WithScrap(null) with { HasCoil = false, HasFlatstock = false, HasDie = true, HasComponent = false, HasDunnage = false };
        yield return WithScrap(null) with { HasCoil = false, HasFlatstock = false, HasDie = false, HasComponent = true, HasDunnage = false };
        yield return WithScrap(null) with { HasCoil = false, HasFlatstock = false, HasDie = false, HasComponent = false, HasDunnage = true };
        yield return WithScrap("Steel Offal");
        yield return WithScrap(null) with { HasActiveJob = true, HasCoil = false, HasFlatstock = false, HasDie = false, HasComponent = false, HasDunnage = false };
        yield return RequestJobPartAvailability.None;
    }

    private static string Describe(RequestJobPartAvailability availability) =>
        $"coil={availability.HasCoil}, flatstock={availability.HasFlatstock}, die={availability.HasDie}, "
        + $"component={availability.HasComponent}, dunnage={availability.HasDunnage}, job={availability.HasActiveJob}";

    private static void AssertVisible(string configuration, RequestJobPartAvailability availability, string[] expected)
    {
        var visible = RequestItemCatalog.Items
            .Where(item => RequestItemPickerRules.IsVisible(item, availability))
            .Select(item => item.Id)
            .ToList();

        CollectionAssert.AreEquivalent(expected, visible, $"Visible set for: {configuration} (" + Describe(availability) + ")");
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
        // Regression guard: the RequiredJobPart/IsVisible rules must handle all 24 canonical rows.
        foreach (var item in RequestItemCatalog.Items)
        {
            _ = RequestItemPickerRules.RequiredJobPart(item);
            _ = RequestItemPickerRules.IsManualEquipment(item);
            _ = RequestItemPickerRules.IsDeliverDestinationWorkCenter(item);
            _ = RequestItemPickerRules.IsVisible(item, RequestJobPartAvailability.None);
        }
    }
}
