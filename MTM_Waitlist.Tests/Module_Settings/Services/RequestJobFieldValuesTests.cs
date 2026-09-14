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
        Assert.AreEqual("FGT0002000-DIE SHOP", RequestJobFieldValues.Resolve("Die", DieJob()));
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
            Assert.AreEqual("FGT0002000-DIE SHOP", RequestJobFieldValues.Resolve(label, DieJob()));
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
