using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Models.UserManagement;
using MTM_Waitlist.Module_Core.Permissions;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.ViewModels;

namespace MTM_Waitlist.Tests.Module_Settings.ViewModels;

/// <summary>
/// The permissions page's behaviour (T059, FR-066 to FR-074, FR-114, SC-010).
/// </summary>
/// <remarks>
/// What reaches the store is proved against the live store in T058. What this suite proves is the page: what is
/// pending, what the reader is asked to confirm, what the undo does, and what a person who outranks the reader
/// looks like.
/// </remarks>
[TestClass]
public sealed class PermissionsViewModelTests
{
    private const long SignedInUserId = 42;
    private const long TargetUserId = 77;

    [TestMethod]
    public async Task RowsMarkWhatIsPending_AndTheWarningStatesTheCount()
    {
        var viewModel = Build(rows:
        [
            new PermissionValueRow(PermissionKeys.SettingsHotWorkCenters, true, PermissionProvenance.Inherited),
            new PermissionValueRow(PermissionKeys.SettingsIgnoredLocations, false, PermissionProvenance.Inherited),
        ]);

        await viewModel.InitializeAsync();

        Assert.AreEqual(0, viewModel.PendingCount);
        Assert.IsFalse(viewModel.HasPendingChanges);
        Assert.IsTrue(viewModel.NothingChanged, "Nothing changed is a state the page states, not just a disabled button (FR-068).");

        var first = viewModel.Rows.Single(row => row.Key == PermissionKeys.SettingsHotWorkCenters);
        first.Value = false;

        Assert.IsTrue(first.IsPending, "A changed row is marked pending until it is saved (FR-074).");
        Assert.AreEqual(1, viewModel.PendingCount);
        Assert.IsTrue(viewModel.HasPendingChanges);
        Assert.IsTrue(viewModel.CanSave);
        Assert.IsFalse(viewModel.NothingChanged);
        Assert.AreEqual(1, viewModel.PendingCount, "The warning is about one row here.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(viewModel.UnsavedWarningText));
        StringAssert.Contains(
            ShippedResourceValue("Permissions_Unsaved.Leaving"),
            "{0}",
            "Leaving warns with how many rows are still pending (FR-073).");
    }

    [TestMethod]
    public async Task TheConfirmation_NamesWhatChangesAndForWhom_AndCountsWhenSeveralChange()
    {
        var viewModel = Build(rows:
        [
            new PermissionValueRow(PermissionKeys.SettingsHotWorkCenters, true, PermissionProvenance.Inherited),
            new PermissionValueRow(PermissionKeys.SettingsIgnoredLocations, false, PermissionProvenance.Inherited),
        ]);

        await viewModel.InitializeAsync();

        viewModel.Rows.Single(row => row.Key == PermissionKeys.SettingsHotWorkCenters).Value = false;

        var single = viewModel.ConfirmationSentences;
        Assert.AreEqual(1, single.Count, "One changed row is one sentence.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(single[0]));

        viewModel.Rows.Single(row => row.Key == PermissionKeys.SettingsIgnoredLocations).Value = true;

        var several = viewModel.ConfirmationSentences;
        Assert.AreEqual(3, several.Count, "Several changes give a count and a sentence per changed row (FR-067).");
        Assert.IsFalse(
            string.IsNullOrWhiteSpace(several[0]),
            "The first sentence is the count for all of them, and the two that follow name each changed row.");

        // The count's own wording is the shipped one, read from the resource file rather than from the lookup: the
        // test host has no resource map, so a lookup answers with the key and substitutes no placeholder.
        var shippedCount = ShippedResourceValue("Permissions_Save.ConfirmationCount");
        StringAssert.Contains(shippedCount, "{0}", "The count's sentence carries the number of changes.");
        Assert.AreEqual(
            viewModel.Rows[0].LabelText,
            several[1],
            "One sentence per changed row names the row, so the reader is told what is changing and not only how many.");
        Assert.AreEqual(viewModel.Rows[1].LabelText, several[2]);
    }

    [TestMethod]
    public async Task TheChangeSet_CarriesBothValuesPerEntry()
    {
        var viewModel = Build(rows:
        [
            new PermissionValueRow(PermissionKeys.SettingsHotWorkCenters, true, PermissionProvenance.Inherited),
        ]);

        await viewModel.InitializeAsync();
        viewModel.Rows[0].Value = false;

        var entry = viewModel.CurrentChangeSet.Entries.Single();

        Assert.AreEqual(PermissionKeys.SettingsHotWorkCenters, entry.Key);
        Assert.IsTrue(entry.From is true, "The value the page last saw travels with the change, so a moved value is refused (FR-070).");
        Assert.IsTrue(entry.To is false);
    }

    [TestMethod]
    public async Task OneImpatientPress_WritesExactlyOneChange()
    {
        var service = new FakePermissionAdministrationService(rows:
        [
            new PermissionValueRow(PermissionKeys.SettingsHotWorkCenters, true, PermissionProvenance.Inherited),
        ])
        {
            SaveGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously),
        };

        var viewModel = Build(service: service);
        await viewModel.InitializeAsync();
        viewModel.Rows[0].Value = false;

        var first = viewModel.SaveAsync();
        await viewModel.SaveAsync();

        Assert.AreEqual(1, service.Applies, "A second press while a save is in flight is the same press (SC-010).");

        service.ReleaseSave();
        await first;
    }

    [TestMethod]
    public async Task TheUndo_ReversesTheWholeSave_RatherThanHalfOfIt()
    {
        var service = new FakePermissionAdministrationService(rows:
        [
            new PermissionValueRow(PermissionKeys.SettingsHotWorkCenters, false, PermissionProvenance.Chosen),
            new PermissionValueRow(PermissionKeys.SettingsIgnoredLocations, true, PermissionProvenance.Chosen),
        ]);

        var viewModel = Build(service: service);
        await viewModel.InitializeAsync();

        await viewModel.UndoAsync();

        Assert.AreEqual(1, service.Reversals, "One undo is one call, which is what makes it reverse the whole save (FR-071).");
        Assert.IsFalse(service.LastReversalOverrodeMovedValue, "The first attempt never overrides a moved value.");
    }

    [TestMethod]
    public async Task AValueThatMoved_IsShownAndAskedAbout_BeforeItIsRestored()
    {
        var service = new FakePermissionAdministrationService(rows:
        [
            new PermissionValueRow(PermissionKeys.SettingsHotWorkCenters, true, PermissionProvenance.Chosen),
        ])
        {
            ReversalResult = PermissionChangeResult.Failed(
                PermissionChangeOutcomeKind.ValueMoved,
                PermissionAdministrationMessages.ValueMovedKey,
                PermissionAdministrationMessages.ValueMoved,
                PermissionKeys.SettingsHotWorkCenters),
        };

        var viewModel = Build(service: service);
        await viewModel.InitializeAsync();

        await viewModel.UndoAsync();

        Assert.IsTrue(viewModel.IsRestorePromptVisible, "The page asks before restoring a value that has moved (FR-070).");
        Assert.IsFalse(string.IsNullOrWhiteSpace(viewModel.RestorePromptText), "It says what the value is now.");
        Assert.AreEqual(1, service.Reversals, "Nothing was written: the undo stopped and asked.");

        service.ReversalResult = PermissionChangeResult.Succeeded();
        await viewModel.ConfirmRestoreAsync();

        Assert.AreEqual(2, service.Reversals);
        Assert.IsTrue(service.LastReversalOverrodeMovedValue, "The reader's answer is what allows the restore.");
        Assert.IsFalse(viewModel.IsRestorePromptVisible);
    }

    [TestMethod]
    public async Task DecliningTheRestore_LeavesTheValueWhereItIs()
    {
        var service = new FakePermissionAdministrationService(rows:
        [
            new PermissionValueRow(PermissionKeys.SettingsHotWorkCenters, true, PermissionProvenance.Chosen),
        ])
        {
            ReversalResult = PermissionChangeResult.Failed(
                PermissionChangeOutcomeKind.ValueMoved,
                PermissionAdministrationMessages.ValueMovedKey,
                PermissionAdministrationMessages.ValueMoved,
                PermissionKeys.SettingsHotWorkCenters),
        };

        var viewModel = Build(service: service);
        await viewModel.InitializeAsync();
        await viewModel.UndoAsync();

        viewModel.DismissRestore();

        Assert.IsFalse(viewModel.IsRestorePromptVisible);
        Assert.AreEqual(1, service.Reversals, "Declining writes nothing at all.");
        Assert.IsFalse(service.LastReversalOverrodeMovedValue);
    }

    [TestMethod]
    public async Task APersonAboveTheReadersRung_IsSelectableToView_WithEveryRowUnavailableAndTheReason()
    {
        var viewModel = Build(
            readerRoleCode: "setup",
            personRoleCode: "plant_manager",
            rows: [new PermissionValueRow(PermissionKeys.SettingsHotWorkCenters, true, PermissionProvenance.Inherited)]);

        await viewModel.InitializeAsync();

        Assert.AreEqual(1, viewModel.People.Count, "The person is in the column, so they can be selected (FR-066).");
        Assert.IsTrue(viewModel.IsPersonReadOnly);
        Assert.IsFalse(string.IsNullOrWhiteSpace(viewModel.ReadOnlyReasonText), "The reason is stated in words.");
        Assert.IsFalse(viewModel.Rows[0].IsAvailable, "Their rows are shown and cannot be changed.");
        Assert.IsFalse(viewModel.Rows[0].CanBeCleared);
        Assert.IsFalse(viewModel.CanSave, "And there is nothing to save for them.");
    }

    [TestMethod]
    public async Task TheFixedRow_IsPresent_Locked_WithItsReason_AndClearableByNobody()
    {
        var viewModel = Build(rows:
        [
            new PermissionValueRow(PermissionKeys.AdminPermissions, true, PermissionProvenance.Inherited),
            new PermissionValueRow(PermissionKeys.SettingsHotWorkCenters, true, PermissionProvenance.Inherited),
        ]);

        await viewModel.InitializeAsync();

        var fixedRow = viewModel.Rows.Single(row => row.Key == PermissionKeys.AdminPermissions);

        Assert.IsTrue(fixedRow.IsFixed, "The row that opens this page is present on it (FR-059).");
        Assert.IsTrue(fixedRow.IsLocked);
        Assert.IsFalse(fixedRow.CanBeCleared, "Nobody can clear it from this page.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(fixedRow.LockReasonText), "And it says why, in words.");

        // Changing it is not offered, and even if it were, the set excludes it rather than asking the reader to
        // confirm a change the store refuses.
        viewModel.Rows.Single(row => row.Key == PermissionKeys.SettingsHotWorkCenters).Value = false;
        var changes = viewModel.CurrentChangeSet.Entries;

        Assert.AreEqual(1, changes.Count, "Only the row that can change is in the set.");

        // And the page shows the value in force beside where it came from (FR-065).
        Assert.IsFalse(string.IsNullOrWhiteSpace(fixedRow.ProvenanceText));
        Assert.IsFalse(string.IsNullOrWhiteSpace(fixedRow.GatesText));
    }

    [TestMethod]
    public async Task APendingChange_IsNotWrittenUntilItIsSaved_AndIsThenNoLongerPending()
    {
        var service = new FakePermissionAdministrationService(rows:
        [
            new PermissionValueRow(PermissionKeys.SettingsHotWorkCenters, true, PermissionProvenance.Inherited),
        ]);

        var viewModel = Build(service: service);
        await viewModel.InitializeAsync();

        viewModel.Rows[0].Value = false;
        Assert.AreEqual(0, service.Applies, "Marking a row does not write anything.");

        await viewModel.SaveAsync();

        Assert.AreEqual(1, service.Applies);
        Assert.AreEqual(0, viewModel.PendingCount, "After the save the row is no longer pending.");
        Assert.IsFalse(viewModel.Rows[0].IsPending);
    }

    [TestMethod]
    public async Task AReaderWithoutThePermission_IsToldSoRatherThanShownAWorkingScreen()
    {
        var viewModel = Build(entitled: false, rows: []);
        await viewModel.InitializeAsync();

        Assert.IsFalse(viewModel.IsEntitled);
        Assert.IsTrue(viewModel.NotEntitled);
        Assert.IsFalse(viewModel.CanSave);
        Assert.IsFalse(viewModel.CanUndo);
    }

    [TestMethod]
    public async Task AnUnreachableStore_IsStatedRatherThanShownAsAnEmptyColumn()
    {
        var service = new FakePermissionAdministrationService(rows: []);
        var people = new FakeUserManagementService("setup") { ThrowOnPeopleRead = new InvalidOperationException("down") };
        var viewModel = Build(service: service, people: people);

        await viewModel.InitializeAsync();

        Assert.IsTrue(viewModel.IsStoreUnavailable);
        Assert.AreEqual(0, viewModel.People.Count, "No sample row takes the column's place.");
        Assert.AreEqual(viewModel.UnavailableText, viewModel.MessageText);
        Assert.IsFalse(viewModel.IsBusy, "The screen does not freeze or refuse interaction while it loads (FR-114).");
    }

    /// <summary>The shipped value of a resource key, read from the resource file rather than from the lookup.</summary>
    private static string ShippedResourceValue(string resourceKey)
    {
        var path = Path.Combine(
            MTM_Waitlist.Tests.Module_Mock.RepositoryPatternScan.FindRepositoryRoot(),
            "Strings",
            "en-us",
            "Resources.resw");

        Assert.IsTrue(File.Exists(path), $"The resource file was not found at '{path}'.");

        var entry = System.Xml.Linq.XDocument.Load(path)
            .Descendants("data")
            .FirstOrDefault(data => data.Attribute("name")?.Value == resourceKey);

        Assert.IsNotNull(entry, $"'{resourceKey}' has no entry, so its sentence would never be shown.");

        return entry!.Element("value")?.Value ?? string.Empty;
    }

    private static PermissionValueRow[] DefaultRows() =>
    [
        new PermissionValueRow(PermissionKeys.SettingsHotWorkCenters, true, PermissionProvenance.Inherited),
    ];

    private static PermissionsViewModel Build(
        IReadOnlyList<PermissionValueRow>? rows = null,
        FakePermissionAdministrationService? service = null,
        FakeUserManagementService? people = null,
        bool entitled = true,
        string readerRoleCode = "developer",
        string personRoleCode = "setup")
    {
        var state = new StartupState
        {
            UserId = SignedInUserId,
            Username = "JSMITH",
            EmployeeName = "Jane Smith",
            CurrentRoleCode = readerRoleCode,
        };

        return new PermissionsViewModel(
            service ?? new FakePermissionAdministrationService(rows ?? DefaultRows()),
            PermissionStub.Holding(entitled ? [PermissionKeys.AdminPermissions] : []),
            people ?? new FakeUserManagementService(personRoleCode),
            new CatalogueStub(),
            new RecordingNavigationService(),
            state);
    }

    private sealed class FakePermissionAdministrationService : IPermissionAdministrationService
    {
        private readonly List<PermissionValueRow> _rows;

        internal FakePermissionAdministrationService(IReadOnlyList<PermissionValueRow> rows) => _rows = [.. rows];

        internal TaskCompletionSource<bool>? SaveGate { get; set; }

        internal PermissionChangeResult ReversalResult { get; set; } = PermissionChangeResult.Succeeded();

        internal int Applies { get; private set; }

        internal int Reversals { get; private set; }

        internal bool LastReversalOverrodeMovedValue { get; private set; }

        /// <summary>Opens the write gate a test closed, so the first press can finish.</summary>
        internal void ReleaseSave() => SaveGate?.TrySetResult(true);

        public Task<IReadOnlyList<PermissionValueRow>> GetForPersonAsync(long userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PermissionValueRow>>(_rows);

        public async Task<PermissionChangeResult> ApplyAsync(
            long userId,
            IReadOnlyList<PermissionChange> changes,
            CancellationToken cancellationToken = default)
        {
            Applies++;

            if (SaveGate is { } gate && !gate.Task.IsCompleted)
            {
                await gate.Task;
            }

            // The store holds what was written, so the page's next read shows it.
            foreach (var change in changes)
            {
                var index = _rows.FindIndex(row => row.Key == change.Key);
                if (index >= 0)
                {
                    _rows[index] = new PermissionValueRow(
                        change.Key,
                        change.To ?? _rows[index].Value,
                        change.To is null ? PermissionProvenance.Inherited : PermissionProvenance.Chosen);
                }
            }

            return PermissionChangeResult.Succeeded();
        }

        public Task<PermissionChangeResult> ReverseLastSaveAsync(
            long userId,
            bool restoreDespiteMovedValue = false,
            CancellationToken cancellationToken = default)
        {
            Reversals++;
            LastReversalOverrodeMovedValue = restoreDespiteMovedValue;
            return Task.FromResult(ReversalResult);
        }

        public Task<PermissionHolders> GetHoldersAsync(string permissionKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PermissionHolders([], []));
    }

    private sealed class FakeUserManagementService : IUserManagementService
    {
        private readonly string _roleCode;

        internal FakeUserManagementService(string roleCode) => _roleCode = roleCode;

        /// <summary>When set, the column of people cannot be read, which is how the unavailable state is reached.</summary>
        internal Exception? ThrowOnPeopleRead { get; set; }

        public Task<IReadOnlyList<UserRosterRow>> SearchAsync(string? searchText, string? roleCode, CancellationToken cancellationToken = default)
        {
            if (ThrowOnPeopleRead is not null)
            {
                return Task.FromException<IReadOnlyList<UserRosterRow>>(ThrowOnPeopleRead);
            }

            return Task.FromResult<IReadOnlyList<UserRosterRow>>(
            [
                new UserRosterRow(TargetUserId, "public-77", "TTARGET", "Tess Target", "6229", _roleCode, _roleCode, 10, true),
            ]);
        }

        public Task<UserAccount?> GetAsync(long userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<UserAccount?>(null);

        public Task<UserManagementResult> CreateAsync(UserAccountEdit edit, CancellationToken cancellationToken = default) =>
            Task.FromResult(UserManagementResult.Succeeded("1234"));

        public Task<UserManagementResult> UpdateAsync(long userId, UserAccountEdit edit, CancellationToken cancellationToken = default) =>
            Task.FromResult(UserManagementResult.Succeeded());

        public Task<UserManagementResult> ResetPasswordAsync(long userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(UserManagementResult.Succeeded("5678"));

        public Task RecordTemporaryCredentialAttemptAsync(long userId, bool wasSuccessful, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class CatalogueStub : IRoleCatalogService
    {
        public Task<IReadOnlyList<RoleCatalogEntry>> GetRolesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RoleCatalogEntry>>(
            [
                new RoleCatalogEntry(1, "developer", "Developer", 100),
                new RoleCatalogEntry(2, "plant_manager", "Plant Manager", 80),
                new RoleCatalogEntry(3, "setup", "Setup", 10),
            ]);

        public void Invalidate()
        {
        }
    }

    private sealed class PermissionStub : IPermissionService
    {
        private PermissionStub(IEnumerable<string> held) => HeldKeys = new HashSet<string>(held, StringComparer.Ordinal);

        internal static PermissionStub Holding(params string[] heldPermissionKeys) => new(heldPermissionKeys);

        internal HashSet<string> HeldKeys { get; }

        public Task<bool> HasPermissionAsync(string permissionKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(HeldKeys.Contains(permissionKey));

        public Task<IReadOnlyDictionary<string, bool>> HasPermissionsAsync(
            IEnumerable<string> permissionKeys,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<string, bool>>(
                permissionKeys.ToDictionary(key => key, HeldKeys.Contains, StringComparer.Ordinal));

        public void Invalidate()
        {
        }
    }

    private sealed class RecordingNavigationService : INavigationService
    {
        public event Microsoft.UI.Xaml.Navigation.NavigatedEventHandler? Navigated
        {
            add { }
            remove { }
        }

        public Microsoft.UI.Xaml.Controls.Frame? Frame
        {
            get => null;
            set { }
        }

        public bool CanGoBack => false;

        public bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false) => true;

        public bool GoBack() => false;

        public void SetListDataItemForNextConnectedAnimation(object item)
        {
        }
    }
}
