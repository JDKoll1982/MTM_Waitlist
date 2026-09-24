using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Shared.Helpers;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Waitlist.Views;

/// <summary>
/// The wizard surfaces that name a part draw it with its picture: the die card and the confirmation step. Each
/// falls back to the one shared placeholder rather than a blank space, and none of them draws family or category
/// artwork in place of a part's picture (FR-014, FR-015, FR-018, FR-023).
/// </summary>
/// <remarks>
/// The preview step was one of these surfaces until 2026-09-24, when it was removed as a duplicate of the
/// confirmation step; the checks it carried are the same rule the confirmation step's checks assert.
/// </remarks>
[TestClass]
public sealed class NewRequestPartPictureMarkupTests
{
    private static readonly XNamespace s_presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    /// <summary>The shared picture control, which is what every surface draws a content picture with (009 A3).</summary>
    private const string PictureControlName = "ClickToEnlargeImageView";

    /// <summary>The die card is no longer text only: it draws the die's own picture.</summary>
    [TestMethod]
    public void DieCard_DrawsThePartsPicture()
    {
        var image = ImageBoundTo("ImagePath", "NewRequestDiePage.xaml");

        Assert.IsNotNull(image, "The die card is still text only (FR-018).");
        Assert.IsTrue(
            ((string?)image!.Attribute("Source"))!.Contains("ResolvedImagePathToSourceConverter", StringComparison.Ordinal),
            "The picture must go through the resolver that applies the application's one picture rule (FR-013).");
    }

    /// <summary>The confirmation step draws the part being asked for, with its picture.</summary>
    [TestMethod]
    public void ConfirmationStep_DrawsThePartWithItsPicture()
    {
        var image = ImageBoundTo("PartPicturePath", "NewRequestSummaryPage.xaml");

        Assert.IsNotNull(image, "The confirmation step does not draw the part's picture (FR-018).");
        Assert.IsTrue(
            ((string?)image!.Attribute("Source"))!.Contains("ResolvedImagePathToSourceConverter", StringComparison.Ordinal),
            "The picture must go through the resolver that applies the application's one picture rule (FR-013).");
        Assert.IsTrue(
            PageBinds("NewRequestSummaryPage.xaml", "PartNumber"),
            "The confirmation step no longer names the part it is asking for (FR-018).");
    }

    /// <summary>The die card draws its picture as a fixed-size thumbnail. A frame bounded only by Min* is measured
    /// against the picture's own natural size, so a large picture is drawn full size inside the card.</summary>
    [TestMethod]
    public void DieCard_DrawsThePictureInAFixedSizeThumbnail()
    {
        AssertPictureFrameIsBounded("NewRequestDiePage.xaml");
    }

    /// <summary>
    /// The confirmation step draws its picture as a fixed-size thumbnail. This is the defect that shipped: with
    /// only Min* on the frame, the picture was drawn at its own natural size and filled the step, pushing the part
    /// number aside.
    /// </summary>
    [TestMethod]
    public void ConfirmationStep_DrawsThePictureInAFixedSizeThumbnail()
    {
        AssertPictureFrameIsBounded("NewRequestSummaryPage.xaml");
    }

    /// <summary>
    /// Every model behind those surfaces has a placeholder path: a part that cannot be pictured draws the one
    /// shared no-image picture rather than an empty source. A surface added without one fails here (FR-014).
    /// </summary>
    [TestMethod]
    public void EveryWizardPartSurface_FallsBackToTheOneSharedPlaceholder()
    {
        Assert.AreEqual(
            ImagePicturePolicy.NoImagePath,
            new NewRequestDieOption { Title = "FGT0002000" }.ImagePath,
            "A die that cannot be pictured must still name the one shared placeholder (FR-014).");
        Assert.AreEqual(
            ImagePicturePolicy.NoImagePath,
            new NewRequestComponentOption { PartNumber = "12345-6" }.ImagePath,
            "A component that cannot be pictured must still name the one shared placeholder (FR-014, FR-023).");
    }

    private static bool PageBinds(string fileName, string propertyName) =>
        File.ReadAllText(Path.Combine(RepositoryPatternScan.FindRepositoryRoot(), "Module_Waitlist", "Views", fileName))
            .Contains($"ViewModel.{propertyName}", StringComparison.Ordinal);

    /// <summary>
    /// The box a surface's picture sits in must be bounded on both axes — a fixed Width and Height, or a pair of
    /// Max* bounds. Min* alone is not a bound: the frame then measures the picture at its natural size and the
    /// picture is drawn full size.
    /// </summary>
    private static void AssertPictureFrameIsBounded(string fileName)
    {
        var image = ImageBoundTo("ResolvedImagePathToSourceConverter", fileName);

        Assert.IsNotNull(image, $"{fileName} no longer draws a part picture (FR-018).");

        var frame = image!.Parent;

        Assert.IsNotNull(frame, $"{fileName} draws its picture with no frame around it, so nothing bounds its size.");

        static bool Declares(XElement element, string attribute) =>
            !string.IsNullOrWhiteSpace((string?)element.Attribute(attribute));

        Assert.IsTrue(
            (Declares(frame!, "Width") && Declares(frame!, "Height"))
                || (Declares(frame!, "MaxWidth") && Declares(frame!, "MaxHeight")),
            $"{fileName} bounds its picture with Min* only, so a large picture is drawn at its own natural size instead of as a thumbnail.");
    }

    private static XElement? ImageBoundTo(string propertyName, string fileName)
    {
        var path = Path.Combine(RepositoryPatternScan.FindRepositoryRoot(), "Module_Waitlist", "Views", fileName);
        Assert.IsTrue(File.Exists(path), $"The markup was not found at '{path}'.");

        return XDocument.Load(path)
            .Descendants()
            .FirstOrDefault(candidate => candidate.Name.LocalName == PictureControlName
                && ((string?)candidate.Attribute("Source"))?.Contains(propertyName, StringComparison.Ordinal) == true);
    }
}
