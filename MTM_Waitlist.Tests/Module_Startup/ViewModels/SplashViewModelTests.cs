using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Startup.Models;
using MTM_Waitlist.Module_Startup.Services;
using MTM_Waitlist.Module_Startup.ViewModels;

namespace MTM_Waitlist.Tests.Module_Startup.ViewModels;

/// <summary>
/// The launch window's state: a line appears as the launch writes it, the count is derived rather than stored, and
/// a stop states its cause rather than leaving a step number on screen
/// (`contracts/launch-step-contract.md` §3, §4; FR-002, FR-004).
/// </summary>
/// <remarks>
/// The feed is the real one rather than a double, because the view model's whole contract is that it draws what
/// the feed holds and adds nothing of its own. A double here would let the two disagree in exactly the way the
/// real pair cannot.
/// </remarks>
[TestClass]
public sealed class SplashViewModelTests
{
    [TestMethod]
    public void EntryAppended_BeforeTheSurfaceAppeared_LeavesTheWorkUnderWayOnItsSingleLine()
    {
        // Arrange: a launch that is already under way when the window appears has lines the window never saw.
        var feed = new LaunchActivityFeed();
        feed.Append(Line("read-local-settings", LaunchFeedEntryKind.StepStarted, "Reading this computer's saved settings", null, null));
        feed.Append(Line("read-local-settings", LaunchFeedEntryKind.StepCompleted, "This computer's saved settings name the store it reaches.", null, true));
        feed.Append(Line("store-reachability", LaunchFeedEntryKind.StepStarted, "Contacting the store", "the store", null));

        // Act
        var viewModel = new SplashViewModel(new LaunchStepCatalog(), feed);

        // Assert: the surface catches up to the work under way rather than replaying what has already finished.
        Assert.AreEqual("Contacting the store", viewModel.CurrentStep);
        Assert.IsTrue(
            viewModel.IsWaitingOnSomethingRemote,
            "the step under way is reaching for the store, so the line says so (FR-039)");
    }

    [TestMethod]
    public void EntryAppended_PutsTheWorkUnderWayOnTheLineBeforeItHasFinished()
    {
        // Arrange: the line a step's own announcement produces, with nothing after it yet. This is the shape a
        // stalled launch leaves behind, and it is the case the surface exists for (US1 scenario 1).
        var feed = new LaunchActivityFeed();
        var viewModel = new SplashViewModel(new LaunchStepCatalog(), feed);

        // Act
        feed.Append(Line("store-reachability", LaunchFeedEntryKind.StepStarted, "Contacting the store", "the store", null));

        // Assert: the line is there while the step is still running, so a stall has a name attached to it.
        Assert.AreEqual("Contacting the store", viewModel.CurrentStep);
        Assert.IsTrue(
            viewModel.IsWaitingOnSomethingRemote,
            "the step named the store, so the bar shows movement rather than a proportion");
    }

    [TestMethod]
    public void Progress_IsDerived_SoItCannotDisagreeWithTheLinesBesideIt()
    {
        // Arrange
        var catalog = new LaunchStepCatalog();
        var feed = new LaunchActivityFeed();
        var viewModel = new SplashViewModel(catalog, feed);

        // Act
        feed.Append(Line("read-local-settings", LaunchFeedEntryKind.StepCompleted, "Read.", null, true));

        // Assert: one step ended, and the length is the sequence's own rather than a figure stored here. The
        // caption a person reads is worded by the resource file, so what is asserted is the pair of numbers it is
        // built from — those are the part that could disagree with the lines beside it.
        Assert.AreEqual(1d, viewModel.ProgressValue, "the bar stands where the count of ended steps puts it");
        Assert.AreEqual((double)catalog.TotalCount, viewModel.ProgressMaximum, "the bar's length is the sequence's own");
    }

    [TestMethod]
    public void Progress_CountsEachStepOnce_EvenWhenItWasRepeated()
    {
        // Arrange: a retried step writes a second terminal line, and the progress must not run past the total.
        var catalog = new LaunchStepCatalog();
        var feed = new LaunchActivityFeed();
        var viewModel = new SplashViewModel(catalog, feed);

        // Act
        feed.Append(Line("store-reachability", LaunchFeedEntryKind.StepFailed, "The store did not answer.", "the store", false));
        feed.Append(Line("store-reachability", LaunchFeedEntryKind.StepCompleted, "The store answered.", "the store", true));

        // Assert
        Assert.AreEqual(1d, viewModel.ProgressValue, "a retried step is counted once, not twice");
        Assert.AreEqual((double)catalog.TotalCount, viewModel.ProgressMaximum);
    }

    [TestMethod]
    public void IsWaitingOnSomethingRemote_WhileAStepReachesForSomethingElsewhere_IsTrue()
    {
        // Arrange: a step that names what it is about is one that waits on something this computer does not hold.
        var feed = new LaunchActivityFeed();
        var viewModel = new SplashViewModel(new LaunchStepCatalog(), feed);

        // Act
        feed.Append(Line("store-reachability", LaunchFeedEntryKind.StepStarted, "Contacting the store", "the store", null));

        // Assert: the surface shows movement rather than a proportion, and names the step it is on.
        Assert.IsTrue(viewModel.IsWaitingOnSomethingRemote, "a step reaching for the store can stall with no progress to report");
        Assert.AreEqual("Contacting the store", viewModel.CurrentStep);
    }

    [TestMethod]
    public void IsWaitingOnSomethingRemote_ForAStepThatTouchesNothingElsewhere_IsFalse()
    {
        // Arrange: reading this computer's own name reaches nothing that can stall.
        var feed = new LaunchActivityFeed();
        var viewModel = new SplashViewModel(new LaunchStepCatalog(), feed);

        // Act
        feed.Append(Line("read-hardware-identity", LaunchFeedEntryKind.StepStarted, "Reading this computer's identity", null, null));

        // Assert
        Assert.IsFalse(viewModel.IsWaitingOnSomethingRemote);
        Assert.AreEqual("Reading this computer's identity", viewModel.CurrentStep);
    }

    [TestMethod]
    public void IsWaitingOnSomethingRemote_OnceThatStepEnds_IsFalse()
    {
        // Arrange
        var feed = new LaunchActivityFeed();
        var viewModel = new SplashViewModel(new LaunchStepCatalog(), feed);

        feed.Append(Line("store-reachability", LaunchFeedEntryKind.StepStarted, "Contacting the store", "the store", null));

        // Act
        feed.Append(Line("store-reachability", LaunchFeedEntryKind.StepCompleted, "The store answered.", "the store", true));

        // Assert: a bar left sweeping after its step has finished reads as a step that is still running.
        Assert.IsFalse(viewModel.IsWaitingOnSomethingRemote);
    }

    [TestMethod]
    public void IsWaitingOnSomethingRemote_WhenAnEarlierStepEnds_DoesNotEndTheStepUnderWay()
    {
        // Arrange: a completion line for a step that is no longer the one in flight must not stop the wait.
        var feed = new LaunchActivityFeed();
        var viewModel = new SplashViewModel(new LaunchStepCatalog(), feed);

        feed.Append(Line("store-reachability", LaunchFeedEntryKind.StepStarted, "Contacting the store", "the store", null));

        // Act: a late line about an earlier step.
        feed.Append(Line("read-local-settings", LaunchFeedEntryKind.StepCompleted, "Read.", null, true));

        // Assert
        Assert.IsTrue(viewModel.IsWaitingOnSomethingRemote, "only the step actually in flight may end its own wait");
    }

    [TestMethod]
    public void Apply_WhenAStepFails_StatesItsCauseRatherThanAStepNumber()
    {
        // Arrange
        var feed = new LaunchActivityFeed();
        var viewModel = new SplashViewModel(new LaunchStepCatalog(), feed);

        // Act
        feed.Append(Line(
            "store-reachability",
            LaunchFeedEntryKind.StepFailed,
            "Contacting the store did not finish within the 15 second(s) it is allowed.",
            "the store",
            false));

        // Assert: the strip repeats the cause the failing line carries, and it is not the step's id or its
        // position, because a person cannot act on either (FR-004).
        Assert.AreEqual(
            "Contacting the store did not finish within the 15 second(s) it is allowed.",
            viewModel.Diagnosis);
        Assert.IsNotNull(viewModel.DiagnosedLine, "the diagnosis is not carried on the line that caused it");
        Assert.AreEqual("the store", viewModel.DiagnosedLine!.Target);
    }

    [TestMethod]
    public void Apply_AfterAStop_KeepsTheCauseItFirstStated()
    {
        // Arrange
        var feed = new LaunchActivityFeed();
        var viewModel = new SplashViewModel(new LaunchStepCatalog(), feed);
        feed.Append(Line("store-reachability", LaunchFeedEntryKind.StepFailed, "The store did not answer.", "the store", false));

        // Act: a later line reports something else that went wrong, which a retry or a best-effort step will do.
        feed.Append(Line("picture-cache", LaunchFeedEntryKind.StepFailed, "The picture share could not be read.", "the picture share", false));

        // Assert: the cause the person is reading does not change under them while they act on it.
        Assert.AreEqual("The store did not answer.", viewModel.Diagnosis);
        Assert.AreEqual("the store", viewModel.DiagnosedLine!.Target, "the strip stopped naming the line that stopped the launch");
    }

    [TestMethod]
    public void Apply_WhileTheLaunchIsGoing_OffersNoCauseAtAll()
    {
        // Arrange
        var feed = new LaunchActivityFeed();
        var viewModel = new SplashViewModel(new LaunchStepCatalog(), feed);

        // Act
        feed.Append(Line("store-reachability", LaunchFeedEntryKind.StepCompleted, "The store answered.", "the store", true));

        // Assert: a line that simply worked is not dressed up as a fault, so the strip stays empty (FR-005).
        Assert.IsNull(viewModel.Diagnosis);
        Assert.IsNull(viewModel.DiagnosedLine);
    }

    [TestMethod]
    public void EntryAppended_IsHandedToTheSurfaceThread_ForEveryLine()
    {
        // Arrange: a launch step's continuation runs off the surface's thread, so the surface's own marshaller is
        // the only thing standing between the feed and an update from the wrong thread.
        var feed = new LaunchActivityFeed();
        var marshalled = new List<string>();
        var viewModel = new SplashViewModel(new LaunchStepCatalog(), feed)
        {
            UiThreadMarshaller = work =>
            {
                marshalled.Add("marshalled");
                work();
            },
        };

        // Act
        feed.Append(Line("read-local-settings", LaunchFeedEntryKind.StepStarted, "Reading this computer's saved settings", null, null));
        feed.Append(Line("read-local-settings", LaunchFeedEntryKind.StepCompleted, "Read.", null, true));

        // Assert: every line went through the marshaller rather than straight onto the surface.
        Assert.AreEqual(2, marshalled.Count, "a line was applied without being handed to the surface's thread");
        Assert.AreEqual("Reading this computer's saved settings", viewModel.CurrentStep);
    }

    /// <summary>One feed line, the way the runner writes it.</summary>
    private static LaunchFeedEntry Line(
        string stepId,
        LaunchFeedEntryKind kind,
        string text,
        string? target,
        bool? succeeded)
        => new(DateTimeOffset.UtcNow, stepId, kind, text, target, succeeded);
}
