using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Startup.Services;

namespace MTM_Waitlist.Tests.Module_Startup.Services;

/// <summary>
/// The machine configuration contract's own promises (<c>contracts/machine-configuration-contract.md</c>
/// sections 1 and 4; FR-006, FR-009, FR-018): the four ways a machine can be unconfigured are told apart rather
/// than collapsed into one, a display name another machine holds is refused before anything is written, and a
/// reset restores only the parts it was told are broken, on this machine and no other.
/// </summary>
/// <remarks>
/// <para>
/// What is pinned here is the seam rather than the store: which procedure is called, against which database,
/// with which values, and what each answer means. The store's own behaviour — the unique key on the display
/// name, the foreign keys a reset must not cross — is the tables' and is asserted where it lives. Where pictures
/// come from is not this machine's to hold, so no per-computer folder is read or written here (FR-040).
/// </para>
/// <para>
/// The machine's identity is stubbed rather than read from the host, so every case here runs the same way on a
/// build agent as on a workstation: the service asks <see cref="IMachineFacts"/> for the name it looks itself up
/// by, and a test can say what that name is.
/// </para>
/// </remarks>
[TestClass]
public sealed class MachineConfigurationServiceTests
{
    private const string LookupProcedure = "sp_core_computers_registry_lookup_by_name_get";
    private const string SourcesProcedure = "sp_config_images_locations_computer_sources_all_get";
    private const string HolderProcedure = "sp_core_computers_registry_display_name_get";
    private const string RegisterProcedure = "sp_core_computers_registry_upsert";
    private const string UpdateProcedure = "sp_core_computers_registry_update";
    private const string WriteSourcesProcedure = "sp_config_images_locations_computer_sources_set";
    private const string ResetProcedure = "sp_machine_configuration_reset";

    private const string Hostname = "MTMFG-161";
    private const string MacAddress = "aa-bb-cc-dd-ee-ff";
    private const long ComputerId = 4L;

    // ---------------------------------------------------------------- the four unconfigured reasons

    [TestMethod]
    public async Task GetStateAsync_WhenTheStoreHoldsNoRegistryRow_ReportsNeverConfigured()
    {
        var stub = new StubMySqlHelperServer();

        var state = await CreateService(stub).GetStateAsync(CancellationToken.None);

        Assert.IsFalse(state.IsConfigured);
        Assert.AreEqual(MachineConfigurationReasons.NeverConfigured, state.UnconfiguredReason);
        Assert.IsNull(state.DisplayName);

        // The machine is resolved by the name it presents, against the operational store, and no per-computer
        // folder is read: where pictures come from is held once for the plant (FR-040).
        Assert.AreEqual(1, stub.CallCount(LookupProcedure));
        Assert.AreEqual(MySqlDatabaseTarget.MtmWaitlist, stub.Last(LookupProcedure).Target);
        Assert.AreEqual(Hostname, stub.Last(LookupProcedure).Parameters["p_name"]);
        Assert.AreEqual(0, stub.CallCount(SourcesProcedure));
    }

    [TestMethod]
    public async Task GetStateAsync_WhenTheStoreHoldsTheRowAndTheName_IsConfigured()
    {
        // A machine is configured by its record and its name, so nothing else has to be captured before it can
        // be used: where its pictures come from is held once for the plant (FR-040, FR-041).
        var stub = new StubMySqlHelperServer().Returns(LookupProcedure, RegistryRow());

        var state = await CreateService(stub).GetStateAsync(CancellationToken.None);

        Assert.IsTrue(state.IsConfigured);
        Assert.IsNull(state.UnconfiguredReason);
        Assert.AreEqual("Shop floor station", state.DisplayName);
        Assert.AreEqual("a row the test owns", state.Description);
        Assert.AreEqual(
            0,
            stub.CallCount(SourcesProcedure),
            "no folder of this machine's own is read: folders are held once for the plant (FR-040)");
    }

    [TestMethod]
    public async Task GetStateAsync_WhenTheMachinesDisplayNameHasBeenCleared_ReportsConfigurationRemoved()
    {
        var stub = new StubMySqlHelperServer()
            .Returns(LookupProcedure, RegistryRow(displayName: "   "));

        var state = await CreateService(stub).GetStateAsync(CancellationToken.None);

        Assert.IsFalse(state.IsConfigured);
        Assert.AreEqual(MachineConfigurationReasons.Removed, state.UnconfiguredReason);

        // What the store holds is what is reported, and a name of nothing but whitespace is no name at all.
        Assert.IsNull(state.DisplayName);
    }

    [TestMethod]
    public async Task GetStateAsync_WhenTheMachineIsRetired_ReportsConfigurationRevoked()
    {
        // The row is retired, and revoked is decided by the registry row alone: nothing else the row holds is
        // consulted.
        var stub = new StubMySqlHelperServer()
            .Returns(LookupProcedure, RegistryRow(isRegistered: 0L));

        var state = await CreateService(stub).GetStateAsync(CancellationToken.None);

        Assert.IsFalse(state.IsConfigured);
        Assert.AreEqual(MachineConfigurationReasons.Revoked, state.UnconfiguredReason);
    }

    [TestMethod]
    public async Task GetStateAsync_WhenTheStoreCannotBeRead_ReportsConfigurationUnreadable()
    {
        var stub = new StubMySqlHelperServer().Fails(LookupProcedure, new InvalidOperationException("store is down"));

        // Losing configuration never degrades into running without it, and a read that could not be made is
        // reported as unconfigured rather than raised: an unreadable configuration is one of the four answers
        // (FR-004), not an exception for every caller to remember to catch (FR-009).
        var state = await CreateService(stub).GetStateAsync(CancellationToken.None);

        Assert.IsFalse(state.IsConfigured);
        Assert.AreEqual(MachineConfigurationReasons.Unreadable, state.UnconfiguredReason);
    }

    [TestMethod]
    public async Task GetStateAsync_RegisteredFlagSurfacedAsBoolean_CountsAsRegistered()
    {
        // The driver surfaces a TINYINT(1) column as a bool, and reading it as a number would throw rather than
        // answer, so the registered flag is pinned in that form too.
        var stub = new StubMySqlHelperServer()
            .Returns(LookupProcedure, RegistryRow(isRegistered: true));

        var state = await CreateService(stub).GetStateAsync(CancellationToken.None);

        Assert.IsTrue(state.IsConfigured);
    }

    // ---------------------------------------------------------------- the save

    [TestMethod]
    public async Task SaveAsync_WhenAnotherMachineHoldsTheDisplayName_IsRefusedAndWritesNothing()
    {
        var stub = new StubMySqlHelperServer()
            .Returns(LookupProcedure, RegistryRow())
            .Returns(HolderProcedure, RegistryRow(id: 9L, computerName: "MTMFG-999", displayName: "Shop floor station"));

        var result = await CreateService(stub).SaveAsync(Draft(), CancellationToken.None);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(MachineConfigurationRefusals.DisplayNameInUse, result.RefusalReason);
        Assert.IsNull(result.ComputerId);

        // The name is checked against the store rather than left to collide on the unique key, and the refusal
        // leaves the machine exactly as it was: nothing is written at all.
        Assert.AreEqual(1, stub.CallCount(HolderProcedure));
        Assert.AreEqual("Shop floor station", stub.Last(HolderProcedure).Parameters["p_display_name"]);
        Assert.AreEqual(0, stub.CallCount(RegisterProcedure));
        Assert.AreEqual(0, stub.CallCount(UpdateProcedure));
        Assert.AreEqual(0, stub.CallCount(WriteSourcesProcedure));
    }

    [TestMethod]
    public async Task SaveAsync_WhenTheDisplayNameIsThisMachinesOwn_WritesTheConfiguration()
    {
        var stub = new StubMySqlHelperServer()
            // One answer per read: the read that asks whether this machine exists, the read-back inside the
            // write, and the read-back that decides whether the save landed.
            .Returns(LookupProcedure, RegistryRow())
            .Queue(LookupProcedure, RegistryRow())
            .Queue(LookupProcedure, RegistryRow())
            .Returns(HolderProcedure, RegistryRow())
            .Affects(UpdateProcedure, 1);

        var result = await CreateService(stub).SaveAsync(Draft(), CancellationToken.None);

        Assert.IsTrue(result.Succeeded, $"refused with '{result.RefusalReason}'");
        Assert.IsNull(result.RefusalReason);
        Assert.AreEqual(ComputerId, result.ComputerId);

        // Rewriting the name the machine already holds is not a collision: the holder is the machine itself.
        var update = stub.Last(UpdateProcedure);
        Assert.AreEqual(ComputerId, update.Parameters["p_id"]);
        Assert.AreEqual(Hostname, update.Parameters["p_computer_name"]);
        Assert.AreEqual("Shop floor station", update.Parameters["p_display_name"]);

        // Completing setup registers the machine, including one that had been retired.
        Assert.AreEqual(1, update.Parameters["p_is_registered"]);

        // The folders are no longer written per machine: they are held once for the plant (FR-040).
        Assert.AreEqual(0, stub.CallCount(WriteSourcesProcedure));
    }

    [TestMethod]
    public async Task SaveAsync_WhenTheMachineHasNoRowYet_RegistersIt()
    {
        var draftName = "Brand new station";
        var stub = new StubMySqlHelperServer()
            // The first read is the "does this machine exist yet" read; the second is the read-back inside the
            // register, which is needed because an upsert reports affected rows and returns no row to take an id
            // from; the third is the read-back that decides whether the save landed.
            .Returns(LookupProcedure, [])
            .Returns(HolderProcedure, [])
            .Affects(RegisterProcedure, 1);

        var service = CreateService(stub);

        // The row appears once it has been registered, which is what the later reads find.
        stub.Queue(LookupProcedure, RegistryRow(displayName: draftName));
        stub.Queue(LookupProcedure, RegistryRow(displayName: draftName));

        var result = await service.SaveAsync(Draft(draftName), CancellationToken.None);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(ComputerId, result.ComputerId);

        var register = stub.Last(RegisterProcedure);
        Assert.AreEqual(Hostname, register.Parameters["p_computer_name"]);
        Assert.AreEqual(Hostname, register.Parameters["p_hostname_normalized"]);
        Assert.AreEqual(MacAddress, register.Parameters["p_mac_address_normalized"]);
        Assert.AreEqual(draftName, register.Parameters["p_display_name"]);

        Assert.AreEqual(0, stub.CallCount(UpdateProcedure));
        Assert.AreEqual(0, stub.CallCount(WriteSourcesProcedure));
    }

    [TestMethod]
    public async Task SaveAsync_WhenTheDisplayNameIsBlank_IsRefusedBeforeAnyStoreCall()
    {
        var stub = new StubMySqlHelperServer();

        var result = await CreateService(stub).SaveAsync(Draft("   "), CancellationToken.None);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(MachineConfigurationRefusals.DisplayNameRequired, result.RefusalReason);
        Assert.AreEqual(0, stub.TotalCallCount);
    }

    [TestMethod]
    public async Task SaveAsync_WhenTheRowDoesNotReadBackAsSaved_ReportsTheFailureWithTheMachine()
    {
        // The store reports zero changed rows for a write that sets a column to the value it already holds, so
        // success is read back rather than counted. A row that does not come back holding the name that was
        // asked for is reported as a failed save, and the machine is still named so the caller can say which.
        var stub = new StubMySqlHelperServer()
            .Returns(LookupProcedure, RegistryRow())
            .Queue(LookupProcedure, RegistryRow(displayName: "Something else entirely"))
            .Queue(LookupProcedure, RegistryRow(displayName: "Something else entirely"))
            .Returns(HolderProcedure, RegistryRow())
            .Affects(UpdateProcedure, 1);

        var result = await CreateService(stub).SaveAsync(Draft(), CancellationToken.None);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(MachineConfigurationRefusals.ConfigurationNotWritten, result.RefusalReason);
        Assert.AreEqual(ComputerId, result.ComputerId);
    }

    [TestMethod]
    public async Task SaveAsync_WhenTheRowWasAlreadyAsAskedFor_IsStillReportedAsSaved()
    {
        // The case that used to be refused wrongly: the write changes nothing because the machine already holds
        // exactly what was asked for, so the store reports no changed rows. Reading the row back is what tells
        // this apart from a write that did not happen.
        var stub = new StubMySqlHelperServer()
            .Returns(LookupProcedure, RegistryRow())
            .Queue(LookupProcedure, RegistryRow())
            .Queue(LookupProcedure, RegistryRow())
            .Returns(HolderProcedure, RegistryRow())
            .Affects(UpdateProcedure, 0);

        var result = await CreateService(stub).SaveAsync(Draft(), CancellationToken.None);

        Assert.IsTrue(result.Succeeded, $"refused with '{result.RefusalReason}'");
        Assert.IsNull(result.RefusalReason);
        Assert.AreEqual(ComputerId, result.ComputerId);
    }

    // ---------------------------------------------------------------- the reset

    [TestMethod]
    public async Task ResetToDefaultsAsync_WithNoPartsNamed_ReadsNothingAndResetsNothing()
    {
        var stub = new StubMySqlHelperServer();

        var result = await CreateService(stub).ResetToDefaultsAsync([], CancellationToken.None);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(0, result.Reset.Count);
        Assert.IsNull(result.FailureReason);

        // A reset restores what it is told is broken. Told nothing, it does nothing at all, not even a read.
        Assert.AreEqual(0, stub.TotalCallCount);
    }

    [TestMethod]
    public async Task ResetToDefaultsAsync_WithAnUnknownPart_ResetsNothing()
    {
        var stub = new StubMySqlHelperServer();

        var result = await CreateService(stub).ResetToDefaultsAsync(["something_else"], CancellationToken.None);

        // An unknown word is not guessed at, and it is not a part of this machine's configuration.
        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(0, result.Reset.Count);
        Assert.AreEqual(0, stub.TotalCallCount);
    }

    [TestMethod]
    public async Task ResetToDefaultsAsync_ResetsOnlyThePartNamedAndOnlyThisMachine()
    {
        var stub = new StubMySqlHelperServer().Returns(LookupProcedure, RegistryRow());

        var result = await CreateService(stub).ResetToDefaultsAsync(
            [MachineConfigurationParts.Description],
            CancellationToken.None);

        Assert.IsTrue(result.Succeeded);
        CollectionAssert.AreEqual(new[] { MachineConfigurationParts.Description }, result.Reset.ToArray());

        // The identifier is this machine's alone, and every flag the caller did not ask for is off, so the
        // reset cannot reach another machine's configuration or a part nobody said was broken.
        var reset = stub.Last(ResetProcedure);
        Assert.AreEqual(ComputerId, reset.Parameters["p_computer_id"]);
        Assert.AreEqual(0, reset.Parameters["p_reset_display_name"]);
        Assert.AreEqual(1, reset.Parameters["p_reset_description"]);
        Assert.AreEqual(0, reset.Parameters["p_reset_picture_sources"]);
        Assert.AreEqual(0, reset.Parameters["p_reset_scoped_preferences"]);
        Assert.AreEqual(MySqlDatabaseTarget.MtmWaitlist, reset.Target);

        // The reset is the only write: nothing else in the service writes a configuration row.
        Assert.AreEqual(0, stub.CallCount(RegisterProcedure));
        Assert.AreEqual(0, stub.CallCount(UpdateProcedure));
        Assert.AreEqual(0, stub.CallCount(WriteSourcesProcedure));
    }

    [TestMethod]
    public async Task ResetToDefaultsAsync_WithTheConfigurationPart_NamesBothPartsBacked()
    {
        var stub = new StubMySqlHelperServer().Returns(LookupProcedure, RegistryRow());

        var result = await CreateService(stub).ResetToDefaultsAsync(
            [MachineConfigurationParts.Configuration],
            CancellationToken.None);

        // The shorthand names the two, and the result says what was restored rather than echoing the word the
        // caller used, so the screen can report exactly what happened.
        CollectionAssert.AreEqual(
            new[]
            {
                MachineConfigurationParts.DisplayName,
                MachineConfigurationParts.Description,
            },
            result.Reset.ToArray());

        var reset = stub.Last(ResetProcedure);
        Assert.AreEqual(1, reset.Parameters["p_reset_display_name"]);
        Assert.AreEqual(1, reset.Parameters["p_reset_description"]);
        Assert.AreEqual(0, reset.Parameters["p_reset_scoped_preferences"]);
        Assert.AreEqual(
            0,
            reset.Parameters["p_reset_picture_sources"],
            "the flag is permanently zero: a machine has no picture source of its own to restore (FR-040)");
    }

    [TestMethod]
    public async Task ResetToDefaultsAsync_WhenTheDefaultDisplayNameIsAnotherMachines_IsRefusedAndWritesNothing()
    {
        var stub = new StubMySqlHelperServer()
            .Returns(LookupProcedure, RegistryRow())
            .Returns(HolderProcedure, RegistryRow(id: 9L, computerName: Hostname, displayName: "MTMFG-161"));

        var result = await CreateService(stub).ResetToDefaultsAsync(
            [MachineConfigurationParts.DisplayName],
            CancellationToken.None);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(MachineConfigurationRefusals.DefaultDisplayNameInUse, result.FailureReason);
        Assert.AreEqual(0, result.Reset.Count);

        // The default name is the machine's own name, and the store makes a display name unique, so a taken
        // default is refused cleanly instead of failing on the key after other parts had already been restored.
        Assert.AreEqual(Hostname, stub.Last(HolderProcedure).Parameters["p_display_name"]);
        Assert.AreEqual(0, stub.CallCount(ResetProcedure));
    }

    [TestMethod]
    public async Task ResetToDefaultsAsync_WhenTheMachineHasNoRow_WritesNothingAndSucceeds()
    {
        var stub = new StubMySqlHelperServer().Returns(LookupProcedure, []);

        var result = await CreateService(stub).ResetToDefaultsAsync(
            [MachineConfigurationParts.Configuration],
            CancellationToken.None);

        // There is no configuration to restore and no identifier to address it by. The machine is already in
        // the state the reset would put it in, which is not a failure.
        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(0, result.Reset.Count);
        Assert.AreEqual(0, stub.CallCount(ResetProcedure));
    }

    [TestMethod]
    public async Task ResetToDefaultsAsync_ForTheScopedPreference_RestoresOnlyThatPart()
    {
        var stub = new StubMySqlHelperServer().Returns(LookupProcedure, RegistryRow());

        var result = await CreateService(stub).ResetToDefaultsAsync(
            [MachineConfigurationParts.ScopedPreference],
            CancellationToken.None);

        Assert.IsTrue(result.Succeeded);
        CollectionAssert.AreEqual(new[] { MachineConfigurationParts.ScopedPreference }, result.Reset.ToArray());

        // The machine's own configuration rows are left alone: its scoped preference is a different part, and
        // the person asked for that one.
        var reset = stub.Last(ResetProcedure);
        Assert.AreEqual(0, reset.Parameters["p_reset_display_name"]);
        Assert.AreEqual(0, reset.Parameters["p_reset_description"]);
        Assert.AreEqual(0, reset.Parameters["p_reset_picture_sources"]);
        Assert.AreEqual(1, reset.Parameters["p_reset_scoped_preferences"]);
        Assert.AreEqual(ComputerId, reset.Parameters["p_computer_id"]);

        // Nothing was asked about a display name, so the name holder was never consulted.
        Assert.AreEqual(0, stub.CallCount(HolderProcedure));
    }

    // ---------------------------------------------------------------- fixtures

    private static MachineConfigurationService CreateService(StubMySqlHelperServer stub) =>
        new(stub, new StubMachineFacts());

    private static MachineConfigurationDraft Draft(string displayName = "Shop floor station") => new(
        displayName,
        "a description the test owns");

    /// <summary>One registry row in the column names the mapping reads.</summary>
    private static Dictionary<string, object?> RegistryRow(
        long id = ComputerId,
        string computerName = Hostname,
        string displayName = "Shop floor station",
        string? description = "a row the test owns",
        string macAddress = MacAddress,
        object? isRegistered = null) => new()
    {
        ["id"] = id,
        ["computer_name"] = computerName,
        ["display_name"] = displayName,
        ["description"] = description,
        ["mac_address_normalized"] = macAddress,
        ["is_registered"] = isRegistered ?? 1L,
    };

    /// <summary>
    /// Records what it was asked for and answers with the rows it was built with, so the seam can be read back
    /// without a database. A procedure with no configured answer returns nothing; a write with no configured
    /// count reports one affected row.
    /// </summary>
    private sealed class StubMySqlHelperServer : IMySqlHelperServer
    {
        private readonly List<Call> _calls = [];
        private readonly Dictionary<string, Queue<IReadOnlyList<Dictionary<string, object?>>>> _rows = new(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _affected = new(StringComparer.Ordinal);
        private readonly Dictionary<string, Exception> _failures = new(StringComparer.Ordinal);

        public int TotalCallCount => _calls.Count;

        public StubMySqlHelperServer Returns(string procedure, params Dictionary<string, object?>[] rows)
        {
            Queue(procedure, rows);
            return this;
        }

        /// <summary>Queues another answer, for a procedure that is read more than once in one call.</summary>
        public StubMySqlHelperServer Queue(string procedure, params Dictionary<string, object?>[] rows)
        {
            if (!_rows.TryGetValue(procedure, out var queue))
            {
                queue = new Queue<IReadOnlyList<Dictionary<string, object?>>>();
                _rows[procedure] = queue;
            }

            queue.Enqueue(rows);
            return this;
        }

        public StubMySqlHelperServer Affects(string procedure, int affected)
        {
            _affected[procedure] = affected;
            return this;
        }

        public StubMySqlHelperServer Fails(string procedure, Exception exception)
        {
            _failures[procedure] = exception;
            return this;
        }

        public Call Last(string procedure) => _calls.Last(call => call.Procedure == procedure);

        public int CallCount(string procedure) => _calls.Count(call => call.Procedure == procedure);

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
        {
            _calls.Add(new Call(storedProcedureName, parameters, databaseTarget));

            if (_failures.TryGetValue(storedProcedureName, out var failure))
            {
                return Task.FromException<IReadOnlyList<Dictionary<string, object?>>>(failure);
            }

            IReadOnlyList<Dictionary<string, object?>> rows =
                _rows.TryGetValue(storedProcedureName, out var queue) && queue.Count > 0
                    ? queue.Dequeue()
                    : [];

            return Task.FromResult(rows);
        }

        public Task<int> ExecuteStoredProcedureNonQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
        {
            _calls.Add(new Call(storedProcedureName, parameters, databaseTarget));

            if (_failures.TryGetValue(storedProcedureName, out var failure))
            {
                return Task.FromException<int>(failure);
            }

            return Task.FromResult(_affected.GetValueOrDefault(storedProcedureName, 1));
        }

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteSqlQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("No statement text is expected from this service (constitution III).");

        public Task<int> ExecuteSqlNonQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("No statement text is expected from this service (constitution III).");

        public sealed record Call(
            string Procedure,
            IReadOnlyDictionary<string, object?> Parameters,
            MySqlDatabaseTarget Target);
    }

    /// <summary>This machine's identity, said by the test rather than read from the host.</summary>
    private sealed class StubMachineFacts : IMachineFacts
    {
        public string Hostname => MachineConfigurationServiceTests.Hostname;

        public string? MacAddress => MachineConfigurationServiceTests.MacAddress;

        public ComputerRecord? RegisteredComputer => null;

        public bool IsRegistered => false;

        public bool HardwareIdentityReadable => true;
    }
}
