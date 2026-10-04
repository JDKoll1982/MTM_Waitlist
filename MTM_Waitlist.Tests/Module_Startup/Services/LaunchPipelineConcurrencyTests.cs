using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Startup.Models;
using MTM_Waitlist.Module_Startup.Services;
using MTM_Waitlist.Tests.Fixtures;

namespace MTM_Waitlist.Tests.Module_Startup.Services;

/// <summary>
/// The launch's concurrency (`contracts/launch-concurrency-contract.md` §1; FR-026, FR-027, FR-031, FR-036): a
/// group's members start together, the group ends when every member has ended, and one member failing does not
/// stop its siblings — each is still held to its own stated maximum by the runner.
/// </summary>
/// <remarks>
/// The steps are recording doubles, and the two tests that prove overlap use a gate: every member signals that it
/// has started and waits, so the test can prove all of them were in flight at the same moment rather than inferring
/// it from timings.
/// </remarks>
[TestClass]
public sealed class LaunchPipelineConcurrencyTests
{
    private const string LocalSettings = "read-local-settings";
    private const string Reachability = "store-reachability";
    private const string HardwareIdentity = "read-hardware-identity";
    private const string JudgeSession = "judge-session";
    private const string ComputerAgainstStore = "check-computer-against-store";
    private const string TemporaryCredential = "check-temporary-credential";

    [TestMethod]
    public async Task RunAsync_TheLocalReadsAndTheFirstStoreContact_AreAllInFlightTogether()
    {
        using var gate = new Gate(3);
        var run = new LaunchRun()
            .With(LocalSettings, gate.WaitAsync)
            .With(HardwareIdentity, gate.WaitAsync)
            .With(Reachability, gate.WaitAsync);

        var launch = run.Pipeline.RunAsync(CancellationToken.None);

        var allStarted = await gate.WaitForAllStartedAsync(TimeSpan.FromSeconds(10));

        Assert.IsTrue(
            allStarted,
            "FR-027: the two local reads run alongside the first contact with the store, so all three start "
            + "before any of them is released");

        gate.Release();

        await launch;

        CollectionAssert.AreEquivalent(
            new[] { LocalSettings, HardwareIdentity, Reachability },
            run.Ran,
            "the group runs its members, and the walk does not run them a second time");
    }

    [TestMethod]
    public async Task RunAsync_TheThreeSessionChecks_AreAllInFlightTogether()
    {
        using var gate = new Gate(3);
        var run = new LaunchRun()
            .SignedIn()
            .With(JudgeSession, gate.WaitAsync)
            .With(ComputerAgainstStore, gate.WaitAsync)
            .With(TemporaryCredential, gate.WaitAsync);

        var launch = run.Pipeline.RunAsync(CancellationToken.None);

        var allStarted = await gate.WaitForAllStartedAsync(TimeSpan.FromSeconds(10));

        Assert.IsTrue(allStarted, "FR-026: the three session checks run at the same time");

        gate.Release();

        await launch;

        CollectionAssert.AreEquivalent(
            new[] { JudgeSession, ComputerAgainstStore, TemporaryCredential },
            run.Ran);
    }

    [TestMethod]
    public async Task RunAsync_WhenOneGroupMemberFails_TheOthersStillRan()
    {
        var run = new LaunchRun()
            .With(LocalSettings, Succeeded())
            .With(HardwareIdentity, Succeeded())
            .With(Reachability, Failed("the store did not answer"));

        var outcome = await run.Pipeline.RunAsync(CancellationToken.None);

        Assert.AreEqual(LaunchOutcome.Blocked, outcome);
        CollectionAssert.AreEquivalent(
            new[] { LocalSettings, HardwareIdentity, Reachability },
            run.Ran,
            "a failed member does not cancel its siblings; each is still held to its own stated maximum");
    }

    [TestMethod]
    public async Task RunAsync_WhenTheGroupSucceeds_TheWalkCarriesOnPastIt()
    {
        var run = new LaunchRun()
            .With(LocalSettings, Succeeded())
            .With(HardwareIdentity, Succeeded())
            .With(Reachability, Succeeded())
            .With("read-machine-configuration", Succeeded());

        var outcome = await run.Pipeline.RunAsync(CancellationToken.None);

        Assert.AreEqual(LaunchOutcome.SignIn, outcome, "the walk carried on to the sign-in decision");
        CollectionAssert.Contains(run.Ran, "read-machine-configuration");
    }

    /// <summary>A gate that lets a group's members prove they are in flight together, then release together.</summary>
    private sealed class Gate : IDisposable
    {
        private readonly TaskCompletionSource _allStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly int _expected;
        private int _started;

        public Gate(int expected) => _expected = expected;

        /// <summary>The work one member does: signal that it started, then wait for the release.</summary>
        public async Task<LaunchStepOutcome> WaitAsync()
        {
            if (Interlocked.Increment(ref _started) >= _expected)
            {
                _allStarted.TrySetResult();
            }

            await _release.Task.ConfigureAwait(false);

            return Succeeded();
        }

        public Task<bool> WaitForAllStartedAsync(TimeSpan timeout)
            => Task.WhenAny(_allStarted.Task, Task.Delay(timeout)).ContinueWith(
                completed => ReferenceEquals(completed.Result, _allStarted.Task),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);

        public void Release() => _release.TrySetResult();

        public void Dispose() => _release.TrySetResult();
    }

    private static LaunchStepOutcome Succeeded() => new(LaunchStepStatus.Succeeded, null, LaunchRemedySet.None);

    private static LaunchStepOutcome Failed(string diagnosis)
        => new(LaunchStepStatus.Failed, diagnosis, LaunchRemedySet.RetryOnly);

    /// <summary>One launch over recording steps, as the retry tests build one.</summary>
    private sealed class LaunchRun
    {
        private readonly List<ILaunchStep> _steps = [];
        private readonly StubMachineConfigurationService _configuration = new();

        private LaunchPipeline? _pipeline;

        public LaunchStepCatalog Catalog { get; } = new();

        public ILaunchActivityFeed Feed { get; } = new LaunchActivityFeed();

        public PersonIdentityService Person { get; } = new(new ThrowingMySqlHelperServer());

        public FakeMachineFacts Machine { get; } = new() { Hostname = "test-workstation" };

        public PendingSignIn PendingSignIn { get; } = new();

        public LaunchPipeline Pipeline => _pipeline ??= new LaunchPipeline(
            Catalog,
            new LaunchStepRunner(Feed),
            Feed,
            _steps,
            Machine,
            Person,
            _configuration,
            PendingSignIn,
            new RecordingProcessRestarter());

        public List<string> Ran { get; } = [];

        public LaunchRun With(string stepId, LaunchStepOutcome answer) => With(stepId, () => Task.FromResult(answer));

        public LaunchRun With(string stepId, Func<Task<LaunchStepOutcome>> work)
        {
            _steps.Add(new RecordingLaunchStep(
                Catalog.Find(stepId) ?? throw new InvalidOperationException($"'{stepId}' is not in the catalogue."),
                work,
                Ran));

            return this;
        }

        public LaunchRun SignedIn()
        {
            Person.Apply(7L, "JKoll", "J. Koll", "1007", "developer", ["developer"]);
            PendingSignIn.Hold("JKoll", "a-secret-the-test-owns");

            return this;
        }
    }

    private sealed class RecordingLaunchStep(
        LaunchStep descriptor,
        Func<Task<LaunchStepOutcome>> work,
        List<string> ran) : ILaunchStep
    {
        public LaunchStep Descriptor { get; } = descriptor;

        public Task<LaunchStepOutcome> RunAsync(LaunchStepContext context, CancellationToken cancellationToken)
        {
            lock (ran)
            {
                ran.Add(Descriptor.Id);
            }

            return work();
        }
    }

    private sealed class RecordingProcessRestarter : IProcessRestarter
    {
        public bool Restart() => false;
    }

    private sealed class StubMachineConfigurationService : IMachineConfigurationService
    {
        public Task<MachineConfigurationState> GetStateAsync(CancellationToken cancellationToken)
            => Task.FromResult(new MachineConfigurationState(true, "Test workstation", null, null));

        public Task<MachineConfigurationSaveResult> SaveAsync(
            MachineConfigurationDraft draft,
            CancellationToken cancellationToken)
            => throw new NotSupportedException("The pipeline never saves this machine's configuration itself.");

        public Task<MachineConfigurationResetResult> ResetToDefaultsAsync(
            IReadOnlyList<string> whatIsBroken,
            CancellationToken cancellationToken)
            => throw new NotSupportedException("The pipeline never resets this machine's configuration itself.");
    }

    private sealed class ThrowingMySqlHelperServer : IMySqlHelperServer
    {
        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException("No test in this file reads the store.");

        public Task<int> ExecuteStoredProcedureNonQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException("No test in this file writes the store.");

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteSqlQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException("No statement text is written in the launch (constitution III).");

        public Task<int> ExecuteSqlNonQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException("No statement text is written in the launch (constitution III).");
    }
}
