using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Activation;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Views;

namespace MTM_Waitlist.Module_Core.Services;

public class ActivationService : IActivationService
{
    private readonly ActivationHandler<LaunchActivatedEventArgs> _defaultHandler;
    private readonly IEnumerable<IActivationHandler> _activationHandlers;
    private readonly IThemeSelectorService _themeSelectorService;
    private readonly IStartupShellStateService _startupShellStateService;
    private UIElement? _shell = null;

    public ActivationService(
        ActivationHandler<LaunchActivatedEventArgs> defaultHandler,
        IEnumerable<IActivationHandler> activationHandlers,
        IThemeSelectorService themeSelectorService,
        IStartupShellStateService startupShellStateService)
    {
        _defaultHandler = defaultHandler;
        _activationHandlers = activationHandlers;
        _themeSelectorService = themeSelectorService;
        _startupShellStateService = startupShellStateService;
    }

    public async Task ActivateAsync(object activationArgs, bool activateMainWindow = true)
    {
        StartupDebugLog.Info("ActivationService", "ActivateAsync started.");

        // Execute tasks before activation.
        await InitializeAsync();
        StartupDebugLog.Info("ActivationService", "InitializeAsync completed.");
        _startupShellStateService.EnterSplashMode();
        StartupDebugLog.Info("ActivationService", "Splash mode entered.");

        // Set the MainWindow Content.
        if (App.MainWindow.Content == null)
        {
            _shell = App.GetService<ShellPage>();
            App.MainWindow.Content = _shell ?? new Frame();
            StartupDebugLog.Info("ActivationService", "Shell content assigned to MainWindow.");
        }

        // Handle activation via ActivationHandlers.
        await HandleActivationAsync(activationArgs);
        StartupDebugLog.Info("ActivationService", "Activation handlers completed.");

        // Activate the MainWindow when requested by the startup flow.
        if (activateMainWindow)
        {
            App.MainWindow.Activate();
            StartupDebugLog.Info("ActivationService", "MainWindow activated.");
        }

        // Execute tasks after activation.
        await StartupAsync();
        StartupDebugLog.Info("ActivationService", "StartupAsync completed.");
    }

    private async Task HandleActivationAsync(object activationArgs)
    {
        var activationHandler = _activationHandlers.FirstOrDefault(h => h.CanHandle(activationArgs));

        if (activationHandler != null)
        {
            StartupDebugLog.Info("ActivationService", $"Using activation handler: {activationHandler.GetType().Name}.");
            await activationHandler.HandleAsync(activationArgs);
        }

        if (_defaultHandler.CanHandle(activationArgs))
        {
            StartupDebugLog.Info("ActivationService", $"Using default activation handler: {_defaultHandler.GetType().Name}.");
            await _defaultHandler.HandleAsync(activationArgs);
        }
    }

    private async Task InitializeAsync()
    {
        await _themeSelectorService.InitializeAsync().ConfigureAwait(false);
        await Task.CompletedTask;
    }

    private async Task StartupAsync()
    {
        await _themeSelectorService.SetRequestedThemeAsync();
        await Task.CompletedTask;
    }
}
