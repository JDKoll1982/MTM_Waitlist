using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Contracts.Services;
using MTM_Waitlist.Contracts.ViewModels;
using MTM_Waitlist.Helpers;
using MTM_Waitlist.Models;

namespace MTM_Waitlist.ViewModels;

public partial class SplashViewModel : ObservableRecipient, INavigationAware
{
    private const string LoggingDestinationRequiredPrefix = "Centralized logging destination is required.";
    private const string LoggingDestinationMissingPrefix = "Centralized logging destination is not configured.";
    private const string DatabaseFailurePrefix = "Could not validate startup session from the database.";

    private readonly IStartupCoordinator _startupCoordinator;
    private readonly IStartupRecoveryService _startupRecoveryService;
    private readonly ILocalSettingsService _localSettingsService;
    private readonly INavigationService _navigationService;
    private readonly IStartupShellStateService _startupShellStateService;
    private readonly StartupState _startupState;
    private bool _startupStarted;
    private string _statusText = "Starting application...";
    private bool _isBusy = true;
    private bool _showActions;
    private bool _showResetToDefaultsAction = true;
    private bool _lastBlockedWasDatabaseFailure;
    private bool _isDatabaseFailure;

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public bool ShowActions
    {
        get => _showActions;
        set => SetProperty(ref _showActions, value);
    }

    public bool ShowResetToDefaultsAction
    {
        get => _showResetToDefaultsAction;
        set => SetProperty(ref _showResetToDefaultsAction, value);
    }

    public bool IsDatabaseFailure
    {
        get => _isDatabaseFailure;
        set => SetProperty(ref _isDatabaseFailure, value);
    }

    public Func<Task<string?>>? LoggingDestinationPromptRequestedAsync { get; set; }

    public SplashViewModel(
        IStartupCoordinator startupCoordinator,
        IStartupRecoveryService startupRecoveryService,
        ILocalSettingsService localSettingsService,
        INavigationService navigationService,
        IStartupShellStateService startupShellStateService,
        StartupState startupState)
    {
        ArgumentNullException.ThrowIfNull(startupCoordinator);
        ArgumentNullException.ThrowIfNull(startupRecoveryService);
        ArgumentNullException.ThrowIfNull(localSettingsService);
        ArgumentNullException.ThrowIfNull(navigationService);
        ArgumentNullException.ThrowIfNull(startupShellStateService);
        ArgumentNullException.ThrowIfNull(startupState);

        _startupCoordinator = startupCoordinator;
        _startupRecoveryService = startupRecoveryService;
        _localSettingsService = localSettingsService;
        _navigationService = navigationService;
        _startupShellStateService = startupShellStateService;
        _startupState = startupState;

        StatusText = _startupState.StatusText;
        IsBusy = _startupState.IsBusy;
    }

    public async Task StartAsync()
    {
        StartupDebugLog.Info("SplashViewModel", "StartAsync invoked.");

        if (_startupStarted)
        {
            StartupDebugLog.Info("SplashViewModel", "Startup already started; StartAsync exits.");
            return;
        }

        _startupStarted = true;
        await RunStartupAsync();
    }

    public async void OnNavigatedTo(object parameter)
    {
        await StartAsync();
    }

    public void OnNavigatedFrom()
    {
    }

    [RelayCommand]
    private async Task RetryAsync()
    {
        StartupDebugLog.Info("SplashViewModel", "Retry command invoked.");
        IsBusy = true;
        ShowActions = false;
        ShowResetToDefaultsAction = true;
        StatusText = _lastBlockedWasDatabaseFailure
            ? "Retrying database connection checks..."
            : "Trying to repair one damaged setting...";
        UpdateState();
        await RunStartupAsync(_lastBlockedWasDatabaseFailure);
    }

    [RelayCommand]
    private async Task ResetToDefaultsAsync()
    {
        StartupDebugLog.Info("SplashViewModel", "ResetToDefaults command invoked.");
        IsBusy = true;
        ShowActions = false;
        StatusText = "Resetting all local settings...";
        UpdateState();

        try
        {
            await _startupRecoveryService.ResetToDefaultsAsync();
            StartupDebugLog.Info("SplashViewModel", "Local settings reset completed.");
            await RunStartupAsync();
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("SplashViewModel", ex, "ResetToDefaults failed.");
            IsBusy = false;
            ShowActions = true;
            StatusText = "Settings could not be reset. Try again or reset all local settings.";
            UpdateState();
        }
    }

    [RelayCommand]
    private void Exit()
    {
        App.Current.Exit();
    }

    private async Task RunStartupAsync(bool retryDatabasePhaseOnly = false)
    {
        StartupDebugLog.Info("SplashViewModel", "RunStartupAsync entered.");
        _startupShellStateService.EnterSplashMode();
        IsBusy = true;
        ShowActions = false;
        ShowResetToDefaultsAction = true;
        _lastBlockedWasDatabaseFailure = false;
        IsDatabaseFailure = false;
        StatusText = "Running startup checks...";
        UpdateState();

        StartupResult result;
        try
        {
            var progress = new Progress<string>(message =>
            {
                StatusText = message;
                UpdateState();
            });

            result = await _startupCoordinator.RunAsync(progress, default, retryDatabasePhaseOnly);
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("SplashViewModel", ex, "Startup coordinator threw an exception.");
            throw;
        }

        StartupDebugLog.Info(
            "SplashViewModel",
            $"Startup result received. Success={result.IsSuccess}, Blocked={result.IsBlocked}, Route={result.RouteTarget}, Status={result.StatusMessage}");

        StatusText = string.IsNullOrWhiteSpace(result.StatusMessage)
            ? "Startup completed."
            : result.StatusMessage;

        if (IsLoggingDestinationPromptRequired(result))
        {
            StatusText = "Before we continue, choose where startup logs should be saved.";
        }

        IsBusy = false;
        UpdateState();

        if (result.IsSuccess && !result.IsBlocked && !string.IsNullOrWhiteSpace(result.RouteTarget))
        {
            StartupDebugLog.Info("SplashViewModel", "Startup succeeded; transitioning to main mode.");
            try
            {
                await _startupShellStateService.EnterMainModeAsync();
                _navigationService.NavigateTo(result.RouteTarget, null, true);
                App.ShowMainWindowAndCloseSplash();
                return;
            }
            catch (Exception ex)
            {
                StartupDebugLog.Error("SplashViewModel", ex, "Transition to main mode failed.");
                throw;
            }
        }

        StartupDebugLog.Info("SplashViewModel", "Startup blocked; showing splash actions.");

        _lastBlockedWasDatabaseFailure = IsDatabaseFailureResult(result);
        IsDatabaseFailure = _lastBlockedWasDatabaseFailure;
        ShowResetToDefaultsAction = !_lastBlockedWasDatabaseFailure;

        if (_startupState.IsDeveloper && IsLoggingDestinationPromptRequired(result))
        {
            var destination = await PromptForLoggingDestinationAsync();
            if (!string.IsNullOrWhiteSpace(destination))
            {
                await _localSettingsService.SaveSettingAsync(StartupLoggingOptions.CentralizedDestinationSettingKey, destination.Trim());
                StartupDebugLog.Info("SplashViewModel", "Centralized logging destination saved from startup prompt.");
                await RunStartupAsync();
                return;
            }

            StatusText = "Startup stopped because centralized logging destination setup was canceled.";
            StatusText = "Setup was canceled. Choose Try again to continue or Exit to close the app.";
            ShowActions = true;
            IsDatabaseFailure = false;
            UpdateState();
            return;
        }

        ShowActions = true;
    }

    private static bool IsLoggingDestinationPromptRequired(StartupResult result)
    {
        return result.IsBlocked
            && !string.IsNullOrWhiteSpace(result.StatusMessage)
            && (result.StatusMessage.StartsWith(LoggingDestinationRequiredPrefix, StringComparison.Ordinal)
                || result.StatusMessage.StartsWith(LoggingDestinationMissingPrefix, StringComparison.Ordinal));
    }

    private static bool IsDatabaseFailureResult(StartupResult result)
    {
        return result.IsBlocked
            && !string.IsNullOrWhiteSpace(result.StatusMessage)
            && result.StatusMessage.StartsWith(DatabaseFailurePrefix, StringComparison.Ordinal);
    }

    private async Task<string?> PromptForLoggingDestinationAsync()
    {
        var promptHandler = LoggingDestinationPromptRequestedAsync;
        if (promptHandler is null)
        {
            return null;
        }

        try
        {
            return await promptHandler();
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("SplashViewModel", ex, "Centralized logging destination prompt failed.");
            return null;
        }
    }

    private void UpdateState()
    {
        _startupState.IsBusy = IsBusy;
        _startupState.StatusText = StatusText;
        _startupState.LastUpdatedUtc = DateTimeOffset.UtcNow;
    }
}
