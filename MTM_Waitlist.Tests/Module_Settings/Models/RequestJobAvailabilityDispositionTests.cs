using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;

namespace MTM_Waitlist.Tests.Module_Settings.Models;

/// <summary>
/// The part's derived item type is written onto the snapshot the picker and the card read (FR-091). The
/// composition root resolves it and hands it over through <c>WithDisposition</c>, so this pins what the picker is
/// handed for each derived state, and what each state does and does not offer (FR-094).
/// </summary>
[TestClass]
public sealed class RequestJobAvailabilityDispositionTests
{
    [TestMethod]
    public void Service_RequestJobAvailabilityDispositionTests_WithDisposition_EachStateRecordsItsOwnGate()
    {
        var cases = new (RequestDisposition Disposition, string ItemId)[]
        {
            (RequestDisposition.FinishedGoods, "pickup-fg"),
            (RequestDisposition.Ncm, "pickup-ncm"),
            (RequestDisposition.WorkInProcess, "pickup-wip"),
            (RequestDisposition.OutsideService, "pickup-outside-service"),
        };

        foreach (var (disposition, itemId) in cases)
        {
            var availability = RequestJobPartAvailability.None.WithDisposition(disposition);

            Assert.AreEqual(disposition, availability.Disposition);
            Assert.IsTrue(
                GateFor(itemId, availability),
                $"'{itemId}' records no gate for a job the derivation reads as {disposition}.");

            foreach (var (_, otherId) in cases.Where(candidate => !string.Equals(candidate.ItemId, itemId, StringComparison.Ordinal)))
            {
                Assert.IsFalse(
                    GateFor(otherId, availability),
                    $"'{otherId}' records a gate for a job the derivation reads as {disposition}.");
            }
        }
    }

    [TestMethod]
    public void Service_RequestJobAvailabilityDispositionTests_WithDisposition_AnActiveJobStillOffersTheJobsOwnProduct()
    {
        // A request may name finished parts or WIP whatever the derivation made of the part: the two pickups name
        // the job's own product, so the job is what offers them, and a derivation that cannot say where the stock
        // sits does not take the choice away (FR-094).
        var job = RequestJobPartAvailability.All.WithDisposition(RequestDisposition.Unknown);

        Assert.IsTrue(job.HasActiveJob);
        Assert.IsTrue(RequestItemPickerRules.IsVisible(RequestItemCatalog.FindById("pickup-fg")!, job));
        Assert.IsTrue(RequestItemPickerRules.IsVisible(RequestItemCatalog.FindById("pickup-wip")!, job));
        Assert.IsFalse(RequestItemPickerRules.IsVisible(RequestItemCatalog.FindById("pickup-ncm")!, job));
        Assert.IsFalse(RequestItemPickerRules.IsVisible(RequestItemCatalog.FindById("pickup-outside-service")!, job));
    }

    [TestMethod]
    public void Service_RequestJobAvailabilityDispositionTests_WithDisposition_NothingDerivedLightsNoReturningPickup()
    {
        // A read that failed, a job the plant does not know, or a snapshot built before the derivation ran: the
        // gates stay false and nothing is invented in their place (FR-089).
        var availability = RequestJobPartAvailability.All.WithDisposition(RequestDisposition.Unknown);

        Assert.AreEqual(RequestDisposition.Unknown, availability.Disposition);
        Assert.IsFalse(availability.HasFinishedGoodsToReturn);
        Assert.IsFalse(availability.HasNonconformingStockToReturn);
        Assert.IsFalse(availability.HasWorkInProgressToReturn);
        Assert.IsFalse(availability.HasOutsideServiceWorkToReturn);
    }

    [TestMethod]
    public void Service_RequestJobAvailabilityDispositionTests_WithDisposition_LeavesEveryPartKindGateAlone()
    {
        // The four returning gates are the derivation's business; the part-kind gates the job snapshot already
        // settled must not move with them.
        var job = RequestJobPartAvailability.All with
        {
            HasFlatstock = false,
            HasDunnage = false,
            HasFinishedGoodsToReturn = false,
            HasNonconformingStockToReturn = false,
            HasWorkInProgressToReturn = false,
            HasOutsideServiceWorkToReturn = false,
        };

        var availability = job.WithDisposition(RequestDisposition.WorkInProcess);

        Assert.IsTrue(availability.HasCoil);
        Assert.IsTrue(availability.HasDie);
        Assert.IsTrue(availability.HasComponent);
        Assert.IsFalse(availability.HasFlatstock);
        Assert.IsFalse(availability.HasDunnage);
        Assert.IsTrue(RequestItemPickerRules.IsVisible(RequestItemCatalog.FindById("pickup-fg")!, availability));
        Assert.IsTrue(RequestItemPickerRules.IsVisible(RequestItemCatalog.FindById("pickup-wip")!, availability));
        Assert.IsTrue(RequestItemPickerRules.IsVisible(RequestItemCatalog.FindById("pickup-coil")!, availability));
    }

    /// <summary>The derivation gate an Item records, as opposed to whether the picker offers it.</summary>
    private static bool GateFor(string itemId, RequestJobPartAvailability availability) => itemId switch
    {
        "pickup-fg" => availability.HasFinishedGoodsToReturn,
        "pickup-ncm" => availability.HasNonconformingStockToReturn,
        "pickup-wip" => availability.HasWorkInProgressToReturn,
        "pickup-outside-service" => availability.HasOutsideServiceWorkToReturn,
        _ => throw new ArgumentOutOfRangeException(nameof(itemId), itemId, "Not a returning pickup."),
    };
}
