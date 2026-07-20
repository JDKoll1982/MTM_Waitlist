using Microsoft.Extensions.Options;

using MTM_Waitlist.Contracts.Services;
using MTM_Waitlist.Models;
using MTM_Waitlist.ViewModels;

namespace MTM_Waitlist.Services;

public sealed class StartupCoordinator : IStartupCoordinator
{
    private readonly LocalSettingsOptions _settingsOptions;
    private readonly ILocalSettingsService _localSettingsService;
    private readonly StartupState _startupState;

    public StartupCoordinator(
        IOptions<LocalSettingsOptions> settingsOptions,
        ILocalSettingsService localSettingsService,
        StartupState startupState)
    {
        ArgumentNullException.ThrowIfNull(settingsOptions);
        ArgumentNullException.ThrowIfNull(localSettingsService);
        ArgumentNullException.ThrowIfNull(startupState);

        _settingsOptions = settingsOptions.Value;
        _localSettingsService = localSettingsService;
        _startupState = startupState;
    }

    public async Task<StartupResult> RunAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var username = Environment.GetEnvironmentVariable("USERNAME")
            ?? Environment.UserName;
        var applicationDataFolder = _settingsOptions.ApplicationDataFolder?.Trim();
        var localSettingsFile = _settingsOptions.LocalSettingsFile?.Trim();

        _startupState.Username = username ?? string.Empty;
        _startupState.ConfigurationFolder = applicationDataFolder ?? string.Empty;
        _startupState.ConfigurationFile = localSettingsFile ?? string.Empty;
        _startupState.CurrentRole = Environment.GetEnvironmentVariable("MTM_WAITLIST_ROLE")?.Trim() ?? string.Empty;
        _startupState.ConfigurationLoaded = false;

        if (string.IsNullOrWhiteSpace(_startupState.Username))
        {
            return StartupResult.Blocked("Windows username is unavailable.");
        }

        if (string.IsNullOrWhiteSpace(applicationDataFolder) || string.IsNullOrWhiteSpace(localSettingsFile))
        {
            return StartupResult.Blocked("Startup configuration is missing required local-settings paths.");
        }

        try
        {
            await _localSettingsService.ReadSettingAsync<string>("Developer.RecoveryProbe");
        }
        catch (Exception)
        {
            return StartupResult.Blocked("Local settings could not be read. Reset to defaults and try again.");
        }

        _startupState.ConfigurationLoaded = true;

        var result = StartupResult.Success(typeof(MainShellViewModel).FullName!);
        return result;
    }
}
