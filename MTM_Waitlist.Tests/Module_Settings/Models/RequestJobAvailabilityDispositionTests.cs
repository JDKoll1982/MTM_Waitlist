using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;

namespace MTM_Waitlist.Tests.Module_Settings.Models;

/// <summary>
/// The part's derived item type is written onto the snapshot the picker and the card read, and the four returning
/// pickups are offered on that same derivation (FR-091, FR-094). The composition root resolves it and hands it
/// over through <c>WithDisposition</c>, so this pins what the picker is handed for each derived state.
/// </summary>
[TestClass]
public sealed class RequestJobAvailabilityDispositionTests
{
    [TestMethod]
    public void Service_RequestJobAvailabilityDispositionTests_WithDisposition_EachStateLightsItsOwnReturningPickup()
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
                RequestItemPickerRules.IsVisible(RequestItemCatalog.FindById(itemId)!, availability),
                $"'{itemId}' must be offered for a job the derivation reads as {disposition}.");

            foreach (var (_, otherId) in cases.Where(candidate => !string.Equals(candidate.ItemId, itemId, StringComparison.Ordinal)))
            {
                Assert.IsFalse(
                    RequestItemPickerRules.IsVisible(RequestItemCatalog.FindById(otherId)!, availability),
                    $"'{otherId}' must not be offered for a job the derivation reads as {disposition}.");
            }
        }
    }

    [TestMethod]
    public void Service_RequestJobAvailabilityDispositionTests_WithDisposition_NothingDerivedLightsNoReturningPickup()
    {
        // A read that failed, a job the plant does not know, or a snapshot built before the derivation ran: the
        // four Items stay hidden and nothing is invented in their place (FR-089).
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
        Assert.IsFalse(RequestItemPickerRules.IsVisible(RequestItemCatalog.FindById("pickup-fg")!, availability));
        Assert.IsTrue(RequestItemPickerRules.IsVisible(RequestItemCatalog.FindById("pickup-wip")!, availability));
        Assert.IsTrue(RequestItemPickerRules.IsVisible(RequestItemCatalog.FindById("pickup-coil")!, availability));
    }
}
