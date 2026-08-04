using Microsoft.Extensions.Options;
using System.Text.Json;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Settings.Models;

using Windows.ApplicationModel;
using Windows.Storage;

namespace MTM_Waitlist.Module_Settings.Services;

public class LocalSettingsService : ILocalSettingsService
{
    private const string CorruptPayload = "{ invalid-json";

    private const string _defaultApplicationDataFolder = "MTM_Waitlist/ApplicationData";
    private const string _defaultLocalSettingsFile = "LocalSettings.json";

    private readonly IFileService _fileService;
    private readonly LocalSettingsOptions _options;

    private readonly string _localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    private readonly string _applicationDataFolder;
    private readonly string _localsettingsFile;

    private IDictionary<string, object> _settings;

    private bool _isInitialized;

    public LocalSettingsService(IFileService fileService, IOptions<LocalSettingsOptions> options)
    {
        _fileService = fileService;
        _options = options.Value;

        _applicationDataFolder = Path.Combine(_localApplicationData, _options.ApplicationDataFolder ?? _defaultApplicationDataFolder);
        _localsettingsFile = _options.LocalSettingsFile ?? _defaultLocalSettingsFile;

        _settings = new Dictionary<string, object>();
    }

    private async Task InitializeAsync()
    {
        if (!_isInitialized)
        {
            _settings = await _fileService.Read<IDictionary<string, object>>(_applicationDataFolder, _localsettingsFile) ?? new Dictionary<string, object>();

            _isInitialized = true;
        }
    }

    public async Task<T?> ReadSettingAsync<T>(string key)
    {
        if (RuntimeHelper.IsMSIX)
        {
            if (ApplicationData.Current.LocalSettings.Values.TryGetValue(key, out var obj))
            {
                return await Json.ToObjectAsync<T>((string)obj);
            }
        }
        else
        {
            await InitializeAsync();

            if (_settings != null && _settings.TryGetValue(key, out var obj))
            {
                var json = GetStoredJson(obj);
                return await Json.ToObjectAsync<T>(json);
            }
        }

        return default;
    }

    public async Task SaveSettingAsync<T>(string key, T value)
    {
        // Edge case 1: Handle null argument values safely up front
        if (value is null)
        {
            if (RuntimeHelper.IsMSIX)
            {
                // Safely remove the key from Windows storage rather than assigning null
                ApplicationData.Current.LocalSettings.Values.Remove(key);
            }
            else
            {
                await InitializeAsync();
                // Safely remove from local cache file mapping
                _settings.Remove(key);
                await _fileService.Save(_applicationDataFolder, _localsettingsFile, _settings);
            }
            return;
        }

        // Edge case 2: Guard against empty or null serialization outputs
        var jsonString = await Json.StringifyAsync(value);
        if (string.IsNullOrWhiteSpace(jsonString))
        {
            return;
        }

        if (RuntimeHelper.IsMSIX)
        {
            ApplicationData.Current.LocalSettings.Values[key] = jsonString;
        }
        else
        {
            await InitializeAsync();
            _settings[key] = jsonString;
            await _fileService.Save(_applicationDataFolder, _localsettingsFile, _settings);
        }
    }

    public async Task ResetSettingAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (RuntimeHelper.IsMSIX)
        {
            ApplicationData.Current.LocalSettings.Values.Remove(key);
            return;
        }

        await InitializeAsync();
        _settings.Remove(key);
        await _fileService.Save(_applicationDataFolder, _localsettingsFile, _settings);
    }

    Task ILocalSettingsService.ResetSettingAsync(string key, CancellationToken cancellationToken)
        => ResetSettingAsync(key, cancellationToken);

    public async Task ResetAsync()
    {
        _settings = new Dictionary<string, object>();
        _isInitialized = true;

        if (RuntimeHelper.IsMSIX)
        {
            ApplicationData.Current.LocalSettings.Values.Clear();
            return;
        }

        await _fileService.Delete(_applicationDataFolder, _localsettingsFile);
    }

    public async Task CorruptForTestAsync()
    {
        if (RuntimeHelper.IsMSIX)
        {
            ApplicationData.Current.LocalSettings.Values["Developer.RecoveryProbe"] = CorruptPayload;

            if (ApplicationData.Current.LocalSettings.Values["Developer.RecoveryProbe"] is not string probeValue
                || !string.Equals(probeValue, CorruptPayload, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Failed to verify corrupted startup probe value in local settings.");
            }

            return;
        }

        Directory.CreateDirectory(_applicationDataFolder);
        var filePath = Path.Combine(_applicationDataFolder, _localsettingsFile);
        await File.WriteAllTextAsync(filePath, CorruptPayload);

        var fileContents = await File.ReadAllTextAsync(filePath);
        if (!string.Equals(fileContents, CorruptPayload, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Failed to verify corrupted startup settings file contents before restart.");
        }
    }

    private static string GetStoredJson(object? value)
    {
        if (value is null)
        {
            return "null";
        }

        if (value is JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString() ?? "null",
                JsonValueKind.Null => "null",
                _ => element.GetRawText()
            };
        }

        return value.ToString() ?? "null";
    }
}
