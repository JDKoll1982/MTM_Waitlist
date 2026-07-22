using Microsoft.Extensions.Options;

using MTM_Waitlist.Contracts.Services;
using MTM_Waitlist.Helpers;
using MTM_Waitlist.Models;
using MTM_Waitlist.ViewModels;

namespace MTM_Waitlist.Services;

public sealed class StartupCoordinator : IStartupCoordinator
{
    private const string RecoveryProbeSettingKey = "Developer.RecoveryProbe";

    private readonly LocalSettingsOptions _settingsOptions;
    private readonly ILocalSettingsService _localSettingsService;
    private readonly IStartupRecoveryService _startupRecoveryService;
    private readonly StartupState _startupState;

    public StartupCoordinator(
        IOptions<LocalSettingsOptions> settingsOptions,
        ILocalSettingsService localSettingsService,
        IStartupRecoveryService startupRecoveryService,
        StartupState startupState)
    {
        ArgumentNullException.ThrowIfNull(settingsOptions);
        ArgumentNullException.ThrowIfNull(localSettingsService);
        ArgumentNullException.ThrowIfNull(startupRecoveryService);
        ArgumentNullException.ThrowIfNull(startupState);

        _settingsOptions = settingsOptions.Value;
        _localSettingsService = localSettingsService;
        _startupRecoveryService = startupRecoveryService;
        _startupState = startupState;
    }

    public async Task<StartupResult> RunAsync(CancellationToken cancellationToken = default)
    {
        StartupDebugLog.Info("StartupCoordinator", "RunAsync started.");
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

        StartupDebugLog.Info(
            "StartupCoordinator",
            $"Resolved startup context. User={_startupState.Username}, ConfigFolder={_startupState.ConfigurationFolder}, ConfigFile={_startupState.ConfigurationFile}, Role={_startupState.CurrentRole}");

        if (string.IsNullOrWhiteSpace(_startupState.Username))
        {
            StartupDebugLog.Info("StartupCoordinator", "Blocked: Windows username unavailable.");
            return StartupResult.Blocked("Windows username is unavailable.");
        }

        if (string.IsNullOrWhiteSpace(applicationDataFolder) || string.IsNullOrWhiteSpace(localSettingsFile))
        {
            StartupDebugLog.Info("StartupCoordinator", "Blocked: local settings paths missing in configuration.");
            return StartupResult.Blocked("Startup configuration is missing required local-settings paths.");
        }

        try
        {
            await _localSettingsService.ReadSettingAsync<string>(RecoveryProbeSettingKey);
            StartupDebugLog.Info("StartupCoordinator", "Local settings read probe succeeded.");
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("StartupCoordinator", ex, "Local settings read probe failed.");

            try
            {
                StartupDebugLog.Info("StartupCoordinator", $"Attempting targeted local settings repair for {RecoveryProbeSettingKey}.");
                await _startupRecoveryService.ResetSettingAsync(RecoveryProbeSettingKey, cancellationToken);
                await _localSettingsService.ReadSettingAsync<string>(RecoveryProbeSettingKey);
                StartupDebugLog.Info("StartupCoordinator", "Targeted local settings repair completed.");
            }
            catch (Exception repairEx)
            {
                StartupDebugLog.Error("StartupCoordinator", repairEx, "Targeted local settings repair failed.");
                return StartupResult.Blocked("One local setting could not be repaired. Try again to repair it, or reset to defaults to clear all local settings.");
            }
        }

        _startupState.ConfigurationLoaded = true;

        var result = StartupResult.Success(typeof(MainShellViewModel).FullName!);
        StartupDebugLog.Info("StartupCoordinator", $"Startup succeeded. Route target={result.RouteTarget}.");
        return result;
    }
}
