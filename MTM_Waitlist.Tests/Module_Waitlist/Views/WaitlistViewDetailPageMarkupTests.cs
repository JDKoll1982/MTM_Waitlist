using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Waitlist.Views;

/// <summary>
/// The request page's hero card read as markup: two pictures, and nothing written under either of them
/// (owner decision, 2026-10-26).
/// </summary>
/// <remarks>
/// The page lives in the application project, which this suite deliberately does not reference, so it is read from
/// disk as XML — the same way the lugger arrangement page and the shell are audited.
/// </remarks>
[TestClass]
public sealed class WaitlistViewDetailPageMarkupTests
{
    private static readonly XNamespace s_presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    /// <summary>
    /// Each hero picture draws alone. The two captions that used to sit beneath them said only what the picture
    /// beside the request's own title already says, and they were drawn in the caption colour as though they were
    /// part of the request's data.
    /// </summary>
    [TestMethod]
    public void UI_WaitlistViewDetailPageMarkupTests_TheHeroPictures_DrawWithoutACaptionBeneathThem()
    {
        var page = LoadPage();

        foreach (var automationId in new[]
        {
            "WaitlistViewDetailPage_RequestTypeImage",
            "WaitlistViewDetailPage_WorkCenterImage",
        })
        {
            var picture = page
                .Descendants()
                .SingleOrDefault(element =>
                    (string?)element.Attribute("AutomationProperties.AutomationId") == automationId);

            Assert.IsNotNull(picture, $"the hero card no longer draws '{automationId}'");
            Assert.AreEqual(
                0,
                picture.ElementsAfterSelf().Count(element => element.Name.LocalName is "TextBlock" or "StackPanel"),
                $"'{automationId}' still has something written under it");
        }
    }

    /// <summary>
    /// Both pictures are unnamed literals no longer: the request picture draws the same image the request's card
    /// draws, and the right-hand picture draws the picture of the work centre the request was raised at.
    /// </summary>
    [TestMethod]
    public void UI_WaitlistViewDetailPageMarkupTests_TheHeroPictures_DrawTheCardsPictureAndTheWorkCentres()
    {
        var page = LoadPage();

        var requestPicture = page
            .Descendants()
            .SingleOrDefault(element =>
                (string?)element.Attribute("AutomationProperties.AutomationId") == "WaitlistViewDetailPage_RequestTypeImage");

        var workCenterPicture = page
            .Descendants()
            .SingleOrDefault(element =>
                (string?)element.Attribute("AutomationProperties.AutomationId") == "WaitlistViewDetailPage_WorkCenterImage");

        Assert.IsNotNull(requestPicture);
        Assert.IsNotNull(workCenterPicture);

        StringAssert.Contains(
            (string?)requestPicture.Attribute("Source") ?? string.Empty,
            "ViewModel.Item.EffectiveImagePath",
            "the left picture is not the row's own picture, which is the one the request's card draws");

        StringAssert.Contains(
            (string?)workCenterPicture.Attribute("Source") ?? string.Empty,
            "ViewModel.Item.EffectiveWorkCenterImagePath",
            "the right picture is not the requesting work centre's picture");
    }

    private static XElement LoadPage()
    {
        var path = Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "Module_Waitlist",
            "Views",
            "WaitlistViewDetailPage.xaml");

        Assert.IsTrue(File.Exists(path), $"The request page was not found at '{path}'.");

        return XDocument.Load(path).Root!;
    }
}
