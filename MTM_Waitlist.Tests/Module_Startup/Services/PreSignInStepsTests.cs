using Microsoft.Extensions.Options;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Startup.Models;
using MTM_Waitlist.Module_Startup.Services;
using MTM_Waitlist.Tests.Fixtures;

namespace MTM_Waitlist.Tests.Module_Startup.Services;

/// <summary>
/// The seven launch steps that run before anybody signs in (`contracts/launch-step-contract.md` §1, §1.1, §2;
/// FR-002, FR-003, FR-015, FR-016, FR-017).
/// </summary>
/// <remarks>
/// <para>
/// Three promises are pinned for every one of them, because they are the promises the launch surface depends on:
/// the descriptor a step publishes is the catalogue's own entry for its id rather than a copy, a step never
/// announces itself, and a dependency that cannot answer produces a stated failure instead of an exception
/// escaping. Each step's own behaviour is then pinned on top of those.
/// </para>
/// <para>
/// The doubles are hand-written recordings, as the suite's conventions require: each one answers with the shape
/// the step asks for and records what it was asked, so a claim about a call can be read back without a store.
/// Where a branch depends on the host — whether this machine presents a usable hardware address — the test says
/// so and stands down rather than asserting a fact about the machine it happens to run on.
/// </para>
/// </remarks>
[TestClass]
public sealed class PreSignInStepsTests
{
    /// <summary>The operations the catalogue declares ahead of the first sign-in, in launch order.</summary>
    private static readonly string[] s_preSignInStepIds =
    [
        "read-local-settings",
        "store-reachability",
        "read-hardware-identity",
        "read-computer-record",
        "read-machine-configuration",
        "save-machine-configuration",
        "read-remembered-sign-in",
    ];

    [TestMethod]
    public void Descriptor_EveryPreSignInStep_IsTheCataloguesOwnEntryForItsId()
    {
        // Arrange
        var catalog = new LaunchStepCatalog();

        // Act
        var steps = AllSteps(catalog);

        // Assert: the sequence the launch walks and the descriptors the steps publish are one set of records, so
        // a step cannot drift from the name, the maximum and the target the catalogue declares (FR-002, FR-003).
        CollectionAssert.AreEqual(
            s_preSignInStepIds,
            steps.Select(step => step.Descriptor.Id).ToArray(),
            "the pre-sign-in steps are not the operations the catalogue declares");

        foreach (var step in steps)
        {
            Assert.AreSame(
                catalog.Find(step.Descriptor.Id),
                step.Descriptor,
                $"'{step.Descriptor.Id}' publishes a descriptor of its own rather than the catalogue's entry");
        }
    }

    [TestMethod]
    public async Task RunAsync_EveryPreSignInStep_AnnouncesNothingOfItsOwn()
    {
        // Arrange: the runner names each step before it runs, so a step that named itself would write the line
        // twice (FR-002). Running every step straight, with no runner, is what isolates that claim.
        var feed = new LaunchActivityFeed();

        // Act
        foreach (var step in AllSteps(new LaunchStepCatalog()))
        {
            await step.RunAsync(Context(feed), CancellationToken.None);
        }

        // Assert
        Assert.AreEqual(
            0,
            feed.Entries.Count,
            "a step added a line of its own; only the runner names a step and records how it ended");
    }

    [TestMethod]
    public async Task ConfigurationStep_RunAsync_WhenTheSavedSettingsNameAStore_ReportsSucceeded()
    {
        // Arrange
        var settings = Options.Create(new WaitlistDatabaseOptions { ConnectionString = "Server=localhost;Database=mtm_waitlist;" });
        var step = new ConfigurationStep(new LaunchStepCatalog(), settings);

        // Act
        var outcome = await step.RunAsync(Context(new LaunchActivityFeed()), CancellationToken.None);

        // Assert
        Assert.AreEqual(LaunchStepStatus.Succeeded, outcome.Status);
        Assert.IsNotNull(outcome.Diagnosis);
    }

    [TestMethod]
    public async Task ConfigurationStep_RunAsync_WhenTheSavedSettingsNameNoStore_ReportsWhatItFoundRatherThanFailing()
    {
        // Arrange: this computer keeps no connection of its own, which is the state of a machine reached from the
        // environment. The read still happened, and the store itself is what the next step consults.
        var step = new ConfigurationStep(new LaunchStepCatalog(), Options.Create(new WaitlistDatabaseOptions()));

        // Act
        var outcome = await step.RunAsync(Context(new LaunchActivityFeed()), CancellationToken.None);

        // Assert
        Assert.AreEqual(LaunchStepStatus.Succeeded, outcome.Status, "a completed read is not a failure");
        StringAssert.Contains(outcome.Diagnosis!, "no store connection");
    }

    [TestMethod]
    public async Task ConfigurationStep_RunAsync_WhenTheSavedSettingsCannotBeRead_ReportsFailedRatherThanThrowing()
    {
        // Arrange
        var step = new ConfigurationStep(new LaunchStepCatalog(), new UnreadableSavedSettings());

        // Act
        var outcome = await step.RunAsync(Context(new LaunchActivityFeed()), CancellationToken.None);

        // Assert: a dependency that cannot answer is reported, not raised, and the diagnosis names what could not
        // be done rather than leaving the person with an unattributed crash (FR-004).
        Assert.AreEqual(LaunchStepStatus.Failed, outcome.Status);
        StringAssert.Contains(outcome.Diagnosis!, "could not be read");
        Assert.IsTrue(outcome.Remedies.CanRetry, "every failed step is repeatable (FR-016)");
        Assert.IsFalse(outcome.Remedies.CanRestoreDefaults, "a reset could not remove the cause (FR-017)");
    }

    [TestMethod]
    public async Task StoreReachabilityStep_RunAsync_WhenTheStoreAnswers_ReadsOneStoredProcedureAndReportsSucceeded()
    {
        // Arrange
        var store = new StubMySqlHelperServer();
        var step = new StoreReachabilityStep(new LaunchStepCatalog(), store);

        // Act
        var outcome = await step.RunAsync(Context(new LaunchActivityFeed()), CancellationToken.None);

        // Assert: the step calls the store seam, which calls a stored procedure, and the probe is one round trip
        // against the records store rather than a statement written here (constitution III).
        Assert.AreEqual(LaunchStepStatus.Succeeded, outcome.Status);
        Assert.AreEqual(1, store.QueryCallCount, "the reachability check must be one round trip");
        Assert.AreEqual("sp_core_computers_registry_get_all", store.LastStoredProcedureName);
        Assert.AreEqual(MySqlDatabaseTarget.MtmWaitlist, store.LastDatabaseTarget);
        Assert.AreEqual(0, store.LastParameters.Count, "the reachability read takes no argument");
    }

    [TestMethod]
    public async Task StoreReachabilityStep_RunAsync_WhenTheStoreDoesNotAnswer_ReportsFailedWithARetryAndNoReset()
    {
        // Arrange
        var step = new StoreReachabilityStep(new LaunchStepCatalog(), new StubMySqlHelperServer(throwOnQuery: true));

        // Act
        var outcome = await step.RunAsync(Context(new LaunchActivityFeed()), CancellationToken.None);

        // Assert: what the store seam reported is carried into the diagnosis, and a reset is not offered because
        // resetting this machine's configuration could not remove an outage (FR-004, FR-017).
        Assert.AreEqual(LaunchStepStatus.Failed, outcome.Status);
        StringAssert.Contains(outcome.Diagnosis!, "refused the connection");
        Assert.IsTrue(outcome.Remedies.CanRetry);
        Assert.IsFalse(outcome.Remedies.CanRestoreDefaults, "a store outage never offers a reset (FR-017)");
    }

    [TestMethod]
    public async Task ReadHardwareIdentityStep_RunAsync_ReportsThisComputersOwnName()
    {
        // Arrange: the step reads the machine from its context, so the machine the case is about can be stated
        // rather than borrowed from whichever host the suite happens to run on.
        var step = new ReadHardwareIdentityStep(new LaunchStepCatalog());
        var machine = new FakeMachineFacts { Hostname = "shop-floor-4", MacAddress = "aa-bb-cc-dd-ee-ff", HardwareIdentityReadable = true };

        // Act
        var outcome = await step.RunAsync(Context(new LaunchActivityFeed(), machine), CancellationToken.None);

        // Assert: the name the step reports is the machine's own, read from the machine rather than from the store.
        Assert.AreEqual(LaunchStepStatus.Succeeded, outcome.Status);
        StringAssert.Contains(outcome.Diagnosis!, "shop-floor-4");
    }

    [TestMethod]
    public async Task ReadHardwareIdentityStep_RunAsync_WhenTheAddressCouldNotBeRead_ReportsTheWorkLeftUndone()
    {
        // Arrange: a machine that presents no usable hardware address, which is the branch this case is about.
        var step = new ReadHardwareIdentityStep(new LaunchStepCatalog());
        var machine = new FakeMachineFacts { Hostname = "shop-floor-4", MacAddress = null, HardwareIdentityReadable = false };

        // Act
        var outcome = await step.RunAsync(Context(new LaunchActivityFeed(), machine), CancellationToken.None);

        // Assert: a fact that could not be read is not a failed check, so the machine is admitted and the launch
        // carries on (FR-015).
        Assert.AreEqual(LaunchStepStatus.Skipped, outcome.Status);
        StringAssert.Contains(outcome.Diagnosis!, "by name alone");
    }

    [TestMethod]
    public async Task ReadComputerRecordStep_RunAsync_WhenTheStoreHoldsTheRow_ReportsSucceeded()
    {
        // Arrange: the address is read from this machine and cannot be given one, so the store's answer is built
        // from what the machine reports rather than from a constant the test would have to keep in step.
        var probe = new MachineFactsService(new StubMySqlHelperServer());

        if (!probe.HardwareIdentityReadable)
        {
            Assert.Inconclusive("This machine reports no hardware address, so the registry read cannot be exercised.");
        }

        var store = new StubMySqlHelperServer([RegistryRow(probe.Hostname, probe.MacAddress)]);
        var step = new ReadComputerRecordStep(new LaunchStepCatalog(), new MachineFactsService(store));

        // Act
        var outcome = await step.RunAsync(Context(new LaunchActivityFeed()), CancellationToken.None);

        // Assert
        Assert.AreEqual(LaunchStepStatus.Succeeded, outcome.Status);
        Assert.AreEqual(1, store.QueryCallCount);
    }

    [TestMethod]
    public async Task ReadComputerRecordStep_RunAsync_WhenTheStoreHoldsNoRow_ReportsWhatItFoundRatherThanFailing()
    {
        // Arrange
        var probe = new MachineFactsService(new StubMySqlHelperServer());

        if (!probe.HardwareIdentityReadable)
        {
            Assert.Inconclusive("This machine reports no hardware address, so the registry read cannot be exercised.");
        }

        var step = new ReadComputerRecordStep(new LaunchStepCatalog(), new MachineFactsService(new StubMySqlHelperServer()));

        // Act
        var outcome = await step.RunAsync(Context(new LaunchActivityFeed()), CancellationToken.None);

        // Assert: a machine the registry does not hold is set up against the store rather than stopped (FR-006).
        Assert.AreEqual(LaunchStepStatus.Succeeded, outcome.Status, "the read happened; the machine is simply unrecorded");
        StringAssert.Contains(outcome.Diagnosis!, "no record");
    }

    [TestMethod]
    public async Task ReadComputerRecordStep_RunAsync_WhenTheStoreThrows_ReportsFailedRatherThanThrowing()
    {
        // Arrange
        var probe = new MachineFactsService(new StubMySqlHelperServer());

        if (!probe.HardwareIdentityReadable)
        {
            Assert.Inconclusive("This machine reports no hardware address, so the registry read cannot be exercised.");
        }

        var step = new ReadComputerRecordStep(
            new LaunchStepCatalog(),
            new MachineFactsService(new StubMySqlHelperServer(throwOnQuery: true)));

        // Act
        var outcome = await step.RunAsync(Context(new LaunchActivityFeed()), CancellationToken.None);

        // Assert
        Assert.AreEqual(LaunchStepStatus.Failed, outcome.Status);
        StringAssert.Contains(outcome.Diagnosis!, "refused the connection");
        Assert.IsTrue(outcome.Remedies.CanRetry);
        Assert.IsFalse(outcome.Remedies.CanRestoreDefaults, "a store outage never offers a reset (FR-017)");
    }

    [TestMethod]
    public async Task MachineReadinessStep_RunAsync_WhenTheMachineIsConfigured_ReportsSucceededNamingIt()
    {
        // Arrange
        var step = new MachineReadinessStep(new LaunchStepCatalog(), new StubMachineConfigurationService(Configured()));

        // Act
        var outcome = await step.RunAsync(Context(new LaunchActivityFeed()), CancellationToken.None);

        // Assert
        Assert.AreEqual(LaunchStepStatus.Succeeded, outcome.Status);
        StringAssert.Contains(outcome.Diagnosis!, "Shop floor station");
    }

    [TestMethod]
    public async Task MachineReadinessStep_RunAsync_WhenTheMachineIsUnconfigured_ReportsSucceededNamingTheReason()
    {
        // Arrange
        var state = Unconfigured(MachineConfigurationReasons.NeverConfigured);
        var step = new MachineReadinessStep(new LaunchStepCatalog(), new StubMachineConfigurationService(state));

        // Act
        var outcome = await step.RunAsync(Context(new LaunchActivityFeed()), CancellationToken.None);

        // Assert: an unconfigured machine is a finding, not a failed read. Refusing to continue is the pipeline's
        // decision, taken from the state rather than from this outcome (FR-006).
        Assert.AreEqual(LaunchStepStatus.Succeeded, outcome.Status);
        StringAssert.Contains(outcome.Diagnosis!, "never been set up");
    }

    [TestMethod]
    public async Task MachineReadinessStep_RunAsync_WhenTheStoreCouldNotBeRead_ReportsFailedWithARetryAndNoReset()
    {
        // Arrange
        var state = Unconfigured(MachineConfigurationReasons.Unreadable);
        var step = new MachineReadinessStep(new LaunchStepCatalog(), new StubMachineConfigurationService(state));

        // Act
        var outcome = await step.RunAsync(Context(new LaunchActivityFeed()), CancellationToken.None);

        // Assert: a store that could not be read is not a machine to set up, because a read that failed may
        // succeed on the next check and setup would invite an answer that was never the problem (FR-009, FR-017).
        Assert.AreEqual(LaunchStepStatus.Failed, outcome.Status);
        Assert.IsTrue(outcome.Remedies.CanRetry);
        Assert.IsFalse(outcome.Remedies.CanRestoreDefaults, "a store outage never offers a reset (FR-017)");
    }

    [TestMethod]
    public async Task SaveMachineConfigurationStep_RunAsync_WhenADraftIsWaiting_SavesItOnceAndReportsItSaved()
    {
        // Arrange
        var configuration = new StubMachineConfigurationService(Unconfigured(MachineConfigurationReasons.NeverConfigured));
        var pending = new PendingMachineConfiguration();
        pending.Hold(Draft("Shop floor station"));
        var step = new SaveMachineConfigurationStep(new LaunchStepCatalog(), configuration, pending);

        // Act
        var outcome = await step.RunAsync(Context(new LaunchActivityFeed()), CancellationToken.None);

        // Assert: the step drives the one writer rather than writing the rows itself, and the draft it handed over
        // is no longer waiting.
        Assert.AreEqual(LaunchStepStatus.Succeeded, outcome.Status);
        Assert.AreEqual(1, configuration.SaveCallCount);
        Assert.AreEqual("Shop floor station", configuration.LastDraft!.DisplayName);
        Assert.IsNull(pending.Pending);
        StringAssert.Contains(outcome.Diagnosis!, "Shop floor station");
    }

    [TestMethod]
    public async Task SaveMachineConfigurationStep_RunAsync_WhenTheMachineIsAlreadyConfigured_SavesNothing()
    {
        // Arrange
        var configuration = new StubMachineConfigurationService(Configured());
        var pending = new PendingMachineConfiguration();
        pending.Hold(Draft("A name nobody asked for"));
        var step = new SaveMachineConfigurationStep(new LaunchStepCatalog(), configuration, pending);

        // Act
        var outcome = await step.RunAsync(Context(new LaunchActivityFeed()), CancellationToken.None);

        // Assert: a machine that already holds its configuration is not written again, and the waiting draft is
        // left alone rather than consumed or silently discarded.
        Assert.AreEqual(LaunchStepStatus.Skipped, outcome.Status);
        Assert.AreEqual(0, configuration.SaveCallCount, "the machine is already named, so the launch must not write it again");
        Assert.IsNotNull(pending.Pending);
    }

    [TestMethod]
    public async Task SaveMachineConfigurationStep_RunAsync_WhenNoDraftIsWaiting_SavesNothing()
    {
        // Arrange
        var configuration = new StubMachineConfigurationService(Unconfigured(MachineConfigurationReasons.NeverConfigured));
        var step = new SaveMachineConfigurationStep(new LaunchStepCatalog(), configuration, new PendingMachineConfiguration());

        // Act
        var outcome = await step.RunAsync(Context(new LaunchActivityFeed()), CancellationToken.None);

        // Assert: there is nothing to write, which is reported as work left undone rather than as a save.
        Assert.AreEqual(LaunchStepStatus.Skipped, outcome.Status);
        Assert.AreEqual(0, configuration.SaveCallCount);
    }

    [TestMethod]
    public async Task SaveMachineConfigurationStep_RunAsync_WhenTheStoreRefusesTheName_ReportsFailedAndKeepsTheDraft()
    {
        // Arrange: a display name another machine holds is refused by the service rather than raised, so the
        // refusal has to be turned into something the person can act on (FR-004).
        var refused = new MachineConfigurationSaveResult(false, MachineConfigurationRefusals.DisplayNameInUse, null);
        var configuration = new StubMachineConfigurationService(Unconfigured(MachineConfigurationReasons.NeverConfigured), refused);
        var pending = new PendingMachineConfiguration();
        pending.Hold(Draft("A name already in use"));
        var step = new SaveMachineConfigurationStep(new LaunchStepCatalog(), configuration, pending);

        // Act
        var outcome = await step.RunAsync(Context(new LaunchActivityFeed()), CancellationToken.None);

        // Assert
        Assert.AreEqual(LaunchStepStatus.Failed, outcome.Status);
        StringAssert.Contains(outcome.Diagnosis!, "another computer already holds that name");
        Assert.IsNotNull(pending.Pending, "the refused draft stays waiting, so the corrected save lands on it");
        Assert.IsTrue(outcome.Remedies.CanRetry);
    }

    [TestMethod]
    public async Task SaveMachineConfigurationStep_RunAsync_WhenTheStoreThrows_ReportsFailedRatherThanThrowing()
    {
        // Arrange
        var configuration = new StubMachineConfigurationService(
            Unconfigured(MachineConfigurationReasons.NeverConfigured),
            saveFailure: new InvalidOperationException("the store did not answer"));
        var pending = new PendingMachineConfiguration();
        pending.Hold(Draft("Shop floor station"));
        var step = new SaveMachineConfigurationStep(new LaunchStepCatalog(), configuration, pending);

        // Act
        var outcome = await step.RunAsync(Context(new LaunchActivityFeed()), CancellationToken.None);

        // Assert
        Assert.AreEqual(LaunchStepStatus.Failed, outcome.Status);
        StringAssert.Contains(outcome.Diagnosis!, "the store did not answer");
        Assert.IsNotNull(pending.Pending, "a draft that was never written stays waiting");
    }

    [TestMethod]
    public async Task ReadRememberedSignInStep_RunAsync_ReportsTheWorkLeftUndoneRatherThanAReadItDidNotMake()
    {
        // Arrange: the read against the store and the unlock through the shared key file belong to the
        // remembered-sign-in service, so until that service exists the honest report is that nothing was read.
        var step = new ReadRememberedSignInStep(new LaunchStepCatalog());

        // Act
        var outcome = await step.RunAsync(Context(new LaunchActivityFeed()), CancellationToken.None);

        // Assert: a step that read nothing must not report success, and it must never stop the launch, because an
        // unreadable remembered sign-in falls back to the ordinary form (FR-014).
        Assert.AreEqual(LaunchStepStatus.Skipped, outcome.Status);
        StringAssert.Contains(outcome.Diagnosis!, "sign-in form will ask");
    }

    /// <summary>The seven pre-sign-in steps, each over a dependency that answers.</summary>
    private static ILaunchStep[] AllSteps(LaunchStepCatalog catalog) =>
    [
        new ConfigurationStep(
            catalog,
            Options.Create(new WaitlistDatabaseOptions { ConnectionString = "Server=localhost;Database=mtm_waitlist;" })),
        new StoreReachabilityStep(catalog, new StubMySqlHelperServer()),
        new ReadHardwareIdentityStep(catalog),
        new ReadComputerRecordStep(catalog, new MachineFactsService(new StubMySqlHelperServer())),
        new MachineReadinessStep(catalog, new StubMachineConfigurationService(Configured())),
        new SaveMachineConfigurationStep(catalog, new StubMachineConfigurationService(Configured()), new PendingMachineConfiguration()),
        new ReadRememberedSignInStep(catalog),
    ];

    /// <summary>What a step is handed: no person before sign-in, this machine's facts, and the feed.</summary>
    private static LaunchStepContext Context(ILaunchActivityFeed feed)
        => Context(feed, new MachineFactsService(new StubMySqlHelperServer()));

    /// <summary>The same, over a machine a case states rather than over the one the suite happens to run on.</summary>
    private static LaunchStepContext Context(ILaunchActivityFeed feed, IMachineFacts machine)
        => new(null, machine, feed);

    /// <summary>A machine that holds its name and its note.</summary>
    private static MachineConfigurationState Configured() => new(
        true,
        "Shop floor station",
        "a machine the test owns",
        null);

    /// <summary>A machine that is not configured, carrying the reason it is not.</summary>
    private static MachineConfigurationState Unconfigured(string reason) => new(false, null, null, reason);

    /// <summary>What the setup surface captured, ready to be written.</summary>
    private static MachineConfigurationDraft Draft(string displayName) => new(
        displayName,
        "a machine the test owns");

    /// <summary>One registry row as the lookup returns it, in the column names the mapping reads.</summary>
    private static Dictionary<string, object?> RegistryRow(string computerName, string? macAddress) => new()
    {
        ["id"] = 7L,
        ["computer_name"] = computerName,
        ["display_name"] = "Shop floor station",
        ["description"] = "a row the test owns",
        ["mac_address_normalized"] = macAddress,
        ["is_registered"] = 1L,
    };

    /// <summary>Saved settings that cannot be read at all, which is the one way this read fails.</summary>
    private sealed class UnreadableSavedSettings : IOptions<WaitlistDatabaseOptions>
    {
        public WaitlistDatabaseOptions Value
            => throw new InvalidOperationException("the saved settings could not be read");
    }

    /// <summary>
    /// Records what it was asked for and answers with the rows it was built with, so the store seam can be read
    /// back without a database.
    /// </summary>
    private sealed class StubMySqlHelperServer : IMySqlHelperServer
    {
        private readonly IReadOnlyList<Dictionary<string, object?>> _rows;
        private readonly bool _throwOnQuery;

        public StubMySqlHelperServer(
            IReadOnlyList<Dictionary<string, object?>>? rows = null,
            bool throwOnQuery = false)
        {
            _rows = rows ?? [];
            _throwOnQuery = throwOnQuery;
        }

        public int QueryCallCount { get; private set; }

        public string? LastStoredProcedureName { get; private set; }

        public MySqlDatabaseTarget LastDatabaseTarget { get; private set; }

        public IReadOnlyDictionary<string, object?> LastParameters { get; private set; } = new Dictionary<string, object?>();

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
        {
            QueryCallCount++;
            LastStoredProcedureName = storedProcedureName;
            LastDatabaseTarget = databaseTarget;
            LastParameters = parameters;

            return _throwOnQuery
                ? Task.FromException<IReadOnlyList<Dictionary<string, object?>>>(
                    new InvalidOperationException("the store refused the connection"))
                : Task.FromResult(_rows);
        }

        public Task<int> ExecuteStoredProcedureNonQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException("The launch steps only read from the store.");

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteSqlQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException("No statement text is written in the launch steps (constitution III).");

        public Task<int> ExecuteSqlNonQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException("No statement text is written in the launch steps (constitution III).");
    }

    /// <summary>
    /// This machine's configuration, answered from what the test built it with, recording the save it was asked
    /// to perform.
    /// </summary>
    private sealed class StubMachineConfigurationService : IMachineConfigurationService
    {
        private readonly MachineConfigurationState _state;
        private readonly MachineConfigurationSaveResult? _saveResult;
        private readonly Exception? _saveFailure;

        public StubMachineConfigurationService(
            MachineConfigurationState state,
            MachineConfigurationSaveResult? saveResult = null,
            Exception? saveFailure = null)
        {
            _state = state;
            _saveResult = saveResult;
            _saveFailure = saveFailure;
        }

        public int SaveCallCount { get; private set; }

        public MachineConfigurationDraft? LastDraft { get; private set; }

        public Task<MachineConfigurationState> GetStateAsync(CancellationToken cancellationToken)
            => Task.FromResult(_state);

        public Task<MachineConfigurationSaveResult> SaveAsync(
            MachineConfigurationDraft draft,
            CancellationToken cancellationToken)
        {
            SaveCallCount++;
            LastDraft = draft;

            return _saveFailure is not null
                ? Task.FromException<MachineConfigurationSaveResult>(_saveFailure)
                : Task.FromResult(_saveResult ?? new MachineConfigurationSaveResult(true, null, 7L));
        }

        public Task<MachineConfigurationResetResult> ResetToDefaultsAsync(
            IReadOnlyList<string> whatIsBroken,
            CancellationToken cancellationToken)
            => throw new NotSupportedException("The launch steps never reset this machine's configuration.");
    }
}
