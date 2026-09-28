using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Startup.Models;
using MTM_Waitlist.Module_Startup.Services;
using MTM_Waitlist.Tests.Fixtures;

namespace MTM_Waitlist.Tests.Module_Startup.Services;

/// <summary>
/// The launch's routing and its retry: a launch ends at exactly one of the five outcomes, the machine's
/// configuration is what decides whether it goes on, and a retry repeats the failed step and what follows it and
/// nothing before it (`contracts/launch-step-contract.md` §5; FR-001, FR-006, FR-008, FR-020).
/// </summary>
/// <remarks>
/// The steps are recording doubles that answer from what the test stated, so what is proved is the pipeline's
/// shape — which steps it runs, in which order, and where it ends — rather than any step's own work. The store,
/// the machine and the person are doubles for the same reason: no test touches a database.
/// </remarks>
[TestClass]
public sealed class LaunchPipelineRetryTests
{
    private const string FirstStepId = "read-local-settings";
    private const string ReachabilityStepId = "store-reachability";
    private const string ReadinessStepId = "read-machine-configuration";
    private const string SignInStepId = "resolve-person";

    [TestMethod]
    public async Task RunAsync_WhenAStepFails_EndsBlocked() 
    {
        // Arrange
        var run = new LaunchRun().With(ReachabilityStepId, Failed("the store did not answer"));

        // Act
        var outcome = await run.Pipeline.RunAsync(CancellationToken.None);

        // Assert: a failed step is the stated stop of FR-001, and the work that stopped it is repeatable (FR-016).
        Assert.AreEqual(LaunchOutcome.Blocked, outcome);
    }

    [TestMethod]
    public async Task RunAsync_WhenTheMachineIsUnconfigured_EndsAtMachineSetup()
    {
        // Arrange: the readiness step answers, and what it found is that this computer has no configuration.
        var run = new LaunchRun().With(ReadinessStepId, Succeeded()).Unconfigured();

        // Act
        var outcome = await run.Pipeline.RunAsync(CancellationToken.None);

        // Assert: the pipeline, not a screen, is what refuses to carry on (FR-006).
        Assert.AreEqual(LaunchOutcome.MachineSetup, outcome);
    }

    [TestMethod]
    public async Task RunAsync_WhenNoSignInIsHeld_EndsAtSignIn()
    {
        // Arrange: the machine is configured and nobody is signed in, so identity cannot be resolved without
        // asking (FR-014).
        var run = new LaunchRun().With(ReadinessStepId, Succeeded());

        // Act
        var outcome = await run.Pipeline.RunAsync(CancellationToken.None);

        // Assert
        Assert.AreEqual(LaunchOutcome.SignIn, outcome);
    }

    [TestMethod]
    public async Task RunAsync_WhenThePersonIsSignedIn_EndsAtTheMainScreensAndRaisesShellReady()
    {
        // Arrange
        var run = new LaunchRun().With(ReadinessStepId, Succeeded()).SignedIn();
        LaunchOutcome? handed = null;
        run.Pipeline.ShellReady += (_, outcome) => handed = outcome;

        // Act
        var outcome = await run.Pipeline.RunAsync(CancellationToken.None);

        // Assert: the shell is reached only through the event the host listens on (S4).
        Assert.AreEqual(LaunchOutcome.MainScreens, outcome);
        Assert.AreEqual(LaunchOutcome.MainScreens, handed);
    }

    [TestMethod]
    public async Task RetryFromAsync_RepeatsTheNamedStepAndWhatFollowsIt_AndNothingBeforeIt()
    {
        // Arrange
        var run = new LaunchRun()
            .With(FirstStepId, Succeeded())
            .With(ReachabilityStepId, Succeeded())
            .With(ReadinessStepId, Succeeded());

        await run.Pipeline.RunAsync(CancellationToken.None);
        run.Ran.Clear();

        // Act: a store-only retry resumes at the reachability step.
        await run.Pipeline.RetryFromAsync(ReachabilityStepId, CancellationToken.None);

        // Assert: FR-020 — step 1 is not repeated, the named step is, and what follows it is.
        CollectionAssert.AreEqual(
            new[] { ReachabilityStepId, ReadinessStepId },
            run.Ran,
            "a retry must repeat only the named step and the steps after it");
    }

    [TestMethod]
    public async Task RetryFromAsync_WhenTheStepIsNotOneTheLaunchDeclares_IsRefused()
    {
        // Arrange
        var run = new LaunchRun();

        // Act
        var refused = await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(
            () => run.Pipeline.RetryFromAsync("not-a-step", CancellationToken.None));

        // Assert
        StringAssert.Contains(refused.Message, "not-a-step");
    }

    [TestMethod]
    public async Task RunAsync_WhenASecondEntryArrivesWhileOneIsRunning_StartsNothing()
    {
        // Arrange: the first step never finishes, so the launch is still under way.
        var gate = new TaskCompletionSource<LaunchStepOutcome>(TaskCreationOptions.RunContinuationsAsynchronously);
        var run = new LaunchRun().With(FirstStepId, () => gate.Task);

        var first = run.Pipeline.RunAsync(CancellationToken.None);

        // Act
        var second = run.Pipeline.RunAsync(CancellationToken.None);
        gate.SetResult(Succeeded());
        var firstOutcome = await first;
        var secondOutcome = await second;

        // Assert: the second entry started nothing and answered the run already under way, so one machine never
        // has two pipelines against it.
        Assert.AreEqual(LaunchOutcome.SignIn, firstOutcome);
        Assert.AreEqual(firstOutcome, secondOutcome);
        Assert.AreEqual(1, run.Ran.Count(stepId => stepId == FirstStepId), "the re-entrant entry ran a second launch");
    }

    [TestMethod]
    public void End_StatesTheReasonBeforeTheProcessGoes()
    {
        // Arrange
        var run = new LaunchRun();
        string? stated = null;
        run.Pipeline.ProcessEnding += (_, reason) => stated = reason;

        // Act
        var outcome = run.Pipeline.End("The person closed this computer's setup.");

        // Assert: the reason is stated before the process goes, so an abort does not read as a crash (FR-008).
        Assert.AreEqual(LaunchOutcome.Ended, outcome);
        Assert.AreEqual("The person closed this computer's setup.", stated);
    }

    /// <summary>The outcome a step answers with, in the shapes a test needs.</summary>
    private static LaunchStepOutcome Succeeded() => new(LaunchStepStatus.Succeeded, null, LaunchRemedySet.None);

    /// <summary>A step that failed with a stated cause.</summary>
    private static LaunchStepOutcome Failed(string diagnosis)
        => new(LaunchStepStatus.Failed, diagnosis, LaunchRemedySet.RetryOnly);

    /// <summary>
    /// One launch, over recording steps: which steps exist, what each answers, and the order they were run in.
    /// </summary>
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

        /// <summary>
        /// The launch under test. Built on first use rather than in the constructor, because the pipeline takes
        /// the steps it will walk once and a step added afterwards would never be seen.
        /// </summary>
        public LaunchPipeline Pipeline => _pipeline ??= new LaunchPipeline(
            Catalog,
            new LaunchStepRunner(Feed),
            Feed,
            _steps,
            Machine,
            Person,
            _configuration,
            PendingSignIn);

        /// <summary>The step ids that ran, in the order they ran.</summary>
        public List<string> Ran { get; } = [];

        /// <summary>Adds a step that answers with one outcome.</summary>
        public LaunchRun With(string stepId, LaunchStepOutcome answer) => With(stepId, () => Task.FromResult(answer));

        /// <summary>Adds a step whose work the test states.</summary>
        public LaunchRun With(string stepId, Func<Task<LaunchStepOutcome>> work)
        {
            _steps.Add(new RecordingLaunchStep(
                Catalog.Find(stepId) ?? throw new InvalidOperationException($"'{stepId}' is not in the catalogue."),
                work,
                Ran));

            return this;
        }

        /// <summary>States that this machine has no configuration.</summary>
        public LaunchRun Unconfigured()
        {
            _configuration.State = new MachineConfigurationState(
                false,
                null,
                null,
                MachineConfigurationReasons.NeverConfigured);

            return this;
        }

        /// <summary>Signs a person in, as the sign-in steps do once they have accepted a credential.</summary>
        public LaunchRun SignedIn()
        {
            Person.Apply(7L, "JKoll", "J. Koll", "1007", "developer", ["developer"]);
            PendingSignIn.Hold("JKoll", "a-secret-the-test-owns");

            return this;
        }
    }

    /// <summary>A step double whose work is a delegate, recording that it ran.</summary>
    private sealed class RecordingLaunchStep(
        LaunchStep descriptor,
        Func<Task<LaunchStepOutcome>> work,
        List<string> ran) : ILaunchStep
    {
        public LaunchStep Descriptor { get; } = descriptor;

        public Task<LaunchStepOutcome> RunAsync(LaunchStepContext context, CancellationToken cancellationToken)
        {
            ran.Add(Descriptor.Id);

            return work();
        }
    }

    /// <summary>This machine's configuration, answered from what the test stated.</summary>
    private sealed class StubMachineConfigurationService : IMachineConfigurationService
    {
        /// <summary>A machine that holds its name and its note.</summary>
        public MachineConfigurationState State { get; set; } = new(
            true,
            "Test workstation",
            "a machine the test owns",
            null);

        public Task<MachineConfigurationState> GetStateAsync(CancellationToken cancellationToken)
            => Task.FromResult(State);

        public Task<MachineConfigurationSaveResult> SaveAsync(
            MachineConfigurationDraft draft,
            CancellationToken cancellationToken)
            => throw new NotSupportedException("The pipeline never saves this machine's configuration itself.");

        public Task<MachineConfigurationResetResult> ResetToDefaultsAsync(
            IReadOnlyList<string> whatIsBroken,
            CancellationToken cancellationToken)
            => throw new NotSupportedException("The pipeline never resets this machine's configuration itself.");
    }

    /// <summary>A store seam that fails every call, so a test that reaches the store is told so loudly.</summary>
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
