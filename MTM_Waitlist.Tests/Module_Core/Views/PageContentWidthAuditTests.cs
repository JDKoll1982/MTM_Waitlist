using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Core.Views;

/// <summary>
/// Every page fills the shell's content area, in both directions, and nothing quietly takes width away from it.
/// </summary>
/// <remarks>
/// <para>
/// <b>The policy, and the two it replaced.</b> The ceiling on a page's content column is gone: a page is meant to fill
/// the window, which is what was asked for plainly — the shell's contents expand horizontally without question. What
/// the ceiling was hiding is now removed at its cause instead.
/// </para>
/// <para>
/// Two silent faults made a page look narrower than the window it lived in, and a ceiling papered over both. A
/// page-level <c>ScrollViewer</c> measures its child with unlimited width unless horizontal scrolling is off, so the
/// column sized to its own content. A capped element that is also left-aligned is content-sized for the same reason: an
/// alignment overrides the stretch that would otherwise fill the space. Measured on 2026-10-25, the waitlist page's card
/// sat in a column far narrower than the window it was in.
/// </para>
/// <para>
/// The third fault is the navigation pane. A pane that grows when the window passes a threshold takes that width out of
/// the page, so the window gets wider and the page gets narrower: measured the same day, the page went from 1311 px at a
/// 1519-px window to 1119 px at 1520. One display mode removes it, because the content area then changes only when the
/// window does.
/// </para>
/// </remarks>
[TestClass]
public sealed class PageContentWidthAuditTests
{
    [TestMethod]
    public void Audit_PageContentWidthAuditTests_TheCeilingIsGone()
    {
        var offenders = new List<string>();

        foreach (var file in XamlFiles())
        {
            var markup = Read(file.Split('/'));

            if (markup.Contains("PageContentMaxWidth", StringComparison.Ordinal))
            {
                offenders.Add(file);
            }
        }

        Assert.AreEqual(
            0,
            offenders.Count,
            "a page that caps its column stops filling the window, which is the opposite of what this application asks "
                + "for: " + string.Join(", ", offenders));
    }

    [TestMethod]
    public void Audit_PageContentWidthAuditTests_TheSetupAndNewRequestStepsFillTheirWindow()
    {
        var setup = XDocument.Parse(Read("Styles", "ModuleSetup.xaml"));
        var xKey = XName.Get("Key", "http://schemas.microsoft.com/winfx/2006/xaml");

        var style = setup.Descendants()
            .FirstOrDefault(e => (string?)e.Attribute(xKey) == "SetupPageGridStyle");

        Assert.IsNotNull(style, "every setup and new-request step wears SetupPageGridStyle, so the rule belongs there.");

        var setters = style.Elements().Where(e => e.Name.LocalName == "Setter").ToList();

        Assert.IsTrue(
            setters.Any(s => (string?)s.Attribute("Property") == "HorizontalAlignment" && (string?)s.Attribute("Value") == "Stretch"),
            "a setup step does not stretch across the window.");

        Assert.IsFalse(
            setters.Any(s => (string?)s.Attribute("Property") == "MaxWidth"),
            "a setup step caps its column, so it stops filling a wide window.");
    }

    [TestMethod]
    public void Audit_PageContentWidthAuditTests_NoPagePinsOrCapsItsContent()
    {
        var offenders = new List<string>();

        foreach (var page in PagesOnTheInventory().Where(page => !IsTheFrame(page)))
        {
            var root = XDocument.Parse(Read(page.Split('/'))).Root;
            var content = root?.Elements().FirstOrDefault();

            if (content is not { } element)
            {
                continue;
            }

            var alignment = (string?)element.Attribute("HorizontalAlignment");

            if (alignment is not null and not "Stretch")
            {
                offenders.Add($"{page} (<{element.Name.LocalName}> HorizontalAlignment={alignment})");
            }

            if (((string?)element.Attribute("MaxWidth")) is { Length: > 0 } maxWidth)
            {
                offenders.Add($"{page} (<{element.Name.LocalName}> MaxWidth={maxWidth})");
            }
        }

        Assert.AreEqual(
            0,
            offenders.Count,
            "an aligned or capped element is sized to its content rather than to the window, so the page stops expanding: "
                + string.Join(", ", offenders));
    }

    [TestMethod]
    public void Audit_PageContentWidthAuditTests_NoPageMeasuresItsContentUnconstrained()
    {
        var offenders = new List<string>();

        foreach (var page in PagesOnTheInventory().Where(page => !IsTheFrame(page)))
        {
            var root = XDocument.Parse(Read(page.Split('/'))).Root;
            var content = root?.Elements().FirstOrDefault();

            if (content is { } element && element.Name.LocalName == "ScrollViewer"
                && (string?)element.Attribute("HorizontalScrollMode") != "Disabled")
            {
                offenders.Add($"{page} (HorizontalScrollMode={((string?)element.Attribute("HorizontalScrollMode") ?? "Enabled")})");
            }
        }

        Assert.AreEqual(
            0,
            offenders.Count,
            "these pages scroll horizontally, so their content is measured with unlimited width and the page never fills "
                + "the window: " + string.Join(", ", offenders));
    }

    [TestMethod]
    public void Audit_PageContentWidthAuditTests_TheShellPageHostStretchesInBothDirections()
    {
        var document = XDocument.Parse(Read("Module_Core", "Views", "ShellPage.xaml"));

        // x:Name lives in the XAML namespace, so it is matched by its qualified name rather than as a bare attribute.
        var xName = XName.Get("Name", "http://schemas.microsoft.com/winfx/2006/xaml");

        foreach (var (name, element) in new[]
        {
            ("the page frame", document.Descendants().FirstOrDefault(e => (string?)e.Attribute(xName) == "NavigationFrame")),
            ("the navigation view", document.Descendants().FirstOrDefault(e => (string?)e.Attribute(xName) == "NavigationViewControl")),
        })
        {
            Assert.IsNotNull(element, $"{name} is not in the shell any more, so the page has nowhere to be shown.");

            Assert.AreEqual("Stretch", (string?)element.Attribute("HorizontalAlignment"), $"{name} does not fill the width of the window.");
            Assert.AreEqual("Stretch", (string?)element.Attribute("VerticalAlignment"), $"{name} does not fill the height of the window.");
        }
    }

    [TestMethod]
    public void Audit_PageContentWidthAuditTests_TheNavigationPaneNeverChangesWidth()
    {
        var shell = XDocument.Parse(Read("Module_Core", "Views", "ShellPage.xaml"));

        var navigation = shell.Descendants().FirstOrDefault(e => e.Name.LocalName == "NavigationView");

        Assert.IsNotNull(navigation, "the shell no longer holds a navigation view");

        Assert.AreEqual(
            "Left",
            (string?)navigation.Attribute("PaneDisplayMode"),
            "the pane switches mode with the window's width, so crossing that width takes the pane's growth out of the "
                + "page and the window gets wider while the page gets narrower");

        foreach (var threshold in new[] { "ExpandedModeThresholdWidth", "CompactModeThresholdWidth" })
        {
            Assert.IsNull(
                navigation.Attribute(threshold),
                $"{threshold} switches the pane's display mode. With the page filling the window, that switch is what makes "
                    + "widening the window narrow the page.");
        }
    }

    /// <summary>Every page the inventory lists, read from the inventory itself.</summary>
    private static IEnumerable<string> PagesOnTheInventory()
    {
        var inventory = Read("docs", "guides", "guides-xaml-surface-inventory.md");

        return System.Text.RegularExpressions.Regex
            .Matches(inventory, "\\|\\s*Page\\s*\\|[^|]*\\|[^|]*\\|\\s*`(?<path>[^`]+)`")
            .Select(match => match.Groups["path"].Value);
    }

    /// <summary>
    /// Whether <paramref name="page"/> is the application frame rather than a screen's content.
    /// </summary>
    /// <remarks>
    /// The shell is the window: it holds the navigation pane and the page frame. It is asserted on its own above, for
    /// stretching and for a pane of one width, so it is skipped by the rules that shape a screen's content.
    /// </remarks>
    private static bool IsTheFrame(string page) =>
        string.Equals(page, "Module_Core/Views/ShellPage.xaml", StringComparison.OrdinalIgnoreCase);

    /// <summary>Every XAML file in the repository, addressed from its root.</summary>
    private static IEnumerable<string> XamlFiles()
    {
        var root = RepositoryPatternScan.FindRepositoryRoot();
        var scope = new RepositoryScanScope { Extensions = [".xaml"] };

        return RepositoryPatternScan
            .EnumerateScannedFiles(root, scope)
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'));
    }

    /// <summary>One repository file, addressed from the repository root.</summary>
    private static string Read(params string[] parts)
    {
        var path = Path.Combine(new[] { RepositoryPatternScan.FindRepositoryRoot() }.Concat(parts).ToArray());

        Assert.IsTrue(File.Exists(path), $"the file this audit reads is not where it was: {path}");

        return File.ReadAllText(path);
    }
}
