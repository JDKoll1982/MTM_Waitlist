using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Waitlist.Converters;
using MTM_Waitlist.Module_Waitlist.Helpers;
using MTM_Waitlist.Module_Waitlist.Models;

using Windows.UI;

namespace MTM_Waitlist.Tests.Module_Waitlist.Helpers;

/// <summary>
/// The status column as one reading: the row's own word, ring and people, and the brush the column is painted
/// with. Each state FR-011 to FR-023 names is composed here from the row and the palette rather than from a
/// build of the application, so the reading is pinned without a database or a window.
/// </summary>
/// <remarks>
/// The row's own behaviour is pinned by <c>SampleOrderTests</c>; what is added here is the join — that the theme a
/// row says it is in is the theme the column is actually painted with, which is the one thing the card rebuilds
/// for itself and the one thing a model test cannot see.
/// </remarks>
[TestClass]
public sealed class RequestStatusColumnTests
{
    /// <summary>The parameter the card passes when it asks the converter for the column's theme brush.</summary>
    private const string ThemeParameter = "StatusColumnTheme";

    [TestMethod]
    public void Helper_RequestStatusColumnTests_ALiveUnclaimedRequestReadsWaitingWithTheRequesterInTheThirdRow()
    {
        var now = DateTimeOffset.UtcNow;
        var row = new SampleOrder
        {
            Status = "Pending",
            RequestedByName = "Ada Lovelace",
            RequestedUtc = now.AddMinutes(-2),
            AllottedMinutes = 15,
            TargetTimeUtc = now.AddMinutes(13),
            RemainingTimeText = "00:13",
        };

        Assert.AreEqual("Waiting", row.StatusWord, "An unclaimed live request must read Waiting (FR-012).");
        Assert.AreEqual(StatusColumnTheme.Normal, row.StatusTheme, "A Waiting row is not an ordinary column (FR-019).");
        Assert.AreEqual("Ada Lovelace", row.PersonText, "The third row must name the requester while nobody has claimed the request (FR-022).");
        Assert.IsTrue(row.HasPerson, "The requester's row was removed even though it has a value (FR-018).");
        Assert.IsFalse(row.HasRequester, "An open request repeats its requester in the fourth row (FR-022).");
        Assert.AreEqual(TimeRingMeasure.TimeLeft, row.RingMeasure, "A live request's ring measures the time left (FR-015).");
        Assert.IsTrue(row.RingValue is > 0d and < 1d, "A live request inside its window does not draw a part-used ring (FR-013).");
        Assert.AreEqual(
            RequestStatusPalette.NormalColumnColor,
            ColumnColour(row.StatusTheme),
            "A Waiting column is not painted in the normal theme (FR-019).");
    }

    [TestMethod]
    public void Helper_RequestStatusColumnTests_AClaimedRequestReadsTheHandlerInTheThirdRow()
    {
        var row = new SampleOrder
        {
            Status = "Accepted",
            RequestedByName = "Ada Lovelace",
            AssignedMaterialHandler = "H-4102",
            AssignedMaterialHandlerName = "Grace Hopper",
        };

        Assert.AreEqual("In Progress", row.StatusWord, "A claimed request must read the claiming state (FR-012).");
        Assert.AreEqual("Grace Hopper", row.PersonText, "The third row must name the material handler once the request is claimed (FR-022).");
        Assert.IsFalse(row.HasRequester, "A claimed request that is still open repeats its requester in the fourth row (FR-022).");
        Assert.AreEqual(
            RequestStatusPalette.NormalColumnColor,
            ColumnColour(row.StatusTheme),
            "A claimed request that is not overdue does not take the normal theme (FR-019).");
    }

    [TestMethod]
    public void Helper_RequestStatusColumnTests_AnOverdueRequestWithNoConditionReadsOverdueAndCarriesItsOverrunOnTheOverdueTheme()
    {
        var now = DateTimeOffset.UtcNow;
        var row = new SampleOrder
        {
            Status = "Accepted",
            RequestedByName = "Ada Lovelace",
            AssignedMaterialHandler = "H-4102",
            AssignedMaterialHandlerName = "Grace Hopper",
            RequestedUtc = now.AddMinutes(-20),
            AllottedMinutes = 15,
            TargetTimeUtc = now.AddMinutes(-5),
        };
        row.RefreshTimeDerivedText(now);

        Assert.AreEqual("Overdue", row.StatusWord, "An overdue request with no condition must read the overdue marking (FR-012, FR-014).");
        Assert.AreEqual(StatusColumnTheme.Overdue, row.StatusTheme, "An overdue request did not take the overdue theme (FR-014).");
        Assert.IsTrue(row.RingValue > 1d, "The overdue row's ring does not carry the overrun (FR-014).");
        Assert.AreEqual(
            RequestStatusPalette.OverdueColumnColor,
            ColumnColour(row.StatusTheme),
            "An overdue column is not painted in the overdue theme (FR-014).");
        Assert.AreNotEqual(
            ColumnColour(StatusColumnTheme.HandlerCondition),
            ColumnColour(row.StatusTheme),
            "An overdue column is painted as a handler's condition, which is exactly the confusion SC-007 forbids (FR-023).");
    }

    [TestMethod]
    public void Helper_RequestStatusColumnTests_ACompletedRequestFreezesItsRingOnTheTimeTakenWithTheHandlerThirdAndTheRequesterFourth()
    {
        var now = DateTimeOffset.UtcNow;
        var requested = now.AddMinutes(-30);
        var closed = now.AddMinutes(-22);
        var row = new SampleOrder
        {
            Status = "Completed",
            RequestedByName = "Ada Lovelace",
            AssignedMaterialHandler = "H-4102",
            AssignedMaterialHandlerName = "Grace Hopper",
            RequestedUtc = requested,
            ClosedUtc = closed,
            AllottedMinutes = 15,
        };

        Assert.AreEqual("Done", row.StatusWord, "A finished request must read the finished word (FR-012).");
        Assert.AreEqual(TimeRingMeasure.TimeTaken, row.RingMeasure, "A closed request's ring must measure the time taken (FR-015).");
        Assert.AreEqual(
            SampleOrder.RingFraction(requested, closed, 15, now.AddHours(2)),
            row.RingValue,
            0.0001d,
            "A closed request's ring is still growing, so it is not frozen at the moment it closed (FR-015).");
        Assert.AreEqual("Grace Hopper", row.PersonText, "A finished request's third row must name the material handler (FR-022).");
        Assert.AreEqual("Ada Lovelace", row.RequesterText, "A finished request's fourth row must name the requester (FR-022).");
        Assert.IsTrue(row.HasRequester, "A finished request's requester row was removed even though it has a value (FR-018).");
    }

    [TestMethod]
    public void Helper_RequestStatusColumnTests_ACancelledRequestReadsThePersonWhoEndedItAndTheRequesterBeneathThem()
    {
        var claimed = new SampleOrder
        {
            Status = "Canceled",
            RequestedByName = "Ada Lovelace",
            AssignedMaterialHandler = "H-4102",
            AssignedMaterialHandlerName = "Grace Hopper",
        };

        Assert.AreEqual("Cancelled", claimed.StatusWord, "A withdrawn request must read the withdrawn word (FR-012).");
        Assert.AreEqual("Grace Hopper", claimed.PersonText, "A withdrawn request that had a handler must name them (FR-022).");
        Assert.AreEqual("Ada Lovelace", claimed.RequesterText, "A withdrawn request must name its requester beneath the handler (FR-022).");

        var withdrawnWhileWaiting = new SampleOrder
        {
            Status = "Canceled",
            RequestedByName = "Ada Lovelace",
        };

        Assert.AreEqual("Ada Lovelace", withdrawnWhileWaiting.PersonText, "A request withdrawn before anyone claimed it must name the person who ended it (FR-022).");
        Assert.AreEqual(
            "Ada Lovelace",
            withdrawnWhileWaiting.RequesterText,
            "A withdrawn request still names its requester beneath the person who ended it (FR-022).");
        Assert.AreEqual(
            RequestStatusPalette.NormalColumnColor,
            ColumnColour(withdrawnWhileWaiting.StatusTheme),
            "A withdrawn request is drawn as overdue, so the overdue theme is no longer unmistakable (FR-019, FR-020).");
    }

    [TestMethod]
    public void Helper_RequestStatusColumnTests_ARequestCarryingAConditionReadsOnlyThatConditionOnTheLighterRedTheme()
    {
        var now = DateTimeOffset.UtcNow;
        var row = new SampleOrder
        {
            Status = "Accepted",
            RequestedByName = "Ada Lovelace",
            AssignedMaterialHandler = "H-4102",
            AssignedMaterialHandlerName = "Grace Hopper",
            RequestedUtc = now.AddMinutes(-20),
            AllottedMinutes = 15,
            TargetTimeUtc = now.AddMinutes(-5),
            HandlerStatus = "Changing Propane",
        };
        row.RefreshTimeDerivedText(now);

        Assert.IsTrue(row.IsOverdue, "The arrangement did not make the row overdue, so the precedence it checks is not being tested (FR-023).");
        Assert.AreEqual("Changing Propane", row.StatusWord, "A reported condition must read verbatim and must hide the overdue marking (FR-012, FR-023).");
        Assert.AreEqual(StatusColumnTheme.HandlerCondition, row.StatusTheme, "A reported condition did not take the handler-condition theme (FR-072).");
        Assert.AreEqual(
            RequestStatusPalette.HandlerConditionColumnColor,
            ColumnColour(row.StatusTheme),
            "A condition's column is not painted in the lighter red (FR-072).");
        Assert.AreNotEqual(
            RequestStatusPalette.OverdueColumnColor,
            ColumnColour(row.StatusTheme),
            "A condition and the overdue marking are both shown, which FR-023 forbids (FR-023).");
    }

    /// <summary>
    /// The colour the card paints the whole column with, asked for exactly the way the card asks for it.
    /// </summary>
    /// <remarks>
    /// Read as a colour rather than through the converter's <c>Convert</c>, because finishing that conversion
    /// means building a <c>SolidColorBrush</c> and a WinUI brush cannot be built outside a UI thread — a probe in
    /// this very host fails with <c>COMException</c>.
    /// </remarks>
    private static Color ColumnColour(StatusColumnTheme theme) =>
        RequestStatusToBrushConverter.ColourFor(theme, ThemeParameter);
}
