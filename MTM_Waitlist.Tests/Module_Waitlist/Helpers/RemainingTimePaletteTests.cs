using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Waitlist.Helpers;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Waitlist.Helpers;

/// <summary>
/// The remaining-time palette's second scale: the status column's ring. The ring draws the row's own fraction
/// and the palette colours that same fraction, so the arc, the time printed inside it and the colour of both are
/// one number rather than three that have to be kept in step by hand (FR-013, FR-017).
/// </summary>
/// <remarks>
/// The pickers are read as colours rather than as brushes because a <c>SolidColorBrush</c> cannot be built
/// outside a UI thread: a probe in this very host fails with <c>COMException</c>. The one-line wrappers that turn
/// each colour into the brush the details page binds are asserted against the artifact that ships, which is this
/// repository's answer for app-project surfaces.
/// </remarks>
[TestClass]
public sealed class RemainingTimePaletteTests
{
    private const string PalettePath = "MTM_Waitlist.Waitlist.View/Helpers/RemainingTimePalette.cs";

    [TestMethod]
    public void Helper_RemainingTimePaletteTests_FractionFor_ReadsTheSameFractionTheRowsTextStates()
    {
        // The ring and the countdown it sits over describe one moment. The row works the fraction out once, and the
        // text is that fraction's minutes; FractionFor is the one place the text turns back into it, so the two can
        // never disagree about how far through its window the request is (FR-013, FR-017).
        var now = DateTimeOffset.UtcNow;
        var requested = now.AddMinutes(-20);
        var row = new SampleOrder
        {
            Status = "Pending",
            RequestedUtc = requested,
            AllottedMinutes = 30,
            TargetTimeUtc = requested.AddMinutes(30),
        };
        row.RefreshTimeDerivedText(now);

        Assert.AreEqual("00:10", row.RemainingTimeText, "The row's countdown is not the ten minutes the arrangement asks for.");

        var fromText = RemainingTimePalette.FractionFor(row.RemainingTimeText, row.AllottedMinutes!.Value);

        // Twenty of the thirty allotted minutes are used, whichever of the two is asked. The tolerance covers the
        // seconds the clock advances between the row being read and its fraction being taken.
        Assert.AreEqual(
            row.RingValue,
            fromText,
            0.01,
            $"The ring reads {row.RingValue:F4} of the allotment while the text beside it reads {fromText:F4} of it (FR-017).");
    }

    [TestMethod]
    public void Helper_RemainingTimePaletteTests_TheRingAndTheTextAgreeAtTheOverdueBoundary()
    {
        // The boundary is where a disagreement would show: past its window the countdown prints "Overdue", the ring
        // is full, and the palette must say overdue however it is asked (FR-014).
        var now = DateTimeOffset.UtcNow;
        var requested = now.AddMinutes(-31);
        var row = new SampleOrder
        {
            Status = "Accepted",
            RequestedUtc = requested,
            AllottedMinutes = 30,
            TargetTimeUtc = requested.AddMinutes(30),
        };
        row.RefreshTimeDerivedText(now);

        Assert.AreEqual("Overdue", row.RemainingTimeText, "A request past its window must print the overdue marking (FR-014).");
        Assert.IsTrue(row.IsOverdue, "A request past its window is overdue (FR-014).");
        Assert.IsTrue(row.RingValue > 1d, "The ring must carry the overrun rather than stopping at a full window (FR-014).");

        Assert.AreEqual(
            RemainingTimePalette.OverdueColour,
            RemainingTimePalette.ColourFor(row.RemainingTimeText, row.IsOverdue),
            "The overdue text is not painted as overdue.");
        Assert.AreEqual(
            RemainingTimePalette.OverdueColour,
            RemainingTimePalette.ColourForFraction(row.RingValue, row.IsOverdue),
            "The overrun the ring carries is not painted as overdue, so the ring and its text disagree (FR-014).");
    }

    [TestMethod]
    public void Helper_RemainingTimePaletteTests_ColourForFraction_PaintsHalfAWindowAmberAndAFullOneRed()
    {
        // The ring's own scale, from nothing used to the whole window gone, and past it for the overrun (FR-013).
        Assert.AreEqual(RemainingTimePalette.ComfortableColour, RemainingTimePalette.ColourForFraction(0.2d, isOverdue: false));
        Assert.AreEqual(RemainingTimePalette.NearDueColour, RemainingTimePalette.ColourForFraction(RemainingTimePalette.NearDueFraction, isOverdue: false));
        Assert.AreEqual(RemainingTimePalette.OverdueColour, RemainingTimePalette.ColourForFraction(RemainingTimePalette.OverdueFraction, isOverdue: false));
        Assert.AreEqual(RemainingTimePalette.OverdueColour, RemainingTimePalette.ColourForFraction(1.4d, isOverdue: false));
    }

    [TestMethod]
    public void Helper_RemainingTimePaletteTests_ColourForFraction_LetsTheOverdueFlagWinOverTheFraction()
    {
        // The store's flag is the authority: a row the store calls overdue reads overdue however little of its
        // window its own timestamps suggest it has used.
        Assert.AreEqual(
            RemainingTimePalette.OverdueColour,
            RemainingTimePalette.ColourForFraction(0.1d, isOverdue: true),
            "The row's overdue flag did not win over its fraction.");
    }

    [TestMethod]
    public void Helper_RemainingTimePaletteTests_FractionFor_TreatsAnUnmeasurableValueAsNoWindowUsedYet()
    {
        // A row with no countdown to read, or no allotment to measure one against, has no ring fraction to state.
        Assert.AreEqual(0d, RemainingTimePalette.FractionFor(null, 30), "A missing countdown must not claim a fraction.");
        Assert.AreEqual(0d, RemainingTimePalette.FractionFor("   ", 30), "A blank countdown must not claim a fraction.");
        Assert.AreEqual(0d, RemainingTimePalette.FractionFor("New", 30), "A row with no target time was given a ring fraction.");
        Assert.AreEqual(0d, RemainingTimePalette.FractionFor("00:10", 0), "An allotment of nothing was given a ring fraction.");
        Assert.AreEqual(0d, RemainingTimePalette.FractionFor("00:10", -5), "An allotment of nothing was given a ring fraction.");
    }

    [TestMethod]
    public void Helper_RemainingTimePaletteTests_FractionFor_ReadsAnOverdueValueAsTheWholeWindowBecauseTheTextCarriesNoOverrun()
    {
        // The countdown prints "Overdue" and no more, so the fraction it can state is the whole allotment used; the
        // overrun lives in the ring's own number, which FractionFor deliberately does not invent (FR-014).
        Assert.AreEqual(
            RemainingTimePalette.OverdueFraction,
            RemainingTimePalette.FractionFor("Overdue", 30),
            "An overdue countdown must read as the whole allotment used.");
    }

    [TestMethod]
    public void Helper_RemainingTimePaletteTests_TheBrushesTheDetailsPageBindsAreWrappedOverTheseColours()
    {
        // A colour the palette decides and a brush it hands out must be one decision: the wrappers read the colour
        // rather than naming one of their own. Read as the artifact that ships, because a brush cannot be built
        // here to compare against.
        var source = File.ReadAllText(Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            PalettePath.Replace('/', Path.DirectorySeparatorChar)));

        foreach (var wrapper in new[]
        {
            "public static Brush Overdue => new SolidColorBrush(OverdueColour);",
            "public static Brush NearDue => new SolidColorBrush(NearDueColour);",
            "public static Brush Comfortable => new SolidColorBrush(ComfortableColour);",
            "new SolidColorBrush(ColourFor(remainingTimeText, isOverdue))",
            "new SolidColorBrush(ColourForFraction(usedFraction, isOverdue))",
        })
        {
            StringAssert.Contains(
                source,
                wrapper,
                "A brush the surface paints with no longer reads the colour this palette decided, so the two can drift apart.");
        }
    }
}
