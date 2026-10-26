using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Startup.Models;
using MTM_Waitlist.Module_Startup.Services;

namespace MTM_Waitlist.Tests.Module_Startup.Services;

/// <summary>
/// The hold on a picture refresh that did not work: the launch window states it and waits to be read before the
/// surface that knows nothing about pictures takes its place.
/// </summary>
/// <remarks>
/// The refresh itself is best effort and never stops the launch (FR-026), so what is pinned here is the window
/// rather than the launch: the hold begins from the line the runner wrote, it ends when the person says so, and it
/// comes back on the next launch that fails.
/// </remarks>
[TestClass]
public sealed class PictureCopyHoldTests
{
    [TestMethod]
    public void Service_PictureCopyHoldTests_HoldIfTheRefreshFailed_WhenTheRefreshLineReportsAFailureHoldsTheWindow()
    {
        // Arrange: the line the runner writes when the refresh could not finish — a best-effort step's failure is
        // recorded as a failed line and the launch carries on past it.
        var hold = new PictureCopyHold();
        var changed = 0;
        hold.Changed += (_, _) => changed++;

        // Act
        var holding = hold.HoldIfTheRefreshFailed(
        [
            Line("picture-cache", LaunchFeedEntryKind.StepStarted, "Refreshing the picture copies", null),
            Line("picture-cache", LaunchFeedEntryKind.StepFailed, "The picture share could not be read.", false),
            Line("read-local-settings", LaunchFeedEntryKind.StepStarted, "Reading this computer's saved settings", null),
        ]);

        // Assert
        Assert.IsTrue(holding, "a failed refresh line has to hold the window, or the statement is never read");
        Assert.IsTrue(hold.IsHolding);
        Assert.AreEqual(1, changed, "the surface is told the hold began");
    }

    [TestMethod]
    public void Service_PictureCopyHoldTests_HoldIfTheRefreshFailed_WhenTheRefreshWorkedDoesNotHoldTheWindow()
    {
        // Arrange
        var hold = new PictureCopyHold();

        // Act
        var holding = hold.HoldIfTheRefreshFailed(
        [
            Line("picture-cache", LaunchFeedEntryKind.StepStarted, "Refreshing the picture copies", null),
            Line("picture-cache", LaunchFeedEntryKind.StepCompleted, "The picture copies were already up to date.", true),
        ]);

        // Assert: a refresh that worked is not a statement, so the launch window is not kept.
        Assert.IsFalse(holding);
        Assert.IsFalse(hold.IsHolding);
    }

    [TestMethod]
    public void Service_PictureCopyHoldTests_HoldIfTheRefreshFailed_WhenAnotherStepFailedHoldsNothing()
    {
        // Arrange
        var hold = new PictureCopyHold();

        // Act: the store failing is the pipeline's business and has its own surface; it is not a picture statement.
        var holding = hold.HoldIfTheRefreshFailed(
        [
            Line("picture-cache", LaunchFeedEntryKind.StepCompleted, "The picture copies were refreshed: 4 copied.", true),
            Line("store-reachability", LaunchFeedEntryKind.StepFailed, "The store did not answer.", false),
        ]);

        // Assert
        Assert.IsFalse(holding);
        Assert.IsFalse(hold.IsHolding);
    }

    [TestMethod]
    public async Task Service_PictureCopyHoldTests_WaitAsync_CompletesWhenThePersonAcknowledgesTheStatement()
    {
        // Arrange
        var hold = new PictureCopyHold();
        hold.HoldIfTheRefreshFailed(
        [
            Line("picture-cache", LaunchFeedEntryKind.StepFailed, "The picture share could not be read.", false),
        ]);

        // Act: the wait stands in for the launch window not being replaced yet.
        var waiting = hold.WaitAsync();
        Assert.IsFalse(waiting.IsCompleted, "the window must wait for the person rather than handing over at once");

        hold.Acknowledge();

        // Assert
        await waiting;
        Assert.IsFalse(hold.IsHolding, "acknowledging ends the hold");
    }

    [TestMethod]
    public async Task Service_PictureCopyHoldTests_WaitAsync_WithNoHoldAnswersAtOnce()
    {
        // Arrange
        var hold = new PictureCopyHold();

        // Act
        var waiting = hold.WaitAsync();

        // Assert: a launch with nothing to state is never delayed by this.
        Assert.IsTrue(waiting.IsCompleted);
        await waiting;
    }

    [TestMethod]
    public void Service_PictureCopyHoldTests_HoldIfTheRefreshFailed_AfterThePersonAcknowledgedItDoesNotAskAgainOnARetry()
    {
        // Arrange: a retry that ends the same launch returns to the same surface, and the statement has been read.
        var hold = new PictureCopyHold();
        var lines = new[]
        {
            Line("picture-cache", LaunchFeedEntryKind.StepFailed, "The picture share could not be read.", false),
        };

        hold.HoldIfTheRefreshFailed(lines);
        hold.Acknowledge();

        // Act
        var holding = hold.HoldIfTheRefreshFailed(lines);

        // Assert: the person is not asked twice within one launch.
        Assert.IsFalse(holding, "the launch the person already read it on must not hold again");
    }

    [TestMethod]
    public void Service_PictureCopyHoldTests_HoldIfTheRefreshFailed_ComesBackOnTheNextLaunchThatCannotRefreshTheCopies()
    {
        // Arrange: a launch is one process, so the next launch is a fresh hold — nothing about this is stored.
        var lines = new[]
        {
            Line("picture-cache", LaunchFeedEntryKind.StepFailed, "The picture share could not be read.", false),
        };

        var thisLaunch = new PictureCopyHold();
        thisLaunch.HoldIfTheRefreshFailed(lines);
        thisLaunch.Acknowledge();

        // Act
        var holding = new PictureCopyHold().HoldIfTheRefreshFailed(lines);

        // Assert
        Assert.IsTrue(holding, "the next launch that cannot refresh the copies states it again");
    }

    /// <summary>One feed line, the way the runner writes it.</summary>
    private static LaunchFeedEntry Line(string stepId, LaunchFeedEntryKind kind, string text, bool? succeeded)
        => new(DateTimeOffset.UtcNow, stepId, kind, text, "the picture share", succeeded);
}
