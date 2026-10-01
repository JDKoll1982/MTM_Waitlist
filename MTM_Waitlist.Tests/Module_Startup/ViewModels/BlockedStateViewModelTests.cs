using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Startup.Models;
using MTM_Waitlist.Module_Startup.Services;
using MTM_Waitlist.Module_Startup.ViewModels;

namespace MTM_Waitlist.Tests.Module_Startup.ViewModels;

/// <summary>
/// The stopped launch's state (`contracts/launch-step-contract.md` §4, §5; FR-004, FR-016, FR-017, FR-019): the
/// cause is the failing line's own words, repeating the failed work starts the application again rather than
/// resuming a half-run sequence, and a fault that can be put right without asking reaches no prompt at all.
/// </summary>
/// <remarks>
/// The feed is the real one, because the state's whole contract is that it reads what the launch wrote. The
/// repeat is asserted against a recording restarter: the state's job is to start a replacement instance, and the
/// pipeline's own tests already prove what a launch does when it runs again.
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
    public void Retry_StartsTheApplicationAgain_AndDoesNotResumeTheStoppedLaunch()
    {
        // FR-016: the failed work is repeated by starting the launch again, which is one pass from its first
        // step, rather than by resuming a half-run sequence at a step id.
        var feed = FeedWith(Failed(ReadRecordStepId, "This computer's record could not be read.", "the store"));
        var viewModel = CreateViewModel(feed, out var restarter, out _);

        viewModel.RetryCommand.Execute(null);

        Assert.AreEqual(1, restarter.Restarts, "the repeat starts a replacement instance");
        Assert.IsFalse(viewModel.HasMessage, "a replacement instance that started has nothing to report");
    }

    [TestMethod]
    public void Retry_WhenTheApplicationCouldNotBeStartedAgain_SaysSoAndLeavesTheSurfaceOpen()
    {
        var feed = FeedWith(Failed(StoreStepId, StoreDiagnosis, "the store"));
        var viewModel = CreateViewModel(feed, out var restarter, out _, start: () => false);

        viewModel.RetryCommand.Execute(null);

        Assert.AreEqual(1, restarter.Restarts);
        Assert.IsTrue(viewModel.HasMessage, "a repeat that did nothing has to tell the person");
    }

    [TestMethod]
    public void Retry_WhenStartingAgainThrew_SaysSoRatherThanEscapingFromTheCommand()
    {
        var feed = FeedWith(Failed(StoreStepId, StoreDiagnosis, "the store"));
        var viewModel = CreateViewModel(
            feed,
            out _,
            out _,
            start: () => throw new InvalidOperationException("The current process path is unavailable."));

        viewModel.RetryCommand.Execute(null);

        Assert.IsTrue(viewModel.HasMessage, "a fault while starting again is reported, not raised at the button");
    }

    [TestMethod]
    public async Task Prepare_WhenTheStopIsNotRepairableQuietly_AsksThePersonAndStartsNothing()
    {
        var feed = FeedWith(Failed(StoreStepId, StoreDiagnosis, "the store"));
        var viewModel = CreateViewModel(feed, out var restarter, out var configuration);

        var needsPerson = await viewModel.PrepareAsync(CancellationToken.None);

        Assert.IsTrue(needsPerson, "a store outage is the person's to decide about");
        Assert.AreEqual(0, restarter.Restarts, "nothing may be started behind a prompt that was raised");
        Assert.AreEqual(0, configuration.ResetRequests.Count, "a store outage has nothing this computer can repair");
    }

    [TestMethod]
    public async Task Prepare_WhenTheFaultCanBePutRightQuietly_RepairsItAndRaisesNoPrompt()
    {
        // FR-019, US4 scenario 3: the fault is repaired, the application starts again, and no question is asked.
        var feed = FeedWith(Failed(SaveConfigurationStepId, "This computer's configuration could not be written.", "the store"));
        var viewModel = CreateViewModel(feed, out var restarter, out var configuration);

        var needsPerson = await viewModel.PrepareAsync(CancellationToken.None);

        Assert.IsFalse(needsPerson, "a fault put right without asking reaches no prompt at all");
        CollectionAssert.Contains(
            configuration.ResetRequests.Single().ToArray(),
            MachineConfigurationParts.ScopedPreference,
            "only the part the scope supplies may be repaired without asking (FR-019)");
        Assert.AreEqual(
            1,
            restarter.Restarts,
            "the launch that carries the repaired fault on is a new one, not a resumed one");
    }

    [TestMethod]
    public async Task Prepare_WhenTheQuietRepairChangedNothing_AsksThePerson()
    {
        var feed = FeedWith(Failed(SaveConfigurationStepId, "This computer's configuration could not be written.", "the store"));
        var viewModel = CreateViewModel(
            feed,
            out var restarter,
            out _,
            _ => new MachineConfigurationResetResult(true, [], null));

        var needsPerson = await viewModel.PrepareAsync(CancellationToken.None);

        Assert.IsTrue(needsPerson, "a fault that was not put right is the person's to decide about");
        Assert.AreEqual(0, restarter.Restarts);
    }

    [TestMethod]
    public async Task Prepare_WhenTheQuietRepairWorkedButTheApplicationCouldNotStartAgain_AsksThePerson()
    {
        var feed = FeedWith(Failed(SaveConfigurationStepId, "This computer's configuration could not be written.", "the store"));
        var viewModel = CreateViewModel(feed, out var restarter, out _, start: () => false);

        var needsPerson = await viewModel.PrepareAsync(CancellationToken.None);

        Assert.IsTrue(needsPerson, "a repair that could not be followed by a restart leaves the surface to the person");
        Assert.AreEqual(1, restarter.Restarts);
        Assert.IsTrue(viewModel.HasMessage);
    }

    [TestMethod]
    public async Task RestoreDefaults_ResetsExactlyThePartsThePreviewNamed_AndStartsTheApplicationAgain()
    {
        // FR-018: what the person agreed to is what is reset, and the launch then runs once more.
        var feed = FeedWith(Failed(SaveConfigurationStepId, "This computer's configuration could not be written.", "the store"));
        var viewModel = CreateViewModel(feed, out var restarter, out var configuration);

        await viewModel.RestoreDefaultsCommand.ExecuteAsync(null);

        CollectionAssert.AreEqual(
            viewModel.ResetParts.ToArray(),
            configuration.ResetRequests.Single().ToArray(),
            "the reset must touch exactly what the preview named");
        Assert.AreEqual(1, restarter.Restarts, "the launch that follows a reset is a new one");
        Assert.IsFalse(viewModel.HasMessage, "a reset that happened has nothing to report");
    }

    [TestMethod]
    public async Task RestoreDefaults_WhenTheStoreRefusedTheReset_SaysSoAndLeavesTheLaunchStopped()
    {
        var feed = FeedWith(Failed(SaveConfigurationStepId, "This computer's configuration could not be written.", "the store"));
        var viewModel = CreateViewModel(
            feed,
            out var restarter,
            out _,
            _ => new MachineConfigurationResetResult(false, [], MachineConfigurationRefusals.DefaultDisplayNameInUse));

        await viewModel.RestoreDefaultsCommand.ExecuteAsync(null);

        Assert.IsTrue(viewModel.HasMessage, "the person has to be told the reset did not happen");
        Assert.AreEqual(0, restarter.Restarts, "nothing may start again over a configuration that was not restored");
    }

    /// <summary>Builds the state over a feed, a recording restarter and a recording configuration service.</summary>
    /// <param name="feed">The lines the launch wrote.</param>
    /// <param name="restarter">How a replacement instance is asked for, and what it answers.</param>
    /// <param name="configuration">The recording configuration service the policy will drive.</param>
    /// <param name="reset">What the store answers a reset with, or <c>null</c> to answer that it happened.</param>
    /// <param name="start">What starting a replacement instance answers, or <c>null</c> to answer that it started.</param>
    private static BlockedStateViewModel CreateViewModel(
        ILaunchActivityFeed feed,
        out RecordingProcessRestarter restarter,
        out RecordingConfigurationService configuration,
        Func<IReadOnlyList<string>, MachineConfigurationResetResult>? reset = null,
        Func<bool>? start = null)
    {
        restarter = new RecordingProcessRestarter(start);
        configuration = new RecordingConfigurationService(reset);

        return new BlockedStateViewModel(
            new LaunchStepCatalog(),
            feed,
            new StartupRecoveryService(configuration),
            restarter);
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

    /// <summary>How a replacement instance is started, recording every attempt and answering what it was told to.</summary>
    private sealed class RecordingProcessRestarter(Func<bool>? start = null) : IProcessRestarter
    {
        private readonly Func<bool> _start = start ?? (() => true);

        /// <summary>How many times a replacement instance was asked for.</summary>
        public int Restarts { get; private set; }

        public bool Restart()
        {
            Restarts++;

            return _start();
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
