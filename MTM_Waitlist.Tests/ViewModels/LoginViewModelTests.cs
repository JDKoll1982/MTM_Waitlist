using CommunityToolkit.Mvvm.Input;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Startup.ViewModels;
using MTM_Waitlist.Module_Waitlist.ViewModels;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.ViewModels;

[TestClass]
public sealed class LoginViewModelTests
{
    private const string LoginPageXamlPath = "Module_Startup/Views/LoginPage.xaml";
    [TestMethod]
    public async Task NewUserAsyncCommand_SubmitsRequestAndUpdatesStateAsync()
    {
        var startupState = new StartupState
        {
            Username = "masked.user.001",
            HostnameNormalized = "dev-workstation-001",
            MacAddressNormalized = "00-00-00-00-00-01",
            RequireNewUserAction = true,
            LoginHint = "This workstation is not registered. Choose New User to request access."
        };

        var registrationService = new RecordingStartupRegistrationService();
        var viewModel = CreateViewModel(startupState, registrationService);

        await viewModel.NewUserCommand.ExecuteAsync(null);

        Assert.AreEqual(1, registrationService.SubmitCallCount);
        Assert.IsFalse(viewModel.ShowNewUserAction);
        Assert.IsFalse(startupState.RequireNewUserAction);
        Assert.AreEqual("New User request saved. A supervisor can finish registration from startup controls.", viewModel.LoginHint);
    }

    [TestMethod]
    public async Task InitializeAsync_WhenRememberedCredentialsExist_LoadsThemAsync()
    {
        var startupState = new StartupState
        {
            Username = "johnk"
        };

        var localSettingsService = new NoOpLocalSettingsService();
        await localSettingsService.SaveSettingAsync("Login.RememberPassword", true);
        await localSettingsService.SaveSettingAsync("Login.RememberedUsername", "jkoll");
        await localSettingsService.SaveSettingAsync("Login.RememberedPassword", "pw-1234");

        var viewModel = CreateViewModel(startupState, localSettingsService: localSettingsService);

        await viewModel.InitializeAsync();

        Assert.IsTrue(viewModel.RememberPassword);
        Assert.AreEqual("jkoll", viewModel.Username);
        Assert.AreEqual("pw-1234", viewModel.Password);
    }

    [TestMethod]
    public async Task SignInAsync_WhenComputerMissing_SetsGateStateWithoutNavigatingAsync()
    {
        var startupState = SignedInState();
        var gateService = new FakeComputerGateService { CheckResult = new ComputerGateCheck(ComputerGateStatus.Missing) };
        var navigation = new RecordingNavigationService();
        var window = new RecordingStartupWindowService();
        var viewModel = CreateViewModel(startupState, gateService: gateService, navigationService: navigation, windowService: window);
        viewModel.Username = "johnk";
        viewModel.Password = "pw-1234";

        await viewModel.SignInCommand.ExecuteAsync(null);

        Assert.AreEqual(ComputerGateStatus.Missing, viewModel.ComputerGateState);
        Assert.AreEqual(0, navigation.NavigateToCalls.Count);
        Assert.AreEqual(0, window.ShowMainWindowCallCount);
    }

    [TestMethod]
    public async Task SignInAsync_WhenComputerRenamedMachine_PrefillsExistingDisplayNameAsync()
    {
        var startupState = SignedInState();
        var existing = new ComputerRecord
        {
            Id = 7,
            ComputerName = "old-host",
            DisplayName = "Old Computer Name",
            Description = "old description",
            MacAddressNormalized = "00-11-22-33-44-55"
        };
        var gateService = new FakeComputerGateService { CheckResult = new ComputerGateCheck(ComputerGateStatus.RenamedMachine, existing) };
        var navigation = new RecordingNavigationService();
        var viewModel = CreateViewModel(startupState, gateService: gateService, navigationService: navigation);
        viewModel.Username = "johnk";
        viewModel.Password = "pw-1234";

        await viewModel.SignInCommand.ExecuteAsync(null);

        Assert.AreEqual(ComputerGateStatus.RenamedMachine, viewModel.ComputerGateState);
        Assert.AreEqual("Old Computer Name", viewModel.ComputerDisplayName);
        Assert.AreEqual("old description", viewModel.ComputerDescription);
        Assert.AreEqual(0, navigation.NavigateToCalls.Count);
    }

    [TestMethod]
    public async Task SignInAsync_WhenDatabaseUnavailable_SetsDatabaseUnavailableStateAsync()
    {
        var startupState = SignedInState();
        var gateService = new FakeComputerGateService { CheckResult = new ComputerGateCheck(ComputerGateStatus.DatabaseUnavailable) };
        var navigation = new RecordingNavigationService();
        var viewModel = CreateViewModel(startupState, gateService: gateService, navigationService: navigation);
        viewModel.Username = "johnk";
        viewModel.Password = "pw-1234";

        await viewModel.SignInCommand.ExecuteAsync(null);

        Assert.AreEqual(ComputerGateStatus.DatabaseUnavailable, viewModel.ComputerGateState);
        Assert.AreEqual(0, navigation.NavigateToCalls.Count);
    }

    [TestMethod]
    public async Task SignInAsync_WhenComputerRegistered_NavigatesToShellAsync()
    {
        var startupState = SignedInState();
        var gateService = new FakeComputerGateService { CheckResult = new ComputerGateCheck(ComputerGateStatus.Registered) };
        var navigation = new RecordingNavigationService();
        var window = new RecordingStartupWindowService();
        var viewModel = CreateViewModel(startupState, gateService: gateService, navigationService: navigation, windowService: window);
        viewModel.Username = "johnk";
        viewModel.Password = "pw-1234";

        await viewModel.SignInCommand.ExecuteAsync(null);

        Assert.AreEqual(1, navigation.NavigateToCalls.Count);
        Assert.AreEqual(typeof(WaitlistViewViewModel).FullName, navigation.NavigateToCalls[0]);
        Assert.AreEqual(1, window.ShowMainWindowCallCount);
    }

    [TestMethod]
    public async Task SignInAsync_WhenComputerMissing_DoesNotPersistSessionTokenAsync()
    {
        // Regression guard: the local session token must NOT be persisted when the
        // computer gate blocks on the Register Computer screen. Otherwise a user who
        // closes the app during the gate would keep a valid session and bypass the
        // gate on the next launch. The token may only be written in
        // FinishLoginNavigationAsync, which is reached after the gate passes.
        var startupState = SignedInState();
        var gateService = new FakeComputerGateService { CheckResult = new ComputerGateCheck(ComputerGateStatus.Missing) };
        var localSettingsService = new NoOpLocalSettingsService();
        var navigation = new RecordingNavigationService();
        var viewModel = CreateViewModel(startupState, gateService: gateService, localSettingsService: localSettingsService, navigationService: navigation);
        viewModel.Username = "johnk";
        viewModel.Password = "pw-1234";

        await viewModel.SignInCommand.ExecuteAsync(null);

        Assert.AreEqual(ComputerGateStatus.Missing, viewModel.ComputerGateState);
        Assert.AreEqual(0, navigation.NavigateToCalls.Count);

        var token = await localSettingsService.ReadSettingAsync<string>("Startup.Session.Token");
        var expiry = await localSettingsService.ReadSettingAsync<string>("Startup.Session.ExpiresUtc");
        Assert.IsTrue(string.IsNullOrEmpty(token), "Session token must not be persisted while the computer gate is pending.");
        Assert.IsTrue(string.IsNullOrEmpty(expiry), "Session expiry must not be persisted while the computer gate is pending.");
    }

    [TestMethod]
    public async Task SignInAsync_WhenComputerRegistered_PersistsSessionTokenAsync()
    {
        // Confirms the session token IS persisted once the gate passes (Registered
        // routes through FinishLoginNavigationAsync). Guards against over-correction
        // that would break remembered sessions.
        var startupState = SignedInState();
        var gateService = new FakeComputerGateService { CheckResult = new ComputerGateCheck(ComputerGateStatus.Registered) };
        var localSettingsService = new NoOpLocalSettingsService();
        var viewModel = CreateViewModel(startupState, gateService: gateService, localSettingsService: localSettingsService);
        viewModel.Username = "johnk";
        viewModel.Password = "pw-1234";

        await viewModel.SignInCommand.ExecuteAsync(null);

        var token = await localSettingsService.ReadSettingAsync<string>("Startup.Session.Token");
        var expiry = await localSettingsService.ReadSettingAsync<string>("Startup.Session.ExpiresUtc");
        Assert.IsFalse(string.IsNullOrEmpty(token), "Session token should be persisted after the gate passes.");
        Assert.IsFalse(string.IsNullOrEmpty(expiry), "Session expiry should be persisted after the gate passes.");
    }

    [TestMethod]
    public async Task CompleteComputerGateAsync_WithEmptyDisplayName_ReturnsFalseWithoutUpsertAsync()
    {
        var startupState = SignedInState();
        var gateService = new FakeComputerGateService { CheckResult = new ComputerGateCheck(ComputerGateStatus.Missing) };
        var registry = new RecordingComputerRegistryService();
        var viewModel = CreateViewModel(startupState, gateService: gateService, registryService: registry);

        viewModel.ComputerDisplayName = string.Empty;

        var saved = await viewModel.CompleteComputerGateAsync();

        Assert.IsFalse(saved);
        Assert.AreEqual("Display name is required.", viewModel.ComputerGateError);
        Assert.AreEqual(0, registry.UpsertCount);
    }

    [TestMethod]
    public async Task CompleteComputerGateAsync_WhenMissing_InsertsAndNavigatesAsync()
    {
        var startupState = SignedInState();
        var gateService = new FakeComputerGateService { CheckResult = new ComputerGateCheck(ComputerGateStatus.Missing) };
        var registry = new RecordingComputerRegistryService();
        var navigation = new RecordingNavigationService();
        var window = new RecordingStartupWindowService();
        var viewModel = CreateViewModel(startupState, gateService: gateService, registryService: registry, navigationService: navigation, windowService: window);

        viewModel.ComputerDisplayName = "John's Computer";
        viewModel.ComputerDescription = "Press room PC";

        var saved = await viewModel.CompleteComputerGateAsync();

        Assert.IsTrue(saved);
        Assert.AreEqual(1, registry.UpsertCount);
        Assert.AreEqual(0, registry.UpdateByMacCount);
        Assert.AreEqual("John's Computer", registry.LastUpsertDisplayName);
        Assert.AreEqual(1, navigation.NavigateToCalls.Count);
        Assert.AreEqual(1, window.ShowMainWindowCallCount);
    }

    [TestMethod]
    public async Task CompleteComputerGateAsync_WhenRenamedMachine_UpdatesByMacAndNavigatesAsync()
    {
        var startupState = SignedInState();
        var existing = new ComputerRecord { Id = 7, ComputerName = "old-host", DisplayName = "Old Name", MacAddressNormalized = "00-11-22-33-44-55" };
        var gateService = new FakeComputerGateService { CheckResult = new ComputerGateCheck(ComputerGateStatus.RenamedMachine, existing) };
        var registry = new RecordingComputerRegistryService();
        var navigation = new RecordingNavigationService();
        var viewModel = CreateViewModel(startupState, gateService: gateService, registryService: registry, navigationService: navigation);
        viewModel.Username = "johnk";
        viewModel.Password = "pw-1234";

        // Drive the gate into the RenamedMachine state first so _pendingGateCheck is set.
        await viewModel.SignInCommand.ExecuteAsync(null);
        Assert.AreEqual(ComputerGateStatus.RenamedMachine, viewModel.ComputerGateState);
        Assert.AreEqual("Old Name", viewModel.ComputerDisplayName);

        viewModel.ComputerDisplayName = "New Computer Name";

        var saved = await viewModel.CompleteComputerGateAsync();

        Assert.IsTrue(saved);
        Assert.AreEqual(1, registry.UpdateByMacCount);
        Assert.AreEqual(0, registry.UpsertCount);
        Assert.AreEqual(1, navigation.NavigateToCalls.Count);
    }

    [TestMethod]
    public async Task CompleteComputerGateAsync_WhenUpsertThrowsDuplicate_ReturnsFalseAndSetsErrorAsync()
    {
        var startupState = SignedInState();
        var gateService = new FakeComputerGateService { CheckResult = new ComputerGateCheck(ComputerGateStatus.Missing) };
        var registry = new RecordingComputerRegistryService { ThrowDuplicateOnUpsert = true };
        var viewModel = CreateViewModel(startupState, gateService: gateService, registryService: registry);

        viewModel.ComputerDisplayName = "John's Computer";

        var saved = await viewModel.CompleteComputerGateAsync();

        Assert.IsFalse(saved);
        Assert.AreEqual("That display name is already in use. Choose a different one.", viewModel.ComputerGateError);
    }

    [TestMethod]
    public async Task RetryComputerGateAsync_WhenNowRegistered_ReturnsRegisteredAndNavigatesAsync()
    {
        var startupState = SignedInState();
        var gateService = new FakeComputerGateService { CheckResult = new ComputerGateCheck(ComputerGateStatus.Registered) };
        var navigation = new RecordingNavigationService();
        var window = new RecordingStartupWindowService();
        var viewModel = CreateViewModel(startupState, gateService: gateService, navigationService: navigation, windowService: window);

        var status = await viewModel.RetryComputerGateAsync();

        Assert.AreEqual(ComputerGateStatus.Registered, status);
        Assert.AreEqual(1, navigation.NavigateToCalls.Count);
        Assert.AreEqual(1, window.ShowMainWindowCallCount);
    }

    [TestMethod]
    public async Task CancelAsync_ExitsApplicationAsync()
    {
        var startupState = new StartupState();
        var window = new RecordingStartupWindowService();
        var viewModel = CreateViewModel(startupState, windowService: window);

        await viewModel.CancelCommand.ExecuteAsync(null);

        Assert.AreEqual(1, window.ExitCallCount);
    }

    [TestMethod]
    public void Constructor_WhenStartupRequiresAPasswordChange_StillShowsTheSignInForm()
    {
        // A pending change is not proof of identity. Startup learns it from a read that carries no credential
        // material, so the person must still present the temporary credential before the change panel opens;
        // otherwise anyone at this workstation could set the password, and the attempt limit could never bite
        // (decision 9, decision 14, FR-029).
        var startupState = SignedInState();
        startupState.RequirePasswordChange = true;
        startupState.PasswordChangeUserId = 42;

        var viewModel = CreateViewModel(startupState);

        Assert.IsFalse(viewModel.ShowPasswordChangePrompt, "The change panel must wait for the temporary credential.");
        Assert.IsTrue(viewModel.ShowSignInForm, "The sign-in form is where the temporary credential is entered.");
        Assert.IsTrue(
            viewModel.LoginHint.Contains("temporary password", StringComparison.Ordinal),
            "The person is told to sign in with the temporary password.");
    }

    [TestMethod]
    public void Constructor_WhenNoPasswordChangeIsRequired_ShowsTheSignInFormOnly()
    {
        var viewModel = CreateViewModel(SignedInState());

        Assert.IsFalse(viewModel.ShowPasswordChangePrompt);
        Assert.IsTrue(viewModel.ShowSignInForm);
    }

    [TestMethod]
    public async Task ChangePasswordAsync_WhenTheTemporaryCredentialWasAccepted_UpdatesThatAccountAsync()
    {
        // The account the change targets comes from the credential that was just accepted, never from anything
        // startup inferred, so an update can only land on the person who proved they hold the credential.
        var repository = new RecordingStartupSessionRepository
        {
            CheckCredentialsResult = StartupCredentialCheckResult.Success(42, "Developer", requiresPasswordChange: true),
        };
        var viewModel = CreateViewModel(
            SignedInState(),
            gateService: new FakeComputerGateService { CheckResult = new ComputerGateCheck(ComputerGateStatus.Registered) },
            navigationService: new RecordingNavigationService(),
            windowService: new RecordingStartupWindowService(),
            sessionRepository: repository);
        viewModel.Username = "johnk";
        viewModel.Password = "4821";

        await viewModel.SignInCommand.ExecuteAsync(null);

        Assert.IsTrue(viewModel.ShowPasswordChangePrompt, "An accepted temporary credential opens the change panel.");

        viewModel.NewPassword = "pw-4321";
        viewModel.ConfirmPassword = "pw-4321";

        await viewModel.ChangePasswordCommand.ExecuteAsync(null);

        Assert.AreEqual(42, repository.LastUpdatedUserId, "The update must target the account that signed in.");
    }

    // ── The remaining-attempts line (FR-040, FR-041, FR-042, FR-044) ────────────────────────────────────

    [TestMethod]
    public async Task SignInAsync_AfterTheFirstWrongTryOnATemporaryCredential_ShowsHowManyAttemptsRemain()
    {
        var repository = new RecordingStartupSessionRepository
        {
            CheckCredentialsResult = StartupCredentialCheckResult.Failed() with
            {
                HoldsTemporaryCredential = true,
                TemporaryCredentialFailedAttempts = 1,
            },
        };
        var viewModel = CreateViewModel(SignedInState(), sessionRepository: repository);
        viewModel.Username = "johnk";
        viewModel.Password = "1234";

        await viewModel.SignInCommand.ExecuteAsync(null);

        Assert.IsTrue(viewModel.ShowRemainingAttempts, "The line appears once an attempt has failed (FR-041).");
        StringAssert.Contains(viewModel.RemainingAttemptsMessage, "4", "Four of the five attempts remain after one failure (FR-041).");
    }

    [TestMethod]
    public async Task SignInAsync_BeforeAnyAttemptHasFailed_ShowsNoAttemptLine()
    {
        var viewModel = CreateViewModel(
            SignedInState(),
            gateService: new FakeComputerGateService { CheckResult = new ComputerGateCheck(ComputerGateStatus.Registered) },
            navigationService: new RecordingNavigationService(),
            windowService: new RecordingStartupWindowService());
        viewModel.Username = "johnk";
        viewModel.Password = "pw-1234";

        await viewModel.SignInCommand.ExecuteAsync(null);

        Assert.IsFalse(viewModel.ShowRemainingAttempts, "Nothing is shown before an attempt has failed (FR-041).");
        Assert.AreEqual(string.Empty, viewModel.RemainingAttemptsMessage);
    }

    [TestMethod]
    public async Task SignInAsync_WhenTheTemporaryCredentialHasStoppedBeingAccepted_SaysAFreshResetIsNeeded()
    {
        var repository = new RecordingStartupSessionRepository
        {
            CheckCredentialsResult = StartupCredentialCheckResult.Failed() with
            {
                HoldsTemporaryCredential = true,
                TemporaryCredentialFailedAttempts = 5,
                TemporaryCredentialAttemptLimitReached = true,
            },
        };
        var viewModel = CreateViewModel(SignedInState(), sessionRepository: repository);
        viewModel.Username = "johnk";
        viewModel.Password = "1234";

        await viewModel.SignInCommand.ExecuteAsync(null);

        Assert.IsTrue(viewModel.ShowRemainingAttempts);
        StringAssert.Contains(
            viewModel.RemainingAttemptsMessage,
            "reset",
            "The person must be told that someone entitled has to reset it (FR-044).");
        StringAssert.Contains(viewModel.RemainingAttemptsMessage, "fresh", "A fresh credential is what they are waiting for (FR-044).");
    }

    [TestMethod]
    public async Task SignInAsync_WhenAnAccountWithoutATemporaryCredentialFails_BehavesExactlyAsBefore()
    {
        // An ordinary account and a sign-in name that does not exist both arrive here as a plain failure, and an
        // ordinary sign-in has no attempt limit at all (FR-040, FR-042).
        var repository = new RecordingStartupSessionRepository { CheckCredentialsResult = StartupCredentialCheckResult.Failed() };
        var viewModel = CreateViewModel(SignedInState(), sessionRepository: repository);
        viewModel.Username = "nobody";
        viewModel.Password = "pw-1234";

        await viewModel.SignInCommand.ExecuteAsync(null);

        Assert.IsFalse(viewModel.ShowRemainingAttempts, "The limit must not be visible for an account that holds no temporary credential.");
        Assert.AreEqual(string.Empty, viewModel.RemainingAttemptsMessage);
        Assert.AreEqual("Sign-in failed. Check your credentials and try again.", viewModel.LoginHint);
    }

    [TestMethod]
    public void TheSignInPage_ShowsTheAttemptLine_AndOffersNoReset()
    {
        var xaml = ReadSource(LoginPageXamlPath);

        StringAssert.Contains(
            xaml,
            "x:Load=\"{x:Bind ViewModel.ShowRemainingAttempts, Mode=OneWay}\"",
            "The line is shown only when there is something to say (FR-041).");
        StringAssert.Contains(xaml, "AutomationProperties.AutomationId=\"LoginPage_RemainingAttemptsText\"");
        StringAssert.Contains(
            xaml,
            "{x:Bind ViewModel.RemainingAttemptsMessage, Mode=OneWay}",
            "The line reads the view model's message.");

        Assert.IsFalse(
            xaml.Contains("reset", StringComparison.OrdinalIgnoreCase),
            "No reset is offered anywhere on the sign-in screen, and a person cannot reset their own password (FR-028).");
    }

    private static string ReadSource(string relativePath)
    {
        var path = Path.Combine(RepositoryPatternScan.FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.IsTrue(File.Exists(path), $"The artifact is missing: {path}");

        return File.ReadAllText(path);
    }

    private static StartupState SignedInState()
    {
        return new StartupState
        {
            Username = "johnk",
            HostnameNormalized = "johnspc",
            MacAddressNormalized = "d8-43-ae-47-d0-d6"
        };
    }

    private static LoginViewModel CreateViewModel(
        StartupState startupState,
        IStartupRegistrationService? registrationService = null,
        ILocalSettingsService? localSettingsService = null,
        IStartupShellStateService? startupShellStateService = null,
        INavigationService? navigationService = null,
        IComputerGateService? gateService = null,
        IComputerRegistryService? registryService = null,
        IStartupWindowService? windowService = null,
        IStartupSessionRepository? sessionRepository = null)
    {
        return new LoginViewModel(
            sessionRepository ?? new RecordingStartupSessionRepository(),
            registrationService ?? new RecordingStartupRegistrationService(),
            localSettingsService ?? new NoOpLocalSettingsService(),
            startupShellStateService ?? new NoOpStartupShellStateService(),
            navigationService ?? new NoOpNavigationService(),
            gateService ?? new FakeComputerGateService(),
            registryService ?? new RecordingComputerRegistryService(),
            windowService ?? new RecordingStartupWindowService(),
            startupState);
    }

    private sealed class RecordingStartupSessionRepository : IStartupSessionRepository
    {
        public StartupCredentialCheckResult CheckCredentialsResult { get; set; } = StartupCredentialCheckResult.Success(1, "Developer", false);

        public StartupPasswordResetRequirement PasswordResetRequirement { get; set; } = StartupPasswordResetRequirement.None;

        public long LastUpdatedUserId { get; private set; }

        public Task<DateTimeOffset?> ReadServerTimeUtcAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<DateTimeOffset?>(null);
        }

        public Task<StartupSessionSnapshot> ReadSessionSnapshotAsync(string username, string hostnameNormalized, string macAddressNormalized, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new StartupSessionSnapshot());
        }

        public Task<StartupPasswordResetRequirement> ReadPasswordResetRequirementAsync(string username, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(PasswordResetRequirement);
        }

        public Task<StartupCredentialCheckResult> CheckCredentialsAsync(string username, string password, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CheckCredentialsResult);
        }

        public Task<bool> UpdatePasswordAsync(long userId, string newPassword, CancellationToken cancellationToken = default)
        {
            LastUpdatedUserId = userId;
            return Task.FromResult(true);
        }
    }

    private sealed class FakeComputerGateService : IComputerGateService
    {
        public ComputerGateCheck CheckResult { get; set; } = new ComputerGateCheck(ComputerGateStatus.Registered);

        public Task<ComputerGateCheck> CheckAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CheckResult);
        }
    }

    private sealed class RecordingComputerRegistryService : IComputerRegistryService
    {
        public int LookupCount { get; private set; }

        public int LookupByMacCount { get; private set; }

        public int UpsertCount { get; private set; }

        public int UpdateByMacCount { get; private set; }

        public string? LastUpsertDisplayName { get; private set; }

        public bool ThrowDuplicateOnUpsert { get; set; }

        public Task<ComputerRecord?> LookupComputerAsync(string computerName, string macAddressNormalized, CancellationToken cancellationToken = default)
        {
            LookupCount++;
            return Task.FromResult<ComputerRecord?>(null);
        }

        public Task<ComputerRecord?> LookupComputerByMacAsync(string macAddressNormalized, CancellationToken cancellationToken = default)
        {
            LookupByMacCount++;
            return Task.FromResult<ComputerRecord?>(null);
        }

        public Task<ComputerRecord> UpsertComputerAsync(string computerName, string hostnameNormalized, string macAddressNormalized, string displayName, string? description, CancellationToken cancellationToken = default)
        {
            if (ThrowDuplicateOnUpsert)
            {
                throw new Exception("Duplicate entry 'John''s Computer' for key 'uq_core_computers_registry_display_name'.");
            }

            UpsertCount++;
            LastUpsertDisplayName = displayName;
            return Task.FromResult(new ComputerRecord
            {
                Id = 1,
                ComputerName = computerName,
                DisplayName = displayName,
                Description = description ?? string.Empty,
                MacAddressNormalized = macAddressNormalized,
                IsRegistered = true
            });
        }

        public Task<ComputerRecord> UpdateComputerByMacAsync(string macAddressNormalized, string newComputerName, string hostnameNormalized, string displayName, string? description, CancellationToken cancellationToken = default)
        {
            UpdateByMacCount++;
            return Task.FromResult(new ComputerRecord
            {
                Id = 1,
                ComputerName = newComputerName,
                DisplayName = displayName,
                Description = description ?? string.Empty,
                MacAddressNormalized = macAddressNormalized,
                IsRegistered = true
            });
        }

        public Task<IReadOnlyList<ComputerRecord>> GetAllComputersAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<ComputerRecord>>(Array.Empty<ComputerRecord>());
        }

        public Task<ComputerRecord> UpdateComputerAsync(long id, string computerName, string hostnameNormalized, string macAddressNormalized, string displayName, string? description, bool isRegistered, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteComputerAsync(long id, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class RecordingStartupWindowService : IStartupWindowService
    {
        public int ShowMainWindowCallCount { get; private set; }

        public int ExitCallCount { get; private set; }

        public void ShowMainWindowAndCloseLoginWindow()
        {
            ShowMainWindowCallCount++;
        }

        public void Exit()
        {
            ExitCallCount++;
        }
    }

    private sealed class NoOpLocalSettingsService : ILocalSettingsService
    {
        private readonly Dictionary<string, object?> _settings = new();

        public Task<T?> ReadSettingAsync<T>(string key)
        {
            if (!_settings.TryGetValue(key, out var value) || value is null)
            {
                return Task.FromResult(default(T));
            }

            return Task.FromResult((T?)value);
        }

        public Task SaveSettingAsync<T>(string key, T value)
        {
            if (value is null)
            {
                _settings.Remove(key);
                return Task.CompletedTask;
            }

            _settings[key] = value;
            return Task.CompletedTask;
        }

        public Task ResetSettingAsync(string key, CancellationToken cancellationToken = default)
        {
            _settings.Remove(key);
            return Task.CompletedTask;
        }

        public Task ResetAsync()
        {
            _settings.Clear();
            return Task.CompletedTask;
        }

        public Task CorruptForTestAsync()
        {
            return Task.CompletedTask;
        }
    }

    private sealed class NoOpStartupShellStateService : IStartupShellStateService
    {
        public event EventHandler? StateChanged
        {
            add { }
            remove { }
        }

        public bool IsNavigationVisible => false;

        public void EnterSplashMode()
        {
        }

        public Task EnterMainModeAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class NoOpNavigationService : INavigationService
    {
        public event NavigatedEventHandler? Navigated
        {
            add { }
            remove { }
        }

        public bool CanGoBack => false;

        public Frame? Frame { get; set; }

        public bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false)
        {
            return true;
        }

        public bool GoBack()
        {
            return false;
        }

        public void SetListDataItemForNextConnectedAnimation(object item)
        {
        }
    }

    private sealed class RecordingNavigationService : INavigationService
    {
        public List<string> NavigateToCalls { get; } = new();

        public event NavigatedEventHandler? Navigated
        {
            add { }
            remove { }
        }

        public bool CanGoBack => false;

        public Frame? Frame { get; set; }

        public bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false)
        {
            NavigateToCalls.Add(pageKey);
            return true;
        }

        public bool GoBack()
        {
            return false;
        }

        public void SetListDataItemForNextConnectedAnimation(object item)
        {
        }
    }

    private sealed class RecordingStartupRegistrationService : IStartupRegistrationService
    {
        public int SubmitCallCount { get; private set; }

        public Task SubmitNewUserRequestAsync(StartupState startupState, CancellationToken cancellationToken = default)
        {
            SubmitCallCount++;
            return Task.CompletedTask;
        }
    }
}
