using Microsoft.Extensions.Options;
using System.Net.NetworkInformation;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Startup.ViewModels;
using MySqlConnector;

namespace MTM_Waitlist.Module_Startup.Services;

public sealed class StartupCoordinator : IStartupCoordinator
{
    private const string RecoveryProbeSettingKey = "Developer.RecoveryProbe";
    private const string LocalSessionTokenKey = "Startup.Session.Token";
    private const string LocalSessionExpiryKey = "Startup.Session.ExpiresUtc";
    private const string WaitlistRoute = "MTM_Waitlist.Module_Waitlist.ViewModels.WaitlistViewViewModel";

    private static class StartupProgress
    {
        public const string Step1 = "Step 1 of 5: Loading application settings...";
        public const string Step2 = "Step 2 of 5: Checking device registration...";
        public const string Step3 = "Step 3 of 5: Verifying user identity...";
        public const string Step4 = "Step 4 of 5: Validating login session...";
        public const string Step5 = "Step 5 of 5: Loading data dashboards...";
    }

    private readonly LocalSettingsOptions _settingsOptions;
    private readonly StartupDatabaseOptions _startupDatabaseOptions;
    private readonly StartupLoggingOptions _startupLoggingOptions;
    private readonly StartupDevelopmentOptions _startupDevelopmentOptions;
    private readonly ILocalSettingsService _localSettingsService;
    private readonly IStartupSessionRepository _startupSessionRepository;
    private readonly IStartupRecoveryService _startupRecoveryService;
    private readonly StartupState _startupState;

    public StartupCoordinator(
        IOptions<LocalSettingsOptions> settingsOptions,
        IOptions<StartupDatabaseOptions> startupDatabaseOptions,
        IOptions<StartupLoggingOptions> startupLoggingOptions,
        IOptions<StartupDevelopmentOptions> startupDevelopmentOptions,
        ILocalSettingsService localSettingsService,
        IStartupSessionRepository startupSessionRepository,
        IStartupRecoveryService startupRecoveryService,
        StartupState startupState)
    {
        ArgumentNullException.ThrowIfNull(settingsOptions);
        ArgumentNullException.ThrowIfNull(startupDatabaseOptions);
        ArgumentNullException.ThrowIfNull(startupLoggingOptions);
        ArgumentNullException.ThrowIfNull(startupDevelopmentOptions);
        ArgumentNullException.ThrowIfNull(localSettingsService);
        ArgumentNullException.ThrowIfNull(startupSessionRepository);
        ArgumentNullException.ThrowIfNull(startupRecoveryService);
        ArgumentNullException.ThrowIfNull(startupState);

        _settingsOptions = settingsOptions.Value;
        _startupDatabaseOptions = startupDatabaseOptions.Value;
        _startupLoggingOptions = startupLoggingOptions.Value;
        _startupDevelopmentOptions = startupDevelopmentOptions.Value;
        _localSettingsService = localSettingsService;
        _startupSessionRepository = startupSessionRepository;
        _startupRecoveryService = startupRecoveryService;
        _startupState = startupState;
    }

    public async Task<StartupResult> RunAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default, bool retryDatabasePhaseOnly = false)
    {
        StartupDebugLog.Info("StartupCoordinator", "RunAsync started.");
        cancellationToken.ThrowIfCancellationRequested();

        if (!retryDatabasePhaseOnly)
        {
            progress?.Report(StartupProgress.Step1);
        }

        var username = Environment.GetEnvironmentVariable("USERNAME")
            ?? Environment.UserName;
        var applicationDataFolder = _settingsOptions.ApplicationDataFolder?.Trim();
        var localSettingsFile = _settingsOptions.LocalSettingsFile?.Trim();

        _startupState.Username = username ?? string.Empty;
        _startupState.ConfigurationFolder = applicationDataFolder ?? string.Empty;
        _startupState.ConfigurationFile = localSettingsFile ?? string.Empty;
        _startupState.HostnameNormalized = NormalizeForLookup(Environment.MachineName);
        _startupState.MacAddressNormalized = ReadPrimaryMacAddressNormalized();
        _startupState.CurrentRole = IsDefaultDeveloperUser(_startupState.Username)
            ? "Developer"
            : string.Empty;
        _startupState.ConfigurationLoaded = false;

        StartupDebugLog.Info(
            "StartupCoordinator",
            $"Resolved startup context. User={_startupState.Username}, ConfigFolder={_startupState.ConfigurationFolder}, ConfigFile={_startupState.ConfigurationFile}, Host={_startupState.HostnameNormalized}, Mac={_startupState.MacAddressNormalized}");

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

        if (!retryDatabasePhaseOnly)
        {
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
        }
        else
        {
            StartupDebugLog.Info("StartupCoordinator", "RunAsync executing retryDatabasePhaseOnly path; skipping local settings repair stage.");
        }

        _startupState.ConfigurationLoaded = true;

        progress?.Report(StartupProgress.Step2);

        var startupConnectionString = ResolveStartupDatabaseConnectionString();
        if (!IsConnectionStringValid(startupConnectionString))
        {
            StartupDebugLog.Info("StartupCoordinator", "Blocked: startup DB connection string is malformed.");
            return StartupResult.Blocked("Startup database configuration is invalid. Contact a developer.");
        }

        StartupSessionSnapshot sessionSnapshot;
        DateTimeOffset serverTimeUtc;
        try
        {
            sessionSnapshot = await _startupSessionRepository.ReadSessionSnapshotAsync(
                _startupState.Username,
                _startupState.HostnameNormalized,
                _startupState.MacAddressNormalized,
                cancellationToken);

            serverTimeUtc = await _startupSessionRepository.ReadServerTimeUtcAsync(cancellationToken)
                ?? DateTimeOffset.UtcNow;
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("StartupCoordinator", ex, "Database-backed startup session lookup failed.");
            return StartupResult.Blocked("Could not validate startup session from the database. Try again.");
        }

        var isComputerRegistered = sessionSnapshot.IsComputerRegistered;
        _startupState.IsComputerRegistered = isComputerRegistered;
        _startupState.IsComputerRegistrationAuthoritative = sessionSnapshot.IsComputerRegistrationAuthoritative;

        progress?.Report(StartupProgress.Step3);
        var isUserMatched = sessionSnapshot.IsUserMatched;
        _startupState.IsUserMatched = isUserMatched;
        _startupState.CurrentRole = sessionSnapshot.CurrentRole;

        if (IsDefaultDeveloperUser(_startupState.Username))
        {
            _startupState.CurrentRole = "Developer";
        }

        var centralizedDestination = await ResolveCentralizedDestinationAsync();
        if (string.IsNullOrWhiteSpace(centralizedDestination))
        {
            StartupDebugLog.Info("StartupCoordinator", "Blocked: centralized logging destination is not configured.");

            if (_startupState.IsDeveloper)
            {
                return StartupResult.Blocked("Centralized logging destination is required. Configure a destination to continue startup, or cancel to stop startup.");
            }

            return StartupResult.Blocked("Centralized logging destination is not configured. Contact a developer.");
        }

        progress?.Report(StartupProgress.Step4);
        var localToken = await _localSettingsService.ReadSettingAsync<string>(LocalSessionTokenKey);
        var localExpiryRaw = await _localSettingsService.ReadSettingAsync<string>(LocalSessionExpiryKey);
        var localExpiry = ParseUtc(localExpiryRaw);

        var tokenSource = StartupState.SessionTokenSourceNone;
        var hasEffectiveToken = false;
        DateTimeOffset? effectiveExpiry = null;

        if (!string.IsNullOrWhiteSpace(localToken))
        {
            tokenSource = StartupState.SessionTokenSourceLocal;
            hasEffectiveToken = true;
            effectiveExpiry = localExpiry;
        }
        else if (sessionSnapshot.HasDatabaseSession)
        {
            tokenSource = StartupState.SessionTokenSourceDatabase;
            hasEffectiveToken = true;
            effectiveExpiry = sessionSnapshot.DatabaseSessionExpiresUtc;
        }

        var sessionIsValid = hasEffectiveToken
            && effectiveExpiry.HasValue
            && effectiveExpiry.Value > serverTimeUtc;

        _startupState.SessionTokenSource = tokenSource;
        _startupState.ServerTimeUtc = serverTimeUtc;
        _startupState.IsSessionValid = sessionIsValid;
        _startupState.RequireNewUserAction =
            _startupState.IsComputerRegistrationAuthoritative
            && !isUserMatched
            && !isComputerRegistered;

        progress?.Report(StartupProgress.Step5);

        if (isUserMatched && sessionIsValid)
        {
            _startupState.LoginHint = string.Empty;
            var result = StartupResult.Success(WaitlistRoute, StartupProgress.Step5);
            StartupDebugLog.Info("StartupCoordinator", $"Startup succeeded. Route target={result.RouteTarget}. TokenSource={tokenSource}");
            return result;
        }

        _startupState.LoginHint = _startupState.RequireNewUserAction
            ? "This computer is not registered. Choose New User to request access."
            : "Sign in to continue.";

        var loginResult = StartupResult.Success(typeof(LoginViewModel).FullName!, _startupState.LoginHint);
        StartupDebugLog.Info(
            "StartupCoordinator",
            $"Startup routed to login. UserMatched={isUserMatched}, ComputerRegistered={isComputerRegistered}, SessionValid={sessionIsValid}, TokenSource={tokenSource}");
        return loginResult;
    }

    private static DateTimeOffset? ParseUtc(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateTimeOffset.TryParse(value, out var parsed)
            ? parsed.ToUniversalTime()
            : null;
    }

    private static string NormalizeForLookup(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();
    }

    private static string ReadPrimaryMacAddressNormalized()
    {
        var selectedInterface = NetworkInterface
            .GetAllNetworkInterfaces()
            .Where(item =>
                item.OperationalStatus == OperationalStatus.Up
                && item.NetworkInterfaceType != NetworkInterfaceType.Loopback
                && item.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
            .FirstOrDefault(item => item.GetPhysicalAddress().GetAddressBytes().Length > 0);

        if (selectedInterface is null)
        {
            return string.Empty;
        }

        var bytes = selectedInterface.GetPhysicalAddress().GetAddressBytes();
        return string.Join("-", bytes.Select(item => item.ToString("X2"))).ToLowerInvariant();
    }

    private bool IsDefaultDeveloperUser(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return false;
        }

        return _startupDevelopmentOptions.DefaultDeveloperUsernames
            .Any(item => string.Equals(item?.Trim(), username.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private async Task<string?> ResolveCentralizedDestinationAsync()
    {
        try
        {
            var localDestination = await _localSettingsService.ReadSettingAsync<string>(StartupLoggingOptions.CentralizedDestinationSettingKey);
            if (!string.IsNullOrWhiteSpace(localDestination))
            {
                return localDestination.Trim();
            }
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("StartupCoordinator", ex, "Failed to read local centralized logging destination; falling back to configured option.");
        }

        var configuredDestination = _startupLoggingOptions.CentralizedDestination?.Trim();
        return string.IsNullOrWhiteSpace(configuredDestination)
            ? null
            : configuredDestination;
    }

    private static bool IsConnectionStringValid(string? connectionString)
    {
        var trimmed = connectionString?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            // Empty connection is allowed for non-authoritative local/dev startup paths.
            return true;
        }

        try
        {
            _ = new MySqlConnectionStringBuilder(trimmed);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private string? ResolveStartupDatabaseConnectionString()
    {
        var environmentVariableName = _startupDatabaseOptions.ConnectionStringEnvironmentVariable?.Trim();
        if (!string.IsNullOrWhiteSpace(environmentVariableName))
        {
            var environmentConnectionString = Environment.GetEnvironmentVariable(environmentVariableName)?.Trim();
            if (!string.IsNullOrWhiteSpace(environmentConnectionString))
            {
                return environmentConnectionString;
            }
        }

        return _startupDatabaseOptions.ConnectionString;
    }
}
