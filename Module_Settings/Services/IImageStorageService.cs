using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// Service for managing image file storage in the shared network folder.
/// Handles validation, copying, and file organization.
/// 
/// Validation Rules (configurable via ImageValidationRules):
/// - File extensions: .png, .jpg, .jpeg only
/// - File size: 1 KB to 10 MB
/// - Image dimensions: 48px to 2048px (both width and height)
/// - Aspect ratio: Square (1:1) with 2% tolerance
/// 
/// Storage Behavior:
/// - Files are copied (not moved) to the configured image share folder
/// - File names include a date stamp to avoid collisions: {scope}_{item}_{date}.png
/// - Archive folder stores previous versions when files are replaced
/// - Share path must be readable/writable; operation fails if unreachable
/// </summary>
public interface IImageStorageService
{
    /// <summary>
    /// Validates an image file against the configured validation rules.
    /// Does not modify the file; only checks format, size, and dimensions.
    /// </summary>
    /// <param name="sourceFilePath">The full path to the source image file</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>ValidationResult indicating pass/fail with details</returns>
    /// <exception cref="ArgumentNullException">If sourceFilePath is null or empty</exception>
    /// <exception cref="InvalidOperationException">If file cannot be read</exception>
    Task<ImageValidationResult> ValidateImageAsync(string sourceFilePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies a validated image file to the shared network folder.
    /// File name includes date stamp: {scope}_{itemId}_{date}.{ext}
    /// If archive behavior is enabled, replaces existing file and archives the old one.
    /// </summary>
    /// <param name="sourceFilePath">The full path to the source image file</param>
    /// <param name="scope">The scope type (request_type, request_subtype, work_center) - used in file name</param>
    /// <param name="itemId">The item identifier - used in file name</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>StorageResult with final file path or error details</returns>
    /// <exception cref="ArgumentNullException">If sourceFilePath, scope, or itemId is null or empty</exception>
    /// <exception cref="InvalidOperationException">If share is unreachable or disk is full</exception>
    Task<ImageStorageResult> CopyImageToStorageAsync(
        string sourceFilePath,
        string scope,
        string itemId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates and copies an image in one operation.
    /// Equivalent to calling ValidateImageAsync then CopyImageToStorageAsync.
    /// </summary>
    /// <param name="sourceFilePath">The full path to the source image file</param>
    /// <param name="scope">The scope type - used in file name</param>
    /// <param name="itemId">The item identifier - used in file name</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>StorageResult with final file path or error/validation details</returns>
    /// <exception cref="ArgumentNullException">If any parameter is null or empty</exception>
    /// <exception cref="InvalidOperationException">If validation or storage fails</exception>
    Task<ImageStorageResult> ValidateAndStoreImageAsync(
        string sourceFilePath,
        string scope,
        string itemId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the configured image share folder is currently accessible.
    /// Useful for pre-flight checks before attempting to save images.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>True if share is readable and writable; false otherwise</returns>
    Task<bool> IsShareAccessibleAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the currently configured image share folder path.
    /// </summary>
    /// <returns>The UNC or local path to the image storage folder</returns>
    string GetConfiguredSharePath();

    /// <summary>
    /// Gets the currently configured validation rules.
    /// </summary>
    /// <returns>The ImageValidationRules being used for validation</returns>
    ImageValidationRules GetValidationRules();

    /// <summary>
    /// Deletes an image file from the storage folder.
    /// Note: Does not affect the database override record; that must be deleted separately.
    /// </summary>
    /// <param name="storedFilePath">The full path to the file in storage (from StorageResult.StoredFilePath)</param>
    /// <param name="moveToArchive">If true, moves file to archive folder; if false, deletes permanently</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>True if deleted/archived successfully; false if file not found</returns>
    /// <exception cref="ArgumentNullException">If storedFilePath is null or empty</exception>
    /// <exception cref="InvalidOperationException">If deletion/archive fails</exception>
    Task<bool> DeleteStoredImageAsync(string storedFilePath, bool moveToArchive = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the available disk space on the share folder.
    /// Useful for checking if there's enough space before copying large files.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Available space in bytes; -1 if unable to determine</returns>
    Task<long> GetAvailableDiskSpaceAsync(CancellationToken cancellationToken = default);
}
