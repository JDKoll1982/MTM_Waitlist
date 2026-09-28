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
    public void TheHost_PutsTheShellIntoTheMainWindow_BeforeTheMainScreensAreShown()
    {
        var host = Read("App.xaml.cs");

        StringAssert.Contains(
            host,
            "MainWindow.Content = _shellContent",
            "the main window declares no content of its own, so the host is the only thing that can fill it");

        StringAssert.Contains(
            host,
            "ShowShellContent();",
            "the main-screens outcome must host the shell before it activates the window");
    }

    [TestMethod]
    public void TheShell_OpensItsFirstPage_WhenItLoads()
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
    public void TheMainScreens_OpenTheMainWindowMaximized_WithoutLettingThatStopTheHandOver()
    {
        var host = Read("App.xaml.cs");

        StringAssert.Contains(
            host,
            "MaximizeMainWindow();",
            "the shell opened maximized in the application this rebuild replaces, so the hand-over has to maximize it");

        StringAssert.Contains(
            host,
            "MainWindow.Maximize();",
            "maximizing the window is the one call that does it");

        StringAssert.Contains(
            host,
            "The main window could not be maximized, so it is keeping the size it opened at.",
            "a window that cannot be maximized must still complete the hand-over rather than stop it");
    }

    /// <summary>One repository file, addressed from the repository root.</summary>
    private static string Read(params string[] parts)
    {
        var path = Path.Combine(new[] { RepositoryPatternScan.FindRepositoryRoot() }.Concat(parts).ToArray());

        Assert.IsTrue(File.Exists(path), $"the file this audit reads is not where it was: {path}");

        return File.ReadAllText(path);
    }
}
