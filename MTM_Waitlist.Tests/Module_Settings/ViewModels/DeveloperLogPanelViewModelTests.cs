using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Permissions;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Logging;
using MTM_Waitlist.Module_Settings.Services.DependencyInjection;
using MTM_Waitlist.Module_Settings.ViewModels;

namespace MTM_Waitlist.Tests.Module_Settings.ViewModels;

/// <summary>
/// The developer log panel: filtering by machine and by error kind together lists only matching entries, filtering
/// by severity and machine together does the same, every read is bounded by a time window and a page size, the
/// gate is enforced where the read and the copy happen, and both copies hand the formatter's text to the
/// clipboard (US5 acceptance scenario 2, SC-005, SC-014, FR-038, `contracts/logging-contract.md` §7).
/// </summary>
/// <remarks>
/// The store is scripted, so what is proved is the panel's own behaviour: what it asks the store for, what it
/// lists back, what it refuses and what it hands to the clipboard. Whether the procedures honour those
/// parameters is the store's half and is proved against a live database.
/// </remarks>
[TestClass]
public sealed class DeveloperLogPanelViewModelTests
{
    [TestMethod]
    public async Task LoadAsync_WhenMachineAndErrorKindAreChosen_AsksForBothAndListsWhatCameBack()
    {
        // Arrange
        var store = new RecordingLogReader();
        store.Rows.Add(EntryRow(message: "the store did not answer", hostId: "MTMFG-161"));

        var viewModel = Build(store);
        viewModel.HostId = "MTMFG-161";
        viewModel.ErrorType = "System.InvalidOperationException";

        // Act
        await viewModel.LoadAsync();

        // Assert: the two filters travel to the store, and the panel lists what the store returned.
        Assert.AreEqual(DeveloperLogPanelViewModel.FilterProcedureName, store.ProcedureNames.Single());
        Assert.AreEqual("MTMFG-161", store.Parameters["p_host_id"]);
        Assert.AreEqual("System.InvalidOperationException", store.Parameters["p_error_type"]);
        Assert.AreEqual(1, viewModel.Entries.Count, "the entry the store returned was not listed");
        Assert.AreEqual("the store did not answer", viewModel.Entries[0].Message);
    }

    [TestMethod]
    public async Task LoadAsync_WhenSeverityAndMachineAreChosen_AsksForBoth()
    {
        // Arrange
        var store = new RecordingLogReader();
        store.Rows.Add(EntryRow(level: "error", hostId: "MTMFG-161"));

        var viewModel = Build(store);
        viewModel.SelectedSeverity = "error";
        viewModel.HostId = "MTMFG-161";

        // Act
        await viewModel.LoadAsync();

        // Assert
        Assert.AreEqual("error", store.Parameters["p_level"]);
        Assert.AreEqual("MTMFG-161", store.Parameters["p_host_id"]);
        Assert.AreEqual(1, viewModel.Entries.Count);
    }

    [TestMethod]
    public async Task LoadAsync_AlwaysBoundsTheReadByATimeWindowAndAPageSize()
    {
        // Arrange: a reader has asked for far more than the panel or the store will return.
        var store = new RecordingLogReader();
        var viewModel = Build(store);
        viewModel.WindowDays = DeveloperLogPanelViewModel.MaximumWindowDays * 10;
        viewModel.PageSize = DeveloperLogPanelViewModel.MaximumPageSize * 100;

        // Act
        await viewModel.LoadAsync();

        // Assert: an unbounded read of a store that only grows is the freeze the ceiling exists to prevent
        // (SC-014, §7).
        var from = (DateTime)store.Parameters["p_from_utc"]!;
        var to = (DateTime)store.Parameters["p_to_utc"]!;

        Assert.IsTrue(to > from, "the read carried no usable window");
        Assert.AreEqual(
            DeveloperLogPanelViewModel.MaximumWindowDays,
            (int)Math.Round((to - from).TotalDays),
            "the window was not clamped to the panel's own ceiling");
        Assert.AreEqual(
            DeveloperLogPanelViewModel.MaximumPageSize,
            store.Parameters["p_page_size"],
            "the page size was not clamped to the store's own ceiling");
    }

    [TestMethod]
    public async Task LoadAsync_KeepsTheNewestFirstOrderTheStoreReturned()
    {
        // Arrange
        var store = new RecordingLogReader();
        store.Rows.Add(EntryRow(id: 3, message: "newest"));
        store.Rows.Add(EntryRow(id: 2, message: "middle"));
        store.Rows.Add(EntryRow(id: 1, message: "oldest"));

        var viewModel = Build(store);

        // Act
        await viewModel.LoadAsync();

        // Assert: the order is the procedure's, and the panel neither reorders nor re-sorts it.
        CollectionAssert.AreEqual(
            new[] { "newest", "middle", "oldest" },
            viewModel.Entries.Select(entry => entry.Message).ToArray());
    }

    [TestMethod]
    public async Task LoadAsync_WhenThePanelIsNotPermitted_ReadsNothingAtAllAndSaysSo()
    {
        // Arrange: the gate is answered before anything is read, which is what makes it a gate rather than a
        // hidden control (plan D11).
        var store = new RecordingLogReader();
        var viewModel = Build(store, isPermitted: false);

        // Act
        await viewModel.LoadAsync();

        // Assert
        Assert.AreEqual(0, store.ProcedureNames.Count, "the store was read behind a closed panel");
        Assert.AreEqual(0, viewModel.Entries.Count);
        Assert.AreEqual("Settings_LogPanel_NotPermitted.Text".GetLocalized(), viewModel.MessageText);
    }

    [TestMethod]
    public async Task LoadPermissionAsync_AsksTheStoreForTheOneKeyThePanelIsGatedOn()
    {
        // Arrange
        var permissions = new RecordingPermissionStub();
        var viewModel = new DeveloperLogPanelViewModel(new RecordingLogReader(), permissions, _ => true);

        // Act
        await viewModel.LoadPermissionAsync();

        // Assert: reading the panel is one permission and it is the declared one (PermissionKeys.SettingsLogPanel).
        CollectionAssert.AreEqual(new[] { PermissionKeys.SettingsLogPanel }, permissions.KeysAsked.ToArray());
        Assert.IsTrue(viewModel.IsPermitted);
    }

    [TestMethod]
    public async Task GroupByFingerprint_ReadsTheGroupedViewRatherThanTheFlatOne()
    {
        // Arrange: one fault raised eleven times is one signal with a count, not eleven rows (SC-014).
        var store = new RecordingLogReader();
        store.Rows.Add(GroupRow(occurrences: 11, errorType: "System.TimeoutException", module: "StoreReachability"));

        var viewModel = Build(store);
        viewModel.GroupByFingerprint = true;

        // Act
        await viewModel.LoadAsync();

        // Assert
        Assert.AreEqual(DeveloperLogPanelViewModel.GroupProcedureName, store.ProcedureNames.Single());
        Assert.IsTrue(viewModel.IsGrouped);
        Assert.AreEqual(1, viewModel.Groups.Count);
        StringAssert.Contains(viewModel.Groups[0], "11");
        StringAssert.Contains(viewModel.Groups[0], "System.TimeoutException");
    }

    [TestMethod]
    public async Task LoadAsync_WhenTheStoreFails_ShowsTheUnavailableStateRatherThanAnEmptyAnswer()
    {
        // Arrange: a store that refuses the read is a stated cause, never a silently empty list (FR-021, FR-001).
        var store = new RecordingLogReader { Failure = new InvalidOperationException("the store did not answer") };
        var viewModel = Build(store);
        store.Rows.Add(EntryRow());

        // Act
        await viewModel.LoadAsync();

        // Assert
        Assert.IsTrue(viewModel.IsStoreUnavailable);
        Assert.AreEqual("Settings_LogPanel_Unavailable.Text".GetLocalized(), viewModel.MessageText);
        Assert.AreEqual(0, viewModel.Entries.Count, "an unreadable store must not read as an empty answer");
    }

    [TestMethod]
    public void CopyEntry_HandsTheFormattersTextToTheClipboardAndSaysSo()
    {
        // Arrange
        string? handedOver = null;
        var viewModel = Build(new RecordingLogReader(), clipboard: text =>
        {
            handedOver = text;
            return true;
        });

        var entry = LogPanelEntry.FromRow(EntryRow(message: "the sign-in could not be confirmed"));

        // Act
        viewModel.CopyEntryCommand.Execute(entry);

        // Assert: one entry, its columns in the panel's order, and the filter it came from (FR-038, SC-017).
        Assert.IsNotNull(handedOver, "nothing reached the clipboard");
        StringAssert.Contains(handedOver!, "the sign-in could not be confirmed");
        StringAssert.Contains(handedOver!, "Chain link:");
        StringAssert.Contains(handedOver!, "Filter: ");
        Assert.AreEqual("Settings_LogPanel_Copied_Entry.Text".GetLocalized(), viewModel.MessageText);
    }

    [TestMethod]
    public async Task CopyList_HandsEveryListedEntryToTheClipboardAsOneCopy()
    {
        // Arrange
        string? handedOver = null;
        var store = new RecordingLogReader();
        store.Rows.Add(EntryRow(id: 2, message: "first listed"));
        store.Rows.Add(EntryRow(id: 1, message: "second listed"));

        var viewModel = Build(store, clipboard: text =>
        {
            handedOver = text;
            return true;
        });

        await viewModel.LoadAsync();

        // Act
        viewModel.CopyListCommand.Execute(null);

        // Assert: the entries currently listed, which the read has already bounded by the page size.
        Assert.IsNotNull(handedOver, "nothing reached the clipboard");
        StringAssert.Contains(handedOver!, "Entries: 2");
        StringAssert.Contains(handedOver!, "first listed");
        StringAssert.Contains(handedOver!, "second listed");
        Assert.AreEqual("Settings_LogPanel_Copied_List.Text".GetLocalized(), viewModel.MessageText);
    }

    [TestMethod]
    public void CopyEntry_WhenThePanelIsNotPermitted_HandsNothingToTheClipboard()
    {
        // Arrange
        var handedOver = 0;
        var viewModel = Build(
            new RecordingLogReader(),
            isPermitted: false,
            clipboard: _ =>
            {
                handedOver++;
                return true;
            });

        // Act
        viewModel.CopyEntryCommand.Execute(LogPanelEntry.FromRow(EntryRow()));

        // Assert: the gate is enforced where the copy happens, not only where the control is drawn (plan D11).
        Assert.AreEqual(0, handedOver, "a closed panel still copied an entry to the clipboard");
    }

    [TestMethod]
    public async Task LoadAsync_WhenTheStoreReturnsNothing_StatesItRatherThanShowingAnEmptyRegion()
    {
        // Arrange
        var viewModel = Build(new RecordingLogReader());

        // Act
        await viewModel.LoadAsync();

        // Assert
        Assert.AreEqual(0, viewModel.Entries.Count);
        Assert.IsTrue(viewModel.HasNothing);
        Assert.AreEqual("Settings_LogPanel_Nothing.Text".GetLocalized(), viewModel.MessageText);
    }

    [TestMethod]
    public void LoadSeverityOptions_OffersTheOneVocabularyTheStoreHolds()
    {
        // Arrange
        var viewModel = Build(new RecordingLogReader());

        // Act
        viewModel.LoadSeverityOptions();

        // Assert: the choices are the seam's own severity vocabulary, lower-cased as the store's level column
        // holds them, so a filter can never be spelled in a second vocabulary (contract §1).
        CollectionAssert.AreEquivalent(
            Enum.GetValues<LogSeverity>().Select(severity => severity.ToString().ToLowerInvariant()).ToArray(),
            viewModel.SeverityOptions.ToArray());
    }

    [TestMethod]
    public void ThePanel_IsRegisteredWithTheSettingsModule()
    {
        // Arrange: the settings module's own registration, plus the two seams the panel's factory needs.
        var services = new ServiceCollection();
        services.AddSettingsModuleServices(new ConfigurationBuilder().Build());
        services.AddSingleton<IMySqlHelperServer>(new RecordingLogReader());
        services.AddSingleton<IPermissionService>(new RecordingPermissionStub());

        using var provider = services.BuildServiceProvider();

        // Assert: the host can build the panel the view asks for by type, which is what makes it reachable.
        Assert.IsNotNull(
            provider.GetService<DeveloperLogPanelViewModel>(),
            "the developer log panel is not registered, so the view cannot be built");
    }

    private static DeveloperLogPanelViewModel Build(
        RecordingLogReader store,
        bool isPermitted = true,
        Func<string, bool>? clipboard = null)
    {
        var viewModel = new DeveloperLogPanelViewModel(
            store,
            new RecordingPermissionStub(),
            clipboard ?? (_ => true));

        viewModel.ApplyPermission(isPermitted);
        return viewModel;
    }

    /// <summary>One row as <c>sp_ops_startup_logs_filter</c> returns it.</summary>
    private static Dictionary<string, object?> EntryRow(
        long id = 1,
        string message = "the store did not answer",
        string level = "error",
        string hostId = "MTMFG-161") => new(StringComparer.Ordinal)
        {
            ["id"] = id,
            ["public_id"] = $"{id:D8}-1111-1111-1111-111111111111",
            ["correlation_id"] = "22222222-2222-2222-2222-222222222222",
            ["created_utc"] = new DateTime(2026, 9, 27, 9, 15, 0, DateTimeKind.Utc),
            ["level"] = level,
            ["event_action"] = "ConfirmSignIn",
            ["outcome"] = "Failure",
            ["actor_kind"] = "user",
            ["actor_id"] = "7",
            ["host_id"] = hostId,
            ["mac_address"] = "aa-bb-cc-dd-ee-ff",
            ["module"] = "SignIn",
            ["error_type"] = "System.InvalidOperationException",
            ["message"] = message,
            ["exception_detail"] = """[{"level":0,"type":"System.InvalidOperationException"}]""",
            ["error_fingerprint"] = new string('c', 64),
            ["previous_hash"] = new string('a', 64),
            ["entry_hash"] = new string('b', 64),
            ["payload_json"] = """{"runtime":{},"ui":{},"database":{}}""",
        };

    /// <summary>One row as <c>sp_ops_startup_logs_fingerprint_groups_get</c> returns it.</summary>
    private static Dictionary<string, object?> GroupRow(
        long occurrences,
        string errorType,
        string module) => new(StringComparer.Ordinal)
        {
            ["error_fingerprint"] = new string('c', 64),
            ["occurrences"] = occurrences,
            ["first_seen_utc"] = new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc),
            ["last_seen_utc"] = new DateTime(2026, 9, 27, 9, 15, 0, DateTimeKind.Utc),
            ["error_type"] = errorType,
            ["module"] = module,
            ["sample_message"] = "the store did not answer within its stated maximum",
        };

    /// <summary>The store seam, recording what the panel asked for and answering with what the test scripted.</summary>
    private sealed class RecordingLogReader : IMySqlHelperServer
    {
        public List<Dictionary<string, object?>> Rows { get; } = new();

        public List<string> ProcedureNames { get; } = new();

        public IReadOnlyDictionary<string, object?> Parameters { get; private set; } =
            new Dictionary<string, object?>(StringComparer.Ordinal);

        /// <summary>When set, every read throws it, which is how the unavailable state is exercised.</summary>
        public Exception? Failure { get; set; }

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
        {
            ProcedureNames.Add(storedProcedureName);
            Parameters = parameters;

            if (Failure is not null)
            {
                return Task.FromException<IReadOnlyList<Dictionary<string, object?>>>(Failure);
            }

            return Task.FromResult<IReadOnlyList<Dictionary<string, object?>>>(Rows.ToList());
        }

        public Task<int> ExecuteStoredProcedureNonQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default) => Task.FromResult(0);

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteSqlQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Dictionary<string, object?>>>(Rows.ToList());

        public Task<int> ExecuteSqlNonQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default) => Task.FromResult(0);
    }

    /// <summary>The permission lookup, recording which keys were asked about.</summary>
    private sealed class RecordingPermissionStub : IPermissionService
    {
        public List<string> KeysAsked { get; } = new();

        public bool Answer { get; set; } = true;

        public Task<bool> HasPermissionAsync(string permissionKey, CancellationToken cancellationToken = default)
        {
            KeysAsked.Add(permissionKey);
            return Task.FromResult(Answer);
        }

        public Task<IReadOnlyDictionary<string, bool>> HasPermissionsAsync(
            IEnumerable<string> permissionKeys,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<string, bool>>(
                permissionKeys.ToDictionary(key => key, _ => Answer, StringComparer.Ordinal));

        public void Invalidate()
        {
        }
    }
}
