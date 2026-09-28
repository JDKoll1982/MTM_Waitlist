using System.Diagnostics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Startup.Models;
using MTM_Waitlist.Module_Startup.Services;
using MTM_Waitlist.Tests.Fixtures;

namespace MTM_Waitlist.Tests.Module_Startup.Services;

/// <summary>
/// The external-system priming: the verdict is settled before the first screen opens, and neither best-effort step
/// can hold the launch past its stated maximum (`contracts/launch-step-contract.md` §1.1, §2; FR-003, FR-026,
/// FR-027).
/// </summary>
/// <remarks>
/// <para>
/// The probe owner is a recording double, because the step's whole job is to ask it once and to wait for the
/// answer. The probe's own hysteresis is pinned where the probe lives, in <c>Module_Mock</c>.
/// </para>
/// <para>
/// The overrun is proved with a purpose-built step whose stated maximum is milliseconds rather than with the
/// catalogue's own five seconds. The rule being pinned is the runner's, it belongs to every best-effort step
/// alike, and spending five real seconds to observe it would buy nothing the millisecond bound does not. That
/// each real step is best effort and bounded below the ceiling is proven separately, on the descriptors the
/// catalogue ships.
/// </para>
/// </remarks>
[TestClass]
public sealed class VisualVerdictPrimingStepTests
{
    /// <summary>The catalogue entry this step answers for, named once so the tests cannot drift from it.</summary>
    private const string StepId = "visual-priming";

    [TestMethod]
    public void Descriptor_IsTheCataloguesOwnEntry_AndIsBestEffortInsideTheCeiling()
    {
        // Arrange
        var catalog = new LaunchStepCatalog();
        var step = new VisualVerdictPrimingStep(catalog, new RecordingVerdictPrimer());

        // Act / Assert
        Assert.AreSame(catalog.Find(StepId), step.Descriptor, "the step publishes a descriptor of its own");
        Assert.IsTrue(step.Descriptor.IsBestEffort, "the priming must be best effort");
        Assert.IsTrue(
            step.Descriptor.MaximumWait < LaunchStepCatalog.Ceiling,
            "a best-effort step must be bounded more tightly than the ceiling");
    }

    [TestMethod]
    public void Descriptor_ThePrimingRunsBeforeTheFirstScreenOpens()
    {
        // Arrange
        var catalog = new LaunchStepCatalog();

        // Act: the sequence's own order decides this, so the claim is read from the sequence rather than from
        // this test's copy of it.
        var priming = IndexOf(catalog, StepId);
        var handOver = IndexOf(catalog, "shell");

        // Assert: the verdict is settled before anything can open, which is the whole of FR-027.
        Assert.IsTrue(
            priming >= 0 && handOver >= 0 && priming < handOver,
            "the priming must run before the hand-over to the first screen");
    }

    [TestMethod]
    public async Task RunAsync_AsksTheProbeOwnerExactlyOnce_AndWithoutCancellingIt()
    {
        // Arrange
        var primer = new RecordingVerdictPrimer();
        var step = new VisualVerdictPrimingStep(new LaunchStepCatalog(), primer);

        // Act
        var outcome = await step.RunAsync(Context(), CancellationToken.None);

        // Assert: one ask, on a token nobody has cancelled, because a token cancelled before the probe starts
        // would make the ask a no-op and leave the verdict exactly where it was.
        Assert.AreEqual(1, primer.Primes, "the step asked the probe owner more than once");
        Assert.IsFalse(primer.Token.IsCancellationRequested, "the step cancelled the ask it made");
        Assert.AreEqual(LaunchStepStatus.Succeeded, outcome.Status);
    }

    [TestMethod]
    public async Task RunAsync_WaitsForTheAnswer_RatherThanLeavingTheVerdictToChance()
    {
        // Arrange
        var primer = new RecordingVerdictPrimer();
        var step = new VisualVerdictPrimingStep(new LaunchStepCatalog(), primer);

        // Act
        await step.RunAsync(Context(), CancellationToken.None);

        // Assert: the ask has finished by the time the step answers, which is what makes the sweep more than a
        // request fired into the dark (FR-027).
        Assert.IsTrue(primer.Answered, "the step did not wait for the verdict to be settled");
    }

    [TestMethod]
    public async Task RunAsync_WhenTheProbeOwnerThrows_IsReportedRatherThanEscapingTheStep()
    {
        // Arrange: the primer is written never to throw, so this is the seam being wrong rather than a case the
        // application expects. It still must not reach the person as an unattributed crash (FR-004).
        var primer = new RecordingVerdictPrimer { Throw = new InvalidOperationException("The probe path failed.") };
        var step = new VisualVerdictPrimingStep(new LaunchStepCatalog(), primer);

        // Act
        var outcome = await step.RunAsync(Context(), CancellationToken.None);

        // Assert
        Assert.AreEqual(LaunchStepStatus.Failed, outcome.Status);
        StringAssert.Contains(outcome.Diagnosis!, "The probe path failed.");
    }

    [TestMethod]
    public async Task RunAsync_ThroughTheRunner_WhenABestEffortStepIgnoresItsBound_TheLaunchIsNotHeld()
    {
        // Arrange: a best-effort step that never honours its token, which is the worst a step can do to the
        // launch. Its own bound is milliseconds so the claim costs the suite nothing.
        var feed = new LaunchActivityFeed();
        var descriptor = new LaunchStep(
            "test-best-effort-overrun",
            "Waiting on something that will not stop",
            "A best-effort step that ignores the bound it was given.",
            LaunchStepCategory.Pictures,
            TimeSpan.FromMilliseconds(60),
            true,
            "the picture share");

        var step = new UnboundedBestEffortStep(descriptor);
        var stopwatch = Stopwatch.StartNew();

        // Act
        var outcome = await new LaunchStepRunner(feed).RunAsync(step, Context(feed), CancellationToken.None);

        // Assert: the runner stopped waiting at the stated maximum, recorded the bound it passed, and let the
        // launch carry on, which is what makes "best effort" mean "cannot hold the splash" (FR-003, FR-026).
        stopwatch.Stop();
        Assert.AreEqual(LaunchStepStatus.Skipped, outcome.Status, "a best-effort step that overruns must not stop the launch");
        Assert.IsTrue(
            stopwatch.Elapsed < TimeSpan.FromSeconds(2),
            $"the runner waited {stopwatch.Elapsed.TotalSeconds:0.##} second(s), so the step held a launch it may not hold");
        Assert.IsTrue(
            feed.Entries.Any(entry => entry.Kind is LaunchFeedEntryKind.StepFailed),
            "the overrun was not recorded on the feed");
    }

    /// <summary>The catalogue index of a step id, or <c>-1</c> when the sequence does not declare it.</summary>
    private static int IndexOf(LaunchStepCatalog catalog, string stepId)
    {
        for (var index = 0; index < catalog.Steps.Count; index++)
        {
            if (string.Equals(catalog.Steps[index].Id, stepId, StringComparison.Ordinal))
            {
                return index;
            }
        }

        return -1;
    }

    /// <summary>What a step is handed: no person yet, this computer's facts, and the feed.</summary>
    private static LaunchStepContext Context(ILaunchActivityFeed? feed = null)
        => new(null, new FakeMachineFacts { Hostname = "test-workstation" }, feed ?? new LaunchActivityFeed());

    /// <summary>A probe owner that records that it was asked, and answers when the test says so.</summary>
    private sealed class RecordingVerdictPrimer : IVisualVerdictPrimer
    {
        /// <summary>How many times the primer was asked to settle the verdict.</summary>
        public int Primes { get; private set; }

        /// <summary>Whether the ask finished, which is what proves the step waited for it.</summary>
        public bool Answered { get; private set; }

        /// <summary>The token the ask was made on, so a prematurely cancelled token can be spotted.</summary>
        public CancellationToken Token { get; private set; }

        /// <summary>The exception the primer throws, when the test is exercising the seam being wrong.</summary>
        public Exception? Throw { get; set; }

        /// <inheritdoc />
        public async Task PrimeAsync(CancellationToken cancellationToken = default)
        {
            Primes++;
            Token = cancellationToken;

            if (Throw is not null)
            {
                throw Throw;
            }

            // A yield, so the answer arrives after the caller has had the chance to carry on without waiting.
            await Task.Yield();
            Answered = true;
        }
    }

    /// <summary>A best-effort step that ignores the token it was given, which is the worst a step can do.</summary>
    private sealed class UnboundedBestEffortStep(LaunchStep descriptor) : ILaunchStep
    {
        /// <inheritdoc />
        public LaunchStep Descriptor { get; } = descriptor;

        /// <inheritdoc />
        public async Task<LaunchStepOutcome> RunAsync(LaunchStepContext context, CancellationToken cancellationToken)
        {
            // Deliberately no token: this step cannot be stopped, so only the runner's own bound ends the wait.
            await Task.Delay(TimeSpan.FromSeconds(3)).ConfigureAwait(false);

            return new LaunchStepOutcome(LaunchStepStatus.Succeeded, "It finished, eventually.", LaunchRemedySet.None);
        }
    }
}
