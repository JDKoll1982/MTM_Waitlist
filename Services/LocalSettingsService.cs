using Microsoft.Extensions.Options;

using MTM_Waitlist.Contracts.Services;
using MTM_Waitlist.Core.Contracts.Services;
using MTM_Waitlist.Core.Helpers;
using MTM_Waitlist.Helpers;
using MTM_Waitlist.Models;

using Windows.ApplicationModel;
using Windows.Storage;

namespace MTM_Waitlist.Services;

public class LocalSettingsService : ILocalSettingsService
{
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
            _settings = await Task.Run(() => _fileService.Read<IDictionary<string, object>>(_applicationDataFolder, _localsettingsFile)) ?? new Dictionary<string, object>();

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
                return await Json.ToObjectAsync<T>((string)obj);
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
                await Task.Run(() => _fileService.Save(_applicationDataFolder, _localsettingsFile, _settings));
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
            await Task.Run(() => _fileService.Save(_applicationDataFolder, _localsettingsFile, _settings));
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
        await Task.Run(() => _fileService.Save(_applicationDataFolder, _localsettingsFile, _settings));
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

        await Task.Run(() => _fileService.Delete(_applicationDataFolder, _localsettingsFile));
    }

    public async Task CorruptForTestAsync()
    {
        if (RuntimeHelper.IsMSIX)
        {
            ApplicationData.Current.LocalSettings.Values["Developer.RecoveryProbe"] = "{ invalid-json";
            return;
        }

        Directory.CreateDirectory(_applicationDataFolder);
        await File.WriteAllTextAsync(Path.Combine(_applicationDataFolder, _localsettingsFile), "{ invalid-json");
    }
}
