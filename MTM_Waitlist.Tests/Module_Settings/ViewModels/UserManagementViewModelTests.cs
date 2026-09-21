using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Models.UserManagement;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.ViewModels;

namespace MTM_Waitlist.Tests.Module_Settings.ViewModels;

/// <summary>
/// The user list (T046, FR-080 to FR-096, SC-011, SC-012, SC-017).
/// </summary>
/// <remarks>
/// The roster's own query — matching a sign-in name, a name, an employee number or a role name — is the store's,
/// and is proved against the live store in T072. What this suite proves is everything above that seam: which of
/// the five states the page shows, what the filter does when it cannot be used, that the filter is remembered for
/// the person, and that a row's single job is to open that person.
/// </remarks>
[TestClass]
public sealed class UserManagementViewModelTests
{
    private const long SignedInUserId = 42;

    [TestMethod]
    public async Task EveryoneIsListed_WithTheSwitchedOffPeopleMarked()
    {
        var viewModel = Build(rows: [Row(1, "JSMITH", "Jane Smith", "6229", "developer", "Developer", true),
            Row(2, "BTAYLOR", "Bob Taylor", "6230", "setup", "Setup", false)]);

        await viewModel.InitializeAsync();

        Assert.AreEqual(2, viewModel.People.Count, "Everyone is listed, switched-off people included (FR-088).");
        Assert.AreEqual(string.Empty, viewModel.People[0].StatusText);
        Assert.AreEqual(
            "UserManagement_Row.SwitchedOff".GetLocalized(),
            viewModel.People[1].StatusText,
            "A switched-off person is listed and marked.");
        Assert.IsFalse(viewModel.IsRosterEmpty);
        Assert.IsFalse(viewModel.IsNoMatch);
        Assert.IsFalse(viewModel.IsStoreUnavailable);
    }

    [TestMethod]
    public async Task AnEmptyRoster_HasItsOwnSentence_WhichIsNotTheFilteresSentence()
    {
        var viewModel = Build(rows: []);

        await viewModel.InitializeAsync();

        Assert.IsTrue(viewModel.IsRosterEmpty, "Nobody at all is a different state from a filter that matches nobody.");
        Assert.IsFalse(viewModel.IsNoMatch);
        Assert.AreNotEqual(
            viewModel.EmptyText,
            viewModel.NoMatchText,
            "The two states must not share a sentence, or the reader cannot tell them apart (FR-090).");
    }

    [TestMethod]
    public async Task AFilterThatMatchesNobody_SaysSo_RatherThanShowingAnEmptyRoster()
    {
        var service = new FakeUserManagementService(rows: [Row(1, "JSMITH", "Jane Smith", "6229", "developer", "Developer", true)]);
        var viewModel = Build(service: service);

        await viewModel.InitializeAsync();

        // The service answers nothing for a term, which is what the store would do for a filter matching nobody.
        service.AnswerWith = [];
        viewModel.SearchText = "zzz";
        await viewModel.LoadAsync();

        Assert.AreEqual(0, viewModel.People.Count);
        Assert.IsTrue(viewModel.IsNoMatch, "A filter that matches nobody states that, and never the empty-roster sentence.");
        Assert.IsFalse(viewModel.IsRosterEmpty);
        Assert.IsTrue(viewModel.IsFilterApplied);
    }

    [TestMethod]
    public async Task TheSearchTerm_IsPassedToTheStore_AndTheRoleFilterNarrowsIt()
    {
        var service = new FakeUserManagementService(rows: [Row(1, "JSMITH", "Jane Smith", "6229", "developer", "Developer", true)]);
        var viewModel = Build(service: service);

        await viewModel.InitializeAsync();

        viewModel.SearchText = "6229";
        await viewModel.LoadAsync();

        CollectionAssert.Contains(
            service.Reads,
            ("6229", string.Empty),
            "The term reaches the store, which is where the four fields are matched. An empty role is what the repository normalises to a filter that narrows nothing.");

        viewModel.SelectedRole = viewModel.RoleOptions.Single(option => option.RoleCode == "setup");
        await viewModel.LoadAsync();

        CollectionAssert.Contains(
            service.Reads,
            ("6229", "setup"),
            "The term and the chosen role travel together, so the result is narrowed rather than widened.");
    }

    [TestMethod]
    public async Task ASavedFilter_IsAppliedOnReturn_AndNamedWithHowManyItHides()
    {
        var stored = new FakeConfigSettingsValueService();
        stored.SetText(UserListFilter.SettingKey, new UserListFilter("smith", string.Empty).ToStoredValue());

        var service = new FakeUserManagementService(rows: [Row(1, "JSMITH", "Jane Smith", "6229", "developer", "Developer", true)]);
        var viewModel = Build(service: service, stored: stored);

        await viewModel.InitializeAsync();

        Assert.AreEqual("smith", viewModel.SearchText, "The filter the person left is applied when they come back (FR-089).");
        Assert.IsTrue(viewModel.IsFilterApplied);
        Assert.IsFalse(string.IsNullOrWhiteSpace(viewModel.HiddenCountText), "The page says how many people the filter hides (FR-094).");
    }

    [TestMethod]
    public async Task ASavedFilterThatCannotBeRead_FallsBackToEveryoneAndSaysSo()
    {
        var stored = new FakeConfigSettingsValueService();
        stored.SetText(UserListFilter.SettingKey, "{ this is not a filter");

        // Diagnostic: the filter the screen will read must be the filter this fixture stored.
        var roundTripped = await stored.GetSettingValueAsync(UserListFilter.SettingKey, $"user:{SignedInUserId}");
        Assert.IsNotNull(roundTripped, "The fixture stored a filter the screen cannot read back.");
        Assert.AreEqual("{ this is not a filter", roundTripped!.SettingValue);

        var service = new FakeUserManagementService(rows: [Row(1, "JSMITH", "Jane Smith", "6229", "developer", "Developer", true)]);
        var viewModel = Build(service: service, stored: stored);

        await viewModel.InitializeAsync();

        Assert.AreEqual(1, viewModel.People.Count, "An unreadable filter shows everyone rather than half a filter.");
        Assert.AreEqual("UserManagement_State.FilterUnreadable".GetLocalized(), viewModel.StateMessage);
        Assert.IsFalse(viewModel.IsFilterApplied);
    }

    [TestMethod]
    public async Task ASavedFilterNamingARoleThatIsGone_FallsBackToEveryoneAndSaysSo()
    {
        var stored = new FakeConfigSettingsValueService();
        stored.SetText(UserListFilter.SettingKey, new UserListFilter(string.Empty, "a_retired_role").ToStoredValue());

        var service = new FakeUserManagementService(rows: [Row(1, "JSMITH", "Jane Smith", "6229", "developer", "Developer", true)]);
        var viewModel = Build(service: service, stored: stored);

        await viewModel.InitializeAsync();

        Assert.AreEqual(1, viewModel.People.Count);
        Assert.AreEqual("UserManagement_State.RoleGone".GetLocalized(), viewModel.StateMessage);
        Assert.IsFalse(viewModel.IsFilterApplied);
    }

    [TestMethod]
    public async Task AnUnreachableStore_ShowsTheUnavailableStateWithARetry_AndNeverAnEmptyList()
    {
        var service = new FakeUserManagementService(rows: []) { ThrowOnRead = new InvalidOperationException("store down") };
        var viewModel = Build(service: service);

        await viewModel.InitializeAsync();

        Assert.IsTrue(viewModel.IsStoreUnavailable);
        Assert.AreEqual("UserManagement_State.Unavailable".GetLocalized(), viewModel.StateMessage);
        Assert.AreEqual(0, viewModel.People.Count, "No list is shown, and no sample row takes its place.");
        Assert.IsTrue(viewModel.IsRosterEmpty is false, "An unavailable store must not be presented as an empty roster.");
        Assert.AreEqual("UserManagement_State.Retry".GetLocalized(), viewModel.RetryText);

        // The retry is the reader's own way back, and it reads again.
        service.ThrowOnRead = null;
        service.AnswerWith = [Row(1, "JSMITH", "Jane Smith", "6229", "developer", "Developer", true)];
        await viewModel.LoadAsync();

        Assert.IsFalse(viewModel.IsStoreUnavailable);
        Assert.AreEqual(1, viewModel.People.Count);
    }

    [TestMethod]
    public async Task ARowOpensThatPerson_AndTheEntitlementIsReCheckedOnEveryArrival()
    {
        var navigation = new RecordingNavigationService();
        var permissions = PermissionStub.Holding("permission.admin.users");
        var viewModel = Build(
            rows: [Row(1, "JSMITH", "Jane Smith", "6229", "developer", "Developer", true)],
            navigation: navigation,
            permissions: permissions);

        await viewModel.InitializeAsync();
        Assert.IsTrue(viewModel.IsEntitled, "Holding the permission admits the reader.");

        var person = viewModel.People[0];
        viewModel.OpenPersonCommand.Execute(person);

        Assert.AreEqual(
            UserManagementViewModel.PersonPageViewModelName,
            navigation.LastPageKey,
            "A row's whole job is to open that person (FR-087).");
        Assert.AreEqual(person.UserId, navigation.LastParameter);

        // The same screen, reached again by somebody whose entitlement has gone: it is re-read, not remembered.
        permissions.HeldKeys.Clear();
        await viewModel.RefreshEntitlementAsync();

        Assert.IsFalse(viewModel.IsEntitled, "The entitlement is read on every arrival rather than kept (FR-081).");
    }

    [TestMethod]
    public async Task ShowEveryone_ClearsTheFilter_SavesIt_AndLoadsEverybody()
    {
        var stored = new FakeConfigSettingsValueService();
        stored.SetText(UserListFilter.SettingKey, new UserListFilter("smith", "setup").ToStoredValue());

        var service = new FakeUserManagementService(rows: [Row(1, "JSMITH", "Jane Smith", "6229", "developer", "Developer", true)]);
        var viewModel = Build(service: service, stored: stored);

        await viewModel.InitializeAsync();
        Assert.IsTrue(viewModel.IsFilterApplied);
        service.Reads.Clear();

        await viewModel.ShowEveryoneAsync();

        Assert.IsFalse(viewModel.IsFilterApplied);
        Assert.AreEqual(1, service.Reads.Count, "Showing everyone reads once: there is no second count read without a filter.");
        Assert.AreEqual((string.Empty, string.Empty), service.Reads[0], "Showing everyone asks the store for everyone.");

        var saved = stored.SavedValues.Last(value => value.SettingKey == UserListFilter.SettingKey);
        Assert.AreEqual(
            new UserListFilter(string.Empty, string.Empty).ToStoredValue(),
            saved.SettingValue,
            "Clearing the filter is saved as a filter, not as a missing value (FR-089).");
        Assert.AreEqual($"user:{SignedInUserId}", saved.ScopeKey, "The filter belongs to the person, not to the screen.");
        Assert.AreEqual("text", saved.ValueType);
    }

    private static UserRosterRow Row(
        long userId, string username, string displayName, string employeeIdentifier, string roleCode, string roleName, bool isActive) =>
        new(userId, $"public-{userId}", username, displayName, employeeIdentifier, roleCode, roleName, 10, isActive);

    private static UserManagementViewModel Build(
        List<UserRosterRow>? rows = null,
        FakeUserManagementService? service = null,
        FakeConfigSettingsValueService? stored = null,
        RecordingNavigationService? navigation = null,
        PermissionStub? permissions = null) =>
        new(
            service ?? new FakeUserManagementService(rows ?? []),
            new FakeRoleCatalogService(),
            permissions ?? PermissionStub.Holding("permission.admin.users"),
            stored ?? new FakeConfigSettingsValueService(),
            navigation ?? new RecordingNavigationService(),
            new StartupState { UserId = SignedInUserId, Username = "JSMITH" });

    /// <summary>
    /// A roster read that answers from a fixed list, and can be told to fail so the unavailable state is reachable.
    /// </summary>
    private sealed class FakeUserManagementService : IUserManagementService
    {
        private readonly List<UserRosterRow> _rows;

        internal FakeUserManagementService(List<UserRosterRow> rows) => _rows = rows;

        /// <summary>What the next read answers, so a test can narrow the roster without a live store.</summary>
        internal List<UserRosterRow>? AnswerWith { get; set; }

        /// <summary>An exception the next read throws, which is how an unreachable store is presented.</summary>
        internal Exception? ThrowOnRead { get; set; }

        internal string? LastSearchText { get; private set; }

        internal string? LastRoleCode { get; private set; }

        /// <summary>
        /// Every read this service answered, in order. A filtered load reads twice — the filter, then the
        /// unfiltered total the hidden count needs — so a test asserts against the whole sequence rather than
        /// against whichever read happened to be last.
        /// </summary>
        internal List<(string? Search, string? Role)> Reads { get; } = [];

        public Task<IReadOnlyList<UserRosterRow>> SearchAsync(
            string? searchText, string? roleCode, CancellationToken cancellationToken = default)
        {
            if (ThrowOnRead is not null)
            {
                return Task.FromException<IReadOnlyList<UserRosterRow>>(ThrowOnRead);
            }

            LastSearchText = searchText;
            LastRoleCode = roleCode;
            Reads.Add((searchText, roleCode));

            return Task.FromResult<IReadOnlyList<UserRosterRow>>(AnswerWith ?? _rows);
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

    /// <summary>The catalogue the filter reads its choices from.</summary>
    private sealed class FakeRoleCatalogService : IRoleCatalogService
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

    /// <summary>A permission service that answers from a fixed set of held keys.</summary>
    private sealed class PermissionStub : IPermissionService
    {
        private PermissionStub(IEnumerable<string> held) => HeldKeys = new HashSet<string>(held, StringComparer.Ordinal);

        internal static PermissionStub Holding(params string[] heldPermissionKeys) => new(heldPermissionKeys);

        internal HashSet<string> HeldKeys { get; }

        public Task<bool> HasPermissionAsync(string permissionKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(HeldKeys.Contains(permissionKey));

        public Task<IReadOnlyDictionary<string, bool>> HasPermissionsAsync(
            IEnumerable<string> permissionKeys, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<string, bool>>(
                permissionKeys.ToDictionary(key => key, HeldKeys.Contains, StringComparer.Ordinal));

        public void Invalidate()
        {
        }
    }

    /// <summary>Records where the screen asked to go.</summary>
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

        internal string? LastPageKey { get; private set; }

        internal object? LastParameter { get; private set; }

        public bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false)
        {
            LastPageKey = pageKey;
            LastParameter = parameter;
            return true;
        }

        public bool GoBack() => false;

        public void SetListDataItemForNextConnectedAnimation(object item)
        {
        }
    }
}
