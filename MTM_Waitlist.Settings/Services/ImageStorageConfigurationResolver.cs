using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Shared.Helpers;

namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// Implementation of IImageStorageConfigurationResolver.
/// Resolves image storage configuration: the machine's own configured folders first, then the plant-wide
/// override, then this build's shipped default. Cached with invalidation support for performance.
/// </summary>
public sealed class ImageStorageConfigurationResolver : IImageStorageConfigurationResolver
{
    private readonly ILogger<ImageStorageConfigurationResolver> _logger;
    private readonly IOptions<ImageStorageOptions> _appsettingsOptions;
    private readonly IConfigSettingsValueService _configService;
    private readonly IMachineConfigurationService? _machineConfiguration;
    
    // Cache for resolved values with TTL
    private readonly ConcurrentDictionary<string, CachedValue<object>> _cache;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Initializes a new ImageStorageConfigurationResolver.
    /// </summary>
    /// <param name="logger">Logger for diagnostics</param>
    /// <param name="appsettingsOptions">The shipped defaults, used only when neither the machine nor the plant names a folder</param>
    /// <param name="configService">Service for reading plant-wide configuration values</param>
    /// <param name="machineConfiguration">
    /// This machine's own configuration, where the shared folder, the keys folder and the dunnage root now live
    /// (T155, FR-025). Optional so a host that has no machine configuration read, such as a test or the cache
    /// service, still resolves the plant-wide value and the shipped default.
    /// </param>
    /// <exception cref="ArgumentNullException">If a required parameter is null</exception>
    public ImageStorageConfigurationResolver(
        ILogger<ImageStorageConfigurationResolver> logger,
        IOptions<ImageStorageOptions> appsettingsOptions,
        IConfigSettingsValueService configService,
        IMachineConfigurationService? machineConfiguration = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _appsettingsOptions = appsettingsOptions ?? throw new ArgumentNullException(nameof(appsettingsOptions));
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        _machineConfiguration = machineConfiguration;
        _cache = new ConcurrentDictionary<string, CachedValue<object>>();
    }

    /// <inheritdoc />
    public async Task<string> GetSharedFolderPathAsync()
    {
        try
        {
            var cacheKey = ConfigSettingKeys.ImageStorageSharedFolderPath;
            
            // Try cache first
            if (_cache.TryGetValue(cacheKey, out var cached) && cached.IsValid())
            {
                _logger.LogDebug("Using cached shared folder path: {Source}", cached.Source);
                return (string)cached.Value;
            }

            // The plant-wide override, which is an IT-level decision that applies to every computer and therefore
            // outranks one machine's own configuration.
            var dbValue = await _configService.GetSettingValueAsync(
                ConfigSettingKeys.ImageStorageSharedFolderPath, "all_users");
            
            if (dbValue != null && !string.IsNullOrWhiteSpace(dbValue.SettingValue))
            {
                _logger.LogInformation("Using database override for shared folder path: {Path}",
                                     dbValue.SettingValue);
                CacheValue(cacheKey, dbValue.SettingValue, "database");
                return dbValue.SettingValue;
            }

            // This machine's own configured folder, which is what machine setup captured (T155, FR-025).
            var machineValue = await ReadMachineFolderAsync(MachineConfigurationSourceKinds.SharedFolder).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(machineValue))
            {
                _logger.LogInformation("Using this machine's configured shared folder path: {Path}", machineValue);
                CacheValue(cacheKey, machineValue, "machine-configuration");
                return machineValue;
            }

            // Fall back to this build's shipped default, which is what a machine that names no folder is left with.
            var appsettingsValue = _appsettingsOptions.Value.SharedFolderPath;
            _logger.LogInformation("Using the shipped default for shared folder path: {Path}",
                                 appsettingsValue);
            CacheValue(cacheKey, appsettingsValue, "default");
            return appsettingsValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve shared folder path configuration");
            throw new InvalidOperationException(
                "Failed to resolve image storage shared folder path configuration", ex);
        }
    }

    /// <inheritdoc />
    public async Task<string> GetKeysFolderPathAsync()
    {
        try
        {
            var cacheKey = ConfigSettingKeys.KeysFolderPath;

            if (_cache.TryGetValue(cacheKey, out var cached) && cached.IsValid())
            {
                _logger.LogDebug("Using cached key-files folder path: {Source}", cached.Source);
                return (string)cached.Value;
            }

            var dbValue = await _configService.GetSettingValueAsync(
                ConfigSettingKeys.KeysFolderPath, "all_users");

            if (dbValue != null && !string.IsNullOrWhiteSpace(dbValue.SettingValue))
            {
                _logger.LogInformation("Using database override for key-files folder path: {Path}",
                                     dbValue.SettingValue);
                CacheValue(cacheKey, dbValue.SettingValue, "database");
                return dbValue.SettingValue;
            }

            var machineValue = await ReadMachineFolderAsync(MachineConfigurationSourceKinds.KeysFolder).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(machineValue))
            {
                _logger.LogInformation("Using this machine's configured key-files folder path: {Path}", machineValue);
                CacheValue(cacheKey, machineValue, "machine-configuration");
                return machineValue;
            }

            var appsettingsValue = _appsettingsOptions.Value.KeysFolderPath;
            _logger.LogInformation("Using the shipped default for key-files folder path: {Path}",
                                 appsettingsValue);
            CacheValue(cacheKey, appsettingsValue, "default");
            return appsettingsValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve the key-files folder configuration");
            throw new InvalidOperationException(
                "Failed to resolve the key-files folder configuration", ex);
        }
    }

    /// <inheritdoc />
    public async Task<string> GetImageCacheFolderPathAsync()
    {
        try
        {
            var cacheKey = ConfigSettingKeys.ImageCacheFolderPath;

            if (_cache.TryGetValue(cacheKey, out var cached) && cached.IsValid())
            {
                return (string)cached.Value;
            }

            var dbValue = await _configService.GetSettingValueAsync(
                ConfigSettingKeys.ImageCacheFolderPath, "all_users");

            if (dbValue != null && !string.IsNullOrWhiteSpace(dbValue.SettingValue))
            {
                _logger.LogInformation("Using database override for the picture cache folder: {Path}",
                                     dbValue.SettingValue);
                CacheValue(cacheKey, dbValue.SettingValue, "database");
                return dbValue.SettingValue;
            }

            var appsettingsValue = _appsettingsOptions.Value.CacheFolderPath;
            _logger.LogInformation("Using appsettings.json default for the picture cache folder: {Path}",
                                 appsettingsValue);
            CacheValue(cacheKey, appsettingsValue, "appsettings");
            return appsettingsValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve the picture cache folder configuration");
            throw new InvalidOperationException(
                "Failed to resolve the picture cache folder configuration", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> GetImageCacheEnabledAsync()
    {
        try
        {
            var cacheKey = ConfigSettingKeys.ImageCacheEnabled;

            if (_cache.TryGetValue(cacheKey, out var cached) && cached.IsValid())
            {
                return (bool)cached.Value;
            }

            var dbValue = await _configService.GetSettingValueAsync(
                ConfigSettingKeys.ImageCacheEnabled, "all_users");

            if (dbValue?.SettingValueBool.HasValue == true)
            {
                _logger.LogInformation("Using database override for the picture cache: {Enabled}",
                                     dbValue.SettingValueBool);
                CacheValue(cacheKey, dbValue.SettingValueBool!.Value, "database");
                return dbValue.SettingValueBool.Value;
            }

            var appsettingsValue = _appsettingsOptions.Value.CacheEnabled;
            CacheValue(cacheKey, appsettingsValue, "appsettings");
            return appsettingsValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve whether the picture cache is enabled");
            throw new InvalidOperationException(
                "Failed to resolve whether the picture cache is enabled", ex);
        }
    }

    /// <inheritdoc />
    public async Task<long> GetMaxFileSizeBytesAsync()
    {
        try
        {
            var cacheKey = ConfigSettingKeys.ImageStorageMaxFileSizeBytes;
            
            // Try cache first
            if (_cache.TryGetValue(cacheKey, out var cached) && cached.IsValid())
            {
                _logger.LogDebug("Using cached max file size: {Source}", cached.Source);
                return (long)cached.Value;
            }

            // Try database override
            var dbValue = await _configService.GetSettingValueAsync(
                ConfigSettingKeys.ImageStorageMaxFileSizeBytes, "all_users");
            
            if (dbValue?.SettingValueInt.HasValue == true)
            {
                _logger.LogInformation("Using database override for max file size: {Size} bytes",
                                     dbValue.SettingValueInt);
                CacheValue(cacheKey, dbValue.SettingValueInt!.Value, "database");
                return dbValue.SettingValueInt.Value;
            }

            // Fall back to appsettings
            var appsettingsValue = _appsettingsOptions.Value.MaxFileSizeBytes;
            _logger.LogInformation("Using appsettings.json default for max file size: {Size} bytes",
                                 appsettingsValue);
            CacheValue(cacheKey, appsettingsValue, "appsettings");
            return appsettingsValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve max file size configuration");
            throw new InvalidOperationException(
                "Failed to resolve image storage max file size configuration", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> GetEnableArchiveVersioningAsync()
    {
        try
        {
            var cacheKey = ConfigSettingKeys.ImageStorageEnableArchiveVersioning;
            
            // Try cache first
            if (_cache.TryGetValue(cacheKey, out var cached) && cached.IsValid())
            {
                _logger.LogDebug("Using cached archive versioning flag: {Source}", cached.Source);
                return (bool)cached.Value;
            }

            // Try database override
            var dbValue = await _configService.GetSettingValueAsync(
                ConfigSettingKeys.ImageStorageEnableArchiveVersioning, "all_users");
            
            if (dbValue?.SettingValueBool.HasValue == true)
            {
                _logger.LogInformation("Using database override for archive versioning: {Enabled}",
                                     dbValue.SettingValueBool);
                CacheValue(cacheKey, dbValue.SettingValueBool!.Value, "database");
                return dbValue.SettingValueBool.Value;
            }

            // Fall back to appsettings
            var appsettingsValue = _appsettingsOptions.Value.EnableArchiveVersioning;
            _logger.LogInformation("Using appsettings.json default for archive versioning: {Enabled}",
                                 appsettingsValue);
            CacheValue(cacheKey, appsettingsValue, "appsettings");
            return appsettingsValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve archive versioning configuration");
            throw new InvalidOperationException(
                "Failed to resolve image storage archive versioning configuration", ex);
        }
    }

    /// <inheritdoc />
    public async Task<int> GetArchiveKeepDaysAsync()
    {
        try
        {
            var cacheKey = ConfigSettingKeys.ImageStorageArchiveKeepDays;
            
            // Try cache first
            if (_cache.TryGetValue(cacheKey, out var cached) && cached.IsValid())
            {
                _logger.LogDebug("Using cached archive keep days: {Source}", cached.Source);
                return (int)(long)cached.Value;
            }

            // Try database override
            var dbValue = await _configService.GetSettingValueAsync(
                ConfigSettingKeys.ImageStorageArchiveKeepDays, "all_users");
            
            if (dbValue?.SettingValueInt.HasValue == true)
            {
                _logger.LogInformation("Using database override for archive keep days: {Days}",
                                     dbValue.SettingValueInt);
                CacheValue(cacheKey, dbValue.SettingValueInt!.Value, "database");
                return (int)dbValue.SettingValueInt.Value;
            }

            // Fall back to appsettings
            var appsettingsValue = _appsettingsOptions.Value.ArchiveKeepDays;
            _logger.LogInformation("Using appsettings.json default for archive keep days: {Days}",
                                 appsettingsValue);
            CacheValue(cacheKey, (long)appsettingsValue, "appsettings");
            return appsettingsValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve archive keep days configuration");
            throw new InvalidOperationException(
                "Failed to resolve image storage archive keep days configuration", ex);
        }
    }

    /// <inheritdoc />
    public async Task<SharedFolderResolution> GetSharedFolderResolutionAsync()
    {
        // The path first, and through the ordinary cascade: the plant-wide override wins, then this machine's own
        // configured folder, then the shipped default, so the folder reported here is the folder every computer
        // reads (FR-009, OQ-3).
        var folderPath = await GetSharedFolderPathAsync().ConfigureAwait(false);
        var machineFolderPath = await ReadMachineFolderAsync(MachineConfigurationSourceKinds.SharedFolder).ConfigureAwait(false);

        return new SharedFolderResolution(
            folderPath,
            machineFolderPath?.Trim() ?? string.Empty);
    }

    /// <summary>
    /// The folder this machine holds for one picture source, or <c>null</c> when it holds none or there is no
    /// machine-configuration read to ask.
    /// </summary>
    /// <remarks>
    /// A store that cannot answer is answered with null rather than raising, so the caller falls through to the
    /// plant-wide override and then to the shipped default. A machine that cannot read its own configuration
    /// still resolves a folder, because a screen that shows pictures should not break over a preference.
    /// </remarks>
    private async Task<string?> ReadMachineFolderAsync(string sourceKind)
    {
        if (_machineConfiguration is null)
        {
            return null;
        }

        try
        {
            var state = await _machineConfiguration
                .GetStateAsync(CancellationToken.None)
                .ConfigureAwait(false);

            var source = state.PictureSources.FirstOrDefault(
                candidate => string.Equals(candidate.Kind, sourceKind, StringComparison.Ordinal));

            return string.IsNullOrWhiteSpace(source?.Path) ? null : source.Path.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "This machine's picture sources could not be read; the plant-wide value was used instead.");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<ImageStorageOptions> GetEffectiveConfigurationAsync()
    {
        _logger.LogInformation("Resolving effective image storage configuration with database overrides");

        try
        {
            _appsettingsOptions.Value.Validate();

            var sharedFolderPath = await GetSharedFolderPathAsync();
            var keysFolderPath = await GetKeysFolderPathAsync();
            var cacheFolderPath = await GetImageCacheFolderPathAsync();
            var cacheEnabled = await GetImageCacheEnabledAsync();
            var maxFileSize = await GetMaxFileSizeBytesAsync();
            var enableArchiveVersioning = await GetEnableArchiveVersioningAsync();
            var archiveKeepDays = await GetArchiveKeepDaysAsync();

            var effectiveOptions = new ImageStorageOptions
            {
                SharedFolderPath = sharedFolderPath,
                KeysFolderPath = keysFolderPath,
                CacheFolderPath = cacheFolderPath,
                CacheEnabled = cacheEnabled,
                MaxFileSizeBytes = maxFileSize,
                AllowedExtensions = _appsettingsOptions.Value.AllowedExtensions,
                RequireSquareAspectRatio = _appsettingsOptions.Value.RequireSquareAspectRatio,
                EnableArchiveVersioning = enableArchiveVersioning,
                ArchiveKeepDays = archiveKeepDays
            };

            effectiveOptions.Validate();

            _logger.LogInformation("Successfully resolved effective image storage configuration");
            return effectiveOptions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve effective image storage configuration");
            throw;
        }
    }

    /// <inheritdoc />
    public void InvalidateCache()
    {
        _logger.LogInformation("Invalidating image storage configuration cache");
        _cache.Clear();
    }

    /// <summary>
    /// Internal helper to cache a value with source tracking.
    /// </summary>
    private void CacheValue(string key, object value, string source)
    {
        var cached = new CachedValue<object>
        {
            Value = value,
            Source = source,
            CachedAtUtc = DateTime.UtcNow
        };
        _cache.AddOrUpdate(key, cached, (_, _) => cached);
    }

    /// <summary>
    /// Internal helper for tracking cached values with TTL.
    /// </summary>
    private sealed class CachedValue<T>
    {
        public T Value { get; init; } = default!;
        public string Source { get; init; } = string.Empty;
        public DateTime CachedAtUtc { get; init; }

        public bool IsValid() =>
            DateTime.UtcNow - CachedAtUtc < CacheTtl;
    }
}

/// <summary>
/// Which folder holds the pictures for every computer, and which folder this machine's own settings file names.
/// </summary>
/// <param name="FolderPath">
/// The folder every computer reads, resolved through the ordinary cascade, so the store's value when there is one.
/// This is the truth: a recorded picture is stored relative to it and resolves under it (FR-009, FR-010).
/// </param>
/// <param name="MachineFolderPath">The folder this machine's own configuration carries, as it was written.</param>
/// <remarks>
/// The two are reported together because the interesting case is the one where they differ: a machine still
/// carrying the old path in its settings file works from the store's answer while appearing to be configured
/// otherwise, and that has to be said in one line rather than left to look like a picture that is simply not there
/// (OQ-3: the store is the truth).
/// </remarks>
public sealed record SharedFolderResolution(string FolderPath, string MachineFolderPath)
{
    /// <summary>
    /// Whether this machine's own configuration names a folder other than the one every computer reads.
    /// </summary>
    /// <remarks>
    /// Compared after unifying separators and without regard to case, because the same share is written both ways
    /// across this application's files and a difference in spelling is not a difference in folder.
    /// </remarks>
    public bool MachineDisagrees =>
        MachineFolderPath.Length > 0
        && string.Equals(
            AppStoragePaths.NormalizeSeparators(MachineFolderPath),
            AppStoragePaths.NormalizeSeparators(FolderPath),
            StringComparison.OrdinalIgnoreCase) is false;
}

/// <summary>
/// Service interface for reading configuration values from the database.
/// Implemented by a data access service that queries config_settings_values.
/// </summary>
public interface IConfigSettingsValueService
{
    /// <summary>
    /// Gets a setting value from the database by key and scope.
    /// </summary>
    /// <param name="settingKey">The setting key (e.g., "image_storage.shared_folder_path")</param>
    /// <param name="scopeKey">The scope key (usually "all_users" for global settings)</param>
    /// <returns>The ConfigSettingValue or null if not found</returns>
    Task<ConfigSettingValue?> GetSettingValueAsync(string settingKey, string scopeKey);

    /// <summary>
    /// Sets or updates a setting value in the database.
    /// </summary>
    /// <param name="setting">The setting to save</param>
    /// <param name="updatedByUserId">The user ID making the change (optional)</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task SetSettingValueAsync(ConfigSettingValue setting, long? updatedByUserId = null);

    /// <summary>
    /// Deletes a setting value from the database.
    /// </summary>
    /// <param name="settingKey">The setting key to delete</param>
    /// <param name="scopeKey">The scope key</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task DeleteSettingValueAsync(string settingKey, string scopeKey);
}
