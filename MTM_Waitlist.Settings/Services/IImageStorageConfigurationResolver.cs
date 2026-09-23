using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// Service for resolving image storage configuration with database override support.
/// Implements the configuration cascade: Database Override → appsettings.json → Hard-coded Default
/// </summary>
public interface IImageStorageConfigurationResolver
{
    /// <summary>
    /// Gets the effective shared folder path with database override support.
    /// Resolution order:
    /// 1. Database override from config_settings_values (if set and not null)
    /// 2. appsettings.json ImageStorage.SharedFolderPath
    /// 3. Hard-coded default from <c>AppStoragePaths.ImagesRootDefault</c>
    /// </summary>
    /// <returns>The effective shared folder path</returns>
    /// <exception cref="InvalidOperationException">If configuration is invalid or inaccessible</exception>
    Task<string> GetSharedFolderPathAsync();

    /// <summary>
    /// Gets the effective key-files folder path with database override support.
    /// Resolution order:
    /// 1. Database override from config_settings_values (if set and not null)
    /// 2. appsettings.json ImageStorage.KeysFolderPath
    /// 3. Hard-coded default from <c>AppStoragePaths.KeysFolderDefault</c>
    /// </summary>
    /// <returns>The effective key-files folder path</returns>
    /// <exception cref="InvalidOperationException">If configuration cannot be resolved</exception>
    Task<string> GetKeysFolderPathAsync();

    /// <summary>
    /// Gets the effective local picture cache folder with database override support.
    /// Resolution order:
    /// 1. Database override from config_settings_values (if set and not null)
    /// 2. appsettings.json ImageStorage.CacheFolderPath
    /// 3. Hard-coded default from <c>ImageCachePaths.DefaultCacheRoot</c>
    /// </summary>
    /// <returns>The effective cache folder path</returns>
    /// <exception cref="InvalidOperationException">If configuration cannot be resolved</exception>
    Task<string> GetImageCacheFolderPathAsync();

    /// <summary>
    /// Gets whether startup mirrors the picture roots onto this computer.
    /// Resolution order:
    /// 1. Database override from config_settings_values (if set and is bool type)
    /// 2. appsettings.json ImageStorage.CacheEnabled
    /// 3. Hard-coded default: true
    /// </summary>
    /// <returns><see langword="true"/> when pictures are cached locally.</returns>
    Task<bool> GetImageCacheEnabledAsync();

    /// <summary>
    /// Gets the effective maximum file size in bytes with database override support.
    /// Resolution order:
    /// 1. Database override from config_settings_values (if set and is int type)
    /// 2. appsettings.json ImageStorage.MaxFileSizeBytes
    /// 3. Hard-coded default: 10485760 (10 MB)
    /// </summary>
    /// <returns>The maximum file size in bytes</returns>
    Task<long> GetMaxFileSizeBytesAsync();

    /// <summary>
    /// Gets the effective archive versioning enabled flag with database override support.
    /// Resolution order:
    /// 1. Database override from config_settings_values (if set and is bool type)
    /// 2. appsettings.json ImageStorage.EnableArchiveVersioning
    /// 3. Hard-coded default: true
    /// </summary>
    /// <returns>True if archive versioning is enabled; false otherwise</returns>
    Task<bool> GetEnableArchiveVersioningAsync();

    /// <summary>
    /// Gets the effective archive retention days with database override support.
    /// Resolution order:
    /// 1. Database override from config_settings_values (if set and is int type)
    /// 2. appsettings.json ImageStorage.ArchiveKeepDays
    /// 3. Hard-coded default: 30
    /// </summary>
    /// <returns>The number of days to retain archived files</returns>
    Task<int> GetArchiveKeepDaysAsync();

    /// <summary>
    /// Gets the folder every computer reads for its pictures, together with the folder this machine's own
    /// configuration names, so a screen can say which is the truth and name the other in one line.
    /// </summary>
    /// <returns>
    /// The resolved folder, which is the store's value whenever the store holds one, and this machine's own
    /// configured folder beside it (FR-009, FR-010).
    /// </returns>
    /// <exception cref="InvalidOperationException">If the folder cannot be resolved at all.</exception>
    Task<SharedFolderResolution> GetSharedFolderResolutionAsync();

    /// <summary>
    /// Gets the complete effective ImageStorageOptions with all database overrides applied.
    /// </summary>
    /// <returns>The effective configuration options</returns>
    /// <exception cref="InvalidOperationException">If configuration is invalid</exception>
    Task<ImageStorageOptions> GetEffectiveConfigurationAsync();

    /// <summary>
    /// Clears the in-memory configuration cache.
    /// Call this when database overrides have been updated.
    /// </summary>
    void InvalidateCache();
}

/// <summary>
/// Configuration resolution result with source tracking.
/// </summary>
public sealed class ConfigurationResolutionResult<T>
{
    /// <summary>
    /// The resolved configuration value.
    /// </summary>
    public T Value { get; init; } = default!;

    /// <summary>
    /// The source of the resolved value: "database", "appsettings", or "default"
    /// </summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>
    /// Human-readable description of where the value came from.
    /// </summary>
    public string SourceDescription { get; init; } = string.Empty;
}
