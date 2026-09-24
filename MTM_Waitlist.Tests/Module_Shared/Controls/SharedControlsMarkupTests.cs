using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Shared.Controls;

/// <summary>
/// The two shared controls of 009 and the surfaces that use them: every content picture is drawn by the
/// click-to-enlarge control (A3), and the shared card's title is one line drawn by the auto-scrolling text control
/// (B1, B2). These are the checks that fail when a later edit hand-rolls either rule again.
/// </summary>
[TestClass]
public sealed class SharedControlsMarkupTests
{
    /// <summary>The one picture rule, in both of its spellings: a content picture always goes through a resolver.</summary>
    private static readonly string[] s_pictureResolvers =
    [
        "ResolvedImagePathToSourceConverter",
        "SetupImagePathToImageSourceConverter",
        "PartPictureCardImageSourceConverter",
    ];

    /// <summary>
    /// No content picture is drawn by a bare <c>Image</c> element any more (A3). Each of the surfaces that used to
    /// was converted, so a new one added without the control fails here rather than quietly missing the behaviour.
    /// </summary>
    [TestMethod]
    public void EveryContentPicture_IsDrawnByTheSharedControl()
    {
        var violations = new List<string>();

        foreach (var file in XamlFiles())
        {
            var document = XDocument.Load(file);

            violations.AddRange(document
                .Descendants()
                .Where(element => element.Name.LocalName == "Image")
                .Where(element => s_pictureResolvers.Any(resolver =>
                    ((string?)element.Attribute("Source"))?.Contains(resolver, StringComparison.Ordinal) == true))
                .Select(_ => Path.GetRelativePath(RepositoryPatternScan.FindRepositoryRoot(), file)));
        }

        Assert.AreEqual(
            0,
            violations.Count,
            "These files draw a content picture with a bare Image element instead of the shared control, so the "
                + "picture cannot be enlarged and the one behaviour is implemented twice:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, violations.Distinct(StringComparer.Ordinal)));
    }

    /// <summary>
    /// The shared card's title is the one-line control, and no longer a wrapping TextBlock — the wrap is what let
    /// a long value push the card out of shape or get cut off.
    /// </summary>
    [TestMethod]
    public void TheSharedCardTitle_IsDrawnByTheOneLineScrollingText()
    {
        var card = Load("Styles", "PartPictureCardTemplate.xaml");

        var title = card
            .Descendants()
            .FirstOrDefault(element => element.Name.LocalName == "AutoScrollTextView"
                && ((string?)element.Attribute("Text"))?.Contains("Title", StringComparison.Ordinal) == true);

        Assert.IsNotNull(title, "The card's title is not drawn by the one-line scrolling text control.");

        Assert.AreEqual(
            0,
            card.Descendants()
                .Count(element => element.Name.LocalName == "TextBlock"
                    && ((string?)element.Attribute("Text"))?.Contains("Title", StringComparison.Ordinal) == true),
            "The card still draws its title with a TextBlock, which is what wrapped.");
    }

    /// <summary>The text control holds its line: one line, no wrapping, and a viewport that hides the overflow.</summary>
    [TestMethod]
    public void TheTextControl_KeepsItsLineOnOneLine()
    {
        var control = Load("Module_Shared", "Controls", "AutoScrollTextView.xaml");

        var line = control
            .Descendants()
            .FirstOrDefault(element => element.Name.LocalName == "TextBlock");

        Assert.IsNotNull(line, "The text control has no line to draw.");
        Assert.AreEqual("1", (string?)line!.Attribute("MaxLines"), "The line must be limited to one line.");
        Assert.AreEqual("NoWrap", (string?)line.Attribute("TextWrapping"), "The line must not wrap; it scrolls instead.");
        Assert.IsNotNull(
            control.Descendants().FirstOrDefault(element => element.Name.LocalName == "RectangleGeometry"),
            "The line is not clipped to its field, so the part that scrolls would be drawn outside it.");
    }

    /// <summary>
    /// The picture control draws the picture it was handed and owns the enlarged view, so a surface that uses it
    /// gets the behaviour without declaring anything of its own.
    /// </summary>
    [TestMethod]
    public void ThePictureControl_OwnsTheEnlargedView()
    {
        var control = Load("Module_Shared", "Controls", "ClickToEnlargeImageView.xaml");

        var picture = control
            .Descendants()
            .FirstOrDefault(element => element.Name.LocalName == "Image");

        Assert.IsNotNull(picture, "The picture control has no picture in it.");
        StringAssert.Contains(
            (string?)picture!.Attribute("Source") ?? string.Empty,
            "Source",
            "The picture must be the one the surface handed over, not a literal of its own.");

        var codeBehind = File.ReadAllText(Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "Module_Shared",
            "Controls",
            "ClickToEnlargeImageView.xaml.cs"));

        StringAssert.Contains(codeBehind, "ContentDialog", "The enlarged view must be the control's own dialog.");
        StringAssert.Contains(
            codeBehind,
            "FullSizeDesired = true",
            "The enlarged view must be shown full size, which is the whole point of enlarging.");

        var resources = File.ReadAllText(Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "Strings",
            "en-us",
            "Resources.resw"));

        StringAssert.Contains(
            resources,
            "Shared_ClickToEnlarge.CloseButton",
            "The enlarged view needs a close affordance the reader can find.");
    }

    private static XDocument Load(params string[] relativeParts)
    {
        var path = Path.Combine([RepositoryPatternScan.FindRepositoryRoot(), .. relativeParts]);

        Assert.IsTrue(File.Exists(path), $"The markup was not found at '{path}'.");

        return XDocument.Load(path);
    }

    private static IEnumerable<string> XamlFiles() =>
        RepositoryPatternScan.EnumerateScannedFiles(
            RepositoryPatternScan.FindRepositoryRoot(),
            new RepositoryScanScope { Extensions = [".xaml"] });
}
