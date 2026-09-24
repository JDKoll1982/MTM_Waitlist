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
    /// <summary>The presentation namespace, so elements are matched by type rather than by local name alone.</summary>
    private static readonly XNamespace s_presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    /// <summary>The XAML language namespace, for the resource keys a control declares for itself.</summary>
    private static readonly XNamespace s_xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

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
    /// <remarks>
    /// An <c>ImageBrush</c> carrying a picture resolver is flagged the same way: the two work-centre photo cards
    /// used to draw their photo that way, which put the photo out of the control's reach — no enlarging, and no
    /// gesture that knows the card's own click comes first.
    /// </remarks>
    [TestMethod]
    public void EveryContentPicture_IsDrawnByTheSharedControl()
    {
        string[] pictureCarriers = ["Image", "ImageBrush"];

        var violations = new List<string>();

        foreach (var file in XamlFiles())
        {
            var relativePath = Path.GetRelativePath(RepositoryPatternScan.FindRepositoryRoot(), file);
            var document = XDocument.Load(file);

            violations.AddRange(document
                .Descendants()
                .Where(element => pictureCarriers.Contains(element.Name.LocalName, StringComparer.Ordinal))
                .Where(element => s_pictureResolvers.Any(resolver =>
                    ((string?)element.Attribute("Source"))?.Contains(resolver, StringComparison.Ordinal) == true
                        || ((string?)element.Attribute("ImageSource"))?.Contains(resolver, StringComparison.Ordinal) == true))
                .Select(_ => relativePath));
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
    /// <remarks>
    /// The enlarged view is the control's own popup covering the page, not a dialog: it has to cover the page the
    /// picture was clicked on, keep that page visible through a semi-transparent backdrop, close when the backdrop
    /// is clicked, and carry its close affordance as an X in the top-right corner. None of that is visible to the
    /// suite without rendering, so these checks read the markup and the code behind.
    /// </remarks>
    [TestMethod]
    public void ThePictureControl_OwnsTheEnlargedView()
    {
        var control = Load("Module_Shared", "Controls", "ClickToEnlargeImageView.xaml");

        var picture = control
            .Descendants(s_presentation + "Image")
            .FirstOrDefault(element => element.Attribute("Source") is not null);

        Assert.IsNotNull(picture, "The picture control has no picture in it.");
        StringAssert.Contains(
            (string?)picture!.Attribute("Source") ?? string.Empty,
            "Source",
            "The picture must be the one the surface handed over, not a literal of its own.");

        // A dialog cannot do the job: it is measured against its content rather than the page, it draws chrome of
        // its own, and it cannot be dismissed by clicking around it.
        var popup = control.Descendants().FirstOrDefault(element => element.Name.LocalName == "Popup");

        Assert.IsNotNull(popup, "The enlarged view must be the control's own popup, so that it can cover the page.");
        Assert.AreEqual(
            "False",
            (string?)popup!.Attribute("IsLightDismissEnabled"),
            "The view must not light-dismiss: that also closes it when the application loses focus, which is not one of the three ways out.");

        var backdrop = popup!.Descendants(s_presentation + "Grid").FirstOrDefault();

        Assert.IsNotNull(backdrop, "The enlarged view has no backdrop for the page to show through.");
        Assert.IsNotNull(backdrop!.Attribute("Tapped"), "A click on the backdrop must close the enlarged view.");
        Assert.IsNotNull(
            backdrop.Attribute("KeyDown"),
            "Esc must close the enlarged view; a popup does not fire key events of its own, so the handler lives on its child.");

        // Semi-transparent: the page stays visible behind the picture, dimmed rather than replaced.
        var fill = (string?)backdrop.Attribute("Background") ?? string.Empty;
        var brushKey = fill.Replace("{StaticResource", string.Empty, StringComparison.Ordinal)
            .Replace("}", string.Empty, StringComparison.Ordinal)
            .Trim();
        var brush = control
            .Descendants(s_presentation + "SolidColorBrush")
            .FirstOrDefault(candidate => (string?)candidate.Attribute(s_xaml + "Key") == brushKey);

        Assert.IsNotNull(brush, $"The backdrop's fill '{fill}' is not a brush this control declares.");
        StringAssert.StartsWith(
            (string?)brush!.Attribute("Color") ?? string.Empty,
            "#99",
            "The backdrop must be semi-transparent — the leading pair of hex digits is its alpha — so the page it covers stays visible behind it.");

        // Close is an X in the top-right corner of the view, not a panel along the bottom.
        var close = popup
            .Descendants(s_presentation + "Button")
            .FirstOrDefault(button => (string?)button.Attribute("AutomationProperties.AutomationId") == "ClickToEnlargeImageView_CloseButton");

        Assert.IsNotNull(close, "The enlarged view has no close button the reader can find.");
        Assert.AreEqual("Right", (string?)close!.Attribute("HorizontalAlignment"), "Close belongs in the top-right corner.");
        Assert.AreEqual("Top", (string?)close.Attribute("VerticalAlignment"), "Close belongs in the top-right corner.");
        Assert.AreEqual(
            "\uE711",
            (string?)close.Descendants(s_presentation + "FontIcon").Single().Attribute("Glyph"),
            "Close is the X glyph (E711) from the Segoe Fluent Icons list, not a word on a bottom panel.");

        var codeBehind = File.ReadAllText(Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "Module_Shared",
            "Controls",
            "ClickToEnlargeImageView.xaml.cs"));

        StringAssert.Contains(codeBehind, "EnlargedView.IsOpen = true", "The enlarged view must be the control's own view to open.");
        StringAssert.Contains(codeBehind, "XamlRoot", "The view must be sized to the page it covers, rather than to its content.");
        StringAssert.Contains(codeBehind, "VirtualKey.Escape", "Esc must close the enlarged view.");
        StringAssert.Contains(codeBehind, "Shared_ClickToEnlarge.CloseButton", "The X needs wording the reader can find.");

        // The gesture follows the host: a picture inside a button, or inside a list row that acts on a click, is
        // enlarged with Shift+click, because there a plain click belongs to that host. The rule is worked out from
        // the picture's ancestors (a ButtonBase, or a ListViewBase that acts on a click) rather than declared by
        // each surface, and the Shift gesture is taken on the press, before the host can see it.
        Assert.IsNotNull(
            picture.Attribute("PointerPressed"),
            "The Shift gesture has to be caught on the press, or the button or row around the picture acts on it too.");
        Assert.IsNotNull(picture.Attribute("Tapped"), "A plain click enlarges the picture where nothing else claims it.");
        StringAssert.Contains(codeBehind, "VirtualKeyModifiers.Shift", "Shift is the enlarging gesture where a plain click is spoken for.");
        StringAssert.Contains(codeBehind, "ButtonBase", "A picture inside a button must be recognised as one whose plain click is spoken for.");
        StringAssert.Contains(codeBehind, "IsItemClickEnabled", "A picture inside a list row that acts on a click must be recognised too.");

        var resources = File.ReadAllText(Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "Strings",
            "en-us",
            "Resources.resw"));

        StringAssert.Contains(
            resources,
            "Shared_ClickToEnlarge.CloseButton",
            "The enlarged view needs a close affordance the reader can find.");
        StringAssert.Contains(
            resources,
            "Shared_ClickToEnlarge.ShiftTooltip",
            "A picture that needs Shift must say so, rather than announcing a plain click that would not enlarge it.");
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
