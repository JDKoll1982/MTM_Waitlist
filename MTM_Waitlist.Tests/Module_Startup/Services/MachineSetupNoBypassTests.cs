using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Startup.Models;
using MTM_Waitlist.Module_Startup.Services;
using MTM_Waitlist.Module_Startup.ViewModels;
using MTM_Waitlist.Tests.Fixtures;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Startup.Services;

/// <summary>
/// The closed gate around an unconfigured computer (`contracts/machine-configuration-contract.md` sections 2 and 3;
/// FR-006, FR-008, SC-004, SC-006): no input sequence and no code path reaches the main screens from a machine that
/// has no configuration, and every route out of setup ends the process with its reason stated first.
/// </summary>
/// <remarks>
/// <para>
/// The enforcement is proved where it lives, which is the pipeline: it is what answers
/// <see cref="LaunchOutcome.MachineSetup"/> without ever handing the shell over, so a dismissal route the setup
/// window does not know about still cannot reach the shell. The window's own wiring is proved by reading it, which
/// is the technique this suite already uses for markup and resource policy.
/// </para>
/// <para>
/// The steps are recording doubles, so what is proved is the routing rather than any step's own work. No case
/// touches a store, a machine or a person.
/// </para>
/// </remarks>
[TestClass]
public sealed class MachineSetupNoBypassTests
{
    private const string ReadinessStepId = "read-machine-configuration";
    private const string ReachabilityStepId = "store-reachability";

    private const string SetupWindowCodeBehind = @"Module_Startup\Views\MachineSetupWindow.xaml.cs";
    private const string ResourceFile = @"Strings\en-us\Resources.resw";

    [TestMethod]
    public async Task RunAsync_WhenTheMachineHasNoConfiguration_EndsAtMachineSetupAndNeverHandsOverTheMainScreens()
    {
        var run = new LaunchRun().Unconfigured();
        var handed = new List<LaunchOutcome>();
        run.Pipeline.ShellReady += (_, outcome) => handed.Add(outcome);

        var outcome = await run.Pipeline.RunAsync(CancellationToken.None);

        Assert.AreEqual(LaunchOutcome.MachineSetup, outcome, "the pipeline, not a screen, is what refuses to carry on (FR-006)");
        CollectionAssert.AreEqual(
            new[] { LaunchOutcome.MachineSetup },
            handed,
            "the host learns the surface it must show, and it is never told the shell is ready");
    }

    [TestMethod]
    public async Task RunAsync_WhenTheMachinesConfigurationCannotBeRead_StopsWithoutHandingOverTheMainScreens()
    {
        var run = new LaunchRun().WithConfiguration(
            new MachineConfigurationState(false, null, null, MachineConfigurationReasons.Unreadable));
        var handed = new List<LaunchOutcome>();
        run.Pipeline.ShellReady += (_, outcome) => handed.Add(outcome);

        var outcome = await run.Pipeline.RunAsync(CancellationToken.None);

        Assert.AreEqual(LaunchOutcome.Blocked, outcome, "a store that could not be read may succeed on the next check (FR-009)");
        CollectionAssert.DoesNotContain(handed, LaunchOutcome.MainScreens);
    }

    [TestMethod]
    public async Task RunAsync_WhenAStepFailsBeforeTheMachineIsChecked_StopsWithoutHandingOverTheMainScreens()
    {
        var run = new LaunchRun().With(ReachabilityStepId, Failed());
        var handed = new List<LaunchOutcome>();
        run.Pipeline.ShellReady += (_, outcome) => handed.Add(outcome);

        var outcome = await run.Pipeline.RunAsync(CancellationToken.None);

        Assert.AreEqual(LaunchOutcome.Blocked, outcome);
        CollectionAssert.DoesNotContain(handed, LaunchOutcome.MainScreens);
    }

    [TestMethod]
    public async Task RunAsync_WhenTheMachineIsConfiguredButNobodyIsSignedIn_EndsAtSignInInsteadOfTheShell()
    {
        var run = new LaunchRun();
        var handed = new List<LaunchOutcome>();
        run.Pipeline.ShellReady += (_, outcome) => handed.Add(outcome);

        var outcome = await run.Pipeline.RunAsync(CancellationToken.None);

        Assert.AreEqual(LaunchOutcome.SignIn, outcome);
        CollectionAssert.DoesNotContain(handed, LaunchOutcome.MainScreens);
    }

    [TestMethod]
    public async Task RunAsync_FromAnUnconfiguredMachineInAnyOrderOfAttempts_NeverHandsOverTheMainScreens()
    {
        // Every sequence a person could drive from an unconfigured machine, run to its end, and the shell is not
        // among the surfaces any of them reaches (SC-004).
        var attempts = new[]
        {
            new LaunchRun().Unconfigured(),
            new LaunchRun().Unconfigured().With(ReachabilityStepId, Failed()),
            new LaunchRun().WithConfiguration(
                new MachineConfigurationState(false, null, null, MachineConfigurationReasons.Removed)),
            new LaunchRun().WithConfiguration(
                new MachineConfigurationState(false, null, null, MachineConfigurationReasons.Revoked)),
            new LaunchRun().WithConfiguration(
                new MachineConfigurationState(false, null, null, MachineConfigurationReasons.Unreadable)),
        };

        foreach (var run in attempts)
        {
            var handed = new List<LaunchOutcome>();
            run.Pipeline.ShellReady += (_, outcome) => handed.Add(outcome);

            var first = await run.Pipeline.RunAsync(CancellationToken.None);

            // A second entry answers the run under way rather than starting a second one, and a retry of the
            // readiness step repeats it and what follows it.
            var second = await run.Pipeline.RetryFromAsync(ReadinessStepId, CancellationToken.None);

            Assert.AreNotEqual(LaunchOutcome.MainScreens, first);
            Assert.AreNotEqual(LaunchOutcome.MainScreens, second);
            CollectionAssert.DoesNotContain(handed, LaunchOutcome.MainScreens);
        }
    }

    [TestMethod]
    public void Abort_EveryRouteOutOfMachineSetup_EndsTheProcessWithItsReasonStatedFirst()
    {
        foreach (var route in MachineSetupAbortRoutes.All)
        {
            var run = new LaunchRun();
            string? stated = null;
            run.Pipeline.ProcessEnding += (_, reason) => stated = reason;

            var statement = MachineSetupAbortRoutes.StatementFor(route);
            var outcome = run.Pipeline.End(statement);

            // FR-008: the ending happens, and the statement of why is what precedes it.
            Assert.AreEqual(LaunchOutcome.Ended, outcome, $"{route} must end the process");
            Assert.AreEqual(statement, stated, $"{route} must state why before the process goes");
            Assert.IsFalse(string.IsNullOrWhiteSpace(statement), $"{route} must say something");
        }
    }

    [TestMethod]
    public void Abort_EveryRouteOutOfMachineSetup_StatesAReasonOfItsOwnThatShipsAsAString()
    {
        var statements = MachineSetupAbortRoutes.All
            .Select(MachineSetupAbortRoutes.StatementFor)
            .ToArray();

        Assert.AreEqual(
            MachineSetupAbortRoutes.All.Count,
            statements.Distinct(StringComparer.Ordinal).Count(),
            "each route states its own reason, so a report of 'it closed' can be answered with which route closed it");

        // The suite runs outside the application, so a resource lookup answers the key itself. The shipped strings
        // are therefore checked where they live, which is the resource file (S15: an unlocalised close reads as
        // instability).
        var resources = File.ReadAllText(Path.Combine(RepositoryPatternScan.FindRepositoryRoot(), ResourceFile));

        foreach (var route in MachineSetupAbortRoutes.All)
        {
            var key = MachineSetupAbortRoutes.ResourceKeyFor(route);

            StringAssert.Contains(
                resources,
                $"name=\"{key}\"",
                $"'{key}' is what {route} is stated from, so it has to ship as a localised string");
        }
    }

    [TestMethod]
    public void Abort_ADismissalRouteTheScreenDoesNotKnowAbout_CannotReachTheShellEither()
    {
        // The last row of the contract's table: the screen is the invitation and the pipeline is the gate, so a
        // route nobody wired up still ends the process rather than carrying on.
        var run = new LaunchRun().Unconfigured();
        var handed = new List<LaunchOutcome>();
        run.Pipeline.ShellReady += (_, outcome) => handed.Add(outcome);
        run.Pipeline.ProcessEnding += (_, _) => { };

        var outcome = run.Pipeline.End("This computer's setup was dismissed in a way the window did not expect.");

        Assert.AreEqual(LaunchOutcome.Ended, outcome);
        CollectionAssert.DoesNotContain(handed, LaunchOutcome.MainScreens);
    }

    [TestMethod]
    public void MachineSetupWindow_AnswersEveryDeclaredAbortRoute()
    {
        // Every route the contract's table names has to be wired to the one ending, so a route added to the set
        // without being handled fails here rather than in a live session.
        var codeBehind = File.ReadAllText(Path.Combine(RepositoryPatternScan.FindRepositoryRoot(), SetupWindowCodeBehind));

        foreach (var route in MachineSetupAbortRoutes.All)
        {
            StringAssert.Contains(
                codeBehind,
                $"MachineSetupAbortRoute.{route}",
                $"{route} is a route out of setup, so the window has to answer it");
        }
    }

    /// <summary>The outcome a step answers with, in the shape a failing step answers with.</summary>
    private static LaunchStepOutcome Failed() => new(LaunchStepStatus.Failed, "the store did not answer", LaunchRemedySet.RetryOnly);

    /// <summary>A relaunch that never happens, because nothing in this file replaces a temporary PIN.</summary>
    private sealed class NoRestart : IProcessRestarter
    {
        public bool Restart() => false;
    }

    /// <summary>
    /// One launch, over recording doubles: which steps exist, what each answers, and what this computer's
    /// configuration says.
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

        public LaunchPipeline Pipeline => _pipeline ??= new LaunchPipeline(
            Catalog,
            new LaunchStepRunner(Feed),
            Feed,
            _steps,
            Machine,
            Person,
            _configuration,
            PendingSignIn,
            new NoRestart());

        /// <summary>Adds a step that answers with one outcome.</summary>
        public LaunchRun With(string stepId, LaunchStepOutcome answer)
        {
            _steps.Add(new RecordingLaunchStep(
                Catalog.Find(stepId) ?? throw new InvalidOperationException($"'{stepId}' is not in the catalogue."),
                () => Task.FromResult(answer)));

            return this;
        }

        /// <summary>States that this computer has no configuration at all.</summary>
        public LaunchRun Unconfigured() => WithConfiguration(
            new MachineConfigurationState(false, null, null, MachineConfigurationReasons.NeverConfigured));

        /// <summary>States what this computer's configuration read answers, and answers for the entry with the step that really does it.</summary>
        /// <remarks>
        /// The real readiness step is registered rather than a double, because which of the two it reports is the
        /// step's own decision: a machine that is not configured is a finding, and a configuration that could not
        /// be read is a stop with a retry (FR-009, FR-017). A double here would assert the test's idea of that
        /// rather than the step's.
        /// </remarks>
        public LaunchRun WithConfiguration(MachineConfigurationState state)
        {
            _configuration.State = state;
            _steps.Add(new MachineReadinessStep(Catalog, _configuration));

            return this;
        }
    }

    /// <summary>A step double whose work is a delegate and whose descriptor is the catalogue's own entry.</summary>
    private sealed class RecordingLaunchStep(LaunchStep descriptor, Func<Task<LaunchStepOutcome>> work) : ILaunchStep
    {
        public LaunchStep Descriptor { get; } = descriptor;

        public Task<LaunchStepOutcome> RunAsync(LaunchStepContext context, CancellationToken cancellationToken) => work();
    }

    /// <summary>This computer's configuration, answered from what the test stated.</summary>
    private sealed class StubMachineConfigurationService : IMachineConfigurationService
    {
        public MachineConfigurationState State { get; set; } = new(
            true,
            "Test workstation",
            "a machine the test owns",
            null);

        public Task<MachineConfigurationState> GetStateAsync(CancellationToken cancellationToken) => Task.FromResult(State);

        public Task<MachineConfigurationSaveResult> SaveAsync(
            MachineConfigurationDraft draft,
            CancellationToken cancellationToken)
            => throw new NotSupportedException("The pipeline never saves this computer's configuration itself.");

        public Task<MachineConfigurationResetResult> ResetToDefaultsAsync(
            IReadOnlyList<string> whatIsBroken,
            CancellationToken cancellationToken)
            => throw new NotSupportedException("The pipeline never resets this computer's configuration itself.");
    }

    /// <summary>A store seam that fails every call, so a case that reaches the store is told so loudly.</summary>
    private sealed class ThrowingMySqlHelperServer : IMySqlHelperServer
    {
        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException("No case in this file reads the store.");

        public Task<int> ExecuteStoredProcedureNonQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException("No case in this file writes the store.");

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteSqlQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException("No statement text is written on the launch path (constitution III).");

        public Task<int> ExecuteSqlNonQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException("No statement text is written on the launch path (constitution III).");
    }
}
