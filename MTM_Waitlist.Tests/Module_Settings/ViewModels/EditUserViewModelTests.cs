using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Models.UserManagement;
using MTM_Waitlist.Module_Core.Permissions;
using MTM_Waitlist.Module_Settings.ViewModels;

namespace MTM_Waitlist.Tests.Module_Settings.ViewModels;

/// <summary>
/// One person's page (T053, FR-019 to FR-028, FR-097 to FR-104, SC-010).
/// </summary>
/// <remarks>
/// The store enforces the rank rule, the self-lockout rule and the sign-in-name rule, and T072 proves that against
/// the live store with no screen involved. What this suite proves is the page: what it offers, what it refuses and
/// what it keeps when something fails.
/// </remarks>
[TestClass]
public sealed class EditUserViewModelTests
{
    private const long SignedInUserId = 42;
    private const long OtherPersonId = 77;

    [TestMethod]
    public async Task APersonAboveTheReadersRung_IsReadable_ButEverythingOnThePageIsUnavailableWithTheReason()
    {
        // The reader stands at Setup on rung 10; the person is a Plant Manager on rung 80.
        var viewModel = Build(
            account: Account(OtherPersonId, "pmanager", "Pat Manager", "plant_manager", 80),
            readerRoleCode: "setup");

        await viewModel.InitializeAsync(OtherPersonId);

        Assert.IsTrue(viewModel.IsLoaded, "The account is read; reading is never refused (FR-027).");
        Assert.AreEqual("PMANAGER", viewModel.Username, "The fields carry what the store holds.");
        Assert.IsTrue(viewModel.IsReadOnly);
        Assert.IsFalse(viewModel.IsEditable);
        Assert.AreEqual(
            "EditUser_ReadOnly.Outranked".GetLocalized(),
            viewModel.ReadOnlyReasonText,
            "The reason is stated in words at the top of the page (FR-101).");
        Assert.IsFalse(viewModel.CanSave);
        Assert.IsFalse(viewModel.CanDeactivate);
    }

    [TestMethod]
    public async Task APeer_IsEditable()
    {
        var viewModel = Build(
            account: Account(OtherPersonId, "slead", "Sam Lead", "setup_lead", 60),
            readerRoleCode: "setup_lead");

        await viewModel.InitializeAsync(OtherPersonId);

        Assert.IsTrue(
            viewModel.IsEditable,
            "A person at the reader's own rung does not outrank them, so peers are editable (FR-020, FR-021).");
        Assert.AreEqual(string.Empty, viewModel.ReadOnlyReasonText);
        Assert.IsTrue(viewModel.CanSave);
    }

    [TestMethod]
    public async Task AReaderWithoutThePermission_IsReadOnly_ForThatReasonRatherThanARankReason()
    {
        var viewModel = Build(
            account: Account(OtherPersonId, "slead", "Sam Lead", "setup_lead", 60),
            readerRoleCode: "setup_lead",
            permissionKeys: []);

        await viewModel.InitializeAsync(OtherPersonId);

        Assert.IsFalse(viewModel.IsEditable);
        Assert.AreEqual(
            "EditUser_ReadOnly.NoPermission".GetLocalized(),
            viewModel.ReadOnlyReasonText,
            "An entitlement refusal and a rank refusal are different facts and are not told with one sentence.");
    }

    [TestMethod]
    public async Task TheReadersOwnAccount_ShowsEverySelfLockoutRefusal_EachWithItsReason()
    {
        var service = new FakeUserManagementService(Account(SignedInUserId, "jsmith", "Jane Smith", "setup_lead", 60));
        var viewModel = Build(
            service: service,
            account: Account(SignedInUserId, "jsmith", "Jane Smith", "setup_lead", 60),
            readerRoleCode: "setup_lead");

        await viewModel.InitializeAsync(SignedInUserId);

        Assert.IsTrue(viewModel.IsSelfAccount);
        Assert.IsTrue(viewModel.IsSelfDeactivateUnavailable, "Deactivating one's own account is refused (FR-023).");
        Assert.IsFalse(viewModel.CanDeactivate);
        Assert.IsTrue(viewModel.IsSelfRenameUnavailable, "Changing one's own sign-in name is refused (FR-024).");
        Assert.IsFalse(string.IsNullOrWhiteSpace(viewModel.SelfDeactivateUnavailableText));
        Assert.IsFalse(string.IsNullOrWhiteSpace(viewModel.SelfRenameUnavailableText));
        Assert.AreNotEqual(
            viewModel.SelfDeactivateUnavailableText,
            viewModel.SelfRenameUnavailableText,
            "The two refusals are different rules and are not stated with one sentence.");

        // The third self refusal: a person cannot reset their own credential (FR-028). The permission answer is
        // deliberately left alone — the reader does hold the key — and the action is what is withheld.
        Assert.IsTrue(viewModel.CanResetPassword, "The reader holds the key, so the permission answer is unchanged.");
        Assert.IsTrue(viewModel.IsSelfResetUnavailable, "But their own account is never offered the reset (FR-028).");
        Assert.IsFalse(viewModel.IsResetPasswordOffered);
        Assert.IsFalse(string.IsNullOrWhiteSpace(viewModel.SelfResetUnavailableText));
        Assert.AreNotEqual(
            viewModel.SelfResetUnavailableText,
            viewModel.SelfRenameUnavailableText,
            "The reset refusal is its own rule and is not stated with the rename's sentence.");

        await viewModel.ResetPasswordAsync();

        Assert.AreEqual(
            viewModel.SelfResetUnavailableText,
            viewModel.MessageText,
            "The action is refused in the same words the control's absence states (FR-028).");
        Assert.AreEqual(0, service.Resets, "No credential is issued for the account the reader is signed in as.");
        Assert.IsNull(viewModel.IssuedCredential, "And no window is raised for one.");
    }

    [TestMethod]
    public async Task TheDeactivationSentence_NamesBothHalves_AndIsStillOnThePageAfterTheConfirmation()
    {
        var viewModel = Build(
            account: Account(OtherPersonId, "slead", "Sam Lead", "setup_lead", 60),
            readerRoleCode: "setup_lead");

        await viewModel.InitializeAsync(OtherPersonId);

        var sentence = viewModel.DeactivateExplanationText;

        // The test host has no resource map, so the sentence comes back as its own key. What the requirement is
        // about is the shipped wording, so that is read from the resource file rather than from the lookup.
        Assert.AreEqual(
            "EditUser_Deactivate.Explanation.Text".GetLocalized(),
            sentence,
            "The sentence resolves through the resource map rather than being written into the page (FR-112).");

        var shipped = ShippedResourceValue("EditUser_Deactivate.Explanation.Text");

        StringAssert.Contains(shipped, "cannot sign in again", "The first half of what deactivation means (FR-102).");
        StringAssert.Contains(shipped, "stays open", "The second half: an open session is not ended (FR-102, FR-035).");
        Assert.IsFalse(
            string.IsNullOrWhiteSpace(viewModel.ReactivateExplanationText),
            "Reactivation says when access returns (FR-102).");
    }

    [TestMethod]
    public async Task AnActionChosenWithUnsavedEdits_IsRefusedWithTheSaveOrDiscardInstruction()
    {
        var service = new FakeUserManagementService(Account(OtherPersonId, "slead", "Sam Lead", "setup_lead", 60));
        var viewModel = Build(service: service, readerRoleCode: "setup_lead");
        await viewModel.InitializeAsync(OtherPersonId);

        viewModel.FirstName = "Samantha";

        Assert.IsTrue(viewModel.HasUnsavedChanges, "The page can tell an edited person from an untouched one (FR-100).");
        Assert.IsTrue(viewModel.ActionNeedsSave);

        await viewModel.ResetPasswordAsync();
        Assert.AreEqual(viewModel.ActionNeedsSaveText, viewModel.MessageText, "A reset is refused with the instruction.");
        Assert.AreEqual(0, service.Resets, "An action never acts on the half-edited person.");

        await viewModel.DeactivateAsync();
        Assert.AreEqual(viewModel.ActionNeedsSaveText, viewModel.MessageText);
        Assert.AreEqual(0, service.Updates, "Deactivating a half-edited person is refused for the same reason.");

        // Discarding is the other half of the instruction, and it puts the form back to the store's values.
        viewModel.DiscardChanges();
        Assert.IsFalse(viewModel.HasUnsavedChanges);
        Assert.AreEqual("Sam", viewModel.FirstName);
    }

    [TestMethod]
    public async Task ASaveThatFails_KeepsWhatTheReaderHad_AndStatesWhatFailed()
    {
        var service = new FakeUserManagementService(Account(OtherPersonId, "slead", "Sam Lead", "setup_lead", 60))
        {
            UpdateResult = UserManagementResult.Failed(
                UserManagementOutcomeKind.StoreUnavailable,
                UserManagementMessages.StoreUnavailableKey,
                UserManagementMessages.StoreUnavailable),
        };

        var viewModel = Build(service: service, readerRoleCode: "setup_lead");
        await viewModel.InitializeAsync(OtherPersonId);

        viewModel.FirstName = "Samantha";
        viewModel.EmployeeNumber = "6230";

        await viewModel.SaveAsync();

        Assert.AreEqual(UserManagementMessages.StoreUnavailable, viewModel.MessageText);
        Assert.AreEqual("Samantha", viewModel.FirstName, "A failed write does not blank the page (FR-104).");
        Assert.AreEqual("6230", viewModel.EmployeeNumber);
        Assert.IsTrue(viewModel.HasUnsavedChanges, "The reader's work is still unsaved, so it is still theirs.");
    }

    [TestMethod]
    public async Task ASaveThatSucceeds_WritesOneCorrection_AndRebasesTheFormOnTheStore()
    {
        var service = new FakeUserManagementService(Account(OtherPersonId, "slead", "Sam Lead", "setup_lead", 60));
        var viewModel = Build(service: service, readerRoleCode: "setup_lead");
        await viewModel.InitializeAsync(OtherPersonId);

        viewModel.FirstName = "Samantha";
        await viewModel.SaveAsync();

        Assert.AreEqual(1, service.Updates, "One press writes one correction.");
        Assert.AreEqual("Samantha", viewModel.FirstName, "The form shows what the store now holds.");
        Assert.IsFalse(
            viewModel.HasUnsavedChanges,
            "After a successful save the form is re-based on the store, so nothing is left looking pending.");
    }

    [TestMethod]
    public async Task TheSignInName_IsCorrectedThroughThisSamePage()
    {
        var service = new FakeUserManagementService(Account(OtherPersonId, "slead", "Sam Lead", "setup_lead", 60));
        var viewModel = Build(service: service, readerRoleCode: "setup_lead");
        await viewModel.InitializeAsync(OtherPersonId);

        Assert.IsTrue(viewModel.IsEditable, "The sign-in name is corrected here rather than on a screen of its own.");
        Assert.IsFalse(viewModel.IsSelfRenameUnavailable, "This is somebody else's account, so its name may be changed.");

        viewModel.Username = "samantha.lead";
        await viewModel.SaveAsync();

        Assert.AreEqual(
            "samantha.lead",
            service.LastWrittenUsername,
            "The page sends the sign-in name it was given; the upper-case stored form is the service's one job, and is proved in T072 against the live store.");
    }

    [TestMethod]
    public async Task TheReset_IsNeitherOfferedNorAllowed_WithoutItsOwnPermission()
    {
        var service = new FakeUserManagementService(Account(OtherPersonId, "slead", "Sam Lead", "setup_lead", 60));
        var viewModel = Build(
            service: service,
            readerRoleCode: "setup_lead",
            permissionKeys: [PermissionKeys.AdminUsers]);

        await viewModel.InitializeAsync(OtherPersonId);

        Assert.IsFalse(viewModel.CanResetPassword, "A reader without the key is never offered the reset (FR-117).");

        await viewModel.ResetPasswordAsync();

        Assert.AreEqual(0, service.Resets, "And the action is refused as well as absent from the control.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(viewModel.MessageText));
    }

    [TestMethod]
    public async Task TheReset_WithItsOwnPermission_IssuesAFourDigitCredential_ThatIsDroppedWhenDismissed()
    {
        var viewModel = Build(
            account: Account(OtherPersonId, "slead", "Sam Lead", "setup_lead", 60),
            readerRoleCode: "setup_lead");

        await viewModel.InitializeAsync(OtherPersonId);

        Assert.IsTrue(viewModel.CanResetPassword, "The reader holds the key, so the control is offered.");

        await viewModel.ResetPasswordAsync();

        Assert.IsNotNull(viewModel.IssuedCredential);
        Assert.AreEqual(4, viewModel.IssuedCredential!.Pin.Length, "The credential is exactly four digits.");
        Assert.AreEqual("Sam Lead", viewModel.IssuedCredential.PersonName);
        Assert.AreEqual("SLEAD", viewModel.IssuedCredential.SignInName);

        viewModel.DismissIssuedCredential();
        Assert.IsNull(viewModel.IssuedCredential, "After the window closes the credential is held nowhere (SC-007).");
    }

    [TestMethod]
    public async Task AStoreRefusal_ReachesTheReaderInItsOwnWords()
    {
        var service = new FakeUserManagementService(Account(OtherPersonId, "slead", "Sam Lead", "setup_lead", 60))
        {
            UpdateResult = UserManagementResult.Failed(
                UserManagementOutcomeKind.SelfDeactivateDenied,
                UserManagementMessages.SelfDeactivateDeniedKey,
                UserManagementMessages.SelfDeactivateDenied),
        };

        var viewModel = Build(service: service, readerRoleCode: "setup_lead");
        await viewModel.InitializeAsync(OtherPersonId);

        await viewModel.DeactivateAsync();

        Assert.AreEqual(
            UserManagementMessages.SelfDeactivateDenied,
            viewModel.MessageText,
            "The store's own refusal token is reported in the words the contract pins (FR-026).");
    }

    [TestMethod]
    public async Task AnIdTheStoreDoesNotHold_IsStatedRatherThanShownAsABlankForm()
    {
        var viewModel = Build(readerRoleCode: "setup_lead", service: new FakeUserManagementService(account: null));

        await viewModel.InitializeAsync(OtherPersonId);

        Assert.IsTrue(viewModel.IsMissing, "A blank form would invite a write against a person who is not there.");
        Assert.IsFalse(viewModel.IsEditable);
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

        var document = System.Xml.Linq.XDocument.Load(path);
        var entry = document.Descendants("data").FirstOrDefault(data => data.Attribute("name")?.Value == resourceKey);

        Assert.IsNotNull(entry, $"'{resourceKey}' has no entry, so the sentence it names would never be shown.");

        return entry!.Element("value")?.Value ?? string.Empty;
    }

    private static UserAccount Account(long userId, string username, string displayName, string roleCode, int roleRank)
    {
        var parts = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return new UserAccount(
            userId,
            $"public-{userId}",
            username.ToUpperInvariant(),
            parts[0],
            parts.Length > 1 ? parts[^1] : parts[0],
            displayName,
            "6229",
            roleCode,
            roleCode,
            roleRank,
            IsActive: true,
            TemporaryCredentialFailedAttempts: 0);
    }

    private static EditUserViewModel Build(
        UserAccount? account = null,
        FakeUserManagementService? service = null,
        string readerRoleCode = "developer",
        string[]? permissionKeys = null)
    {
        var state = new StartupState
        {
            UserId = SignedInUserId,
            Username = "JSMITH",
            EmployeeName = "Jane Smith",
            CurrentRoleCode = readerRoleCode,
        };

        var held = permissionKeys ?? [PermissionKeys.AdminUsers, PermissionKeys.AdminResetPassword];

        return new EditUserViewModel(
            service ?? new FakeUserManagementService(account ?? Account(OtherPersonId, "slead", "Sam Lead", "setup_lead", 60)),
            new CatalogueStub(),
            PermissionStub.Holding(held),
            new RecordingNavigationService(),
            state);
    }

    /// <summary>Answers from one account, and records every write, so a refused action is provably not one.</summary>
    private sealed class FakeUserManagementService : IUserManagementService
    {
        private UserAccount? _account;

        internal FakeUserManagementService(UserAccount? account) => _account = account;

        internal UserManagementResult UpdateResult { get; set; } = UserManagementResult.Succeeded();

        internal int Updates { get; private set; }

        internal int Resets { get; private set; }

        internal string LastWrittenUsername { get; private set; } = string.Empty;

        public Task<IReadOnlyList<UserRosterRow>> SearchAsync(string? searchText, string? roleCode, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<UserRosterRow>>([]);

        public Task<UserAccount?> GetAsync(long userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_account is not null && _account.UserId == userId ? _account : null);

        public Task<UserManagementResult> CreateAsync(UserAccountEdit edit, CancellationToken cancellationToken = default) =>
            Task.FromResult(UserManagementResult.Succeeded("1234"));

        public Task<UserManagementResult> UpdateAsync(long userId, UserAccountEdit edit, CancellationToken cancellationToken = default)
        {
            Updates++;
            LastWrittenUsername = edit.Username;

            if (UpdateResult.IsSuccess && _account is not null)
            {
                // A store that accepted the write holds what it was sent, which is what makes the re-base a real
                // read of the store's state rather than a value the test keeps in step by hand.
                _account = _account with
                {
                    UsernameNormalized = edit.Username.ToUpperInvariant(),
                    FirstName = edit.FirstName,
                    LastName = edit.LastName,
                    DisplayName = $"{edit.FirstName} {edit.LastName}".Trim(),
                    EmployeeIdentifier = edit.EmployeeIdentifier,
                    RoleCode = edit.RoleCode,
                    IsActive = edit.IsActive,
                };
            }

            return Task.FromResult(UpdateResult);
        }

        public Task<UserManagementResult> ResetPasswordAsync(long userId, CancellationToken cancellationToken = default)
        {
            Resets++;
            return Task.FromResult(UserManagementResult.Succeeded("5678"));
        }

        public Task RecordTemporaryCredentialAttemptAsync(long userId, bool wasSuccessful, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    /// <summary>The catalogue, at the rungs the shipped seed gives them.</summary>
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
