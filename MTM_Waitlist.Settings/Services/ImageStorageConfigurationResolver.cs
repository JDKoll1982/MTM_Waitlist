using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// Implementation of IImageStorageConfigurationResolver.
/// Resolves image storage configuration with support for database overrides.
/// Uses a cache with invalidation support for performance.
/// </summary>
public sealed class ImageStorageConfigurationResolver : IImageStorageConfigurationResolver
{
    private readonly ILogger<ImageStorageConfigurationResolver> _logger;
    private readonly IOptions<ImageStorageOptions> _appsettingsOptions;
    private readonly IConfigSettingsValueService _configService;
    
    // Cache for resolved values with TTL
    private readonly ConcurrentDictionary<string, CachedValue<object>> _cache;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Initializes a new ImageStorageConfigurationResolver.
    /// </summary>
    /// <param name="logger">Logger for diagnostics</param>
    /// <param name="appsettingsOptions">Configuration options from appsettings.json</param>
    /// <param name="configService">Service for reading database configuration values</param>
    /// <exception cref="ArgumentNullException">If any parameter is null</exception>
    public ImageStorageConfigurationResolver(
        ILogger<ImageStorageConfigurationResolver> logger,
        IOptions<ImageStorageOptions> appsettingsOptions,
        IConfigSettingsValueService configService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _appsettingsOptions = appsettingsOptions ?? throw new ArgumentNullException(nameof(appsettingsOptions));
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
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

            // Try database override
            var dbValue = await _configService.GetSettingValueAsync(
                ConfigSettingKeys.ImageStorageSharedFolderPath, "all_users");
            
            if (dbValue != null && !string.IsNullOrWhiteSpace(dbValue.SettingValue))
            {
                _logger.LogInformation("Using database override for shared folder path: {Path}",
                                     dbValue.SettingValue);
                CacheValue(cacheKey, dbValue.SettingValue, "database");
                return dbValue.SettingValue;
            }

            // Fall back to appsettings
            var appsettingsValue = _appsettingsOptions.Value.SharedFolderPath;
            _logger.LogInformation("Using appsettings.json default for shared folder path: {Path}",
                                 appsettingsValue);
            CacheValue(cacheKey, appsettingsValue, "appsettings");
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
    public async Task<ImageStorageOptions> GetEffectiveConfigurationAsync()
    {
        _logger.LogInformation("Resolving effective image storage configuration with database overrides");

        try
        {
            _appsettingsOptions.Value.Validate();

            var sharedFolderPath = await GetSharedFolderPathAsync();
            var maxFileSize = await GetMaxFileSizeBytesAsync();
            var enableArchiveVersioning = await GetEnableArchiveVersioningAsync();
            var archiveKeepDays = await GetArchiveKeepDaysAsync();

            var effectiveOptions = new ImageStorageOptions
            {
                SharedFolderPath = sharedFolderPath,
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
