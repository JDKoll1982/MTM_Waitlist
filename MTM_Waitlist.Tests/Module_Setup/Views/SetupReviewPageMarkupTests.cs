using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Setup.Models;
using MTM_Waitlist.Module_Shared.Helpers;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Setup.Views;

/// <summary>
/// The Setup review's subordinate-part rows each carry <b>the part's own picture</b>, and stay grouped by family
/// exactly as they were. A part the application cannot picture draws the one shared placeholder — never a blank
/// space, and never the family's artwork standing in for the part (FR-014, FR-015, FR-018).
/// </summary>
[TestClass]
public sealed class SetupReviewPageMarkupTests
{
    private static readonly XNamespace s_presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    /// <summary>The shared picture control, which is what every surface draws a content picture with (009 A3).</summary>
    private const string PictureControlName = "ClickToEnlargeImageView";

    /// <summary>
    /// The row's picture box holds an image bound to the part's own picture. Without it the row is text only,
    /// which is what this feature removes.
    /// </summary>
    [TestMethod]
    public void SubordinatePartRow_DrawsThePartsPicture()
    {
        var image = SubordinatePartRowImage();

        Assert.IsTrue(
            ((string?)image.Attribute("Source"))!.Contains("ImagePath", StringComparison.Ordinal),
            "The review row's picture must come from the row's own part picture, not from a literal.");
        Assert.IsTrue(
            ((string?)image.Attribute("Source"))!.Contains("ResolvedImagePathToSourceConverter", StringComparison.Ordinal),
            "The picture must go through the resolver that applies the application's one picture rule (FR-013).");
    }

    /// <summary>
    /// The row's picture is drawn as a fixed-size thumbnail. A frame bounded only by Min* measured in an
    /// auto-width column sizes itself to the picture's own natural size, which blew the row up around a large
    /// picture.
    /// </summary>
    [TestMethod]
    public void SubordinatePartRow_DrawsThePictureInAFixedSizeThumbnail()
    {
        var frame = SubordinatePartRowImage().Parent;

        Assert.IsNotNull(frame, "The review row's picture has no frame around it, so nothing bounds its size.");

        static bool Declares(XElement element, string attribute) =>
            !string.IsNullOrWhiteSpace((string?)element.Attribute(attribute));

        Assert.IsTrue(
            (Declares(frame!, "Width") && Declares(frame!, "Height"))
                || (Declares(frame!, "MaxWidth") && Declares(frame!, "MaxHeight")),
            "The review row bounds its picture with Min* only, so a large picture is drawn at its own natural size instead of as a thumbnail.");
    }

    /// <summary>
    /// The group header names the family and draws no picture of its own. A picture on the header would be family
    /// artwork offered in place of a part's picture, which FR-015 forbids.
    /// </summary>
    [TestMethod]
    public void GroupHeader_DrawsNoFamilyArtwork()
    {
        var header = LoadPage()
            .Descendants(s_presentation + "TextBlock")
            .Where(text => ((string?)text.Attribute("Text"))?.Contains("CategoryDisplayName", StringComparison.Ordinal) == true)
            .Select(text => text.Parent)
            .FirstOrDefault(parent => parent is not null);

        Assert.IsNotNull(header, "The subordinate-parts group header is gone.");
        Assert.AreEqual(
            0,
            header!.Descendants(s_presentation + "Image").Count(),
            "The group header draws a picture, which would be family artwork standing in for an unpictured part (FR-015).");
    }

    /// <summary>
    /// The surface has a placeholder path: a row whose picture was never resolved draws the one shared no-image
    /// picture rather than an empty source. This is the check that fails if a new surface is added without one.
    /// </summary>
    [TestMethod]
    public void SubordinatePartRow_FallsBackToTheOneSharedPlaceholder()
    {
        Assert.AreEqual(
            ImagePicturePolicy.NoImagePath,
            new SetupSubordinatePart { PartNumber = "MMC0001000" }.ImagePath,
            "A row that resolved no picture must still name the one shared placeholder (FR-014).");
    }

    private static XElement SubordinatePartRowImage()
    {
        var page = LoadPage();

        var image = page
            .Descendants()
            .FirstOrDefault(candidate => candidate.Name.LocalName == PictureControlName
                && ((string?)candidate.Attribute("Source"))?.Contains("ImagePath", StringComparison.Ordinal) == true);

        Assert.IsNotNull(image, "The Setup review's subordinate-part row no longer draws the part's picture (FR-018).");

        var row = image!.Ancestors(s_presentation + "Grid")
            .FirstOrDefault(grid => ((string?)grid.Attribute("AutomationProperties.Name"))?.Contains("PartNumber", StringComparison.Ordinal) == true);

        Assert.IsNotNull(row, "The picture does not belong to a subordinate-part row.");

        return image;
    }

    private static XDocument LoadPage()
    {
        var path = Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "Module_Setup",
            "Views",
            "SetupReviewPage.xaml");

        Assert.IsTrue(File.Exists(path), $"The review markup was not found at '{path}'.");

        return XDocument.Load(path);
    }
}
