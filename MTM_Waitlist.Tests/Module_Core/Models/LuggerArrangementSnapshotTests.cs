using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Tests.Module_Core.Models;

/// <summary>
/// What a snapshot says about whether it was read at all. Both an outage and an arrangement with no rows leave
/// <see cref="LuggerArrangementSnapshot.Rows"/> empty, and telling them apart is what keeps an outage from being
/// drawn as a configuration gap (FR-010).
/// </summary>
[TestClass]
public sealed class LuggerArrangementSnapshotTests
{
    [TestMethod]
    public void Model_LuggerArrangementSnapshotTests_Create_OverRowsAnswersItWasRead()
    {
        var snapshot = LuggerArrangementSnapshot.Create([Row("Door 6 Lugger", "Steel")]);

        Assert.IsTrue(snapshot.WasRead, "A snapshot built from the read's rows is a read that answered.");
        Assert.AreEqual(1, snapshot.Rows.Count);
    }

    [TestMethod]
    public void Model_LuggerArrangementSnapshotTests_Create_OverZeroRowsStillAnswersItWasRead()
    {
        var snapshot = LuggerArrangementSnapshot.Create(Array.Empty<ScrapAssignment>());

        Assert.IsTrue(
            snapshot.WasRead,
            "An arrangement with no rows was read, so it is not the same answer as an arrangement that could not be read.");
        Assert.AreEqual(0, snapshot.Rows.Count);
    }

    [TestMethod]
    public void Model_LuggerArrangementSnapshotTests_Empty_AnswersItWasNotRead()
    {
        Assert.IsFalse(LuggerArrangementSnapshot.Empty.WasRead, "The empty snapshot is the outage, not a read with no rows.");
        Assert.AreEqual(0, LuggerArrangementSnapshot.Empty.Rows.Count);
    }

    [TestMethod]
    public void Model_LuggerArrangementSnapshotTests_Create_OfNothingAnswersTheUnreadDefault()
    {
        // A read that answered nothing at all hands back the empty snapshot, which is the type's own unread value.
        var snapshot = LuggerArrangementSnapshot.Create(null);

        Assert.IsFalse(snapshot.WasRead, "A read that answered nothing is not a read that found an empty arrangement.");
        Assert.AreSame(LuggerArrangementSnapshot.Empty, snapshot);
    }

    [TestMethod]
    public void Model_LuggerArrangementSnapshotTests_Outage_ReportsUnreadAndEmptyTogether()
    {
        var outage = LuggerArrangementSnapshot.Empty;

        Assert.IsFalse(outage.WasRead);
        Assert.AreEqual(0, outage.Rows.Count);
        Assert.AreEqual(0, outage.ScrapTypes.Count, "An outage names no scrap type either, so nothing is offered from it.");
    }

    private static ScrapAssignment Row(string luggerName, string scrapType) => new()
    {
        LuggerPublicId = luggerName.Replace(" ", "-", StringComparison.Ordinal).ToLowerInvariant(),
        LuggerName = luggerName,
        LuggerStatus = LuggerStatus.Available,
        LuggerDisplayPosition = 1,
        ScrapType = scrapType,
        ScrapTypeKey = scrapType.Trim().ToLowerInvariant(),
        IsDumpPoint = true,
        SortPosition = 1,
    };
}
