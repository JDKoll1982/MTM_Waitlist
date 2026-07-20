using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Contracts.Services;
using MTM_Waitlist.Contracts.ViewModels;
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

    public async void OnNavigatedTo(object parameter)
    {
        if (_startupStarted)
        {
            return;
        }

        _startupStarted = true;
        await RunStartupAsync();
    }

    public void OnNavigatedFrom()
    {
    }

    [RelayCommand]
    private async Task RetryAsync()
    {
        await RunStartupAsync();
    }

    [RelayCommand]
    private async Task ResetToDefaultsAsync()
    {
        IsBusy = true;
        ShowActions = false;
        StatusText = "Resetting local settings...";
        UpdateState();

        try
        {
            await _startupRecoveryService.ResetToDefaultsAsync();
            await RunStartupAsync();
        }
        catch (Exception)
        {
            IsBusy = false;
            ShowActions = true;
            StatusText = "Settings could not be reset. Try again or exit.";
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
        _startupShellStateService.EnterSplashMode();
        IsBusy = true;
        ShowActions = false;
        StatusText = "Running startup checks...";
        UpdateState();

        var result = await _startupCoordinator.RunAsync();

        StatusText = string.IsNullOrWhiteSpace(result.StatusMessage)
            ? "Startup completed."
            : result.StatusMessage;
        IsBusy = false;
        UpdateState();

        if (result.IsSuccess && !result.IsBlocked && !string.IsNullOrWhiteSpace(result.RouteTarget))
        {
            await _startupShellStateService.EnterMainModeAsync();
            _navigationService.NavigateTo(result.RouteTarget, null, true);
            return;
        }

        ShowActions = true;
    }

    private void UpdateState()
    {
        _startupState.IsBusy = IsBusy;
        _startupState.StatusText = StatusText;
        _startupState.LastUpdatedUtc = DateTimeOffset.UtcNow;
    }
}
