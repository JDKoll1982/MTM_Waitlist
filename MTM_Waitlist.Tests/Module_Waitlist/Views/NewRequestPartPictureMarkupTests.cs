using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Shared.Helpers;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Waitlist.Views;

/// <summary>
/// The three wizard surfaces that name a part draw it with its picture: the die card, the confirmation step and
/// the preview. Each falls back to the one shared placeholder rather than a blank space, and none of them draws
/// family or category artwork in place of a part's picture (FR-014, FR-015, FR-018, FR-023).
/// </summary>
[TestClass]
public sealed class NewRequestPartPictureMarkupTests
{
    private static readonly XNamespace s_presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

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

    /// <summary>The preview draws the same list the same way as the confirmation step.</summary>
    [TestMethod]
    public void Preview_DrawsThePartWithItsPicture()
    {
        var image = ImageBoundTo("PartPicturePath", "NewRequestPreviewPage.xaml");

        Assert.IsNotNull(image, "The preview does not draw the part's picture (FR-018).");
        Assert.IsTrue(
            ((string?)image!.Attribute("Source"))!.Contains("ResolvedImagePathToSourceConverter", StringComparison.Ordinal),
            "The picture must go through the resolver that applies the application's one picture rule (FR-013).");
        Assert.IsTrue(
            PageBinds("NewRequestPreviewPage.xaml", "PartNumber"),
            "The preview no longer names the part it is asking for (FR-018).");
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

    private static XElement? ImageBoundTo(string propertyName, string fileName)
    {
        var path = Path.Combine(RepositoryPatternScan.FindRepositoryRoot(), "Module_Waitlist", "Views", fileName);
        Assert.IsTrue(File.Exists(path), $"The markup was not found at '{path}'.");

        return XDocument.Load(path)
            .Descendants(s_presentation + "Image")
            .FirstOrDefault(candidate => ((string?)candidate.Attribute("Source"))?.Contains(propertyName, StringComparison.Ordinal) == true);
    }
}
