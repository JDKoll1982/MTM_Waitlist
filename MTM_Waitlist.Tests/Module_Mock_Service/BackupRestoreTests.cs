using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Mock.Service.Models;
using MTM_Waitlist.Mock.Service.Services;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Module_Mock_Service;

/// <summary>
/// FR-009/FR-010/FR-013 and FR-023's backup/restore gate: each store's backups are its own, a missing tool is
/// reported without inventing an artifact, an unconfirmed restore changes nothing, and a restore that cannot
/// take its safety snapshot refuses rather than destroying the store.
/// </summary>
/// <remarks>
/// <para>
/// <b>What is asserted here, and what is not.</b> These tests drive the real <see cref="BackupEngine"/> and
/// <see cref="RestoreService"/> with the MySQL tools deliberately absent, which is the state that matters most:
/// it is the state in which the service must refuse to act rather than act badly. The happy path — a confirmed
/// restore that really replaces and verifies a store — needs a live MySQL server and is covered by the
/// environment-gated live-database test (T118), exactly as the refresh swap is.
/// </para>
/// <para>
/// <b>No code path from the API to a restore</b> is asserted separately, by reflection, in
/// <c>ServiceApiSecurityTests.NoCodePathLeadsFromTheApiToARestore</c>, and at the listener level by
/// <c>ServiceApiTests.Restore_IsNotReachableOverTheNetwork_EvenWithAValidCredential</c>.
/// </para>
/// </remarks>
[TestClass]
public sealed class BackupRestoreTests
{
    private static readonly DateTime BaseUtc = new(2026, 9, 11, 8, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Every variable the connection resolver consults. A machine that carries real credentials — the cache host
    /// does — would otherwise let a backup reach a live server, and the "nothing is configured" case could not be
    /// tested at all.
    /// </summary>
    private static readonly string[] s_connectionEnvironmentVariables =
    [
        "MTM_MOCK_DB_CONNECTION_STRING",
        "MTM_WAITLIST_DB_CONNECTION_STRING",
        "MTM_WIP_APPLICATION_DB_CONNECTION_STRING",
        "MTM_RECEIVING_APPLICATION_DB_CONNECTION_STRING",
        "MTM_MYSQL_PASSWORD",
    ];

    private string? _originalPath;
    private string?[] _originalConnectionVariables = [];

    /// <summary>
    /// A stand-in for <c>mysqldump</c>: it answers the version probe, records the command line it was handed, and
    /// writes a non-empty file at the dump path. It uses only cmd.exe built-ins, so the cleared <c>PATH</c> in the
    /// fixture cannot affect it.
    /// </summary>
    /// <remarks>
    /// <b>Why the last argument.</b> cmd.exe splits its batch parameters at <c>=</c> as well as at spaces, so
    /// <c>--result-file=C:\path</c> arrives as the two tokens <c>--result-file</c> and <c>C:\path</c> — the flag and
    /// its value cannot be matched together. The engine appends <c>--result-file</c> last
    /// (<see cref="BackupEngine.BuildArguments"/>), so the final token is the path, and this reads it that way. A
    /// fixture that depends on argument order is a fair trade for exercising the real engine against a real process;
    /// the genuine end-to-end dump needs live MySQL and is covered by T118.
    /// </remarks>
    private const string FakeToolScript = """
        @echo off
        setlocal enabledelayedexpansion
        echo %* > "%~dp0args.txt"
        if "%~1"=="--version" ( echo mysqldump  Ver 8.0.46 & exit /b 0 )
        set "LAST="
        :parse
        if "%~1"=="" goto parsed
        set "LAST=%~1"
        shift
        goto parse
        :parsed
        if defined LAST >"%LAST%" echo -- fake dump
        exit /b 0
        """;

    /// <summary>
    /// <see cref="BackupEngine.ResolveToolPath"/> falls back to searching <c>PATH</c> for <c>mysqldump.exe</c>,
    /// so a developer machine that happens to have the MySQL client tools installed would silently exercise a
    /// different path than the one under test. <c>PATH</c> is cleared for the duration of each test and restored
    /// afterwards; the suite runs sequentially (no assembly-level <c>Parallelize</c>), so nothing else is affected.
    /// </summary>
    [TestInitialize]
    public void HideAnyInstalledMySqlClientTools()
    {
        _originalPath = Environment.GetEnvironmentVariable("PATH");
        Environment.SetEnvironmentVariable("PATH", string.Empty);

        _originalConnectionVariables = s_connectionEnvironmentVariables
            .Select(Environment.GetEnvironmentVariable)
            .ToArray();

        foreach (var variable in s_connectionEnvironmentVariables)
        {
            Environment.SetEnvironmentVariable(variable, null);
        }
    }

    [TestCleanup]
    public void RestoreEnvironment()
    {
        Environment.SetEnvironmentVariable("PATH", _originalPath);

        for (var index = 0; index < s_connectionEnvironmentVariables.Length; index++)
        {
            Environment.SetEnvironmentVariable(
                s_connectionEnvironmentVariables[index],
                _originalConnectionVariables[index]);
        }
    }

    [TestMethod]
    public async Task RunAsync_WhenTheToolIsMissing_ReportsToolUnavailableAndRecordsNoArtifact()
    {
        using var fixture = new StoreFixture();
        var engine = fixture.CreateEngine(mysqldumpPath: Path.Combine(fixture.Root, "no-such-tool.exe"));

        var run = await engine.RunAsync(BackupStore.MtmWaitlist);

        Assert.AreEqual(BackupRunOutcome.ToolUnavailable, run.Outcome);
        Assert.IsNull(run.ArtifactPath, "A missing tool produces no artifact path.");
        Assert.AreEqual(
            0,
            fixture.Store.GetArtifacts(BackupStore.MtmWaitlist).Count,
            "A missing tool must record no artifact — a partial or zero-length dump is never a success (FR-013).");
    }

    [TestMethod]
    public async Task RunAsync_ForOneStore_LeavesEveryOtherStoreUntouched()
    {
        using var fixture = new StoreFixture();
        var otherStoreArtifacts = await fixture.SeedAsync(BackupStore.MtmReceivingApplication, 2);
        var engine = fixture.CreateEngine(mysqldumpPath: Path.Combine(fixture.Root, "no-such-tool.exe"));

        _ = await engine.RunAsync(BackupStore.MtmWaitlist);

        var other = fixture.Store.GetArtifacts(BackupStore.MtmReceivingApplication);
        Assert.AreEqual(2, other.Count, "Disabling or failing one store must not change another store's artifacts.");
        Assert.IsTrue(
            otherStoreArtifacts.All(seeded => other.Any(candidate => candidate.FilePath == seeded.FilePath)),
            "The other store's artifacts must be exactly the ones it had before this run.");
        Assert.IsTrue(
            other.All(artifact => File.Exists(artifact.FilePath)),
            "Another store's files must still be on disk.");
    }

    [TestMethod]
    public void BuildArguments_TargetsTheResolvedConnection_NotTheSettingsRecord()
    {
        var arguments = BackupEngine.BuildArguments(
            "mtm_waitlist",
            @"C:\state\artifacts\mtm_waitlist_20260912T060000Z.sql",
            "Server=172.16.1.104;Port=3307;Database=mtm_waitlist;User Id=root;Password=root;",
            "--defaults-extra-file=C:\\state\\mysql-client-abc.cnf");

        StringAssert.Contains(arguments, "--host=172.16.1.104", "The dump must target the resolved host.");
        StringAssert.Contains(arguments, "--port=3307", "The dump must use the resolved port.");
        StringAssert.Contains(arguments, "--user=root", "The dump must use the resolved login.");
        StringAssert.Contains(arguments, "--defaults-extra-file=", "The credentials file must be passed.");
        StringAssert.Contains(arguments, "--databases mtm_waitlist");
        StringAssert.Contains(arguments, "--single-transaction");
        StringAssert.Contains(arguments, "--routines");
        StringAssert.Contains(arguments, "--result-file=");
    }

    [TestMethod]
    public void BuildArguments_NeverPutsThePasswordOnTheCommandLine()
    {
        var arguments = BackupEngine.BuildArguments(
            "mtm_mock",
            @"C:\state\artifacts\mtm_mock.sql",
            "Server=172.16.1.104;Port=3306;Database=mtm_mock;User Id=root;Password=sup3r-s3cret;",
            credentialsFileArgument: null);

        Assert.IsFalse(
            arguments.Contains("sup3r-s3cret", StringComparison.Ordinal),
            "The password must never reach the command line (FR-026).");
        Assert.IsFalse(
            arguments.Contains("--password", StringComparison.Ordinal),
            "The password is supplied only through an option file (FR-026).");
    }

    [TestMethod]
    public async Task RunAsync_WhenTheResolvedConnectionSuppliesTheCredentials_DumpsAgainstThatHost()
    {
        using var fixture = new StoreFixture();
        var toolPath = fixture.CreateFakeMySqlDump();
        var engine = fixture.CreateEngine(mysqldumpPath: toolPath);

        // The store's own variable, which is the one a read of mtm_mock resolves through as well.
        Environment.SetEnvironmentVariable(
            "MTM_MOCK_DB_CONNECTION_STRING",
            "Server=172.16.1.104;Port=3306;Database=mtm_mock;User Id=root;Password=sup3r-s3cret;");

        var run = await engine.RunAsync(BackupStore.MtmMock);

        Assert.AreEqual(BackupRunOutcome.Succeeded, run.Outcome, run.ErrorMessage);
        Assert.IsNotNull(run.ArtifactPath);

        var arguments = File.ReadAllText(fixture.FakeToolArgumentsPath);

        // The expectation is "the dump targets the host the connection resolved to", not a literal address:
        // the resolver substitutes this machine's own server when the configured one does not answer
        // (MySqlHostFallback, T165/T171), so a literal would make this test pass or fail by machine rather
        // than by behaviour. Applying the same rule to the same input keeps the assertion about the engine.
        var effectiveConnection = MySqlHostFallback.Apply(
            Environment.GetEnvironmentVariable("MTM_MOCK_DB_CONNECTION_STRING"))!;
        var resolvedHost = new MySqlConnector.MySqlConnectionStringBuilder(effectiveConnection).Server;

        StringAssert.Contains(arguments, $"--host={resolvedHost}", "The resolved host must be the dump target.");
        StringAssert.Contains(arguments, "--port=3306");
        StringAssert.Contains(arguments, "--user=root");
        StringAssert.Contains(
            arguments,
            "--defaults-extra-file=",
            "The password must travel through a credentials file, not the command line.");
        Assert.IsFalse(
            arguments.Contains("sup3r-s3cret", StringComparison.Ordinal),
            "The password must never appear on the command line (FR-026).");

        Assert.AreEqual(
            0,
            Directory.GetFiles(fixture.Root, "*.cnf", SearchOption.AllDirectories).Length,
            "A generated credentials file must not outlive the invocation.");
    }

    [TestMethod]
    public async Task RunAsync_WhenNoConnectionIsConfigured_ReportsTheDesignedReasonAndDumpsNothing()
    {
        using var fixture = new StoreFixture();
        var toolPath = fixture.CreateFakeMySqlDump();
        var engine = fixture.CreateEngine(mysqldumpPath: toolPath);

        var run = await engine.RunAsync(BackupStore.MtmWaitlist);

        Assert.AreEqual(BackupRunOutcome.Failed, run.Outcome);
        Assert.AreEqual(MySqlConnectionStringResolver.NotConfiguredMessage, run.ErrorMessage);
        Assert.IsNull(run.ArtifactPath, "An unconfigured backup produces no artifact.");

        // The version probe legitimately runs the tool; the dump must not be requested on top of it.
        var arguments = File.ReadAllText(fixture.FakeToolArgumentsPath);
        StringAssert.Contains(arguments, "--version");
        Assert.IsFalse(
            arguments.Contains("--result-file", StringComparison.Ordinal),
            "Nothing is configured, so no dump may be requested.");
    }

    [TestMethod]
    public async Task RequestRestore_RecordsTheIntentWithoutChangingAnything()
    {
        using var fixture = new StoreFixture();
        var artifacts = await fixture.SeedAsync(BackupStore.MtmWaitlist, 1);
        var restore = fixture.CreateRestoreService();
        var before = Directory.GetFiles(fixture.Root, "*", SearchOption.AllDirectories).Length;

        var request = restore.RequestRestore(artifacts[0]);

        Assert.AreEqual(RestoreOutcomeKind.NotConfirmed, request.Outcome);
        Assert.AreEqual(artifacts[0].ArtifactId, request.ArtifactId);
        Assert.AreEqual(BackupStore.MtmWaitlist, request.Store);
        Assert.IsNull(request.ConfirmedUtc, "An unconfirmed request has not been confirmed.");
        Assert.AreEqual(
            1,
            fixture.Store.GetArtifacts(BackupStore.MtmWaitlist).Count,
            "Requesting a restore must not record a run or an artifact.");
        Assert.AreEqual(
            before,
            Directory.GetFiles(fixture.Root, "*", SearchOption.AllDirectories).Length,
            "Requesting a restore must not write anything to disk.");
    }

    [TestMethod]
    public async Task ConfirmAndRestoreAsync_WhenTheSafetySnapshotCannotBeTaken_RefusesAndChangesNothing()
    {
        using var fixture = new StoreFixture();
        var artifacts = await fixture.SeedAsync(BackupStore.MtmWaitlist, 1);
        var restore = fixture.CreateRestoreService();
        var request = restore.RequestRestore(artifacts[0]);

        var outcome = await restore.ConfirmAndRestoreAsync(request, artifacts[0]);

        Assert.AreEqual(
            RestoreOutcomeKind.FailedReload,
            outcome.Outcome,
            "Without a recovery point the restore must abort rather than drop the store (FR-010).");
        StringAssert.Contains(outcome.VerificationSummary, "nothing was changed");
        Assert.IsNull(
            outcome.SafetySnapshotArtifactId,
            "No safety snapshot exists, so none may be named as a recovery path.");
        Assert.AreEqual(
            1,
            fixture.Store.GetArtifacts(BackupStore.MtmWaitlist).Count,
            "The aborted restore must not record a new artifact.");
        Assert.IsTrue(
            File.Exists(artifacts[0].FilePath),
            "The artifact the operator selected must still be there.");
    }

    [TestMethod]
    public async Task ConfirmAndRestoreAsync_WithAnArtifactThatWasNotRequested_IsRejected()
    {
        using var fixture = new StoreFixture();
        var artifacts = await fixture.SeedAsync(BackupStore.MtmWaitlist, 2);
        var restore = fixture.CreateRestoreService();
        var request = restore.RequestRestore(artifacts[0]);

        // Confirming with a different artifact is the mistake this guard exists to stop.
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => restore.ConfirmAndRestoreAsync(request, artifacts[1]));
    }

    [TestMethod]
    public async Task ConfirmAndRestoreAsync_WhenTheArtifactIsGone_IsRejected()
    {
        using var fixture = new StoreFixture();
        var artifacts = await fixture.SeedAsync(BackupStore.MtmWaitlist, 1);
        var restore = fixture.CreateRestoreService();
        var request = restore.RequestRestore(artifacts[0]);
        File.Delete(artifacts[0].FilePath);

        await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => restore.ConfirmAndRestoreAsync(request, artifacts[0]));
    }

    /// <summary>
    /// One throwaway app-data root, with the MySQL tools deliberately absent and a configuration that points
    /// at a tool path the fixture controls.
    /// </summary>
    private sealed class StoreFixture : IDisposable
    {
        private int _sequence;

        public StoreFixture()
        {
            Root = Path.Combine(Path.GetTempPath(), "mtm-backup-restore-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);

            Store = new BackupArtifactStore(Root);
        }

        public string Root { get; }

        public BackupArtifactStore Store { get; }

        public BackupEngine CreateEngine(string mysqldumpPath)
        {
            var configuration = ServiceConfiguration.CreateDefault(Root) with { MysqldumpPath = mysqldumpPath };

            return new BackupEngine(
                Store,
                new MySqlConnectionStringResolver(configuration.MySqlConnection),
                () => configuration,
                NullLogger<BackupEngine>.Instance);
        }

        /// <summary>Where the fake tool records the command line it was given.</summary>
        public string FakeToolArgumentsPath => Path.Combine(Root, "fake-tool", "args.txt");

        /// <summary>
        /// Writes a stand-in for <c>mysqldump</c> that answers the version probe, records its command line, and
        /// writes a non-empty file at <c>--result-file</c>.
        /// </summary>
        /// <remarks>
        /// A <c>.cmd</c> rather than a stub type: the engine runs a real process, and the behaviour under test —
        /// which host the command line names and where the password does <i>not</i> appear — is only observable on
        /// a real command line. It uses no external command, so the cleared <c>PATH</c> in the fixture is harmless.
        /// </remarks>
        public string CreateFakeMySqlDump()
        {
            var directory = Path.Combine(Root, "fake-tool");
            Directory.CreateDirectory(directory);

            var toolPath = Path.Combine(directory, "fake-mysqldump.cmd");
            File.WriteAllText(toolPath, FakeToolScript, new System.Text.UTF8Encoding(false));

            return toolPath;
        }

        public RestoreService CreateRestoreService()
        {
            var engine = CreateEngine(Path.Combine(Root, "no-such-tool.exe"));
            var configuration = ServiceConfiguration.CreateDefault(Root);

            return new RestoreService(
                engine,
                Store,
                new MySqlConnectionStringResolver(configuration.MySqlConnection),
                () => configuration,
                NullLogger<RestoreService>.Instance);
        }

        /// <summary>Records artifacts with increasing creation times and real files on disk.</summary>
        public async Task<IReadOnlyList<BackupArtifact>> SeedAsync(BackupStore store, int count)
        {
            var created = new List<BackupArtifact>(count);

            for (var index = 0; index < count; index++)
            {
                var timestamp = BaseUtc.AddMinutes(index);
                var directory = Path.Combine(Root, "artifacts", store.ToDatabaseName());
                Directory.CreateDirectory(directory);

                var filePath = Path.Combine(
                    directory,
                    $"{store.ToDatabaseName()}_{timestamp:yyyyMMdd'T'HHmmss'Z'}_{_sequence++:D3}.sql");
                await File.WriteAllTextAsync(filePath, "-- dump");

                var artifact = new BackupArtifact
                {
                    Store = store,
                    CreatedUtc = timestamp,
                    FilePath = filePath,
                    SizeBytes = new FileInfo(filePath).Length,
                    IsRetained = true,
                    IsSafetySnapshot = false,
                };

                await Store.RecordAsync(
                    new BackupRunRecord
                    {
                        Store = store,
                        StartedUtc = timestamp,
                        FinishedUtc = timestamp,
                        Outcome = BackupRunOutcome.Succeeded,
                        ArtifactPath = filePath,
                        IsSafetySnapshot = false,
                    },
                    artifact);

                created.Add(artifact);
            }

            return created;
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Root))
                {
                    Directory.Delete(Root, recursive: true);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
