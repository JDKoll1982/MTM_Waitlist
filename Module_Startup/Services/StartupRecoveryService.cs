using System.Diagnostics;
using System.Reflection;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;

namespace MTM_Waitlist.Module_Startup.Services;

public sealed class StartupRecoveryService : IStartupRecoveryService
{
    private readonly ILocalSettingsService _localSettingsService;

    public StartupRecoveryService(ILocalSettingsService localSettingsService)
    {
        ArgumentNullException.ThrowIfNull(localSettingsService);
        _localSettingsService = localSettingsService;
    }

    public Task ResetSettingAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        return _localSettingsService.ResetSettingAsync(key, cancellationToken);
    }

    public Task ResetToDefaultsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _localSettingsService.ResetAsync();
    }

    public async Task CorruptAndRestartAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        StartupDebugLog.Info("StartupRecoveryService", "CorruptAndRestartAsync started.");

        await _localSettingsService.CorruptForTestAsync();
        StartupDebugLog.Info("StartupRecoveryService", "Corrupt startup data verification succeeded; launching restart.");

        var processPath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(processPath))
        {
            throw new InvalidOperationException("The current process path is unavailable.");
        }

        var processStartInfo = new ProcessStartInfo(processPath)
        {
            UseShellExecute = true
        };

        if (string.Equals(Path.GetFileNameWithoutExtension(processPath), "dotnet", StringComparison.OrdinalIgnoreCase))
        {
            var entryAssemblyPath = Assembly.GetEntryAssembly()?.Location;
            if (string.IsNullOrWhiteSpace(entryAssemblyPath))
            {
                throw new InvalidOperationException("The application assembly path is unavailable.");
            }

            processStartInfo.Arguments = $"\"{entryAssemblyPath}\"";
        }

        Process.Start(processStartInfo);

        App.Current.Exit();
    }
}