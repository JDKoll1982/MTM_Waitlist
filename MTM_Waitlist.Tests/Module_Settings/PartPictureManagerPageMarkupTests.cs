using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Settings;

/// <summary>
/// The part-picture screen's two lists — the change record and the parts still to photograph — scroll inside the
/// screen (FR-026). The screen has no scroll viewer of its own, so a list measured with unlimited height grows to
/// its full content height and nothing can scroll it: with 192 parts that still have no picture, most of the list
/// sat below the bottom edge of the window and no wheel could reach it.
/// </summary>
[TestClass]
public sealed class PartPictureManagerPageMarkupTests
{
    private static readonly XNamespace s_presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    /// <summary>
    /// Neither list is measured with unlimited height, and each is given a vertical scroll bar of its own. A
    /// vertical <c>StackPanel</c> is the trap: it offers every child as much height as the child asks for, so a
    /// list inside one is never the thing that scrolls.
    /// </summary>
    [TestMethod]
    public void PartPictureLists_ScrollTheirOwnContent_InsteadOfGrowingPastTheWindow()
    {
        foreach (var listId in new[] { "PartPictureManagerPage_ChangeRecord", "PartPictureManagerPage_MissingList" })
        {
            var list = ListWithId(listId);

            var unboundedParent = list
                .Ancestors()
                .FirstOrDefault(candidate => candidate.Name == s_presentation + "StackPanel" && IsVertical(candidate));

            Assert.IsNull(
                unboundedParent,
                $"{listId} is measured inside a vertical StackPanel, which hands it unlimited height: the list grows "
                    + "to its full content height and nothing scrolls it, so a long list runs off the bottom of the window.");

            Assert.IsNotNull(
                list.Attribute("ScrollViewer.VerticalScrollBarVisibility"),
                $"{listId} does not ask for a vertical scroll bar of its own, so a wheel over it has nothing to scroll.");
        }
    }

    private static XElement ListWithId(string automationId)
    {
        var page = XDocument.Load(Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "Module_Settings",
            "Views",
            "PartPictureManagerPage.xaml"));

        var list = page
            .Descendants(s_presentation + "ListView")
            .FirstOrDefault(candidate => (string?)candidate.Attribute("AutomationProperties.AutomationId") == automationId);

        Assert.IsNotNull(list, $"'{automationId}' was not found in the part-picture screen's markup.");

        return list!;
    }

    private static bool IsVertical(XElement stackPanel) =>
        (string?)stackPanel.Attribute("Orientation") is null or "Vertical";
}
