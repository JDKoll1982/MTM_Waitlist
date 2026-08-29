using CommunityToolkit.Mvvm.Input;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Startup.Models;
using MTM_Waitlist.Module_Startup.ViewModels;
using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Tests.ViewModels;

[TestClass]
public sealed class LoginViewModelTests
{
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

        public Task<DateTimeOffset?> ReadServerTimeUtcAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<DateTimeOffset?>(null);
        }

        public Task<StartupSessionSnapshot> ReadSessionSnapshotAsync(string username, string hostnameNormalized, string macAddressNormalized, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new StartupSessionSnapshot());
        }

        public Task<StartupCredentialCheckResult> CheckCredentialsAsync(string username, string password, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CheckCredentialsResult);
        }

        public Task<bool> UpdatePasswordAsync(long userId, string newPassword, CancellationToken cancellationToken = default)
        {
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
