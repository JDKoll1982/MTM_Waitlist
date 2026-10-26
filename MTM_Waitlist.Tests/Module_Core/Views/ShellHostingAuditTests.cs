using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Core.Views;

/// <summary>
/// The wiring that puts the shell on screen, which no unit test can observe because all of it happens on the real
/// window.
/// </summary>
/// <remarks>
/// <para>
/// <b>The defect this pins.</b> Found by hand on 2026-09-28: a launch reached the main screens and the person was
/// shown a blank window. The main window declares no content of its own and nothing assigned any, and a
/// <c>NavigationView</c> selects no item by itself, so the frame stayed empty and no page ever asked the store
/// for anything — which is exactly what the store's own log showed.
/// </para>
/// <para>
/// <b>Why the check reads the wiring rather than driving it.</b> Every part of it lives in a file that cannot be
/// instantiated without a window and a UI thread, so no unit test can reach it; the wiring is read the way the
/// repository's other source audits read it. Each assertion names the mechanism and the reason, so a reader who
/// finds this failing knows what was removed.
/// </para>
/// </remarks>
[TestClass]
public sealed class ShellHostingAuditTests
{
    [TestMethod]
    public void Audit_ShellHostingAuditTests_TheHost_PutsTheShellIntoTheMainWindowBeforeTheMainScreensAreShown()
    {
        var host = Read("App.xaml.cs");

        StringAssert.Contains(
            host,
            "MainWindow.ShowStage(LaunchStage.Shell, _shellContent!);",
            "the one window is the only thing that can show the shell, so the host is what fills it");

        StringAssert.Contains(
            host,
            "ShowShellContent();",
            "the main-screens outcome must host the shell before it activates the window");
    }

    [TestMethod]
    public void Audit_ShellHostingAuditTests_TheShell_OpensItsFirstPageWhenItLoads()
    {
        var shell = Read("Module_Core", "Views", "ShellPage.xaml.cs");

        StringAssert.Contains(
            shell,
            "OpenTheFirstPage();",
            "a NavigationView starts with no item selected, so the shell has to open a page on load");

        StringAssert.Contains(
            shell,
            "ViewModel.NavigationService.NavigateTo(firstPageKey)",
            "opening the first page means navigating to the key its own menu item carries");
    }

    [TestMethod]
    public void Audit_ShellHostingAuditTests_TheMainScreens_OpenTheMainWindowMaximizedWithoutLettingThatStopTheHandOver()
    {
        var host = Read("App.xaml.cs");

        StringAssert.Contains(
            host,
            "MainWindow.ShowStage(LaunchStage.Shell, _shellContent!);",
            "the hand-over hands the shell to the one window, and showing the shell stage is what maximizes it");

        var window = Read("MainWindow.xaml.cs");

        StringAssert.Contains(
            window,
            "_placement.Maximise(AppWindow);",
            "maximizing the window is the one call that does it");

        StringAssert.Contains(
            window,
            "The main window could not be maximized, so it is keeping the size it opened at.",
            "a window that cannot be maximized must still complete the hand-over rather than stop it");
    }

    /// <summary>
    /// The cached-data notice belongs at the foot of the shell, not above the page.
    /// </summary>
    /// <remarks>
    /// It is a notice about the data, and every page under it is worked on while it is shown. Sitting in the row
    /// above the frame, it pushed the page down the moment Infor Visual went unreachable and pulled it back up
    /// when it recovered, so the screen moved under the person's hands for a reason that had nothing to do with
    /// what they were doing. It has a row of its own, after the frame, and takes no height while it is away.
    /// </remarks>
    [TestMethod]
    public void Audit_ShellHostingAuditTests_TheShell_KeepsTheCachedDataNoticeInItsOwnRowBelowThePage()
    {
        var shell = Read("Module_Core", "Views", "ShellPage.xaml");

        var frameIndex = shell.IndexOf("x:Name=\"NavigationFrame\"", StringComparison.Ordinal);
        var noticeIndex = shell.IndexOf("<mock:ReadStatusIndicator", StringComparison.Ordinal);

        Assert.IsTrue(noticeIndex >= 0, "the shell no longer hosts the read-only cached-data status at all");
        Assert.IsTrue(
            noticeIndex > frameIndex,
            "the cached-data notice is declared before the frame, so it is drawn above the page rather than below it");

        // The frame is declared over several lines because it also states that it fills its row in both directions,
        // so the row it keeps is asserted from the element's attributes rather than from one exact line.
        var frame = System.Xml.Linq.XDocument.Parse(shell).Descendants().FirstOrDefault(
            e => (string?)e.Attribute(System.Xml.Linq.XName.Get("Name", "http://schemas.microsoft.com/winfx/2006/xaml")) == "NavigationFrame");

        Assert.IsNotNull(frame, "the shell no longer hosts a page frame");
        Assert.AreEqual(
            "0",
            (string?)frame.Attribute("Grid.Row"),
            "the page must keep the row that expands, so the notice below it cannot push it down");

        StringAssert.Contains(
            shell,
            "Grid.Row=\"1\"",
            "the notice must sit in the row after the page");

        // It is a notice inside the shell, not a strip of chrome along the window's edge: the row it sits in
        // reaches the bottom of the window, so the gap below it has to be declared here. The shell's own content
        // margin carries no bottom inset (56,24,56,0), so nothing else provides one.
        var notice = shell[noticeIndex..];
        var noticeEnd = notice.IndexOf("/>", StringComparison.Ordinal);
        var margin = System.Text.RegularExpressions.Regex.Match(
            notice[..(noticeEnd < 0 ? notice.Length : noticeEnd)],
            "Margin=\"([^\"]*)\"").Groups[1].Value.Split(',');

        Assert.AreEqual(4, margin.Length, "the cached-data notice carries no margin of its own, so it sits flush against the window's bottom edge");
        Assert.AreNotEqual("0", margin[3].Trim(), "the cached-data notice has no gap below it and sits at the very bottom of the window");
    }

    /// <summary>
    /// Widening the window never narrows the page.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The defect this pins.</b> Measured on 2026-10-24 while taking screenshots at laptop widths: with the pane's
    /// threshold at 1280, a window of 1360 showed the page a <em>narrower</em> viewport than a window of 1281 — 971 px
    /// against 1104 px — because the navigation pane expanded and took 260 px out of a window that had only grown by 79.
    /// It came back with the content ceiling removed: measured again on 2026-10-25, the page went from 1311 px at a
    /// 1519-px window to 1119 px at 1520.
    /// </para>
    /// <para>
    /// Both times the cause was the same, so this asserts the cause is gone rather than working out whether the
    /// threshold happens to be far enough along: the pane has one display mode and one width, so the content area
    /// changes only when the window does. The window's own minimum keeps the pane from squeezing the page out of
    /// existence.
    /// </para>
    /// </remarks>
    [TestMethod]
    public void Audit_ShellHostingAuditTests_WideningTheWindowNeverNarrowsThePage()
    {
        var shell = Read("Module_Core", "Views", "ShellPage.xaml");

        var pane = AttributeValue(shell, "OpenPaneLength");
        Assert.IsTrue(pane > 0, "the pane declares no width, so this audit cannot tell what the page has left.");

        StringAssert.Contains(
            shell,
            "PaneDisplayMode=\"Left\"",
            "the pane picks its display mode from the window's width, so crossing that width grows the pane and shrinks "
                + "the page: widening the window would narrow the page");

        Assert.IsFalse(
            shell.Contains("ExpandedModeThresholdWidth", StringComparison.Ordinal),
            "a width threshold is still declared, so the pane still switches mode and still takes its growth out of the page");

        var minimumWindow = AttributeValue(Read("MainWindow.xaml"), "MinWidth");

        var inset = System.Text.RegularExpressions.Regex
            .Match(Read("Styles", "Thickness.xaml"), @"x:Key=""NavigationViewPageContentMargin"">([^<]+)<")
            .Groups[1].Value
            .Split(',')
            .Select(part => int.Parse(part.Trim(), System.Globalization.CultureInfo.InvariantCulture))
            .ToArray();

        var narrowestPage = minimumWindow - pane - inset[0] - inset[2];

        Assert.IsTrue(
            narrowestPage >= 300,
            $"at the window's own minimum of {minimumWindow} px the page is {narrowestPage} px wide, which is too narrow to "
                + "read a card or a settings row in. Raise the window's minimum rather than letting the pane take the page.");
    }

    /// <summary>One integer markup attribute's value, which must be declared exactly once in the file.</summary>
    private static int AttributeValue(string markup, string attribute)
    {
        var matches = System.Text.RegularExpressions.Regex.Matches(markup, attribute + @"=""(\d+)""");

        Assert.AreEqual(1, matches.Count, $"'{attribute}' is declared {matches.Count} time(s) in the markup read here.");

        return int.Parse(matches[0].Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>One repository file, addressed from the repository root.</summary>
    private static string Read(params string[] parts)
    {
        var path = Path.Combine(new[] { RepositoryPatternScan.FindRepositoryRoot() }.Concat(parts).ToArray());

        Assert.IsTrue(File.Exists(path), $"the file this audit reads is not where it was: {path}");

        return File.ReadAllText(path);
    }
}
