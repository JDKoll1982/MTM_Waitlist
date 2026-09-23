using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Setup.Models;
using MTM_Waitlist.Module_Shared.Helpers;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Setup.Views;

/// <summary>
/// The Setup part list's entries carry each part's picture, and choosing one still selects the part
/// (FR-018, FR-021). A part the application cannot picture is still offered, drawing the one shared placeholder
/// rather than a blank space (FR-014, FR-023).
/// </summary>
[TestClass]
public sealed class SetupPartSelectionPageMarkupTests
{
    private static readonly XNamespace s_presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    /// <summary>Each entry draws the part's own picture, through the application's one picture rule.</summary>
    [TestMethod]
    public void PartListEntry_DrawsThePartsPicture()
    {
        var image = LoadPage()
            .Descendants(s_presentation + "Image")
            .FirstOrDefault(candidate => ((string?)candidate.Attribute("Source"))?.Contains("ImagePath", StringComparison.Ordinal) == true);

        Assert.IsNotNull(image, "The Setup part list's entries no longer carry the part's picture (FR-018).");

        var source = (string?)image!.Attribute("Source") ?? string.Empty;

        Assert.IsTrue(
            source.Contains("{x:Bind ImagePath", StringComparison.Ordinal),
            "The entry's picture must come from the entry's own part picture, not from a literal.");
        Assert.IsTrue(
            source.Contains("ResolvedImagePathToSourceConverter", StringComparison.Ordinal),
            "The picture must go through the resolver that applies the application's one picture rule (FR-013).");
    }

    /// <summary>
    /// Choosing an entry still selects the part: the list stays a single-selection list whose selected item is
    /// bound both ways, so converting the entry into a card did not turn the step into a display-only one.
    /// </summary>
    [TestMethod]
    public void PartList_StillChoosesThePart()
    {
        var list = LoadPage()
            .Descendants(s_presentation + "ListView")
            .FirstOrDefault(candidate => ((string?)candidate.Attribute("AutomationProperties.AutomationId"))?.Contains("SetupPartSelectionPage_PartList", StringComparison.Ordinal) == true);

        Assert.IsNotNull(list, "The Setup part list is gone.");
        Assert.AreEqual("Single", (string?)list!.Attribute("SelectionMode"), "The part list must still choose exactly one part.");
        Assert.AreEqual(
            "{x:Bind ViewModel.SelectedPart, Mode=TwoWay}",
            ((string?)list.Attribute("SelectedItem"))?.Trim(),
            "The chosen part must still travel back to the step, not only be highlighted.");
        Assert.AreEqual(
            "{x:Bind ViewModel.Parts, Mode=OneWay}",
            ((string?)list.Attribute("ItemsSource"))?.Trim(),
            "The list must still be bound to the parts the step found.");
    }

    /// <summary>
    /// The surface has a placeholder path: an entry whose picture was never resolved draws the one shared
    /// no-image picture rather than an empty source (FR-014, FR-023).
    /// </summary>
    [TestMethod]
    public void PartListEntry_FallsBackToTheOneSharedPlaceholder()
    {
        var entry = new SetupPartResult { PartNumber = "MMC0001000" };

        Assert.AreEqual(
            ImagePicturePolicy.NoImagePath,
            entry.ImagePath,
            "An entry that resolved no picture must still name the one shared placeholder (FR-014).");
        Assert.AreEqual(
            "SetupPartSelectionPage_Part_MMC0001000",
            entry.AutomationId,
            "The entry stays findable by the part it offers.");
    }

    private static XDocument LoadPage()
    {
        var path = Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "Module_Setup",
            "Views",
            "SetupPartSelectionPage.xaml");

        Assert.IsTrue(File.Exists(path), $"The part list markup was not found at '{path}'.");

        return XDocument.Load(path);
    }
}
