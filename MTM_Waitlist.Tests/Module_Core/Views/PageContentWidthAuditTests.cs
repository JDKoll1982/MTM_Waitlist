using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Core.Views;

/// <summary>
/// Every page draws its content in a capped column instead of stretching to the window.
/// </summary>
/// <remarks>
/// <para>
/// <b>The defect this pins.</b> Found on the running app on 2026-10-24: with no ceiling on a page's content, a
/// window wider than about 1600 px stretched the page to the window. A waitlist card grew to two thousand pixels —
/// its work centre at one end, its action buttons at the other, its title floating in the middle — and a settings
/// card did the same. The content read as if it had come away from its own frame, which is exactly how it was
/// reported.
/// </para>
/// <para>
/// The ceiling is one number, declared once, and this audit keeps it that way: a page that grows its own literal
/// width would drift from every other page the first time one of them was adjusted.
/// </para>
/// </remarks>
[TestClass]
public sealed class PageContentWidthAuditTests
{
    /// <summary>The pages whose content column is capped by the shared resource rather than by a literal.</summary>
    private static readonly string[] s_cappedPages =
    [
        "Module_Waitlist/Views/WaitlistViewPage.xaml",
        "Module_Waitlist/Views/WaitlistViewDetailPage.xaml",
        "Module_Settings/Views/SettingsPage.xaml",
        "Module_Settings/Views/GeneralSettingsPage.xaml",
        "Module_Settings/Views/OperationsSettingsPage.xaml",
        "Module_Settings/Views/PicturesSettingsPage.xaml",
        "Module_Settings/Views/DevicesSettingsPage.xaml",
        "Module_Settings/Views/DiagnosticsSettingsPage.xaml",
        "Module_Settings/Views/AdministrationSettingsPage.xaml",
    ];

    [TestMethod]
    public void Audit_PageContentWidthAuditTests_TheCeilingIsDeclaredOnce()
    {
        var thickness = Read("Styles", "Thickness.xaml");

        StringAssert.Contains(
            thickness,
            "<x:Double x:Key=\"PageContentMaxWidth\">1080</x:Double>",
            "the widest a page's content column may grow is declared once, as a resource every page refers to");
    }

    [TestMethod]
    public void Audit_PageContentWidthAuditTests_TheSetupAndNewRequestStepsAreCappedByTheirOwnStyle()
    {
        var setup = Read("Styles", "ModuleSetup.xaml");

        StringAssert.Contains(
            setup,
            "<Setter Property=\"MaxWidth\" Value=\"{StaticResource PageContentMaxWidth}\" />",
            "every setup and new-request step wears SetupPageGridStyle, so the ceiling belongs there rather than in fourteen pages");
    }

    [TestMethod]
    public void Audit_PageContentWidthAuditTests_EachPageRefersToTheSharedCeiling()
    {
        foreach (var page in s_cappedPages)
        {
            var markup = Read(page.Split('/'));

            StringAssert.Contains(
                markup,
                "MaxWidth=\"{StaticResource PageContentMaxWidth}\"",
                $"{page} does not cap its content column, so it stretches the full width of a wide window and reads as "
                    + "if it had come away from its frame");

            Assert.IsFalse(
                markup.Contains("MaxWidth=\"1080\"", StringComparison.Ordinal),
                $"{page} repeats the ceiling as a literal, so the one place the number lives is no longer one place");
        }
    }

    /// <summary>
    /// Every page caps its content column, not only the nine that were fixed by hand.
    /// </summary>
    /// <remarks>
    /// The list above began as the pages a person had seen stretch. This test is the one that keeps the ceiling a rule
    /// rather than a habit: it reads the inventory, takes every page from it, and requires a cap unless the page is
    /// inside the one application window and is a shell rather than content. A page added later is covered the moment it
    /// is listed.
    /// </remarks>
    [TestMethod]
    public void Audit_PageContentWidthAuditTests_EveryPageOnTheInventoryIsCapped()
    {
        var offenders = new List<string>();

        foreach (var page in PagesOnTheInventory().Where(page => !IsTheFrame(page)))
        {
            var markup = Read(page.Split('/'));

            var capped = markup.Contains("MaxWidth=\"{StaticResource PageContentMaxWidth}\"", StringComparison.Ordinal)
                || markup.Contains("Style=\"{StaticResource SetupPageGridStyle}\"", StringComparison.Ordinal)
                || markup.Contains("{StaticResource PageContentMaxWidth}", StringComparison.Ordinal);

            if (!capped)
            {
                offenders.Add(page);
            }
        }

        Assert.AreEqual(
            0,
            offenders.Count,
            "these pages draw their content straight into the window, so on a wide screen the content reads as if it had "
                + "come away from its own frame: " + string.Join(", ", offenders));
    }

    [TestMethod]
    public void Audit_PageContentWidthAuditTests_TheResourceOnlyFilesAreStillCappedByTheSameNumber()
    {
        foreach (var dictionary in ResourceOnlyFiles())
        {
            var markup = Read(dictionary.Split('/'));

            Assert.IsFalse(
                markup.Contains("MaxWidth=\"1080\"", StringComparison.Ordinal),
                $"{dictionary} repeats the ceiling as a literal, so the one place the number lives is no longer one place");
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
    /// The shell fills the window in both directions, so a page is never left at the width its own content asked for.
    /// </summary>
    /// <remarks>
    /// <b>The defect this pins.</b> Reported on the running app: the page in the shell read as if it had a minimum width
    /// the window could not widen. Two things caused it, and both are silent. A page-level <c>ScrollViewer</c> measures
    /// its child with unlimited width unless horizontal scrolling is switched off, so the capped column sized to its own
    /// content; and a capped element that is left-aligned is also content-sized, because alignment overrides the stretch
    /// that would otherwise fill the column. Neither shows up in a build, and neither shows up at one window size.
    /// </remarks>
    [TestMethod]
    public void Audit_PageContentWidthAuditTests_TheShellPageHostStretchesInBothDirections()
    {
        var document = System.Xml.Linq.XDocument.Parse(Read("Module_Core", "Views", "ShellPage.xaml"));

        // x:Name lives in the XAML namespace, so it is matched by its qualified name rather than as a bare attribute.
        var xName = System.Xml.Linq.XName.Get("Name", "http://schemas.microsoft.com/winfx/2006/xaml");

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
    public void Audit_PageContentWidthAuditTests_NoPageMeasuresItsColumnUnconstrained()
    {
        var offenders = new List<string>();

        foreach (var page in PagesOnTheInventory().Where(page => !IsTheFrame(page)))
        {
            var root = System.Xml.Linq.XDocument.Parse(Read(page.Split('/'))).Root;
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
            "these pages scroll horizontally, so their content is measured with unlimited width and the capped column "
                + "sizes to its own content instead of to the window: " + string.Join(", ", offenders));
    }

    [TestMethod]
    public void Audit_PageContentWidthAuditTests_NoPagePinsItsCappedColumn()
    {
        var offenders = new List<string>();

        foreach (var page in PagesOnTheInventory().Where(page => !IsTheFrame(page)))
        {
            foreach (var element in System.Xml.Linq.XDocument.Parse(Read(page.Split('/'))).Descendants())
            {
                var capped = ((string?)element.Attribute("MaxWidth") ?? string.Empty)
                    .Contains("PageContentMaxWidth", StringComparison.Ordinal);

                if (!capped)
                {
                    continue;
                }

                var alignment = (string?)element.Attribute("HorizontalAlignment");

                if (alignment is not null and not "Stretch")
                {
                    offenders.Add($"{page} (<{element.Name.LocalName}> HorizontalAlignment={alignment})");
                }
            }
        }

        Assert.AreEqual(
            0,
            offenders.Count,
            "an aligned element is content-sized rather than stretch-sized, so the capped column stops filling the window "
                + "and the page reads as having a minimum width: " + string.Join(", ", offenders));
    }

    /// <summary>
    /// Whether <paramref name="page"/> is the application frame rather than a screen's content.
    /// </summary>
    /// <remarks>
    /// The shell is the window: it holds the navigation pane and the page frame, and the pages it navigates to carry the
    /// ceiling themselves. Capping the shell would cap the pane with it, so the one page that is a frame is skipped here
    /// and every page it hosts is still required to be capped.
    /// </remarks>
    private static bool IsTheFrame(string page) =>
        string.Equals(page, "Module_Core/Views/ShellPage.xaml", StringComparison.OrdinalIgnoreCase);

    /// <summary>Every styles file the inventory lists.</summary>
    private static IEnumerable<string> ResourceOnlyFiles()
    {
        var inventory = Read("docs", "guides", "guides-xaml-surface-inventory.md");

        return System.Text.RegularExpressions.Regex
            .Matches(inventory, "\\|\\s*ResourceDictionary\\s*\\|[^|]*\\|[^|]*\\|\\s*`(?<path>[^`]+)`")
            .Select(match => match.Groups["path"].Value);
    }

    /// <summary>One repository file, addressed from the repository root.</summary>
    private static string Read(params string[] parts)
    {
        var path = Path.Combine(new[] { RepositoryPatternScan.FindRepositoryRoot() }.Concat(parts).ToArray());

        Assert.IsTrue(File.Exists(path), $"the file this audit reads is not where it was: {path}");

        return File.ReadAllText(path);
    }
}
