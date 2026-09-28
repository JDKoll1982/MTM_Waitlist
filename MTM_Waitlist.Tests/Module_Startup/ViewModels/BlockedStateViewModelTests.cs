using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Startup.Models;
using MTM_Waitlist.Module_Startup.Services;
using MTM_Waitlist.Module_Startup.ViewModels;

namespace MTM_Waitlist.Tests.Module_Startup.ViewModels;

/// <summary>
/// The stopped launch's state (`contracts/launch-step-contract.md` §4, §5; FR-004, FR-016, FR-017, FR-019,
/// FR-020): the cause is the failing line's own words, a repeat repeats only the failed piece and what follows
/// it, and a fault that can be put right without asking reaches no prompt at all.
/// </summary>
/// <remarks>
/// The feed is the real one, because the state's whole contract is that it reads what the launch wrote. A repeat
/// is asserted against a recording launch rather than against the real pipeline: the state's job is to name the
/// step to resume at, and the pipeline's own tests already prove that naming it repeats it and nothing before it.
/// </remarks>
[TestClass]
public sealed class BlockedStateViewModelTests
{
    private const string StoreStepId = "store-reachability";
    private const string ReadRecordStepId = "read-computer-record";
    private const string SaveConfigurationStepId = "save-machine-configuration";
    private const string StoreDiagnosis = "Contacting the store did not finish within the 15 second(s) it is allowed.";

    [TestMethod]
    public void Constructor_WhenAStepFailed_StatesThatStepsOwnWordsRatherThanItsNumber()
    {
        var feed = FeedWith(Failed(StoreStepId, StoreDiagnosis, "the store"));

        var viewModel = CreateViewModel(feed, out _, out _);

        Assert.AreEqual(StoreDiagnosis, viewModel.Diagnosis);
        Assert.AreEqual(StoreStepId, viewModel.FailedStepId);
    }

    [TestMethod]
    public void Constructor_WhenABestEffortStepAlsoFailed_KeepsTheStopItDidNotCause()
    {
        // The picture refresh records its failure on the same feed and cannot stop the launch (FR-026). A surface
        // that took the last line would show a recorded, harmless failure as the reason a person cannot carry on.
        var feed = FeedWith(
            Failed(SaveConfigurationStepId, "This computer's configuration could not be written.", "the store"),
            Failed("picture-cache", "The picture share could not be read.", "the picture share"));

        var viewModel = CreateViewModel(feed, out _, out _);

        Assert.AreEqual(
            SaveConfigurationStepId,
            viewModel.FailedStepId,
            "a best-effort failure is reported and is not a stop, so it must not become the stop");
        StringAssert.Contains(viewModel.Diagnosis, "could not be written");
    }

    [TestMethod]
    public void Constructor_WhenALaterStepFailedAfterAnEarlierOne_KeepsTheStopThatJustHappened()
    {
        // A retry that carried on and a later step that failed both leave lines behind, and the surface is rebuilt
        // for each stop, so the reading has to be the latest stop rather than the first one ever seen.
        var feed = FeedWith(
            Failed(StoreStepId, StoreDiagnosis, "the store"),
            Failed(ReadRecordStepId, "This computer's record could not be read.", "the store"));

        var viewModel = CreateViewModel(feed, out _, out _);

        Assert.AreEqual(ReadRecordStepId, viewModel.FailedStepId);
    }

    [TestMethod]
    public void Constructor_WhenTheStoreCouldNotBeReached_OffersNoResetAtAll()
    {
        // FR-017, SC-010, US4 scenario 1: resetting cannot make a store answer, so it is not among the actions.
        var feed = FeedWith(Failed(StoreStepId, StoreDiagnosis, "the store"));

        var viewModel = CreateViewModel(feed, out _, out _);

        Assert.IsTrue(viewModel.CanRetry, "a stop always offers to repeat the failed work (FR-016)");
        Assert.IsFalse(viewModel.CanRestoreDefaults);
        Assert.IsNull(viewModel.RestoreDefaultsPreview);
    }

    [TestMethod]
    public void Constructor_WhenThisComputersConfigurationIsBroken_NamesWhatAResetWouldTouch()
    {
        var feed = FeedWith(Failed(SaveConfigurationStepId, "This computer's configuration could not be written.", "the store"));

        var viewModel = CreateViewModel(feed, out _, out _);

        Assert.IsTrue(viewModel.CanRestoreDefaults);
        Assert.IsFalse(string.IsNullOrWhiteSpace(viewModel.RestoreDefaultsPreview));
        CollectionAssert.Contains(viewModel.ResetParts.ToArray(), MachineConfigurationParts.DisplayName);
    }

    [TestMethod]
    public async Task Retry_RepeatsTheFailedPieceAndWhatFollowsIt_AndNothingBeforeIt()
    {
        // FR-020: a retry resumes at the step that failed, so the steps before it are not run again.
        var feed = FeedWith(Failed(ReadRecordStepId, "This computer's record could not be read.", "the store"));
        var viewModel = CreateViewModel(feed, out var launch, out _);

        await viewModel.RetryCommand.ExecuteAsync(null);

        CollectionAssert.AreEqual(
            new[] { ReadRecordStepId },
            launch.Retried.ToArray(),
            "only the failed piece and what follows it may be repeated (FR-020)");
        Assert.AreNotEqual(
            new LaunchStepCatalog().Steps[0].Id,
            launch.Retried[0],
            "a store-only retry must never repeat the first step (FR-020)");
    }

    [TestMethod]
    public async Task Prepare_WhenTheStopIsNotRepairableQuietly_AsksThePersonAndCarriesNothingOn()
    {
        var feed = FeedWith(Failed(StoreStepId, StoreDiagnosis, "the store"));
        var viewModel = CreateViewModel(feed, out var launch, out var configuration);

        var needsPerson = await viewModel.PrepareAsync(CancellationToken.None);

        Assert.IsTrue(needsPerson, "a store outage is the person's to decide about");
        Assert.AreEqual(0, launch.Retried.Count, "nothing may be carried on behind a prompt that was raised");
        Assert.AreEqual(0, configuration.ResetRequests.Count, "a store outage has nothing this computer can repair");
    }

    [TestMethod]
    public async Task Prepare_WhenTheFaultCanBePutRightQuietly_RepairsItAndRaisesNoPrompt()
    {
        // FR-019, US4 scenario 3: the fault is repaired, the launch carries on, and no question is asked.
        var feed = FeedWith(Failed(SaveConfigurationStepId, "This computer's configuration could not be written.", "the store"));
        var viewModel = CreateViewModel(feed, out var launch, out var configuration);

        var needsPerson = await viewModel.PrepareAsync(CancellationToken.None);

        Assert.IsFalse(needsPerson, "a fault put right without asking reaches no prompt at all");
        CollectionAssert.Contains(
            configuration.ResetRequests.Single().ToArray(),
            MachineConfigurationParts.ScopedPreference,
            "only the part the scope supplies may be repaired without asking (FR-019)");
        CollectionAssert.AreEqual(
            new[] { SaveConfigurationStepId },
            launch.Retried.ToArray(),
            "the launch carries on from the step that failed (FR-020)");
    }

    [TestMethod]
    public async Task Prepare_WhenTheQuietRepairChangedNothing_AsksThePerson()
    {
        var feed = FeedWith(Failed(SaveConfigurationStepId, "This computer's configuration could not be written.", "the store"));
        var viewModel = CreateViewModel(
            feed,
            out var launch,
            out _,
            _ => new MachineConfigurationResetResult(true, [], null));

        var needsPerson = await viewModel.PrepareAsync(CancellationToken.None);

        Assert.IsTrue(needsPerson, "a fault that was not put right is the person's to decide about");
        Assert.AreEqual(0, launch.Retried.Count);
    }

    [TestMethod]
    public async Task RestoreDefaults_ResetsExactlyThePartsThePreviewNamed_AndCarriesTheLaunchOn()
    {
        // FR-018: what the person agreed to is what is reset, and then the failed piece is repeated.
        var feed = FeedWith(Failed(SaveConfigurationStepId, "This computer's configuration could not be written.", "the store"));
        var viewModel = CreateViewModel(feed, out var launch, out var configuration);

        await viewModel.RestoreDefaultsCommand.ExecuteAsync(null);

        CollectionAssert.AreEqual(
            viewModel.ResetParts.ToArray(),
            configuration.ResetRequests.Single().ToArray(),
            "the reset must touch exactly what the preview named");
        CollectionAssert.AreEqual(new[] { SaveConfigurationStepId }, launch.Retried.ToArray());
        Assert.IsFalse(viewModel.HasMessage, "a reset that happened has nothing to report");
    }

    [TestMethod]
    public async Task RestoreDefaults_WhenTheStoreRefusedTheReset_SaysSoAndLeavesTheLaunchStopped()
    {
        var feed = FeedWith(Failed(SaveConfigurationStepId, "This computer's configuration could not be written.", "the store"));
        var viewModel = CreateViewModel(
            feed,
            out var launch,
            out _,
            _ => new MachineConfigurationResetResult(false, [], MachineConfigurationRefusals.DefaultDisplayNameInUse));

        await viewModel.RestoreDefaultsCommand.ExecuteAsync(null);

        Assert.IsTrue(viewModel.HasMessage, "the person has to be told the reset did not happen");
        Assert.AreEqual(0, launch.Retried.Count, "the launch must not carry on with a configuration that was not restored");
    }

    /// <summary>Builds the state over a feed, a recording launch and a recording configuration service.</summary>
    /// <param name="feed">The lines the launch wrote.</param>
    /// <param name="launch">The recording launch the state will name a step to.</param>
    /// <param name="configuration">The recording configuration service the policy will drive.</param>
    /// <param name="reset">What the store answers a reset with, or <c>null</c> to answer that it happened.</param>
    private static BlockedStateViewModel CreateViewModel(
        ILaunchActivityFeed feed,
        out RecordingLaunchPipeline launch,
        out RecordingConfigurationService configuration,
        Func<IReadOnlyList<string>, MachineConfigurationResetResult>? reset = null)
    {
        launch = new RecordingLaunchPipeline();
        configuration = new RecordingConfigurationService(reset);

        return new BlockedStateViewModel(
            new LaunchStepCatalog(),
            feed,
            launch,
            new StartupRecoveryService(configuration));
    }

    /// <summary>A feed holding the given lines, in the order they were written.</summary>
    private static LaunchActivityFeed FeedWith(params LaunchFeedEntry[] entries)
    {
        var feed = new LaunchActivityFeed();

        foreach (var entry in entries)
        {
            feed.Append(entry);
        }

        return feed;
    }

    /// <summary>One failing line, in the shape the step runner writes it.</summary>
    private static LaunchFeedEntry Failed(string stepId, string text, string? target)
        => new(DateTimeOffset.UtcNow, stepId, LaunchFeedEntryKind.StepFailed, text, target, false);

    /// <summary>The launch, answering that it ended where it was and recording the step a retry was named.</summary>
    private sealed class RecordingLaunchPipeline : ILaunchPipeline
    {
        /// <summary>Every step a repeat was named for, in the order it was named.</summary>
        public List<string> Retried { get; } = [];

        /// <summary>Every reason an ending was declared with.</summary>
        public List<string> Ended { get; } = [];

        // The state never listens for either of these, so they are declared without a field: an unused event
        // field would be a warning, and a warning is a build failure in this repository.
        public event EventHandler<LaunchOutcome>? ShellReady
        {
            add { }
            remove { }
        }

        public event EventHandler<string>? ProcessEnding
        {
            add { }
            remove { }
        }

        public Task<LaunchOutcome> RunAsync(CancellationToken cancellationToken)
            => Task.FromResult(LaunchOutcome.Blocked);

        public Task<LaunchOutcome> RetryFromAsync(string failedStepId, CancellationToken cancellationToken)
        {
            Retried.Add(failedStepId);

            return Task.FromResult(LaunchOutcome.Blocked);
        }

        public LaunchOutcome End(string reason)
        {
            Ended.Add(reason);

            return LaunchOutcome.Ended;
        }
    }

    /// <summary>This computer's configuration, recording what it was asked to reset.</summary>
    private sealed class RecordingConfigurationService(
        Func<IReadOnlyList<string>, MachineConfigurationResetResult>? reset = null) : IMachineConfigurationService
    {
        private readonly Func<IReadOnlyList<string>, MachineConfigurationResetResult> _reset =
            reset ?? (parts => new MachineConfigurationResetResult(true, parts.ToList(), null));

        /// <summary>Every reset the policy asked for, in the order it asked, as the parts it named.</summary>
        public List<IReadOnlyList<string>> ResetRequests { get; } = [];

        public Task<MachineConfigurationState> GetStateAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException("The stopped launch's state never reads this computer's configuration.");

        public Task<MachineConfigurationSaveResult> SaveAsync(
            MachineConfigurationDraft draft,
            CancellationToken cancellationToken)
            => throw new NotSupportedException("The stopped launch's state never saves this computer's configuration.");

        public Task<MachineConfigurationResetResult> ResetToDefaultsAsync(
            IReadOnlyList<string> whatIsBroken,
            CancellationToken cancellationToken)
        {
            ResetRequests.Add(whatIsBroken.ToList());

            return Task.FromResult(_reset(whatIsBroken));
        }
    }
}
