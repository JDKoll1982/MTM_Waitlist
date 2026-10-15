using Microsoft.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Waitlist.Helpers;

using Windows.UI;

namespace MTM_Waitlist.Tests.Module_Waitlist.Helpers;

/// <summary>
/// The request card's status badge: one colour per lifecycle state, each carrying readable text. The badge is
/// the only element on the card whose whole job is to say where a request is, so a single colour for every
/// state wastes it.
/// </summary>
[TestClass]
public sealed class RequestStatusPaletteTests
{
    [TestMethod]
    public void Helper_RequestStatusPaletteTests_EveryLifecycleStateGetsItsOwnRole()
    {
        Assert.AreEqual(RequestStatusBadgeRole.Waiting, RequestStatusPalette.RoleFor("Pending"));
        Assert.AreEqual(RequestStatusBadgeRole.InProgress, RequestStatusPalette.RoleFor("Accepted"));
        Assert.AreEqual(RequestStatusBadgeRole.Done, RequestStatusPalette.RoleFor("Completed"));
        Assert.AreEqual(RequestStatusBadgeRole.Cancelled, RequestStatusPalette.RoleFor("Canceled"));
    }

    [TestMethod]
    public void Helper_RequestStatusPaletteTests_BothSpellingsOfCancelledReachTheSameRole()
    {
        // The store holds one L; the English copy uses two. A colour that depended on the spelling would be a
        // behaviour the data can silently change.
        Assert.AreEqual(
            RequestStatusPalette.RoleFor("Canceled"),
            RequestStatusPalette.RoleFor("Cancelled"),
            "The stored and displayed spellings of cancelled must paint the same badge.");
    }

    [TestMethod]
    public void Helper_RequestStatusPaletteTests_StatusesAreRecognisedRegardlessOfCaseOrSurroundingSpace()
    {
        foreach (var spelling in new[] { "pending", "PENDING", "  Pending  " })
        {
            Assert.AreEqual(
                RequestStatusBadgeRole.Waiting,
                RequestStatusPalette.RoleFor(spelling),
                $"'{spelling}' is a Pending status and must be recognised.");
        }
    }

    [TestMethod]
    public void Helper_RequestStatusPaletteTests_NoStatusMeansNoBadge()
    {
        // The card hides the badge on HasStatusBadge, so an unrecognised status must not paint one either — a
        // transparent pill behind white text would be an empty shape.
        foreach (var missing in new[] { null, "", "   ", "Archived", "released" })
        {
            Assert.AreEqual(
                RequestStatusBadgeRole.None,
                RequestStatusPalette.RoleFor(missing),
                $"'{missing ?? "null"}' is not a lifecycle status, so it must claim no badge.");
            Assert.AreEqual(
                Colors.Transparent,
                RequestStatusPalette.BackgroundFor(missing),
                $"'{missing ?? "null"}' must not paint a background.");
        }
    }

    [TestMethod]
    public void Helper_RequestStatusPaletteTests_NoTwoStatusesShareAColour()
    {
        // The point of the change: four states have to be tellable apart at a glance, so no two may share a
        // colour, and the transport states must not be near-duplicates either.
        var colours = new (string Status, Color Colour)[]
        {
            ("Pending", RequestStatusPalette.BackgroundFor("Pending")),
            ("Accepted", RequestStatusPalette.BackgroundFor("Accepted")),
            ("Completed", RequestStatusPalette.BackgroundFor("Completed")),
            ("Canceled", RequestStatusPalette.BackgroundFor("Canceled")),
        };

        for (var i = 0; i < colours.Length; i++)
        {
            for (var j = i + 1; j < colours.Length; j++)
            {
                Assert.AreNotEqual(
                    colours[i].Colour,
                    colours[j].Colour,
                    $"{colours[i].Status} and {colours[j].Status} share a badge colour.");
            }
        }
    }

    [TestMethod]
    public void Helper_RequestStatusPaletteTests_EveryStatusBadgeIsReadable()
    {
        // This is why the badge does not use the SystemFillColor* theme tokens despite their being the obvious
        // choice: those are built to carry a dark glyph (FCE100 caution yellow among them), so white text on
        // them fails. Every colour here must clear the WCAG AA threshold of 4.5:1 against the badge's text.
        var foreground = RequestStatusPalette.BadgeForeground;

        foreach (var status in new[] { "Pending", "Accepted", "Completed", "Canceled" })
        {
            var contrast = ContrastRatio(RequestStatusPalette.BackgroundFor(status), foreground);

            Assert.IsTrue(
                contrast >= 4.5,
                $"The {status} badge has a contrast ratio of {contrast:F2}:1 against its text, which is below the 4.5:1 needed to read it.");
        }
    }

    [TestMethod]
    public void Helper_RequestStatusPaletteTests_TheBadgeTextIsNotPaintedPerStatus_SoEveryRoleSharesOneForeground()
    {
        // A per-status foreground would be a second thing to keep in step with the backgrounds. One white is
        // enough because the backgrounds are chosen around it, which the contrast test above holds them to.
        Assert.AreEqual(Colors.White, RequestStatusPalette.BadgeForeground);
    }

    [TestMethod]
    public void Helper_RequestStatusPaletteTests_EveryOneOfTheEightStatusWordsGetsARoleAndAColour()
    {
        // FR-102 opened the vocabulary from four words to eight. The four lifecycle words are read from the card
        // as words rather than as stored statuses, so the lookup has to know them by the words it draws.
        foreach (var word in TheEightStatusWords())
        {
            var role = RequestStatusPalette.RoleFor(word);

            Assert.AreNotEqual(
                RequestStatusBadgeRole.None,
                role,
                $"'{word}' is one of the eight status words and must claim a role (FR-102).");
            Assert.AreNotEqual(
                Colors.Transparent,
                RequestStatusPalette.BackgroundFor(word),
                $"'{word}' is one of the eight status words and must claim a colour, or the column draws nothing (FR-102).");
        }
    }

    [TestMethod]
    public void Helper_RequestStatusPaletteTests_EachOfTheFourConditionsResolvesToItsOwnRoleRatherThanNothing()
    {
        // T046: the palette used to answer None outside the four lifecycle words, which is the defect — a handler
        // who reports a condition would have seen no status at all.
        var roles = HandlerStatusCatalog.Conditions
            .Select(RequestStatusPalette.RoleFor)
            .ToList();

        foreach (var condition in HandlerStatusCatalog.Conditions)
        {
            Assert.AreNotEqual(
                RequestStatusBadgeRole.None,
                RequestStatusPalette.RoleFor(condition),
                $"'{condition}' is a reported condition and must not resolve to no role (FR-102).");
        }

        Assert.AreEqual(
            HandlerStatusCatalog.Conditions.Count,
            roles.Distinct().Count(),
            "Two conditions share a role, so a reader cannot tell which one was reported (T046).");
    }

    [TestMethod]
    public void Helper_RequestStatusPaletteTests_AStoredConditionIsRecognisedVerbatimAndAnAlteredCopyIsNot()
    {
        // The vocabulary is closed and ordinal (HandlerStatusCatalog): a differently cased or padded value is not
        // a condition, so the word falls through to the overdue marking or the lifecycle word instead. That is the
        // same rule the row restores a stored value by, so the colour and the word cannot disagree about whether a
        // condition was really reported.
        foreach (var altered in new[] { "product buried", "PRODUCT BURIED", "  Product Buried  ", "Product  Buried", "ProductBuried" })
        {
            Assert.AreEqual(
                RequestStatusBadgeRole.None,
                RequestStatusPalette.RoleFor(altered),
                $"'{altered}' is not one of the four verbatim conditions, so it must claim no condition role.");
        }
    }

    [TestMethod]
    public void Helper_RequestStatusPaletteTests_TheOverdueMarkingGetsItsOwnColourRatherThanNoColourAtAll()
    {
        // FR-014: an overdue request reads `Overdue` and themes the whole column as overdue, so the marking is a
        // state the palette has to answer for like any other.
        Assert.AreNotEqual(
            RequestStatusBadgeRole.None,
            RequestStatusPalette.RoleFor("Overdue"),
            "The overdue marking is a status the column draws and must claim a role (FR-014).");
        Assert.AreNotEqual(
            Colors.Transparent,
            RequestStatusPalette.BackgroundFor("Overdue"),
            "The overdue marking must claim a colour, or FR-014's whole-column theme has nothing to paint (FR-014).");
    }

    [TestMethod]
    public void Helper_RequestStatusPaletteTests_TheOverdueMarkingIsCarriedByItsOwnColourRatherThanTheRowsPosition()
    {
        // FR-020: overdue has to stay its own state under every sort, so the treatment is a function of the
        // marking and of nothing else — it carries no position, and it is not the lifecycle grey, the finished
        // green or the handler-condition red, which is how it stays unmistakable wherever the row happens to sit.
        var overdue = RequestStatusPalette.BackgroundFor("Overdue");

        Assert.AreNotEqual(Colors.Transparent, overdue, "The overdue marking claims no colour at all (FR-014).");
        Assert.AreEqual(
            overdue,
            RequestStatusPalette.BackgroundFor("Overdue"),
            "The overdue treatment depends on something other than the marking, so it cannot survive a re-sort.");

        foreach (var other in new[] { "Pending", "Completed", HandlerStatusCatalog.Conditions[0] })
        {
            Assert.AreNotEqual(
                overdue,
                RequestStatusPalette.BackgroundFor(other),
                $"'{other}' is painted exactly as overdue, so an overdue row is not its own state (FR-020).");
        }
    }

    [TestMethod]
    public void Helper_RequestStatusPaletteTests_ThePaletteOffersNoPercentageFigureForTheRing()
    {
        // FR-021: the status zone draws no percentage, so the palette must not hand one out — a fraction of the
        // allotment is the ring's own scale (0 to 1) and a pre-multiplied percentage is exactly what must not ship.
        var percentageMembers = typeof(RequestStatusPalette)
            .GetMembers()
            .Select(member => member.Name)
            .Where(name => name.Contains("Percent", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.AreEqual(
            0,
            percentageMembers.Count,
            $"The palette offers {string.Join(", ", percentageMembers)}, and the status zone draws no percentage (FR-021).");
    }

    [TestMethod]
    public void Helper_RequestStatusPaletteTests_TheThreeColumnThemesAreDistinctAndTheConditionOneIsTheLighterRed()
    {
        // FR-019, FR-072, SC-007: three themes, and the condition theme is a lighter red than overdue — lighter in
        // fact, not only in the name, because the one confusion the pair exists to prevent is a handler's
        // condition being read as an overdue request or the reverse.
        var normal = RequestStatusPalette.NormalColumnColor;
        var condition = RequestStatusPalette.HandlerConditionColumnColor;
        var overdue = RequestStatusPalette.OverdueColumnColor;

        Assert.AreNotEqual(normal, condition, "The condition theme is the normal theme (FR-019).");
        Assert.AreNotEqual(normal, overdue, "The overdue theme is the normal theme, so overdue is not its own state (FR-019).");
        Assert.AreNotEqual(condition, overdue, "The two reds are one colour, so the two themes are not distinct (FR-072).");

        Assert.IsTrue(
            RelativeLuminance(condition) > RelativeLuminance(overdue),
            $"The handler-condition red is not lighter than the overdue red ({RelativeLuminance(condition):F4} against {RelativeLuminance(overdue):F4}), and FR-072 asks for the lighter one (SC-007).");

        Assert.IsTrue(
            RelativeLuminance(normal) > RelativeLuminance(overdue),
            "The overdue red is not darker than the neutral column, so the two separate by hue alone (FR-020).");
    }

    [TestMethod]
    public void Helper_RequestStatusPaletteTests_AConditionAndTheOverdueMarkingAreNeverPaintedAsEachOther()
    {
        // FR-023, SC-007: the two are never both shown, and the condition takes precedence for as long as it is
        // set — so the condition's colour is the lighter red and never the overdue one, whatever else is true of
        // the row.
        var overdue = RequestStatusPalette.BackgroundFor("Overdue");

        foreach (var condition in HandlerStatusCatalog.Conditions)
        {
            var colour = RequestStatusPalette.BackgroundFor(condition);

            Assert.AreEqual(
                RequestStatusPalette.HandlerConditionColumnColor,
                colour,
                $"'{condition}' is not painted in the handler-condition theme (FR-072).");
            Assert.AreNotEqual(overdue, colour, $"'{condition}' is painted exactly as an overdue request (FR-023).");
        }
    }

    [TestMethod]
    public void Helper_RequestStatusPaletteTests_AStatesColumnColourIsTheColourOfTheThemeItTakes()
    {
        // The column is painted from the theme, so a state's colour and its theme's colour are one decision and
        // not two that could drift apart (FR-019).
        Assert.AreEqual(
            RequestStatusPalette.NormalColumnColor,
            RequestStatusPalette.ColorFor(RequestStatusBadgeRole.Waiting),
            "A lifecycle word is not drawn in the normal theme's colour (FR-019).");
        Assert.AreEqual(
            RequestStatusPalette.OverdueColumnColor,
            RequestStatusPalette.ColorFor(RequestStatusBadgeRole.Overdue),
            "The overdue marking is not drawn in the overdue theme's colour (FR-014).");
        Assert.AreEqual(
            Colors.Transparent,
            RequestStatusPalette.ColorFor(RequestStatusBadgeRole.None),
            "A row with no status must claim no colour, so that nothing is drawn for it (FR-018).");
    }

    [TestMethod]
    public void Helper_RequestStatusPaletteTests_AStoredStatusAndTheWordItIsDrawnAsClaimTheSameRole()
    {
        // The stored status is what the row carries and the word is what the card prints. If the two vocabularies
        // could drift apart, a row would be painted for one state and read as another (FR-012).
        var pairs = new (string Stored, string Word)[]
        {
            ("Pending", "Waiting"),
            ("Accepted", "In Progress"),
            ("Completed", "Done"),
            ("Canceled", "Cancelled"),
        };

        foreach (var (stored, word) in pairs)
        {
            Assert.AreEqual(
                RequestStatusPalette.RoleFor(stored),
                RequestStatusPalette.RoleFor(word),
                $"'{stored}' and the word '{word}' it is drawn as must claim the same role (FR-012).");
        }
    }

    /// <summary>
    /// The eight status words FR-102 settles: the four lifecycle words the card draws, and the four conditions a
    /// handler can report. Read from the one vocabulary rather than re-listed, so the picker and the card cannot
    /// drift apart (FR-104).
    /// </summary>
    private static IReadOnlyList<string> TheEightStatusWords()
    {
        var words = new List<string> { "Waiting", "In Progress", "Done", "Cancelled" };
        words.AddRange(HandlerStatusCatalog.Conditions);
        return words;
    }

    /// <summary>
    /// The WCAG relative-luminance contrast ratio between two opaque colours.
    /// </summary>
    private static double ContrastRatio(Color background, Color foreground)
    {
        var lighter = Math.Max(RelativeLuminance(background), RelativeLuminance(foreground));
        var darker = Math.Min(RelativeLuminance(background), RelativeLuminance(foreground));
        return (lighter + 0.05) / (darker + 0.05);
    }

    private static double RelativeLuminance(Color colour) =>
        (0.2126 * Channel(colour.R)) + (0.7152 * Channel(colour.G)) + (0.0722 * Channel(colour.B));

    private static double Channel(byte value)
    {
        var scaled = value / 255.0;
        return scaled <= 0.03928 ? scaled / 12.92 : Math.Pow((scaled + 0.055) / 1.055, 2.4);
    }
}
