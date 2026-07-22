using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Contracts.Services;
using MTM_Waitlist.Contracts.ViewModels;
using MTM_Waitlist.Helpers;
using MTM_Waitlist.Models;

namespace MTM_Waitlist.ViewModels;

public partial class SplashViewModel : ObservableRecipient, INavigationAware
{
    private readonly IStartupCoordinator _startupCoordinator;
    private readonly IStartupRecoveryService _startupRecoveryService;
    private readonly INavigationService _navigationService;
    private readonly IStartupShellStateService _startupShellStateService;
    private readonly StartupState _startupState;
    private bool _startupStarted;
    private string _statusText = "Starting application...";
    private bool _isBusy = true;
    private bool _showActions;

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

    public SplashViewModel(
        IStartupCoordinator startupCoordinator,
        IStartupRecoveryService startupRecoveryService,
        INavigationService navigationService,
        IStartupShellStateService startupShellStateService,
        StartupState startupState)
    {
        ArgumentNullException.ThrowIfNull(startupCoordinator);
        ArgumentNullException.ThrowIfNull(startupRecoveryService);
        ArgumentNullException.ThrowIfNull(navigationService);
        ArgumentNullException.ThrowIfNull(startupShellStateService);
        ArgumentNullException.ThrowIfNull(startupState);

        _startupCoordinator = startupCoordinator;
        _startupRecoveryService = startupRecoveryService;
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
        StatusText = "Trying to repair one damaged setting...";
        UpdateState();
        await RunStartupAsync();
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

    private async Task RunStartupAsync()
    {
        StartupDebugLog.Info("SplashViewModel", "RunStartupAsync entered.");
        _startupShellStateService.EnterSplashMode();
        IsBusy = true;
        ShowActions = false;
        StatusText = "Running startup checks...";
        UpdateState();

        StartupResult result;
        try
        {
            result = await _startupCoordinator.RunAsync();
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
        IsBusy = false;
        UpdateState();

        if (result.IsSuccess && !result.IsBlocked && !string.IsNullOrWhiteSpace(result.RouteTarget))
        {
            StartupDebugLog.Info("SplashViewModel", "Startup succeeded; transitioning to main mode.");
            await _startupShellStateService.EnterMainModeAsync();
            _navigationService.NavigateTo(result.RouteTarget, null, true);
            App.ShowMainWindowAndCloseSplash();
            return;
        }

        StartupDebugLog.Info("SplashViewModel", "Startup blocked; showing splash actions.");
        ShowActions = true;
    }

    private void UpdateState()
    {
        _startupState.IsBusy = IsBusy;
        _startupState.StatusText = StatusText;
        _startupState.LastUpdatedUtc = DateTimeOffset.UtcNow;
    }
}
