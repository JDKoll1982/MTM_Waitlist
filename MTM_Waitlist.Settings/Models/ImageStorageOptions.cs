namespace MTM_Waitlist.Module_Settings.Models;

using MTM_Waitlist.Module_Shared.Helpers;

/// <summary>
/// Configuration options for image storage locations and shared network folder settings.
/// 
/// The shared folder and the keys folder are this machine's own configuration, captured by machine setup and
/// read from the machine's configuration rows (T155, FR-025). The values here are the shipped defaults, used only
/// when neither this machine nor the plant names a folder, so a build carries a sane starting point without
/// carrying any machine's or person's state.
/// 
/// Every path is resolved through the same cascade:
/// 1. This machine's own configured folder, from its configuration rows
/// 2. The plant-wide override in config_settings_values, when one exists
/// 3. The shipped default on this class, when neither of the above names a folder
/// </summary>
public sealed class ImageStorageOptions
{
    /// <summary>
    /// Key used in configuration binding: "ImageStorage"
    /// </summary>
    public const string SectionName = "ImageStorage";

    /// <summary>
    /// The root every configured picture is copied into, one folder per scope beneath it.
    /// Default: \\mtmanu-fs01\Expo Drive\MH_RESOURCE\Material_Handler\MTM Applications\MTM Waitlist Application\Images
    /// The folder in force is the one this machine was configured with at setup, so the same picture is read from
    /// every computer rather than only from the one that mapped the drive. What is written here is the fallback a
    /// machine that names no folder is left with.
    /// </summary>
    public string SharedFolderPath { get; init; } = AppStoragePaths.ImagesRootDefault;

    /// <summary>
    /// The folder holding the key files, one per key, named "{keyname}.txt".
    /// Default: \\mtmanu-fs01\Expo Drive\MH_RESOURCE\Material_Handler\MTM Applications\Keys - DO NOT EDIT FILES\MTM Waitlist Application
    /// Configured with the other folders at machine setup, and this is the fallback.
    /// Nothing reads these files yet. See <see cref="AppStoragePaths.KeysFolderDefault"/>.
    /// </summary>
    public string KeysFolderPath { get; init; } = AppStoragePaths.KeysFolderDefault;

    /// <summary>
    /// The folder on this computer that every picture is mirrored into at startup.
    /// Default: %LOCALAPPDATA%\MTM_Waitlist\ImageCache
    /// Can be overridden by IT Department or Developer through the database (config_settings_values).
    /// </summary>
    public string CacheFolderPath { get; init; } = ImageCachePaths.DefaultCacheRoot;

    /// <summary>
    /// Whether startup mirrors the picture roots onto this computer.
    /// Default: true
    /// When false, nothing is copied and every screen reads its pictures from the share.
    /// </summary>
    public bool CacheEnabled { get; init; } = true;

    /// <summary>
    /// Maximum file size in bytes for uploaded images.
    /// Default: 10 MB (10485760 bytes)
    /// Reject images larger than this size with a user-friendly error message.
    /// </summary>
    public long MaxFileSizeBytes { get; init; } = 10485760; // 10 MB

    /// <summary>
    /// Collection of allowed file extensions (e.g., .png, .jpg, .jpeg).
    /// Default: [".png", ".jpg", ".jpeg"]
    /// Case-insensitive matching.
    /// </summary>
    public IReadOnlyList<string> AllowedExtensions { get; init; } = new[] { ".png", ".jpg", ".jpeg" };

    /// <summary>
    /// Indicates if uploaded images must have a square aspect ratio.
    /// Default: true
    /// If true, reject non-square images with a validation error.
    /// </summary>
    public bool RequireSquareAspectRatio { get; init; } = true;

    /// <summary>
    /// Indicates if archive versioning is enabled for replaced image files.
    /// Default: true
    /// When enabled, old files are renamed with a timestamp suffix before being replaced.
    /// Example: "custom-request-type.png" → "custom-request-type.2026-08-18_14-30-45.png"
    /// </summary>
    public bool EnableArchiveVersioning { get; init; } = true;

    /// <summary>
    /// Number of days to keep archived image files before cleanup.
    /// Default: <see cref="ConfigSettingKeys.ImageStorageArchiveKeepDaysDefault"/> (ninety days)
    /// Used for retention policy when archive cleanup is run.
    /// Set to 0 to disable automatic cleanup.
    /// </summary>
    /// <remarks>
    /// The value here is only the fallback for a machine whose configuration file does not carry the key. The
    /// period in force is the stored setting, which the storage screen owns (FR-037).
    /// </remarks>
    public int ArchiveKeepDays { get; init; } = ConfigSettingKeys.ImageStorageArchiveKeepDaysDefault;

    /// <summary>
    /// Validates that the configuration is well-formed.
    /// Throws InvalidOperationException if validation fails.
    /// </summary>
    /// <exception cref="InvalidOperationException">If configuration is invalid</exception>
    public void Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(SharedFolderPath))
        {
            errors.Add("SharedFolderPath cannot be null or empty");
        }

        if (MaxFileSizeBytes <= 0)
        {
            errors.Add("MaxFileSizeBytes must be greater than 0");
        }

        if (MaxFileSizeBytes > 1073741824) // 1 GB
        {
            errors.Add("MaxFileSizeBytes cannot exceed 1 GB (1073741824 bytes)");
        }

        if (AllowedExtensions == null || AllowedExtensions.Count == 0)
        {
            errors.Add("AllowedExtensions must contain at least one extension");
        }
        else
        {
            foreach (var ext in AllowedExtensions)
            {
                if (string.IsNullOrWhiteSpace(ext) || !ext.StartsWith("."))
                {
                    errors.Add($"Invalid extension format: '{ext}' (must start with '.')");
                }
            }
        }

        if (ArchiveKeepDays < 0)
        {
            errors.Add("ArchiveKeepDays cannot be negative");
        }

        if (errors.Any())
        {
            throw new InvalidOperationException(
                $"ImageStorageOptions validation failed:{Environment.NewLine}" +
                string.Join(Environment.NewLine, errors.Select(e => $"  - {e}")));
        }
    }

    /// <summary>
    /// Checks if a file extension is allowed.
    /// Case-insensitive matching.
    /// </summary>
    /// <param name="extension">The file extension to check (with or without leading dot)</param>
    /// <returns>True if the extension is allowed; false otherwise</returns>
    public bool IsExtensionAllowed(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return false;
        }

        var ext = extension.StartsWith(".") ? extension : $".{extension}";
        return AllowedExtensions.Any(a => string.Equals(a, ext, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets a human-readable list of allowed file extensions for error messages.
    /// </summary>
    /// <returns>Comma-separated list of allowed extensions (e.g., ".png, .jpg, .jpeg")</returns>
    public string GetAllowedExtensionsDisplay() =>
        string.Join(", ", AllowedExtensions.OrderBy(e => e));

    /// <summary>
    /// Formats the maximum file size as a human-readable string.
    /// </summary>
    /// <returns>Formatted size string (e.g., "10 MB")</returns>
    public string GetMaxFileSizeDisplay()
    {
        const long kilobyte = 1024;
        const long megabyte = kilobyte * 1024;
        const long gigabyte = megabyte * 1024;

        return MaxFileSizeBytes switch
        {
            >= gigabyte => $"{MaxFileSizeBytes / (double)gigabyte:F1} GB",
            >= megabyte => $"{MaxFileSizeBytes / (double)megabyte:F1} MB",
            >= kilobyte => $"{MaxFileSizeBytes / (double)kilobyte:F1} KB",
            _ => $"{MaxFileSizeBytes} bytes"
        };
    }
}
