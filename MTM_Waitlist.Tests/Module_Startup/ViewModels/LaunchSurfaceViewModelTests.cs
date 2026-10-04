using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Startup.Models;
using MTM_Waitlist.Module_Startup.Services;
using MTM_Waitlist.Module_Startup.ViewModels;

namespace MTM_Waitlist.Tests.Module_Startup.ViewModels;

/// <summary>
/// The launch surface with several operations in flight (`contracts/launch-concurrency-contract.md` §3; FR-031):
/// every open line is rendered at once, each line appears before the work it describes, and a group of operations
/// is never presented behind a single line.
/// </summary>
[TestClass]
public sealed class LaunchSurfaceViewModelTests
{
    [TestMethod]
    public void TwoOperationsInFlight_RenderAsTwoOpenLines()
    {
        var feed = new LaunchActivityFeed();
        var surface = new LaunchSurfaceViewModel(feed);

        feed.Append(Started("judge-session", "Checking whether their session still stands"));
        feed.Append(Started("check-computer-against-store", "Checking this computer against the store"));

        Assert.AreEqual(2, surface.OpenLineCount, "two lines are open at once, not one current step (FR-031)");
        Assert.AreEqual(2, surface.Lines.Count, "each operation holds its own line");
        Assert.IsTrue(surface.HasOpenLines);
    }

    [TestMethod]
    public void AnOperationLine_AppearsBeforeItsWorkBegins()
    {
        var feed = new LaunchActivityFeed();
        var surface = new LaunchSurfaceViewModel(feed);

        feed.Append(Started("store-reachability", "Contacting the store"));

        // The line is on the surface before the step's work has produced anything, which is what makes a stall
        // attributable to a named line.
        Assert.AreEqual(1, surface.Lines.Count);
        Assert.IsTrue(surface.Lines[0].IsOpen);
        Assert.AreEqual("store-reachability", surface.Lines[0].StepId);
    }

    [TestMethod]
    public void WhenAnOperationEndsItsOwnLine_IsClosedAndTheOtherStaysOpen()
    {
        var feed = new LaunchActivityFeed();
        var surface = new LaunchSurfaceViewModel(feed);

        feed.Append(Started("judge-session", "Checking whether their session still stands"));
        feed.Append(Started("check-temporary-credential", "Checking whether a new PIN is needed"));
        feed.Append(new LaunchFeedEntry(
            DateTimeOffset.UtcNow,
            "judge-session",
            LaunchFeedEntryKind.StepCompleted,
            "The session stands.",
            "the store",
            true));

        Assert.AreEqual(1, surface.OpenLineCount, "the check that ended no longer holds an open line");
        Assert.IsFalse(
            surface.Lines.Any(line => line.StepId == "judge-session" && line.IsOpen),
            "the operation that ended has closed its own line");
        Assert.IsTrue(surface.Lines.Any(line => line.StepId == "check-temporary-credential" && line.IsOpen));
    }

    [TestMethod]
    public void AGroupOfOperations_NeverHidesBehindASingleLine()
    {
        var feed = new LaunchActivityFeed();
        var surface = new LaunchSurfaceViewModel(feed);

        foreach (var id in new[] { "judge-session", "check-computer-against-store", "check-temporary-credential" })
        {
            feed.Append(Started(id, id));
        }

        Assert.AreEqual(3, surface.OpenLineCount, "a group is three lines, never one line standing for a group");
    }

    [TestMethod]
    public void ALaunchAlreadyUnderWay_IsDrawnFromTheFeedItNeverSaw()
    {
        var feed = new LaunchActivityFeed();
        feed.Append(Started("store-reachability", "Contacting the store"));

        var surface = new LaunchSurfaceViewModel(feed);

        Assert.AreEqual(1, surface.Lines.Count, "a line written before the surface appeared is still drawn");
        Assert.IsTrue(surface.Lines[0].IsOpen);
    }

    private static LaunchFeedEntry Started(string stepId, string text)
        => new(DateTimeOffset.UtcNow, stepId, LaunchFeedEntryKind.StepStarted, text, "the store", null);
}
