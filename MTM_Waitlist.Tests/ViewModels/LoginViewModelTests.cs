using CommunityToolkit.Mvvm.Input;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Startup.Models;
using MTM_Waitlist.Module_Startup.ViewModels;

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
        var viewModel = new LoginViewModel(
            new NoOpStartupSessionRepository(),
            registrationService,
            new NoOpLocalSettingsService(),
            new NoOpStartupShellStateService(),
            new NoOpNavigationService(),
            startupState);

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

        var viewModel = new LoginViewModel(
            new NoOpStartupSessionRepository(),
            new RecordingStartupRegistrationService(),
            localSettingsService,
            new NoOpStartupShellStateService(),
            new NoOpNavigationService(),
            startupState);

        await viewModel.InitializeAsync();

        Assert.IsTrue(viewModel.RememberPassword);
        Assert.AreEqual("jkoll", viewModel.Username);
        Assert.AreEqual("pw-1234", viewModel.Password);
    }

    private sealed class NoOpStartupSessionRepository : IStartupSessionRepository
    {
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
            return Task.FromResult(StartupCredentialCheckResult.Failed());
        }

        public Task<bool> UpdatePasswordAsync(long userId, string newPassword, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
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
