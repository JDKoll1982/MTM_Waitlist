using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Tests.Module_Settings.Models;

/// <summary>
/// One lugger as its card reads it: the words the card draws, including the two actions on the card itself, which
/// have to resolve to something because a button labelled with nothing is a box a reader cannot identify
/// (FR-030, FR-104).
/// </summary>
[TestClass]
public sealed class LuggerArrangementRowTests
{
    [TestMethod]
    public void Model_LuggerArrangementRowTests_Actions_CarryTheShippedWords()
    {
        var row = new LuggerArrangementRow { Name = "Door 6 Lugger", Status = LuggerStatus.Available };

        Assert.AreEqual(
            "Settings_Lugger_Arrangement.EditAction".GetLocalized(),
            row.EditActionText,
            "The card's Edit action reads the shipped word, not a literal and not the page's view model.");
        Assert.AreEqual(
            "Settings_Lugger_Arrangement.RenameAction".GetLocalized(),
            row.RenameActionText,
            "The card's Rename action reads the shipped word.");

        Assert.IsFalse(string.IsNullOrWhiteSpace(row.EditActionText), "The Edit button would be drawn with no text.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(row.RenameActionText), "The Rename button would be drawn with no text.");
    }

    [TestMethod]
    public void Model_LuggerArrangementRowTests_Status_IsDrawnInItsOwnColour()
    {
        var full = new LuggerArrangementRow { Name = "Door 9 Lugger", Status = LuggerStatus.Full };

        Assert.AreEqual(LuggerStatusBadge.Full, full.StatusBadgeBackground, "A full lugger is not drawn in the full colour.");
        Assert.AreEqual(LuggerStatusBadge.Foreground, full.StatusBadgeForeground, "The badge's text colour is not the one the palette was chosen against.");
        Assert.AreEqual("Settings_Lugger_Arrangement.StatusFull".GetLocalized(), full.StatusText, "The badge's word is not the shipped one.");
    }

    [TestMethod]
    public void Model_LuggerArrangementRowTests_StatusReason_ReadsBesideTheStatusWhileItIsRequired()
    {
        var blocked = new LuggerArrangementRow
        {
            Name = "Scrap Trailer",
            Status = LuggerStatus.DoNotUse,
            StatusReason = "Towed away",
        };

        StringAssert.Contains(blocked.StatusAndReasonText, "Towed away", "A blocked lugger's reason is not readable on its card.");

        var available = new LuggerArrangementRow { Name = "Door 6 Lugger", Status = LuggerStatus.Available };
        Assert.AreEqual(available.StatusText, available.StatusAndReasonText, "An available lugger's badge reads as the status alone.");
    }
}
