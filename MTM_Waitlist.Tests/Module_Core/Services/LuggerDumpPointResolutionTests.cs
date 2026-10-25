using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Tests.Module_Core.Services;

/// <summary>
/// The owner's walk-through, proven over scripted arrangements: the card reads the lugger the engine left holding
/// each scrap type's dump point at every step, and exactly one lugger holds it unless every lugger for that type is
/// blocked (FR-013, FR-015, FR-016, FR-018, FR-021, FR-024).
/// </summary>
/// <remarks>
/// The arrangements below are the states the shipped engine produces, so the walk-through reads the same rule the
/// card reads without a second copy of the engine in the test.
/// </remarks>
[TestClass]
public sealed class LuggerDumpPointResolutionTests
{
    private const string Door6 = "Door 6 Lugger";
    private const string Door7 = "Door 7 Lugger";
    private const string Extra = "Extra Lugger (Outside)";
    private const string Scrap = "3003 aluminum";
    private const string Spelled = "3003 Aluminum";

    [TestMethod]
    public void Service_LuggerDumpPointResolutionTests_WalkThrough_Door6AvailableNamesDoor6()
    {
        Assert.AreEqual(Door6, Snapshot((Extra, "Backup", 2, false), (Door6, "Available", 1, true)).Resolve(Spelled).LuggerLocation);
    }

    [TestMethod]
    public void Service_LuggerDumpPointResolutionTests_WalkThrough_Door6FullNamesExtraLuggerOutside()
    {
        // Door 6 is blocked, so the engine promoted the backup and sorted Door 6 after it.
        Assert.AreEqual(Extra, Snapshot((Extra, "Backup", 1, true), (Door6, "Full", 2, false)).Resolve(Spelled).LuggerLocation);
    }

    [TestMethod]
    public void Service_LuggerDumpPointResolutionTests_WalkThrough_EmptyingDoor6LeavesExtraHoldingTheRole()
    {
        Assert.AreEqual(Extra, Snapshot((Extra, "Backup", 1, true), (Door6, "Available", 2, false)).Resolve(Spelled).LuggerLocation);
    }

    [TestMethod]
    public void Service_LuggerDumpPointResolutionTests_WalkThrough_BlockingExtraHandsTheRoleBackToDoor6()
    {
        Assert.AreEqual(Door6, Snapshot((Extra, "Full", 2, false), (Door6, "Available", 1, true)).Resolve(Spelled).LuggerLocation);
    }

    [TestMethod]
    public void Service_LuggerDumpPointResolutionTests_ExplicitPromotion_ReadsThePromotedLuggerAndLeavesThePreviousHolderBackup()
    {
        var snapshot = Snapshot((Door6, "Backup", 2, false), (Extra, "Available", 1, true));

        Assert.AreEqual(Extra, snapshot.Resolve(Spelled).LuggerLocation);
        Assert.AreEqual(1, snapshot.Rows.Count(row => row.IsDumpPoint == true), "A promotion leaves exactly one dump point.");
    }

    [TestMethod]
    public void Service_LuggerDumpPointResolutionTests_AllBlocked_NamesTheFirstLuggerWithItsStatus()
    {
        var snapshot = Snapshot((Door6, "Do Not Use", 1, false), (Extra, "Full", 2, false));

        Assert.AreEqual($"{Door6} (Do Not Use)", snapshot.Resolve(Spelled).LuggerLocation);
        Assert.AreEqual(0, snapshot.Rows.Count(row => row.IsDumpPoint == true), "No blocked lugger holds the role (FR-013).");
    }

    [TestMethod]
    public void Service_LuggerDumpPointResolutionTests_TwoScrapTypes_OneBlockedLuggerPromotesEachTypesOwnNextLugger()
    {
        // Door 6 took both types and was blocked, so each type promoted the lugger next in its own order.
        var snapshot = LuggerArrangementSnapshot.Create(
        [
            Row(Door7, "Available", Scrap, 1, true),
            Row(Door6, "Full", Scrap, 2, false),
            Row(Extra, "Available", "galvanized steel", 1, true),
            Row(Door6, "Full", "galvanized steel", 2, false),
        ]);

        Assert.AreEqual(Door7, snapshot.Resolve(Spelled).LuggerLocation);
        Assert.AreEqual(Extra, snapshot.Resolve("Galvanized Steel").LuggerLocation);
        Assert.AreEqual(1, snapshot.Rows.Count(row => row.ScrapTypeKey == Scrap && row.IsDumpPoint == true));
        Assert.AreEqual(1, snapshot.Rows.Count(row => row.ScrapTypeKey == "galvanized steel" && row.IsDumpPoint == true));
    }

    private static LuggerArrangementSnapshot Snapshot(params (string Lugger, string Status, int Position, bool IsDumpPoint)[] rows) =>
        LuggerArrangementSnapshot.Create(rows.Select(row => Row(row.Lugger, row.Status, Scrap, row.Position, row.IsDumpPoint)));

    private static ScrapAssignment Row(string lugger, string status, string scrapTypeKey, int position, bool isDumpPoint) => new()
    {
        AssignmentPublicId = $"assignment-{lugger}-{scrapTypeKey}",
        LuggerPublicId = $"lugger-{lugger}",
        LuggerName = lugger,
        LuggerStatus = status,
        LuggerDisplayPosition = position,
        ScrapType = scrapTypeKey == Scrap ? Spelled : char.ToUpperInvariant(scrapTypeKey[0]) + scrapTypeKey[1..],
        ScrapTypeKey = scrapTypeKey,
        IsDumpPoint = isDumpPoint,
        SortPosition = position,
    };
}
