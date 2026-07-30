using System.Text;

using Microsoft.Extensions.Options;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Startup.Models;

namespace MTM_Waitlist.Module_Startup.Services;

public sealed class StartupLogForwarder : IStartupLogForwarder
{
    private readonly StartupLoggingOptions _options;
    private readonly ILocalSettingsService _localSettingsService;

    public StartupLogForwarder(IOptions<StartupLoggingOptions> options, ILocalSettingsService localSettingsService)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(localSettingsService);
        _options = options.Value;
        _localSettingsService = localSettingsService;
    }

    public async Task ForwardAsync(string jsonLine, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jsonLine))
        {
            return;
        }

        var destination = await ResolveDestinationAsync();
        if (string.IsNullOrWhiteSpace(destination))
        {
            return;
        }

        var resolvedPath = ResolvePath(destination);
        Directory.CreateDirectory(resolvedPath);

        var filePath = Path.Combine(resolvedPath, $"startup_forwarded_{DateTimeOffset.UtcNow:yyyy_MM_dd}.jsonl");
        await File.AppendAllTextAsync(filePath, jsonLine + Environment.NewLine, Encoding.UTF8, cancellationToken);
    }

    private static string ResolvePath(string path)
    {
        if (Path.IsPathRooted(path))
        {
            return path;
        }

        var basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(basePath, path);
    }

    private async Task<string?> ResolveDestinationAsync()
    {
        try
        {
            var localDestination = await _localSettingsService.ReadSettingAsync<string>(StartupLoggingOptions.CentralizedDestinationSettingKey);
            if (!string.IsNullOrWhiteSpace(localDestination))
            {
                return localDestination.Trim();
            }
        }
        catch
        {
            // Preserve startup logging by falling back to configured destination.
        }

        var configuredDestination = _options.CentralizedDestination?.Trim();
        return string.IsNullOrWhiteSpace(configuredDestination)
            ? null
            : configuredDestination;
    }
}
