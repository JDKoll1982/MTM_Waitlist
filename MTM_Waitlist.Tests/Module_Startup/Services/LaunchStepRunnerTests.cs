using System.Diagnostics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Startup.Models;
using MTM_Waitlist.Module_Startup.Services;
using MTM_Waitlist.Tests.Fixtures;

namespace MTM_Waitlist.Tests.Module_Startup.Services;

/// <summary>
/// The runner's two behavioural promises: the step is named before it runs, and it is held to its stated maximum
/// (`contracts/launch-step-contract.md` §2, §3; FR-002, FR-003, FR-026, SC-002).
/// </summary>
/// <remarks>
/// <para>
/// Every bound here is a short stated maximum against a step that deliberately never finishes, so what is proved
/// is the runner's shape — it stops waiting at the bound and records why — rather than how fast this machine is.
/// The observation window exists only so a broken runner fails instead of hanging the suite.
/// </para>
/// <para>
/// The step doubles are hand-written recordings, as the suite's conventions require: the step is a delegate, so
/// each test states the work it wants (finish, throw, run past the bound) without a mocking library.
/// </para>
/// </remarks>
[TestClass]
public sealed class LaunchStepRunnerTests
{
    /// <summary>The ceiling on "the runner returned", used only so a broken runner fails rather than hangs.</summary>
    private static readonly TimeSpan s_observationWindow = TimeSpan.FromSeconds(10);

    /// <summary>A stated maximum short enough to keep the suite quick, and long enough to be a real race.</summary>
    private static readonly TimeSpan s_tightBound = TimeSpan.FromMilliseconds(50);

    [TestMethod]
    public async Task RunAsync_WhenTheStepRuns_TheStepIsAlreadyNamedOnTheFeed()
    {
        // Arrange
        var feed = new LaunchActivityFeed();
        var runner = new LaunchStepRunner(feed);
        IReadOnlyList<LaunchFeedEntry> linesWhenTheWorkStarts = [];

        var step = StepWith(
            "configuration",
            (context, _) =>
            {
                linesWhenTheWorkStarts = [.. context.Feed.Entries];
                return Task.FromResult(Succeeded());
            });

        // Act
        var outcome = await runner.RunAsync(step, Context(feed), CancellationToken.None);

        // Assert: the announcement is on the feed before the step's own first statement, which is what makes a
        // stall attributable to a named line rather than to a step number (FR-002, US1 scenario 1).
        Assert.AreEqual(LaunchStepStatus.Succeeded, outcome.Status);
        Assert.AreEqual(1, linesWhenTheWorkStarts.Count, "the step's work ran before it was named");
        Assert.AreEqual(LaunchFeedEntryKind.StepStarted, linesWhenTheWorkStarts[0].Kind);
        Assert.AreEqual("configuration", linesWhenTheWorkStarts[0].StepId);
        Assert.AreEqual("configuration name", linesWhenTheWorkStarts[0].Text);
    }

    [TestMethod]
    public async Task RunAsync_WhenTheStepThrowsImmediately_TheAnnouncementIsStillOnTheFeed()
    {
        // Arrange: the announcement has to survive the worst case, a step that fails before it awaits anything.
        var feed = new LaunchActivityFeed();
        var runner = new LaunchStepRunner(feed);

        var step = StepWith(
            "store-reachability",
            (_, _) => throw new InvalidOperationException("the store did not answer"));

        // Act
        var outcome = await runner.RunAsync(step, Context(feed), CancellationToken.None);

        // Assert
        Assert.AreEqual(LaunchStepStatus.Failed, outcome.Status);
        Assert.IsNotNull(outcome.Diagnosis);
        StringAssert.Contains(outcome.Diagnosis!, "the store did not answer");
        Assert.AreEqual("StepStarted, StepFailed", KindSequence(feed));
        Assert.IsFalse(feed.Entries[^1].Succeeded, "a failed step must not read as an unattempted one");
    }

    [TestMethod]
    public async Task RunAsync_WhenTheStepPassesItsStatedMaximum_ReportsFailedRatherThanWaitingOnIt()
    {
        // Arrange
        var feed = new LaunchActivityFeed();
        var runner = new LaunchStepRunner(feed);

        var step = StepWith("store-reachability", (_, _) => NeverFinishes(), maximum: s_tightBound);

        // Act
        var started = Stopwatch.GetTimestamp();
        var outcome = await runner.RunAsync(step, Context(feed), CancellationToken.None);
        var elapsed = Stopwatch.GetElapsedTime(started);

        // Assert: the launch is never left on a step without end (FR-003, SC-002).
        Assert.AreEqual(LaunchStepStatus.Failed, outcome.Status);
        Assert.IsNotNull(outcome.Diagnosis);
        StringAssert.Contains(outcome.Diagnosis!, "did not finish");
        Assert.AreEqual("StepStarted, StepFailed", KindSequence(feed));
        Assert.IsTrue(
            elapsed < s_observationWindow,
            $"the runner waited {elapsed.TotalSeconds:0.#} seconds on a step it had bounded itself");
    }

    [TestMethod]
    public async Task RunAsync_WhenABestEffortStepPassesItsStatedMaximum_IsSkippedAndStillRecorded()
    {
        // Arrange
        var feed = new LaunchActivityFeed();
        var runner = new LaunchStepRunner(feed);

        var step = StepWith(
            "picture-cache",
            (_, _) => NeverFinishes(),
            maximum: s_tightBound,
            bestEffort: true);

        // Act
        var outcome = await runner.RunAsync(step, Context(feed), CancellationToken.None);

        // Assert: the failure is reported, and it cannot stop the launch (FR-026).
        Assert.AreEqual(LaunchStepStatus.Skipped, outcome.Status, "a best-effort step must not be able to stop the launch");
        Assert.IsNotNull(outcome.Diagnosis);
        Assert.AreEqual("StepStarted, StepFailed", KindSequence(feed), "a best-effort failure must still get a line");
        Assert.IsFalse(feed.Entries[^1].Succeeded);
    }

    [TestMethod]
    public async Task RunAsync_WhenABestEffortStepThrows_IsSkippedSoTheLaunchCarriesOn()
    {
        // Arrange
        var feed = new LaunchActivityFeed();
        var runner = new LaunchStepRunner(feed);

        var step = StepWith(
            "visual-priming",
            (_, _) => throw new TimeoutException("the external system did not answer"),
            bestEffort: true);

        // Act
        var outcome = await runner.RunAsync(step, Context(feed), CancellationToken.None);

        // Assert: whatever a best-effort step does, the launch is not stopped by it (FR-026, FR-027).
        Assert.AreEqual(LaunchStepStatus.Skipped, outcome.Status);
        Assert.IsNotNull(outcome.Diagnosis);
        StringAssert.Contains(outcome.Diagnosis!, "did not answer");
        Assert.AreEqual("StepStarted, StepFailed", KindSequence(feed));
    }

    [TestMethod]
    public async Task RunAsync_WhenTheStepReportsFailure_ReturnsFailedWithTheStepsOwnDiagnosis()
    {
        // Arrange
        var feed = new LaunchActivityFeed();
        var runner = new LaunchStepRunner(feed);

        var step = StepWith(
            "machine-setup",
            (_, _) => Task.FromResult(
                new LaunchStepOutcome(LaunchStepStatus.Failed, "the computer's settings could not be read", LaunchRemedySet.RetryOnly)));

        // Act
        var outcome = await runner.RunAsync(step, Context(feed), CancellationToken.None);

        // Assert
        Assert.AreEqual(LaunchStepStatus.Failed, outcome.Status);
        Assert.AreEqual("the computer's settings could not be read", outcome.Diagnosis);
        Assert.IsTrue(outcome.Remedies.CanRetry, "a failed step is repeatable (FR-016)");
        Assert.AreEqual("StepStarted, StepFailed", KindSequence(feed));
    }

    [TestMethod]
    public async Task RunAsync_WhenTheStepSucceeds_RecordsCompletedWithASucceededOutcome()
    {
        // Arrange
        var feed = new LaunchActivityFeed();
        var runner = new LaunchStepRunner(feed);

        // Act
        var outcome = await runner.RunAsync(StepWith("shell", (_, _) => Task.FromResult(Succeeded())), Context(feed), CancellationToken.None);

        // Assert
        Assert.AreEqual(LaunchStepStatus.Succeeded, outcome.Status);
        Assert.AreEqual("StepStarted, StepCompleted", KindSequence(feed));
        Assert.IsTrue(feed.Entries[^1].Succeeded);
    }

    [TestMethod]
    public async Task RunAsync_WhenTheStepReportsSkipped_RecordsAnEndWithoutAnOutcome()
    {
        // Arrange: a step whose work did not apply is not a failure, so it must not read as one.
        var feed = new LaunchActivityFeed();
        var runner = new LaunchStepRunner(feed);

        var step = StepWith(
            "session",
            (_, _) => Task.FromResult(new LaunchStepOutcome(LaunchStepStatus.Skipped, "no session to check", LaunchRemedySet.None)));

        // Act
        var outcome = await runner.RunAsync(step, Context(feed), CancellationToken.None);

        // Assert
        Assert.AreEqual(LaunchStepStatus.Skipped, outcome.Status);
        Assert.AreEqual("StepStarted, StepCompleted", KindSequence(feed));
        Assert.IsNull(feed.Entries[^1].Succeeded);
    }

    [TestMethod]
    public async Task RunAsync_WhenTheStepDeclaresNoMaximum_IsRefused()
    {
        // Arrange: the runner refuses to invent a bound, because an unbounded wait must never reach a launch.
        var feed = new LaunchActivityFeed();
        var runner = new LaunchStepRunner(feed);
        var step = StepWith("configuration", (_, _) => Task.FromResult(Succeeded()), maximum: TimeSpan.Zero);

        // Act / Assert
        await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(
            () => runner.RunAsync(step, Context(feed), CancellationToken.None));

        Assert.AreEqual(0, feed.Entries.Count, "a step the runner refused should not have been announced");
    }

    [TestMethod]
    public async Task RunAsync_WhenTheLaunchIsAbandoned_DoesNotRecordTheStepAsFailed()
    {
        // Arrange
        var feed = new LaunchActivityFeed();
        var runner = new LaunchStepRunner(feed);

        var step = StepWith(
            "session",
            (_, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult(Succeeded());
            });

        using var abandoned = new CancellationTokenSource();
        abandoned.Cancel();

        // Act / Assert: a person closing the launch must not leave a step reading as if it had failed.
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(
            () => runner.RunAsync(step, Context(feed), abandoned.Token));

        Assert.AreEqual("StepStarted", KindSequence(feed));
    }

    /// <summary>Builds a step whose work is the delegate the test supplies.</summary>
    private static ILaunchStep StepWith(
        string id,
        Func<LaunchStepContext, CancellationToken, Task<LaunchStepOutcome>> work,
        TimeSpan? maximum = null,
        bool bestEffort = false)
        => new RecordingLaunchStep(
            new LaunchStep(
                id,
                $"{id} name",
                $"{id} description",
                LaunchStepCategory.Configuration,
                maximum ?? TimeSpan.FromSeconds(5),
                bestEffort),
            work);

    /// <summary>The context a step runs with: no person yet, a machine double, and the feed under test.</summary>
    private static LaunchStepContext Context(LaunchActivityFeed feed)
        => new(Person: null, Machine: new FakeMachineFacts(), Feed: feed);

    /// <summary>A step outcome that says the work was done and has nothing to add.</summary>
    private static LaunchStepOutcome Succeeded()
        => new(LaunchStepStatus.Succeeded, null, LaunchRemedySet.None);

    /// <summary>The feed's kinds, in order, as one string a test can compare in a single assertion.</summary>
    private static string KindSequence(ILaunchActivityFeed feed)
        => string.Join(", ", feed.Entries.Select(entry => entry.Kind.ToString()));

    /// <summary>A task that never completes, for the steps that must be stopped by their own bound.</summary>
    private static Task<LaunchStepOutcome> NeverFinishes()
        => new TaskCompletionSource<LaunchStepOutcome>(TaskCreationOptions.RunContinuationsAsynchronously).Task;

    /// <summary>A step double whose work is a delegate, so each test states the work it means.</summary>
    private sealed class RecordingLaunchStep(
        LaunchStep descriptor,
        Func<LaunchStepContext, CancellationToken, Task<LaunchStepOutcome>> work) : ILaunchStep
    {
        public LaunchStep Descriptor { get; } = descriptor;

        public Task<LaunchStepOutcome> RunAsync(LaunchStepContext context, CancellationToken cancellationToken)
            => work(context, cancellationToken);
    }
}
