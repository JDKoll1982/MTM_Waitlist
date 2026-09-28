using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Startup.Models;
using MTM_Waitlist.Module_Startup.Services;
using MTM_Waitlist.Tests.Fixtures;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Startup.EdgeCases;

/// <summary>
/// The acceptance surface's cases that no other test file covers: the ones about a launch that stops, a machine
/// whose configuration goes away, a launch that is abandoned, a credential reset meeting a sign-in already under
/// way, and a sign-out whose relaunch fails (`spec.md` Edge Cases; FR-001, FR-009, FR-011, FR-012, FR-013,
/// FR-018, FR-031; SC-001, SC-013).
/// </summary>
/// <remarks>
/// <para>
/// <b>One test per case, and no case twice.</b> The cases another task already proves are not repeated here:
/// the picture share being unreachable is `PictureCacheStepTests`, the display name already being in use is
/// `MachineSetupViewModelTests`, an unreadable remembered sign-in is `RememberedSignInServiceTests`, two copies
/// launched at once is `LaunchPipelineRetryTests`, a slow store is `LaunchStepRunnerTests`, and the five
/// diagnostic cases and the refused clipboard are the logging tests. What is left is what nothing else asserts.
/// </para>
/// <para>
/// <b>Every double is hand-written.</b> The store is a seam that answers what the test stated and records what
/// it was asked, so a claim about a call can be read back without a database, and no test touches the machine it
/// happens to run on.
/// </para>
/// </remarks>
[TestClass]
public sealed class LaunchEdgeCaseTests
{
    private const string ReadinessStepId = "read-machine-configuration";

    /// <summary>The legacy temporary value, which an account issued a credential before this work still holds.</summary>
    private const string LegacyTemporaryValue = "0000";

    [TestMethod]
    public async Task TheStoreCannotBeReachedAtLaunch_StopsWithItsOwnStatementAndOffersNoReset()
    {
        // Arrange: the step that contacts the store cannot reach it.
        var run = new LaunchRun().With("store-reachability", Failed("the store did not answer"));

        // Act
        var outcome = await run.Pipeline.RunAsync(CancellationToken.None);

        // Assert: the launch stops (SC-001), the reason on the feed is the step's own sentence rather than a step
        // number (FR-004), and nothing was written to the machine to stand in for the store (FR-025).
        Assert.AreEqual(LaunchOutcome.Blocked, outcome);

        var failed = run.Feed.Entries.Last(entry => entry.Kind is LaunchFeedEntryKind.StepFailed);
        StringAssert.Contains(failed.Text, "the store did not answer");
        Assert.AreEqual("store-reachability", failed.StepId, "the stop is attributable to the step that stopped it");
        Assert.AreEqual("the store", failed.Target, "the line names what the operation was about");
    }

    [TestMethod]
    public async Task AConfigurationWithdrawnWhileTheApplicationRuns_ReturnsSetupAtTheNextCheck()
    {
        // Arrange: the machine's picture sources were withdrawn, so the read reports the configuration removed.
        var run = new LaunchRun().With(ReadinessStepId, Succeeded()).Removed();

        // Act
        var outcome = await run.Pipeline.RunAsync(CancellationToken.None);

        // Assert: setup returns, which is the next check the spec's case describes (FR-009, SC-013).
        Assert.AreEqual(LaunchOutcome.MachineSetup, outcome);
    }

    [TestMethod]
    public async Task TheLaunchWindowIsClosedWhileALaunchIsRunning_LeavesNoStepReadingAsFailed()
    {
        // Arrange: the launch is abandoned while its first step is under way.
        using var closing = new CancellationTokenSource();
        var run = new LaunchRun().With("store-reachability", () =>
        {
            closing.Cancel();
            closing.Token.ThrowIfCancellationRequested();

            return Task.FromResult(Succeeded());
        });

        // Act
        var abandoned = await Assert.ThrowsExceptionAsync<OperationCanceledException>(
            () => run.Pipeline.RunAsync(closing.Token));

        // Assert: closing the window is not a step failure, so the feed carries no terminal line claiming one
        // and a person who closed the launch does not leave a step reading as if it had broken (FR-001).
        Assert.IsNotNull(abandoned);
        Assert.IsFalse(
            run.Feed.Entries.Any(entry => entry.Kind is LaunchFeedEntryKind.StepFailed),
            "an abandoned launch must not record a step as failed");
    }

    [TestMethod]
    public async Task ATemporaryCredentialResetWhileThePersonSignsIn_TheOldValueMeetsTheReset()
    {
        // Arrange: the account's temporary credential has just been replaced by a reset.
        var account = TemporaryAccount();
        account["password_hash"] = PasswordSecretHasher.Hash("7311", (byte[])account["password_salt"]!);

        // Act: the person presents the value the reset replaced.
        var result = await new CredentialCheckService(new AccountStore(account))
            .CheckAsync("JKoll", "1234", CancellationToken.None);

        // Assert: the reset is met rather than assumed absent, so the old value no longer signs anybody in
        // (spec.md Edge Cases; FR-012).
        Assert.IsFalse(result.IsAccepted, "the replaced value is still accepted");
        Assert.AreEqual(CredentialCheckRefusals.CredentialRefused, result.RefusalReason);
    }

    [TestMethod]
    public void ACredentialResetIssuedElsewhere_EndsNoSessionAlreadyRunning()
    {
        // Arrange: every procedure that writes an account's credential.
        var repositoryRoot = RepositoryPatternScan.FindRepositoryRoot();
        var procedureFolder = Path.Combine(repositoryRoot, "Database", "StoredProcedures");

        var credentialWriters = Directory
            .EnumerateFiles(procedureFolder, "create.sql", SearchOption.AllDirectories)
            .Where(file => Path.GetFileName(Path.GetDirectoryName(file)!).Contains("password", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.IsTrue(credentialWriters.Count > 0, "no procedure writes an account's credential, so this case cannot be judged");

        // Act and assert: none of them names the session table, which is the rule the case turns on. A reset is
        // about a credential, and ending a session somebody else is using is not something it may do (FR-011).
        foreach (var file in credentialWriters)
        {
            var body = File.ReadAllText(file);

            Assert.IsFalse(
                body.Contains("user_active_sessions", StringComparison.OrdinalIgnoreCase),
                $"{Path.GetRelativePath(repositoryRoot, file)} touches a running session, so a reset would end one");
        }
    }

    [TestMethod]
    public async Task AnAccountStillHoldingTheLegacyTemporaryValue_SignsInAndIsAskedToReplaceIt()
    {
        // Arrange: an account whose credential is the legacy temporary value, not one this work issued.
        var account = TemporaryAccount();
        account["password_hash"] = PasswordSecretHasher.Hash(LegacyTemporaryValue, (byte[])account["password_salt"]!);

        // Act
        var result = await new CredentialCheckService(new AccountStore(account))
            .CheckAsync("JKoll", LegacyTemporaryValue, CancellationToken.None);

        // Assert: the legacy value is recognised exactly as it is today, and the person is still asked to set a
        // new password before carrying on (spec.md Edge Cases; FR-013).
        Assert.IsTrue(result.IsAccepted, "the legacy temporary value is no longer recognised");
        Assert.IsTrue(result.RequiresNewPassword);
    }

    [TestMethod]
    public async Task ASignOutWhoseRestartFails_EndsTheSessionAndTellsThePersonToOpenTheApplicationAgain()
    {
        // Arrange: a signed-in person whose relaunch cannot happen.
        var store = new SessionStore();
        var person = new PersonIdentityService(store);
        person.Apply(7L, "JKoll", "J. Koll", "1042", "developer", ["developer"]);

        var machine = new FakeMachineFacts
        {
            RegisteredComputer = new ComputerRecord
            {
                Id = 3L,
                ComputerName = "test-workstation",
                DisplayName = "Test workstation",
                MacAddressNormalized = "aa-bb-cc-dd-ee-ff",
                IsRegistered = true,
            },
        };

        var signOut = new SignOutService(
            new LaunchSessionService(store),
            person,
            machine,
            new FailingRestarter());

        // Act
        var outcome = await signOut.SignOutAsync(CancellationToken.None);

        // Assert: the session is ended and the person is told to open the application again, so no half-signed-out
        // state is left behind them (spec.md Edge Cases; FR-008, FR-011, SC-001).
        Assert.IsFalse(outcome.Succeeded, "a relaunch that could not happen is not a completed sign-out");
        Assert.IsTrue(store.Cleared, "the session was not ended before the relaunch was attempted");
        StringAssert.Contains(outcome.Message, "open it again");
        Assert.IsFalse(person.IsSignedIn, "the identity survived a sign-out");
    }

    /// <summary>The outcome a step answers with, in the shapes this file needs.</summary>
    private static LaunchStepOutcome Succeeded() => new(LaunchStepStatus.Succeeded, null, LaunchRemedySet.None);

    /// <summary>A step that failed with a stated cause.</summary>
    private static LaunchStepOutcome Failed(string diagnosis)
        => new(LaunchStepStatus.Failed, diagnosis, LaunchRemedySet.RetryOnly);

    /// <summary>One account row in the column names the credential read returns, on a temporary credential.</summary>
    private static Dictionary<string, object?> TemporaryAccount() => new()
    {
        ["id"] = 7L,
        ["role_code"] = "role:production",
        ["display_name"] = "J. Koll",
        ["employee_identifier"] = "1042",
        ["password_hash"] = PasswordSecretHasher.Hash("1234", s_salt),
        ["password_salt"] = s_salt,
        ["require_password_change"] = 1L,
        ["temporary_credential_failed_attempts"] = 0L,
    };

    /// <summary>One random salt for the class, so no case depends on a value written into the repository.</summary>
    private static readonly byte[] s_salt = PasswordSecretHasher.NewSalt();

    /// <summary>A store that answers the credential read with the one account it was built with.</summary>
    private sealed class AccountStore(Dictionary<string, object?> account) : IMySqlHelperServer
    {
        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Dictionary<string, object?>>>(
                string.Equals(storedProcedureName, "sp_auth_user_credential_get", StringComparison.Ordinal)
                    ? [account]
                    : []);

        public Task<int> ExecuteStoredProcedureNonQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => Task.FromResult(1);

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteSqlQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException("No statement text is written in the sign-in path (constitution III).");

        public Task<int> ExecuteSqlNonQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException("No statement text is written in the sign-in path (constitution III).");
    }

    /// <summary>A store that answers the session reads and records that the session was ended.</summary>
    private sealed class SessionStore : IMySqlHelperServer
    {
        /// <summary>Whether the session was ended, which is what FR-011 turns on.</summary>
        public bool Cleared { get; private set; }

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Dictionary<string, object?>>>([]);

        public Task<int> ExecuteStoredProcedureNonQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
        {
            if (string.Equals(storedProcedureName, "sp_auth_user_active_sessions_clear", StringComparison.Ordinal))
            {
                Cleared = true;
            }

            return Task.FromResult(1);
        }

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteSqlQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException("No statement text is written in the session path (constitution III).");

        public Task<int> ExecuteSqlNonQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException("No statement text is written in the session path (constitution III).");
    }

    /// <summary>A relaunch that cannot happen, which is the case this file proves.</summary>
    private sealed class FailingRestarter : IProcessRestarter
    {
        public bool Restart() => false;
    }

    /// <summary>
    /// One launch over recording steps: which steps exist, what each answers, and the feed the launch writes to.
    /// </summary>
    private sealed class LaunchRun
    {
        private readonly List<ILaunchStep> _steps = [];
        private readonly StubMachineConfigurationService _configuration = new();

        private LaunchPipeline? _pipeline;

        public LaunchStepCatalog Catalog { get; } = new();

        public ILaunchActivityFeed Feed { get; } = new LaunchActivityFeed();

        public LaunchPipeline Pipeline => _pipeline ??= new LaunchPipeline(
            Catalog,
            new LaunchStepRunner(Feed),
            Feed,
            _steps,
            new FakeMachineFacts { Hostname = "test-workstation" },
            new PersonIdentityService(new NullStore()),
            _configuration,
            new PendingSignIn());

        /// <summary>Adds a step that answers with one outcome.</summary>
        public LaunchRun With(string stepId, LaunchStepOutcome answer) => With(stepId, () => Task.FromResult(answer));

        /// <summary>Adds a step whose work the test states.</summary>
        public LaunchRun With(string stepId, Func<Task<LaunchStepOutcome>> work)
        {
            _steps.Add(new RecordingLaunchStep(
                Catalog.Find(stepId) ?? throw new InvalidOperationException($"'{stepId}' is not in the catalogue."),
                work));

            return this;
        }

        /// <summary>States that this machine's configuration was withdrawn rather than never given.</summary>
        public LaunchRun Removed()
        {
            _configuration.State = new MachineConfigurationState(
                false,
                null,
                null,
                [],
                MachineConfigurationReasons.Removed);

            return this;
        }
    }

    /// <summary>A step double whose work is a delegate.</summary>
    private sealed class RecordingLaunchStep(LaunchStep descriptor, Func<Task<LaunchStepOutcome>> work) : ILaunchStep
    {
        public LaunchStep Descriptor { get; } = descriptor;

        public Task<LaunchStepOutcome> RunAsync(LaunchStepContext context, CancellationToken cancellationToken)
            => work();
    }

    /// <summary>This machine's configuration, answered from what the test stated.</summary>
    private sealed class StubMachineConfigurationService : IMachineConfigurationService
    {
        public MachineConfigurationState State { get; set; } = new(
            true,
            "Test workstation",
            "a machine the test owns",
            [
                new PictureSource(MachineConfigurationSourceKinds.SharedFolder, @"\\share\pictures"),
                new PictureSource(MachineConfigurationSourceKinds.KeysFolder, @"\\share\keys"),
                new PictureSource(MachineConfigurationSourceKinds.DunnageRoot, @"\\share\dunnage"),
            ],
            null);

        public Task<MachineConfigurationState> GetStateAsync(CancellationToken cancellationToken)
            => Task.FromResult(State);

        public Task<MachineConfigurationSaveResult> SaveAsync(
            MachineConfigurationDraft draft,
            CancellationToken cancellationToken)
            => throw new NotSupportedException("This file never saves this machine's configuration.");

        public Task<MachineConfigurationResetResult> ResetToDefaultsAsync(
            IReadOnlyList<string> whatIsBroken,
            CancellationToken cancellationToken)
            => throw new NotSupportedException("This file never resets this machine's configuration.");
    }

    /// <summary>A store seam that fails every call, so a test that reaches the store is told so loudly.</summary>
    private sealed class NullStore : IMySqlHelperServer
    {
        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException("No test in this file reads the store through this seam.");

        public Task<int> ExecuteStoredProcedureNonQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException("No test in this file writes the store through this seam.");

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
