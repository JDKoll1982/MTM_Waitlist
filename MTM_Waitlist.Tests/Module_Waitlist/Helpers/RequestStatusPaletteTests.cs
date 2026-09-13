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
    public void EveryLifecycleStateGetsItsOwnRole()
    {
        Assert.AreEqual(RequestStatusBadgeRole.Waiting, RequestStatusPalette.RoleFor("Pending"));
        Assert.AreEqual(RequestStatusBadgeRole.InProgress, RequestStatusPalette.RoleFor("Accepted"));
        Assert.AreEqual(RequestStatusBadgeRole.Done, RequestStatusPalette.RoleFor("Completed"));
        Assert.AreEqual(RequestStatusBadgeRole.Cancelled, RequestStatusPalette.RoleFor("Canceled"));
    }

    [TestMethod]
    public void BothSpellingsOfCancelledReachTheSameRole()
    {
        // The store holds one L; the English copy uses two. A colour that depended on the spelling would be a
        // behaviour the data can silently change.
        Assert.AreEqual(
            RequestStatusPalette.RoleFor("Canceled"),
            RequestStatusPalette.RoleFor("Cancelled"),
            "The stored and displayed spellings of cancelled must paint the same badge.");
    }

    [TestMethod]
    public void StatusesAreRecognisedRegardlessOfCaseOrSurroundingSpace()
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
    public void NoStatusMeansNoBadge()
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
    public void NoTwoStatusesShareAColour()
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
    public void EveryStatusBadgeIsReadable()
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
    public void TheBadgeTextIsNotPaintedPerStatus_SoEveryRoleSharesOneForeground()
    {
        // A per-status foreground would be a second thing to keep in step with the backgrounds. One white is
        // enough because the backgrounds are chosen around it, which the contrast test above holds them to.
        Assert.AreEqual(Colors.White, RequestStatusPalette.BadgeForeground);
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
