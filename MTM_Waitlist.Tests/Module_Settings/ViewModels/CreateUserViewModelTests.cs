using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Models.UserManagement;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.ViewModels;

namespace MTM_Waitlist.Tests.Module_Settings.ViewModels;

/// <summary>
/// The create form (T053, FR-004, FR-005, FR-007, FR-008, FR-022, FR-104, SC-010).
/// </summary>
/// <remarks>
/// The store's own side of these rules — the transaction, the rank refusal, the audit rows — is proved against the
/// live store in T072. What this suite proves is what the screen is responsible for: one press creates one person,
/// the picker offers nothing above the reader's rung, and a refusal costs the reader none of what they typed.
/// </remarks>
[TestClass]
public sealed class CreateUserViewModelTests
{
    private const long SignedInUserId = 42;

    [TestMethod]
    public async Task OnePress_CreatesExactlyOnePerson()
    {
        var repository = new InMemoryUserManagementRepository();
        var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        repository.CreateGate = gate.Task;

        var viewModel = Build(repository: repository);
        await viewModel.InitializeAsync();
        Fill(viewModel, username: "jsmith");

        // The first press is in flight; the second arrives before it has answered.
        var first = viewModel.CreateAsync();
        await viewModel.CreateAsync();

        Assert.AreEqual(
            1,
            repository.WritesEntered,
            "A second press while a create is in flight is the same press, so the store is asked once (FR-104).");

        gate.SetResult(true);
        await first;

        Assert.AreEqual(1, repository.CreateCalls);
        Assert.IsTrue(viewModel.LastSaveSucceeded);
    }

    [TestMethod]
    public async Task ADuplicateSignInName_BecomesTheOneTypedAnswer_KeepsWhatWasTyped_AndIsNotRetried()
    {
        var repository = new InMemoryUserManagementRepository();
        repository.Seed("JSMITH", "Jane Smith", "6229", "developer");

        var viewModel = Build(repository: repository);
        await viewModel.InitializeAsync();

        // Typed in lower case: the stored form is upper case, so this is the same sign-in name (FR-002).
        Fill(viewModel, username: "jsmith", firstName: "Jane", lastName: "Smith");

        await viewModel.CreateAsync();

        Assert.AreEqual(
            UserManagementMessages.DuplicateUsername,
            viewModel.MessageText,
            "The provider's duplicate-key answer becomes the one typed 'already taken' result (FR-008).");
        Assert.IsFalse(viewModel.LastSaveSucceeded);
        Assert.AreEqual("jsmith", viewModel.Username, "What was typed stays where it is, so the name is not retyped.");
        Assert.AreEqual("Jane", viewModel.FirstName);
        Assert.AreEqual("6229", viewModel.EmployeeNumber);
        Assert.AreEqual(
            1,
            repository.CreateCalls,
            "The path has no blind retry: the refusal is reported rather than attempted again (FR-008).");
    }

    [TestMethod]
    public async Task AnOverLengthName_IsRefusedWithTheLimitStated_AndKeepsWhatWasTyped()
    {
        var repository = new InMemoryUserManagementRepository();
        var viewModel = Build(repository: repository);
        await viewModel.InitializeAsync();

        var tooLong = new string('a', UserManagementService.MaxNameLength + 1);
        Fill(viewModel, username: tooLong, firstName: "Jane", lastName: "Smith");

        await viewModel.CreateAsync();

        Assert.AreEqual(
            UserManagementMessages.NameTooLong,
            viewModel.MessageText,
            "The refusal states the limit in the reader's words (FR-005).");
        StringAssert.Contains(viewModel.MessageText, "128", "The limit itself is named, not just the fact of a limit.");
        Assert.AreEqual(tooLong, viewModel.Username, "The over-long value is kept so it can be corrected rather than retyped.");
        Assert.AreEqual(0, repository.CreateCalls, "A value that breaks a rule never reaches the store.");
    }

    [TestMethod]
    public async Task ThePicker_OffersNothingAboveTheReadersOwnRung()
    {
        var viewModel = Build(readerRoleCode: "plant_manager");
        await viewModel.InitializeAsync();

        var offered = viewModel.RoleOptions.Select(option => option.RoleCode).ToArray();

        CollectionAssert.DoesNotContain(offered, "developer", "A role above the reader's rung is not offered at all (FR-022).");
        CollectionAssert.DoesNotContain(offered, "it_department");
        CollectionAssert.Contains(offered, "plant_manager", "The reader's own rung is offered, because peers may be created.");
        CollectionAssert.Contains(offered, "setup");
        Assert.IsFalse(
            string.IsNullOrWhiteSpace(viewModel.RoleBoundText),
            "The bound is stated in words rather than left for the reader to infer (FR-022).");
    }

    [TestMethod]
    public async Task TheDerivedDisplayName_StaysInsideItsLimit()
    {
        var repository = new InMemoryUserManagementRepository();
        var viewModel = Build(repository: repository);
        await viewModel.InitializeAsync();

        // Two maximum-length parts derive a 257-character name, which the column would refuse (FR-005).
        Fill(
            viewModel,
            username: "jsmith",
            firstName: new string('a', UserManagementService.MaxNameLength),
            lastName: new string('b', UserManagementService.MaxNameLength));

        await viewModel.CreateAsync();

        Assert.IsTrue(viewModel.LastSaveSucceeded, "The write is not refused for a reason the reader cannot act on.");
        Assert.IsTrue(
            repository.LastWrittenDisplayName.Length <= UserManagementService.MaxDisplayNameLength,
            "The derived display name is bounded before it reaches the store (FR-005).");
    }

    [TestMethod]
    public async Task TwoPeopleSharingOneEmployeeNumber_AreBothCreatedAndBothListed()
    {
        var repository = new InMemoryUserManagementRepository();
        var state = Reader();
        var service = new UserManagementService(repository, state);
        var viewModel = Build(repository: repository, service: service, state: state);
        await viewModel.InitializeAsync();

        // 6229 is deliberately not unique, so the second person must be accepted (FR-004).
        Fill(viewModel, username: "jsmith", firstName: "Jane", lastName: "Smith");
        await viewModel.CreateAsync();
        Assert.IsTrue(viewModel.LastSaveSucceeded, "The first person sharing the number is created.");

        Fill(viewModel, username: "bpatel", firstName: "Bina", lastName: "Patel");
        await viewModel.CreateAsync();
        Assert.IsTrue(viewModel.LastSaveSucceeded, "The employee number is deliberately not unique, so the second is created too.");

        var roster = await service.SearchAsync(null, null);

        Assert.AreEqual(2, roster.Count, "Both people are listed, and neither hides the other.");
        CollectionAssert.AreEquivalent(
            new[] { "JSMITH", "BPATEL" },
            roster.Select(row => row.UsernameNormalized).ToArray());
        Assert.AreEqual(
            "6229",
            roster[0].EmployeeIdentifier,
            "The two people really do share one employee number, which is the point of the case.");
    }

    [TestMethod]
    public async Task ASuccessfulCreate_IssuesAFourDigitCredential_ThatIsDroppedWhenItIsDismissed()
    {
        var viewModel = Build(repository: new InMemoryUserManagementRepository());
        await viewModel.InitializeAsync();
        Fill(viewModel, username: "jsmith");

        await viewModel.CreateAsync();

        Assert.IsNotNull(viewModel.IssuedCredential, "A create issues a one-time credential (FR-028).");
        Assert.AreEqual(4, viewModel.IssuedCredential!.Pin.Length, "The credential is exactly four digits.");
        Assert.IsTrue(viewModel.IssuedCredential.Pin.All(char.IsAsciiDigit));
        Assert.AreEqual("JSMITH", viewModel.IssuedCredential.SignInName, "The window names the sign-in name as stored.");

        viewModel.DismissIssuedCredential();

        Assert.IsNull(viewModel.IssuedCredential, "Once the window has shown it, the credential is held nowhere.");
        Assert.IsFalse(viewModel.HasIssuedCredential);
    }

    [TestMethod]
    public async Task AReaderWithoutThePermission_IsToldSoAndTheFormIsUnavailable()
    {
        var viewModel = Build(entitled: false);
        await viewModel.InitializeAsync();

        Assert.IsFalse(viewModel.IsEntitled);
        Assert.IsTrue(viewModel.NotEntitled, "The form is shown unavailable with the reason rather than hidden (FR-101).");
        Assert.IsFalse(viewModel.CanSave);
        Assert.AreEqual(0, viewModel.RoleOptions.Count, "No role is offered to a reader who may not create a person.");
    }

    private static void Fill(
        CreateUserViewModel viewModel,
        string username,
        string firstName = "Jane",
        string lastName = "Smith",
        string employeeNumber = "6229")
    {
        viewModel.Username = username;
        viewModel.FirstName = firstName;
        viewModel.LastName = lastName;
        viewModel.EmployeeNumber = employeeNumber;
    }

    private static CreateUserViewModel Build(
        InMemoryUserManagementRepository? repository = null,
        IUserManagementService? service = null,
        StartupState? state = null,
        bool entitled = true,
        string readerRoleCode = "developer")
    {
        var reader = state ?? Reader(readerRoleCode);

        return new CreateUserViewModel(
            service ?? new UserManagementService(repository ?? new InMemoryUserManagementRepository(), reader),
            new CatalogueStub(),
            PermissionStub.Holding(entitled ? ["permission.admin.users"] : []),
            new RecordingNavigationService(),
            reader);
    }

    private static StartupState Reader(string roleCode = "developer") => new()
    {
        UserId = SignedInUserId,
        Username = "JSMITH",
        EmployeeName = "Jane Smith",
        CurrentRoleCode = roleCode,
    };

    /// <summary>
    /// A small in-memory store standing in for the six procedures. It answers the same typed outcomes the
    /// repository does, including the duplicate sign-in name, so the screen is proved against the shapes it will
    /// actually meet.
    /// </summary>
    private sealed class InMemoryUserManagementRepository : IUserManagementRepository
    {
        private readonly List<UserAccount> _accounts = [];
        private long _nextId = 1;

        /// <summary>Held open by a test that needs a write in flight, which is how the repeated press is proved.</summary>
        internal Task? CreateGate { get; set; }

        internal int CreateCalls { get; private set; }

        /// <summary>How many creates were entered, counted before the gate is awaited so an in-flight write is visible.</summary>
        internal int WritesEntered { get; private set; }

        internal string LastWrittenDisplayName { get; private set; } = string.Empty;

        internal void Seed(string username, string displayName, string employeeIdentifier, string roleCode) =>
            _accounts.Add(new UserAccount(
                _nextId++,
                $"public-{_nextId}",
                username,
                displayName.Split(' ')[0],
                displayName.Split(' ')[^1],
                displayName,
                employeeIdentifier,
                roleCode,
                roleCode,
                10,
                IsActive: true,
                TemporaryCredentialFailedAttempts: 0));

        public Task<IReadOnlyList<UserRosterRow>> ListAsync(string? searchText, string? roleCode, CancellationToken cancellationToken = default)
        {
            var rows = _accounts
                .Where(account => string.IsNullOrWhiteSpace(searchText)
                    || account.UsernameNormalized.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                    || account.DisplayName.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                    || account.EmployeeIdentifier.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                .Where(account => string.IsNullOrWhiteSpace(roleCode)
                    || string.Equals(account.RoleCode, roleCode, StringComparison.Ordinal))
                .Select(account => new UserRosterRow(
                    account.UserId,
                    account.PublicId,
                    account.UsernameNormalized,
                    account.DisplayName,
                    account.EmployeeIdentifier,
                    account.RoleCode,
                    account.RoleName,
                    account.RoleRank,
                    account.IsActive))
                .ToArray();

            return Task.FromResult<IReadOnlyList<UserRosterRow>>(rows);
        }

        public Task<UserAccount?> GetAsync(long userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_accounts.FirstOrDefault(account => account.UserId == userId));

        public async Task<UserManagementResult> CreateAsync(
            string username,
            string firstName,
            string lastName,
            string displayName,
            string employeeIdentifier,
            string roleCode,
            long actorUserId,
            string passwordHash,
            byte[] passwordSalt,
            string changeGroupId,
            CancellationToken cancellationToken = default)
        {
            WritesEntered++;

            if (CreateGate is { } gate)
            {
                await gate;
            }

            CreateCalls++;
            LastWrittenDisplayName = displayName;

            if (_accounts.Any(account => string.Equals(account.UsernameNormalized, username, StringComparison.Ordinal)))
            {
                // The provider's 1062, surfaced as the one typed answer the contract pins (FR-008).
                return UserManagementResult.Failed(
                    UserManagementOutcomeKind.DuplicateUsername,
                    UserManagementMessages.DuplicateUsernameKey,
                    UserManagementMessages.DuplicateUsername);
            }

            _accounts.Add(new UserAccount(
                _nextId++,
                $"public-{_nextId}",
                username,
                firstName,
                lastName,
                displayName,
                employeeIdentifier,
                roleCode,
                roleCode,
                10,
                IsActive: true,
                TemporaryCredentialFailedAttempts: 0));

            return UserManagementResult.Succeeded("1234");
        }

        public Task<UserManagementResult> UpdateAsync(
            long userId,
            string username,
            string firstName,
            string lastName,
            string displayName,
            string employeeIdentifier,
            string roleCode,
            bool isActive,
            long actorUserId,
            string changeGroupId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(UserManagementResult.Succeeded());

        public Task<UserManagementResult> ResetPasswordAsync(
            long userId,
            long actorUserId,
            string passwordHash,
            byte[] passwordSalt,
            string changeGroupId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(UserManagementResult.Succeeded("5678"));

        public Task RecordTemporaryCredentialAttemptAsync(long userId, bool wasSuccessful, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    /// <summary>The catalogue the picker reads, at the rungs the shipped seed gives them.</summary>
    private sealed class CatalogueStub : IRoleCatalogService
    {
        public Task<IReadOnlyList<RoleCatalogEntry>> GetRolesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RoleCatalogEntry>>(
            [
                new RoleCatalogEntry(1, "developer", "Developer", 100),
                new RoleCatalogEntry(2, "it_department", "IT Department", 90),
                new RoleCatalogEntry(3, "plant_manager", "Plant Manager", 80),
                new RoleCatalogEntry(4, "setup_lead", "Setup Lead", 60),
                new RoleCatalogEntry(5, "setup", "Setup", 10),
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
