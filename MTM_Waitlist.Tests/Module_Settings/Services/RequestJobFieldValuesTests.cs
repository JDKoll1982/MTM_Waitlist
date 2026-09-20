using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;

namespace MTM_Waitlist.Tests.Module_Settings.Services;

/// <summary>
/// A declared detail row whose source is <c>job</c> names a value the requesting job carries and the request never
/// stored, so the request page could not draw it until the job was handed in (FR-052, FR-053).
/// </summary>
[TestClass]
public sealed class RequestJobFieldValuesTests
{
    private static RequestJobPartAvailability DieJob() => RequestJobPartAvailability.None with
    {
        HasActiveJob = true,
        HasDie = true,
        JobPartNumber = "PART-9003",
        DieNumber = "FGT0002000",
        DieLocation = "DIE SHOP",
    };

    [TestMethod]
    public void Resolve_Die_IsTheNumberAndTheLocationTogether()
    {
        Assert.AreEqual("FGT0002000", RequestJobFieldValues.Resolve("Die", DieJob()));
    }

    /// <summary>A job carrying two dies, with the second one the request is actually for (FR-057).</summary>
    private static RequestJobPartAvailability TwoDieJob() => DieJob().WithDies(
    [
        new RequestDiePart { PartNumber = "FGT0002000", Location = "DIE SHOP" },
        new RequestDiePart { PartNumber = "D-9001", Location = "PRESS BAY" },
    ]);

    [TestMethod]
    public void Resolve_Die_ListsEveryDieTheJobCarries_NotJustTheFirst()
    {
        // FR-057: a job carrying more than one die is one row per die, never the first die standing in for all
        // of them. The requester reads the row to see which dies the job has.
        Assert.AreEqual(
            "FGT0002000, D-9001",
            RequestJobFieldValues.Resolve("Die", TwoDieJob()));
    }

    [TestMethod]
    public void Resolve_PickupLocation_ListsEveryDieLocationTheJobCarries()
    {
        // FR-057, stated directly: the request page lists every die location. The locations are drawn in the
        // job's own order, so the row reads in the same order as the job does.
        Assert.AreEqual(
            "DIE SHOP, PRESS BAY",
            RequestJobFieldValues.Resolve("Pickup location", TwoDieJob()));
    }

    [TestMethod]
    public void Resolve_Die_WithADieThatHasNoLocation_LeavesNoDanglingSeparatorInTheList()
    {
        // The second die has no recorded location, so its entry is its number alone and no separator is left
        // hanging off it.
        var twoDies = DieJob().WithDies(
        [
            new RequestDiePart { PartNumber = "FGT0002000", Location = "DIE SHOP" },
            new RequestDiePart { PartNumber = "D-9001", Location = string.Empty },
        ]);

        Assert.AreEqual("FGT0002000, D-9001", RequestJobFieldValues.Resolve("Die", twoDies));
    }

    [TestMethod]
    public void Resolve_PickupLocation_WithOnlyOneDieThatHasNoLocation_YieldsNothing()
    {
        // A location the job does not record is omitted rather than drawn as a blank standing in for a value.
        var noLocations = DieJob().WithDies(
        [
            new RequestDiePart { PartNumber = "FGT0002000", Location = string.Empty },
            new RequestDiePart { PartNumber = "D-9001", Location = string.Empty },
        ]);

        Assert.IsNull(RequestJobFieldValues.Resolve("Pickup location", noLocations));
    }

    [TestMethod]
    public void Resolve_PickupLocation_IsWhereTheDieIs()
    {
        Assert.AreEqual("DIE SHOP", RequestJobFieldValues.Resolve("Pickup location", DieJob()));
    }

    [TestMethod]
    public void Resolve_PartNumber_IsThePartTheDieIsAssignedTo()
    {
        Assert.AreEqual("PART-9003", RequestJobFieldValues.Resolve("Part number", DieJob()));
    }

    [TestMethod]
    public void Resolve_IsCaseAndSpacingInsensitive()
    {
        // The label comes from the Item's stored configuration, so the row's own spacing must not decide it.
        foreach (var label in new[] { "die", "DIE", "  Die  " })
        {
            Assert.AreEqual("FGT0002000", RequestJobFieldValues.Resolve(label, DieJob()));
        }
    }

    [TestMethod]
    public void Resolve_DieWithNoKnownLocation_LeavesNoDanglingSeparator()
    {
        var noLocation = DieJob() with { DieLocation = string.Empty };

        Assert.AreEqual("FGT0002000", RequestJobFieldValues.Resolve("Die", noLocation));
        Assert.IsNull(RequestJobFieldValues.Resolve("Pickup location", noLocation), "a location it does not have is not drawn.");
    }

    [TestMethod]
    public void Resolve_JobWithNoDie_YieldsNothing_SoNoEmptyRowIsDrawn()
    {
        var noDie = RequestJobPartAvailability.None with { HasActiveJob = true, HasDie = false };

        Assert.IsNull(RequestJobFieldValues.Resolve("Die", noDie));
        Assert.IsNull(RequestJobFieldValues.Resolve("Pickup location", noDie));
    }

    [TestMethod]
    public void Resolve_WithoutAJob_YieldsNothing()
    {
        // The page resolves before the job arrives; it must show the rows the request itself carries and no more.
        Assert.IsNull(RequestJobFieldValues.Resolve("Die", job: null));
    }

    [TestMethod]
    public void Resolve_UnknownOrEmptyLabel_YieldsNothing()
    {
        // A label this snapshot cannot supply is omitted rather than drawn as a blank standing in for a value.
        Assert.IsNull(RequestJobFieldValues.Resolve("Average coil weight", DieJob()));
        Assert.IsNull(RequestJobFieldValues.Resolve("Quantity", DieJob()));
        Assert.IsNull(RequestJobFieldValues.Resolve(string.Empty, DieJob()));
        Assert.IsNull(RequestJobFieldValues.Resolve(null, DieJob()));
    }
}
