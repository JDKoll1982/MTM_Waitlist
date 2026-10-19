using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Tests.Module_Settings.Models;

/// <summary>
/// The status a lugger is drawn in: one colour per status, readable white text over each, and a colour for a status
/// this build does not know (FR-012, FR-019).
/// </summary>
[TestClass]
public sealed class LuggerStatusBadgeTests
{
    [TestMethod]
    public void Model_LuggerStatusBadgeTests_BackgroundFor_GivesEachStatusItsOwnColour()
    {
        var colours = new[]
        {
            LuggerStatus.Available,
            LuggerStatus.Backup,
            LuggerStatus.Full,
            LuggerStatus.DoNotUse,
        }
            .Select(LuggerStatusBadge.BackgroundFor)
            .ToArray();

        Assert.AreEqual(4, colours.Distinct(StringComparer.OrdinalIgnoreCase).Count(), "Two statuses share a colour, so a blocked lugger looks like one that can take material.");

        foreach (var colour in colours)
        {
            StringAssert.StartsWith(colour, "#FF", "A badge colour is not opaque, so the card shows through its words.");
            Assert.AreEqual(9, colour.Length, $"'{colour}' is not an #AARRGGBB colour.");
        }
    }

    [TestMethod]
    public void Model_LuggerStatusBadgeTests_BackgroundFor_TrimsAndFallsBackForAnUnknownStatus()
    {
        Assert.AreEqual(
            LuggerStatusBadge.Available,
            LuggerStatusBadge.BackgroundFor("  Available  "),
            "A stored status with padding around it is still that status.");

        Assert.AreEqual(
            LuggerStatusBadge.Unknown,
            LuggerStatusBadge.BackgroundFor("Some Other Status"),
            "A status this build does not know draws the neutral badge rather than nothing at all.");

        Assert.AreEqual(LuggerStatusBadge.Unknown, LuggerStatusBadge.BackgroundFor(null), "No status at all draws the neutral badge.");
    }
}
