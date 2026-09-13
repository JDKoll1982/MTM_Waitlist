using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Activation;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Tests.Module_Mock;
using MTM_Waitlist.Tests.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Tests.Module_Core.Activation;

/// <summary>
/// US4 checks (<c>contracts/verification-gates.md</c> G4 #10, contract C2). Opening a notification while the
/// app is running must behave like a cold start: a recognised argument opens the request, an unrecognised one
/// changes nothing visible, and neither ever shows an internal placeholder.
/// </summary>
[TestClass]
public sealed class RequestDeepLinkHandlerTests
{
    [TestCleanup]
    public void RestoreStartupLog() => StartupDebugLog.Configure(null);

    [TestMethod]
    public void MappedArgument_OpensTheRequestDetailAndShowsNothing()
    {
        var requestId = Guid.NewGuid();
        var navigation = new WaitlistTestNavigationService();
        var window = new RecordingDeepLinkWindow();
        var handler = new RequestDeepLinkHandler(navigation, window);

        var handled = handler.TryHandleRequestDeepLink(WaitlistRequestLink.Build(requestId));

        Assert.IsTrue(handled, "A mapped argument must be reported as handled.");
        Assert.AreEqual(1, navigation.NavigateToCallCount, "A mapped argument must queue exactly one navigation.");
        Assert.AreEqual(
            RequestDeepLinkHandler.WaitlistRequestDetailPageKey,
            navigation.LastRequestedPageKey,
            "A mapped argument must open the request's detail page.");

        // The detail page resolves a request by its list id, which is request.Id.GetHashCode().
        Assert.AreEqual(requestId.GetHashCode(), navigation.LastRequestedParameter, "The navigation must carry the request the activation named.");

        Assert.IsTrue(window.BringToFrontCount > 0, "The window must come forward so the user sees the request.");
    }

    [TestMethod]
    public void UnmappedArgument_ShowsNothingRecordsTheArgumentAndComesForward()
    {
        var log = new RecordingStartupLogService();
        StartupDebugLog.Configure(log);

        var navigation = new WaitlistTestNavigationService();
        var window = new RecordingDeepLinkWindow();
        var handler = new RequestDeepLinkHandler(navigation, window);

        var handled = handler.TryHandleRequestDeepLink("action=Settings");

        Assert.IsFalse(handled, "An unmapped argument must be reported as unhandled.");
        Assert.AreEqual(0, navigation.NavigateToCallCount, "An unmapped argument must not navigate anywhere.");
        Assert.IsTrue(window.BringToFrontCount > 0, "The window still comes forward: the user did interact with the app.");

        Assert.IsTrue(
            log.Messages.Any(message => message.Contains("action=Settings", StringComparison.Ordinal)),
            "The raw argument must be recorded so an unrecognised activation can be diagnosed:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, log.Messages));
    }

    [TestMethod]
    public void RepeatedActivation_ReachesTheSameOutcomeEachTime()
    {
        var requestId = Guid.NewGuid();
        var navigation = new WaitlistTestNavigationService();
        var window = new RecordingDeepLinkWindow();
        var handler = new RequestDeepLinkHandler(navigation, window);

        for (var attempt = 0; attempt < 3; attempt++)
        {
            Assert.IsTrue(handler.TryHandleRequestDeepLink(WaitlistRequestLink.Build(requestId)));
        }

        Assert.AreEqual(3, navigation.NavigateToCallCount, "Each activation reaches the same outcome; none accumulates.");
        Assert.AreEqual(0, navigation.GoBackCallCount, "An activation must not grow the navigation stack backwards.");
    }

    /// <summary>
    /// The load-bearing half of G4 #10: no dialog is reachable from either activation path. Asserted against the
    /// source because a dialog needs a UI thread to appear, and because the check must see a dialog *re-added*
    /// to any of the three files.
    /// </summary>
    [TestMethod]
    public void NeitherActivationPathCanPresentADialogOrADeveloperMarker()
    {
        (string RelativePath, string Description)[] activationPaths =
        [
            ("MTM_Waitlist.Core/Services/AppNotificationService.cs", "the while-running path"),
            ("MTM_Waitlist.Core/Activation/AppNotificationActivationHandler.cs", "the cold-start path"),
            ("MTM_Waitlist.Core/Activation/RequestDeepLinkHandler.cs", "the shared handler"),
        ];

        (string Description, string Pattern)[] forbidden =
        [
            ("a modal dialog", @"ShowMessageDialogAsync|ContentDialog|MessageBox"),
            ("a developer placeholder marker", @"\bTODO\b"),
        ];

        var repositoryRoot = RepositoryPatternScan.FindRepositoryRoot();

        foreach (var (relativePath, description) in activationPaths)
        {
            var fullPath = Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Assert.IsTrue(File.Exists(fullPath), $"{description} was not found at '{relativePath}'.");

            var source = File.ReadAllText(fullPath);

            foreach (var (forbiddenDescription, pattern) in forbidden)
            {
                Assert.IsFalse(
                    Regex.IsMatch(source, pattern),
                    $"{description} ({relativePath}) still reaches {forbiddenDescription}; an activation is not a place to show internal state.");
            }
        }
    }

    /// <summary>Records what a window was asked to do, running queued work immediately.</summary>
    private sealed class RecordingDeepLinkWindow : IDeepLinkWindow
    {
        public int BringToFrontCount { get; private set; }

        public int QueuedActionCount { get; private set; }

        public void RunWhenIdle(Action action)
        {
            QueuedActionCount++;
            action();
        }

        public void BringToFront() => BringToFrontCount++;
    }

    /// <summary>Captures the diagnostics the handler writes.</summary>
    private sealed class RecordingStartupLogService : IStartupLogService
    {
        public List<string> Messages { get; } = [];

        public List<string> Areas { get; } = [];

        public void Info(string area, string message)
        {
            Areas.Add(area);
            Messages.Add(message);
        }

        public void Error(string area, Exception? exception, string message)
        {
            Areas.Add(area);
            Messages.Add(message);
        }
    }
}
