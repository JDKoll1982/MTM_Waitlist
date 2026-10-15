using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Shared.Services;
using MTM_Waitlist.Module_Startup.Models;
using MTM_Waitlist.Module_Startup.Services;
using MTM_Waitlist.Tests.Fixtures;

namespace MTM_Waitlist.Tests.Module_Startup.Services;

/// <summary>
/// The picture refresh: it cannot stop the launch, and a source it could not read is reported with the copies
/// already on this computer left exactly where they are (`contracts/launch-step-contract.md` §1.1; FR-002, FR-026).
/// </summary>
/// <remarks>
/// <para>
/// The mirror is a recording double, because what is pinned here is what the step does with the mirror's answer
/// rather than what the mirror itself does. The mirror's own behaviour, including leaving an unreachable source
/// untouched, is pinned beside it in <c>Module_Shared</c>.
/// </para>
/// <para>
/// The claim that the step cannot stop the launch is made through the runner, because the runner is what turns a
/// best-effort failure into a skip. Asking the step directly would only prove the step failed, which is not the
/// same statement at all.
/// </para>
/// </remarks>
[TestClass]
public sealed class PictureCacheStepTests
{
    /// <summary>The catalogue entry this step answers for, named once so the tests cannot drift from it.</summary>
    private const string StepId = "picture-cache";

    [TestMethod]
    public void Service_PictureCacheStepTests_Descriptor_IsTheCataloguesOwnEntryAndIsBestEffortInsideTheCeiling()
    {
        // Arrange
        var catalog = new LaunchStepCatalog();
        var step = new PictureCacheStep(catalog, new RecordingPictureCache());

        // Act / Assert: the step restates nothing, and the two things that make it safe to run are the
        // catalogue's own declarations rather than this file's (FR-002, FR-003, FR-026).
        Assert.AreSame(catalog.Find(StepId), step.Descriptor, "the step publishes a descriptor of its own");
        Assert.IsTrue(step.Descriptor.IsBestEffort, "the refresh must be best effort");
        Assert.IsTrue(
            step.Descriptor.MaximumWait < LaunchStepCatalog.Ceiling,
            "a best-effort step must be bounded more tightly than the ceiling");
    }

    [TestMethod]
    public async Task Service_PictureCacheStepTests_RunAsync_WhenASourceCannotBeReadNamesItAndReportsAFailure()
    {
        // Arrange: the mirror reports that one share could not be read, which is the shape the real mirror
        // answers with rather than an exception.
        var cache = new RecordingPictureCache
        {
            Result = new ImageCacheSyncResult(2, 0, ["the Dunnage share"]),
        };

        var step = new PictureCacheStep(new LaunchStepCatalog(), cache);

        // Act
        var outcome = await step.RunAsync(Context(), CancellationToken.None);

        // Assert: the failure is stated with the source named, so the line on the feed says which share was
        // unreachable rather than only that something went wrong (FR-004, FR-026), and it says which pictures are
        // being drawn, so a copy that is a day old never reads as a current one (FR-042, SC-019).
        Assert.AreEqual(LaunchStepStatus.Failed, outcome.Status);
        StringAssert.Contains(outcome.Diagnosis!, "the Dunnage share");
        StringAssert.Contains(
            outcome.Diagnosis!,
            "the copies held on this computer are the ones being used",
            "the line has to say which pictures are being drawn, not only that the share could not be read");
    }

    [TestMethod]
    public async Task Service_PictureCacheStepTests_RunAsync_WhenASourceCannotBeReadLeavesTheCopiesOnThisComputerAlone()
    {
        // Arrange
        var cache = new RecordingPictureCache
        {
            Result = new ImageCacheSyncResult(0, 0, ["the Dunnage share"]),
        };

        var step = new PictureCacheStep(new LaunchStepCatalog(), cache);

        // Act
        await step.RunAsync(Context(), CancellationToken.None);

        // Assert: the step asks the mirror once and does nothing else. Emptying a cache during a network blip
        // would take the pictures away precisely when the cache is the only thing still working, so the step
        // must have no second path that could touch a file.
        Assert.AreEqual(1, cache.Synchronisations, "the step ran the mirror more than once, or ran something else");
    }

    [TestMethod]
    public async Task Service_PictureCacheStepTests_RunAsync_ThroughTheRunnerWhenASourceCannotBeReadDoesNotStopTheLaunch()
    {
        // Arrange
        var feed = new LaunchActivityFeed();
        var cache = new RecordingPictureCache
        {
            Result = new ImageCacheSyncResult(0, 0, ["the Dunnage share"]),
        };

        var step = new PictureCacheStep(new LaunchStepCatalog(), cache);

        // Act
        var outcome = await new LaunchStepRunner(feed).RunAsync(step, Context(feed), CancellationToken.None);

        // Assert: the launch carries on, and the failure is still on the feed rather than hidden, because a
        // failure that is reported is not a failure that was concealed (FR-026).
        Assert.AreEqual(LaunchStepStatus.Skipped, outcome.Status, "a best-effort failure must not stop the launch");
        Assert.IsTrue(
            feed.Entries.Any(entry => entry.Kind is LaunchFeedEntryKind.StepFailed),
            "the unreachable source was not recorded on the feed");
    }

    [TestMethod]
    public async Task Service_PictureCacheStepTests_RunAsync_WhenTheCopiesWereBroughtUpToDateReportsWhatChanged()
    {
        // Arrange
        var cache = new RecordingPictureCache
        {
            Result = new ImageCacheSyncResult(3, 1, [], 2),
        };

        var step = new PictureCacheStep(new LaunchStepCatalog(), cache);

        // Act
        var outcome = await step.RunAsync(Context(), CancellationToken.None);

        // Assert: the line says what the refresh did, so a slow refresh is attributable to something specific.
        Assert.AreEqual(LaunchStepStatus.Succeeded, outcome.Status);
        StringAssert.Contains(outcome.Diagnosis!, "3 copied");
        StringAssert.Contains(outcome.Diagnosis!, "1 removed");
        StringAssert.Contains(outcome.Diagnosis!, "2 replaced copies cleaned up");
    }

    /// <summary>
    /// A folder the copies cannot be written into is named as the folder, not as the share. The two faults have
    /// different remedies, and the line on the splash screen is the only clue the person reading it gets.
    /// </summary>
    [TestMethod]
    public async Task Service_PictureCacheStepTests_RunAsync_WhenTheCopiesCannotBeWrittenNamesTheFolderRatherThanTheShare()
    {
        // Arrange: the share was readable and the folder the copies go into could not be written.
        var cache = new RecordingPictureCache
        {
            Result = new ImageCacheSyncResult(0, 0, [])
            {
                DestinationsUnavailable = ["dunnage pictures"],
            },
        };

        var step = new PictureCacheStep(new LaunchStepCatalog(), cache);

        // Act
        var outcome = await step.RunAsync(Context(), CancellationToken.None);

        // Assert
        Assert.AreEqual(LaunchStepStatus.Failed, outcome.Status);
        StringAssert.Contains(outcome.Diagnosis!, "dunnage pictures");
        StringAssert.Contains(outcome.Diagnosis!, "could not be written to");
        StringAssert.DoesNotMatch(
            outcome.Diagnosis!,
            new System.Text.RegularExpressions.Regex("share could not be read"),
            "sending an administrator to a share that is healthy spends the one clue they get");
    }

    /// <summary>
    /// The failure that leaves a machine holding only the few pictures an installer put there is recorded through
    /// the log seam, so it reaches the store from every computer. The feed it is also reported on is in memory and
    /// dies with the launch, so a fault that only ever reached the feed is a fault nobody can look back at.
    /// </summary>
    [TestMethod]
    public async Task Service_PictureCacheStepTests_RunAsync_WhenTheCopiesCannotBeWrittenIsRecordedRatherThanOnlyShown()
    {
        var sink = new RecordingAppLogSink();
        AppLog.Configure(sink);

        try
        {
            var cache = new RecordingPictureCache
            {
                Result = new ImageCacheSyncResult(0, 0, [])
                {
                    DestinationsUnavailable = ["dunnage pictures"],
                },
            };

            var step = new PictureCacheStep(new LaunchStepCatalog(), cache);

            // Act
            await step.RunAsync(Context(), CancellationToken.None);

            // Assert
            Assert.AreEqual(1, sink.Messages.Count, "the fault has to be recorded once rather than not at all");
            StringAssert.Contains(sink.Messages[0], "dunnage pictures");
            CollectionAssert.Contains(sink.Areas.ToArray(), "PictureCache");
        }
        finally
        {
            AppLog.Configure(null);
        }
    }

    [TestMethod]
    public async Task Service_PictureCacheStepTests_RunAsync_WhenThereWasNothingToDoSaysTheCopiesWereAlreadyUpToDate()
    {
        // Arrange
        var cache = new RecordingPictureCache { Result = ImageCacheSyncResult.NothingToDo };
        var step = new PictureCacheStep(new LaunchStepCatalog(), cache);

        // Act
        var outcome = await step.RunAsync(Context(), CancellationToken.None);

        // Assert: a run that changed nothing is not dressed up as work that happened.
        Assert.AreEqual(LaunchStepStatus.Succeeded, outcome.Status);
        StringAssert.Contains(outcome.Diagnosis!, "already up to date");
    }

    [TestMethod]
    public async Task Service_PictureCacheStepTests_RunAsync_WhenTheMirrorThrowsIsReportedRatherThanEscapingTheStep()
    {
        // Arrange: the mirror is written never to throw, so this is the seam being wrong rather than a case the
        // application expects. It still must not reach the person as an unattributed crash (FR-004).
        var cache = new RecordingPictureCache { Throw = new IOException("The share could not be reached.") };
        var step = new PictureCacheStep(new LaunchStepCatalog(), cache);

        // Act
        var outcome = await step.RunAsync(Context(), CancellationToken.None);

        // Assert
        Assert.AreEqual(LaunchStepStatus.Failed, outcome.Status);
        StringAssert.Contains(outcome.Diagnosis!, "The share could not be reached.");
    }

    /// <summary>What a step is handed: no person yet, this computer's facts, and the feed.</summary>
    private static LaunchStepContext Context(ILaunchActivityFeed? feed = null)
        => new(null, new FakeMachineFacts { Hostname = "test-workstation" }, feed ?? new LaunchActivityFeed());

    /// <summary>Captures what the facade forwards to the seam.</summary>
    private sealed class RecordingAppLogSink : IAppLogSink
    {
        /// <summary>What was recorded, in the order it was recorded.</summary>
        public List<string> Messages { get; } = [];

        /// <summary>The part of the application each entry came from.</summary>
        public List<string> Areas { get; } = [];

        /// <inheritdoc />
        public void Info(string module, string message, string? target = null)
        {
            Areas.Add(module);
            Messages.Add(message);
        }

        /// <inheritdoc />
        public void Warn(string module, string message, string? errorType = null)
        {
            Areas.Add(module);
            Messages.Add(message);
        }

        /// <inheritdoc />
        public void Error(string module, string message, Exception? exception = null)
        {
            Areas.Add(module);
            Messages.Add(message);
        }
    }

    /// <summary>A mirror that answers from what the test stated and records that it was asked.</summary>
    private sealed class RecordingPictureCache : IImageCacheSyncService
    {
        /// <summary>How many times the mirror was asked to run.</summary>
        public int Synchronisations { get; private set; }

        /// <summary>What the mirror answers with, unless it is told to throw instead.</summary>
        public ImageCacheSyncResult Result { get; set; } = ImageCacheSyncResult.NothingToDo;

        /// <summary>The exception the mirror throws, when the test is exercising the seam being wrong.</summary>
        public Exception? Throw { get; set; }

        /// <inheritdoc />
        public Task<ImageCacheSyncResult> SynchronizeAsync(CancellationToken cancellationToken = default)
        {
            Synchronisations++;

            return Throw is not null
                ? Task.FromException<ImageCacheSyncResult>(Throw)
                : Task.FromResult(Result);
        }
    }
}
