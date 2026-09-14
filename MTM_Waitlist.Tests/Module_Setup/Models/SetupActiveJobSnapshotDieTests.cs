using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Setup.Models;

namespace MTM_Waitlist.Tests.Module_Setup.Models;

/// <summary>
/// The die rows the request workflow reads (FR-055).
/// <para>
/// A die row is not always a die. The subordinate-parts query gives a job with no die a die row described
/// <c>No Die</c> — normally the default <c>FGT0001-01</c> with an empty location. The rows below are the two
/// shapes that query returns, taken from the live <c>setup_active_jobs</c> rows of the coil-only station
/// <c>100-3</c> (the placeholder) and the die-only station <c>100-7</c> (a real die).
/// </para>
/// <para>
/// <see cref="SetupActiveJobSnapshot.Dies"/> keeps everything the query returned, because the Setup screens show
/// the job as it came back. <see cref="SetupActiveJobSnapshot.RealDies"/> is what the request workflow reads, so
/// a job with no die is never offered the Die Items.
/// </para>
/// </summary>
[TestClass]
public sealed class SetupActiveJobSnapshotDieTests
{
    [TestMethod]
    public void RealDies_ExcludesTheNoDiePlaceholder()
    {
        var snapshot = Snapshot(
            Die("FGT0001-01", "No Die", string.Empty),
            Subordinate("Coil", "MMC0001000", "Coil 0.062 x 48 wide", "A-01-01"));

        Assert.AreEqual(
            1,
            snapshot.Dies.Count,
            "The row the query returned is still there — the Setup screens show what the job returned.");
        Assert.AreEqual(
            0,
            snapshot.RealDies.Count,
            "The placeholder is the absence of a die, so a job whose only die row is the placeholder has no die.");
        Assert.IsNull(snapshot.PrimaryDie);
        Assert.AreEqual(string.Empty, snapshot.DieLocation);
    }

    [TestMethod]
    public void RealDies_KeepsEveryRealDieTheJobCarries()
    {
        var snapshot = Snapshot(
            Die("FGT0002000", "Die 9003-A", "DIE SHOP"),
            Die("FGT0002001", "Die 9006-B", "PRESS BAY"));

        Assert.AreEqual(2, snapshot.RealDies.Count, "Every die the job carries is offered; the operator picks.");
        Assert.AreEqual("FGT0002000", snapshot.PrimaryDie!.PartNumber);
        Assert.AreEqual("DIE SHOP", snapshot.DieLocation);
    }

    [TestMethod]
    public void RealDies_DropsAPlaceholderSittingBesideARealDie()
    {
        var snapshot = Snapshot(
            Die("FGT0001-01", "  no die  ", string.Empty),
            Die("FGT0002000", "Die 9003-A", "DIE SHOP"));

        Assert.AreEqual(1, snapshot.RealDies.Count);
        Assert.AreEqual("FGT0002000", snapshot.RealDies[0].PartNumber);
    }

    [TestMethod]
    public void RealDies_DropsADieRowWithNoPartNumber()
    {
        var snapshot = Snapshot(Die(string.Empty, "Die 9003-A", "DIE SHOP"));

        Assert.AreEqual(1, snapshot.Dies.Count);
        Assert.AreEqual(
            0,
            snapshot.RealDies.Count,
            "The die's FGT number is what every card shows, so a row without one is not usable as a die.");
    }

    [TestMethod]
    public void Dies_FollowTheCategory_SoAnotherCategorysRowIsNeverADie()
    {
        var snapshot = Snapshot(
            Subordinate("Component", "FGT0002000", "Die 9003-A", "DIE SHOP"));

        Assert.AreEqual(0, snapshot.RealDies.Count);
    }

    private static SetupSubordinatePart Die(string partNumber, string description, string location) =>
        Subordinate("Die", partNumber, description, location);

    private static SetupSubordinatePart Subordinate(string category, string partNumber, string description, string location) =>
        new()
        {
            Category = category,
            PartNumber = partNumber,
            Description = description,
            Location = location,
        };

    private static SetupActiveJobSnapshot Snapshot(params SetupSubordinatePart[] parts) => new()
    {
        WorkCenter = "100-3",
        WorkOrder = "WO-900001",
        PartNumber = "PART-9001",
        SubordinateParts = parts,
    };
}
