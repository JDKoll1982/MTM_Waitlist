using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Setup.Views;

/// <summary>
/// The work-centre cards answer the same gesture the Continue button does: a double-click on a card chooses that
/// work centre and moves on, so a person who has already found their station does not have to click it and then
/// reach for the button (FR-002).
/// </summary>
[TestClass]
public sealed class SetupWorkCenterPageMarkupTests
{
    private static readonly XNamespace s_presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    /// <summary>
    /// Both work-centre grids — the local ones and the ones behind the expander — carry the double-click handler,
    /// because a card must behave the same wherever it is listed.
    /// </summary>
    [TestMethod]
    public void UI_SetupWorkCenterPageMarkupTests_EveryWorkCenterGrid_AnswersADoubleClick()
    {
        var grids = LoadPage()
            .Descendants(s_presentation + "GridView")
            .Where(grid => (string?)grid.Attribute("ItemClick") is not null)
            .ToArray();

        Assert.AreEqual(2, grids.Length, "The page's two work-centre grids carry the card click handler.");

        foreach (var grid in grids)
        {
            Assert.AreEqual(
                "WorkCenterCard_DoubleTapped",
                (string?)grid.Attribute("DoubleTapped"),
                "A work-centre grid lost its double-click handler, so a double-click again does nothing but re-select.");
        }
    }

    /// <summary>
    /// The gesture lands on the cards, not on the buttons beside them: the Continue button keeps its own command,
    /// so the double-click is an addition to the page rather than a replacement of it.
    /// </summary>
    [TestMethod]
    public void UI_SetupWorkCenterPageMarkupTests_ContinueButton_KeepsItsOwnCommand()
    {
        var continueButton = LoadPage()
            .Descendants(s_presentation + "Button")
            .SingleOrDefault(button =>
                (string?)button.Attribute("Command") == "{x:Bind ViewModel.ContinueCommand}");

        Assert.IsNotNull(continueButton, "The Continue button no longer carries the Continue command (FR-002).");
    }

    private static XDocument LoadPage()
    {
        var path = Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "Module_Setup",
            "Views",
            "SetupWorkCenterPage.xaml");

        Assert.IsTrue(File.Exists(path), $"The work-centre markup was not found at '{path}'.");

        return XDocument.Load(path);
    }
}
