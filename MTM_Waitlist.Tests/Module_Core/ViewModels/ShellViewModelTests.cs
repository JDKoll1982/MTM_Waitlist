using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Core.ViewModels;

/// <summary>
/// US6 (FR-033, FR-034, SC-013). The signed-in badge offers <b>Sign out</b>, and choosing it invokes the
/// sign-out path — the application relaunches and comes back at the sign-in screen rather than the displayed
/// name simply going blank.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this class asserts the shell's source rather than a constructed view model.</b> <c>ShellViewModel</c>
/// lives in the application project and builds a <c>Microsoft.UI.Xaml.Media.SolidColorBrush</c> as the badge's
/// initial value, so it cannot be instantiated outside a WinUI UI thread: a probe against this very test host
/// fails with <c>COMException</c> — "For UWP projects, if you are using UI objects in test consider using
/// [UITestMethod]". The repository's established answer for app-project surfaces is an assertion against the
/// artifact that ships (<c>SettingsPageMarkupTests</c>, <c>NewRequestSummaryHonestyTests</c>), and that is what
/// this class does.
/// </para>
/// <para>
/// The <b>negative case</b> is the point of FR-034 and is asserted below: the sign-out path must reach
/// <c>ISignOutService</c> and must not merely clear <c>CurrentUserDisplayName</c>. A view model that never
/// calls the service is a view model that leaves the session — and the person — signed in.
/// </para>
/// </remarks>
[TestClass]
public sealed class ShellViewModelTests
{
    private const string ShellPageXamlPath = "Module_Core/Views/ShellPage.xaml";
    private const string ShellViewModelPath = "ViewModels/ShellViewModel.cs";
    private const string ShellPageCodeBehindPath = "Module_Core/Views/ShellPage.xaml.cs";
    private const string ServiceRegistrationPath = "Services/DependencyInjection/ServiceRegistrationExtensions.cs";
    private const string ResourcesPath = "Strings/en-us/Resources.resw";

    // ── The badge offers Sign out (FR-033) ──────────────────────────────────────────────────────────────

    [DataTestMethod]
    [DataRow("ShellPage_CurrentUserButton", DisplayName = "the badge in the header template")]
    [DataRow("ShellPage_CurrentUserButtonDefault", DisplayName = "the badge in the live (Default) header template")]
    public void TheSignedInBadge_OffersSignOut(string badgeAutomationId)
    {
        var badge = BadgeMarkup(badgeAutomationId);

        StringAssert.Contains(
            badge,
            "Button.Flyout",
            $"The badge '{badgeAutomationId}' must open a flyout; without one there is nowhere for Sign out to live (FR-033).");

        var signOutEntry = SignOutEntry(badge);

        StringAssert.Contains(
            signOutEntry,
            "SignOutLabel",
            $"The Sign out entry on '{badgeAutomationId}' must take its text from the view model's resource-resolved label (FR-022, FR-033).");
        StringAssert.Contains(
            signOutEntry,
            "SignOutItem_Click",
            $"Choosing Sign out on '{badgeAutomationId}' must reach the shell's sign-out path rather than being an inert menu entry (FR-033).");
    }

    [TestMethod]
    public void TheSignedInBadge_CarriesAnAutomationId_SoTheEntryCanBeAddressed()
    {
        // The UI-automation recipe addresses controls by AutomationId; a renamed one silently breaks the
        // sign-out gate in tasks.md T138.
        var source = ReadSource(ShellPageXamlPath);

        foreach (var id in new[] { "ShellPage_SignOutItem", "ShellPage_SignOutItemDefault" })
        {
            StringAssert.Contains(source, $"AutomationProperties.AutomationId=\"{id}\"", $"The Sign out entry must carry the stable id '{id}'.");
        }
    }

    // ── Choosing it invokes the sign-out path (FR-034) ──────────────────────────────────────────────────

    [TestMethod]
    public void SignOut_ReachesTheSignOutService_AndDoesNotMerelyClearTheDisplayedName()
    {
        var source = ReadSource(ShellViewModelPath);
        var command = SignOutCommandBody(source);

        StringAssert.Contains(
            source,
            "ISignOutService",
            "The shell must depend on the sign-out service; a badge that cannot reach one cannot end a session (FR-034).");
        StringAssert.Contains(
            command,
            "_signOutService.SignOutAsync(",
            "Choosing Sign out must invoke the sign-out path — the service that clears the session and relaunches (FR-034).");

        Assert.IsFalse(
            Regex.IsMatch(command, @"CurrentUserDisplayName\s*="),
            "Clearing the displayed name while the session persists is exactly what FR-034 rules out: the sign-out path must not merely blank the badge.");
    }

    [TestMethod]
    public void SignOut_ReportsARefusedRelaunch_InPlainLanguage()
    {
        var source = ReadSource(ShellViewModelPath);
        var command = SignOutCommandBody(source);

        StringAssert.Contains(
            command,
            "return _signOutService.SignOutAsync(",
            "The sign-out path must hand the outcome back rather than discarding it, so a refused relaunch can be reported (FR-026).");

        // The outcome is acted on where it can be shown: the shell's code-behind, over the message the service
        // resolved through the resource mechanism.
        var codeBehind = ReadSource(ShellPageCodeBehindPath);
        StringAssert.Contains(
            codeBehind,
            "SignOutItem_Click",
            "The click must be handled in the shell's code-behind, which is where a refused relaunch can be shown (FR-026).");
        StringAssert.Contains(
            codeBehind,
            "result.Succeeded",
            "The shell must look at the outcome it was handed rather than assuming the sign-out worked (FR-026).");
        StringAssert.Contains(
            codeBehind,
            "result.Message",
            "A refused relaunch must be shown to the person in the language the service resolved (FR-022, FR-026).");
    }

    // ── The service is registered and named by the resource mechanism ───────────────────────────────────

    [TestMethod]
    public void TheSignOutService_IsRegisteredInTheCompositionRoot()
    {
        StringAssert.Contains(
            ReadSource(ServiceRegistrationPath),
            "ISignOutService",
            "The composition root must register the sign-out service, or the shell cannot be constructed (FR-033).");
    }

    [DataTestMethod]
    [DataRow("Shell_SignOut.Label", DisplayName = "the Sign out label")]
    [DataRow("Shell_SignOut.Tooltip", DisplayName = "the Sign out tooltip")]
    [DataRow("Shell_SignOut.RestartFailed", DisplayName = "the refused-relaunch report")]
    public void EveryStringThisCapabilityShows_ComesFromTheResourceMechanism(string key)
    {
        StringAssert.Contains(
            ReadSource(ResourcesPath),
            $"name=\"{key}\"",
            $"'{key}' must be declared in the resource file, because every user-visible string this capability adds comes through the resource mechanism (FR-022).");
    }

    // ── helpers ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The markup of one badge button, from its automation id to the end of its element.</summary>
    private static string BadgeMarkup(string badgeAutomationId)
    {
        var source = ReadSource(ShellPageXamlPath);
        var start = source.IndexOf($"AutomationProperties.AutomationId=\"{badgeAutomationId}\"", StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, $"The signed-in badge '{badgeAutomationId}' was not found in {ShellPageXamlPath}.");

        var end = source.IndexOf("</Button>", start, StringComparison.Ordinal);
        Assert.IsTrue(end > start, $"The badge '{badgeAutomationId}' has no closing element.");

        return source[start..end];
    }

    /// <summary>The Sign out menu entry inside a badge's markup.</summary>
    private static string SignOutEntry(string badgeMarkup)
    {
        var start = badgeMarkup.IndexOf("SignOutLabel", StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, "The badge's flyout carries no entry whose text is the Sign out label (FR-033).");

        var entryStart = badgeMarkup.LastIndexOf('<', start);
        var entryEnd = badgeMarkup.IndexOf("/>", start, StringComparison.Ordinal);
        var close = badgeMarkup.IndexOf('>', start);

        var end = entryEnd >= 0 ? Math.Max(entryEnd, close) : close;
        return entryStart >= 0 && end > entryStart ? badgeMarkup[entryStart..end] : badgeMarkup;
    }

    /// <summary>The body of the shell's sign-out command, which is where the session must actually end.</summary>
    private static string SignOutCommandBody(string source)
    {
        var marker = source.IndexOf("SignOutAsync(", StringComparison.Ordinal);
        if (marker < 0)
        {
            marker = source.IndexOf("SignOut(", StringComparison.Ordinal);
        }

        Assert.IsTrue(
            marker >= 0,
            "The shell carries no sign-out command; the badge has nothing to invoke (FR-033, FR-034).");

        var open = source.IndexOf('{', marker);
        Assert.IsTrue(open > marker, "The sign-out command has no body.");

        var depth = 0;
        for (var index = open; index < source.Length; index++)
        {
            if (source[index] == '{')
            {
                depth++;
            }
            else if (source[index] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return source[open..index];
                }
            }
        }

        Assert.Fail("The sign-out command's body is not closed.");
        return string.Empty;
    }

    private static string ReadSource(string relativePath)
    {
        var fullPath = Path.Combine(RepositoryPatternScan.FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.IsTrue(File.Exists(fullPath), $"'{relativePath}' was not found.");
        return File.ReadAllText(fullPath);
    }
}
