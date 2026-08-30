using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Startup.ViewModels;

namespace MTM_Waitlist.Tests.ViewModels;

[TestClass]
public sealed class SplashViewModelTests
{
    [TestMethod]
    public async Task StartAsync_WhenDatabaseFailureBlocked_HidesResetActionAndShowsManualActionsAsync()
    {
        var coordinator = new RecordingStartupCoordinator(new[]
        {
            StartupResult.Blocked("Could not validate startup session from the database. Try again.")
        });

        var viewModel = CreateViewModel(coordinator);

        await viewModel.StartAsync();

        Assert.IsFalse(viewModel.IsBusy);
        Assert.IsTrue(viewModel.ShowActions);
        Assert.IsFalse(viewModel.ShowResetToDefaultsAction);
    }

    [TestMethod]
    public async Task RetryAsync_AfterDatabaseFailure_RerunsDatabasePhaseOnlyAsync()
    {
        var coordinator = new RecordingStartupCoordinator(new[]
        {
            StartupResult.Blocked("Could not validate startup session from the database. Try again."),
            StartupResult.Blocked("Could not validate startup session from the database. Try again.")
        });

        var viewModel = CreateViewModel(coordinator);

        await viewModel.StartAsync();
        await viewModel.RetryCommand.ExecuteAsync(null);

        Assert.AreEqual(2, coordinator.RetryDatabasePhaseOnlyFlags.Count);
        Assert.IsFalse(coordinator.RetryDatabasePhaseOnlyFlags[0]);
        Assert.IsTrue(coordinator.RetryDatabasePhaseOnlyFlags[1]);
    }

    private static SplashViewModel CreateViewModel(RecordingStartupCoordinator coordinator)
    {
        return new SplashViewModel(
            coordinator,
            new NoOpStartupRecoveryService(),
            new NoOpLocalSettingsService(),
            new NoOpNavigationService(),
            new NoOpStartupShellStateService(),
            new NoOpAppLifecycleService(),
            new StartupState());
    }

    private sealed class RecordingStartupCoordinator : IStartupCoordinator
    {
        private readonly Queue<StartupResult> _results;

        public RecordingStartupCoordinator(IEnumerable<StartupResult> results)
        {
            _results = new Queue<StartupResult>(results);
        }

        public List<bool> RetryDatabasePhaseOnlyFlags { get; } = new();

        public Task<StartupResult> RunAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default, bool retryDatabasePhaseOnly = false)
        {
            RetryDatabasePhaseOnlyFlags.Add(retryDatabasePhaseOnly);
            var result = _results.Count > 0
                ? _results.Dequeue()
                : StartupResult.Blocked("Could not validate startup session from the database. Try again.");
            return Task.FromResult(result);
        }
    }

    private sealed class NoOpStartupRecoveryService : IStartupRecoveryService
    {
        public Task ResetSettingAsync(string key, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task ResetToDefaultsAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task CorruptAndRestartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class NoOpLocalSettingsService : ILocalSettingsService
    {
        public Task<T?> ReadSettingAsync<T>(string key) => Task.FromResult(default(T));

        public Task SaveSettingAsync<T>(string key, T value) => Task.CompletedTask;

        public Task ResetSettingAsync(string key, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task ResetAsync() => Task.CompletedTask;

        public Task CorruptForTestAsync() => Task.CompletedTask;
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

        public bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false) => true;

        public bool GoBack() => false;

        public void SetListDataItemForNextConnectedAnimation(object item)
        {
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

        public Task EnterMainModeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
