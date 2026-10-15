using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Tests.Module_Settings.Models;

/// <summary>
/// The dunnage part carries the home location the receiving store records, so the card's `Home Location` cell has
/// a value on the snapshot instead of only in the store (FR-033, FR-045, FR-089).
/// </summary>
[TestClass]
public sealed class RequestDunnagePartTests
{
    [TestMethod]
    public void Model_RequestDunnagePartTests_WithDunnageParts_CarriesTheHomeLocationOntoTheSnapshot()
    {
        var part = new RequestDunnagePart
        {
            Id = "p1",
            TypeId = "t1",
            PartNumber = "DN-STL-4",
            DisplayName = "Steel rack",
            HomeLocation = "V-A0-01",
            IsAssignedToJob = true,
        };

        var availability = RequestJobPartAvailability.None.WithDunnageParts(new[] { part });

        Assert.AreEqual(1, availability.DunnageParts.Count);
        Assert.AreEqual(
            "V-A0-01",
            availability.DunnageParts[0].HomeLocation,
            "the snapshot is what the card reads, so the location has to survive the trip onto it");
    }

    [TestMethod]
    public void Model_RequestDunnagePartTests_HomeLocation_DefaultsToEmptyWhenTheStoreRecordsNone()
    {
        // An unrecorded location is not a location: the card leaves the cell out rather than showing a blank.
        Assert.AreEqual(string.Empty, new RequestDunnagePart { PartNumber = "DN-STL-4" }.HomeLocation);
    }
}
