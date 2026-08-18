namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// Represents a configuration setting stored in config_settings_values table.
/// Used for retrieving and managing overridable configuration values from the database.
/// </summary>
public sealed class ConfigSettingValue
{
    /// <summary>
    /// Primary key (BIGINT AUTO_INCREMENT)
    /// </summary>
    public long Id { get; init; }

    /// <summary>
    /// Public GUID identifier (CHAR(36))
    /// </summary>
    public string PublicId { get; init; } = string.Empty;

    /// <summary>
    /// Setting identifier key (e.g., "image_storage.shared_folder_path")
    /// Used for lookups and matching settings across scope boundaries.
    /// </summary>
    public string SettingKey { get; init; } = string.Empty;

    /// <summary>
    /// Scope type (e.g., "all_users", "workstation", "user")
    /// Determines the scope of this setting's application.
    /// </summary>
    public string ScopeType { get; init; } = "all_users";

    /// <summary>
    /// Scope key (e.g., "all_users", workstation_id, user_id)
    /// Paired with ScopeType to identify the scope target.
    /// </summary>
    public string ScopeKey { get; init; } = "all_users";

    /// <summary>
    /// Workstation ID (optional, for workstation-scoped settings)
    /// Nullable; used when scope_type is "workstation".
    /// </summary>
    public long? WorkstationId { get; init; }

    /// <summary>
    /// User ID (optional, for user-scoped settings)
    /// Nullable; used when scope_type is "user".
    /// </summary>
    public long? UserId { get; init; }

    /// <summary>
    /// Text value (for string settings)
    /// Null if value_type is not 'text' or if no text value is set.
    /// </summary>
    public string? SettingValue { get; init; }

    /// <summary>
    /// Integer value (for numeric settings)
    /// Null if value_type is not 'int' or if no int value is set.
    /// </summary>
    public long? SettingValueInt { get; init; }

    /// <summary>
    /// Boolean value (for flag settings)
    /// Null if value_type is not 'bool' or if no bool value is set.
    /// </summary>
    public bool? SettingValueBool { get; init; }

    /// <summary>
    /// Decimal value (for numeric precision settings)
    /// Null if value_type is not 'decimal' or if no decimal value is set.
    /// </summary>
    public decimal? SettingValueDecimal { get; init; }

    /// <summary>
    /// DateTime value (for timestamp settings)
    /// Null if value_type is not 'datetime' or if no datetime value is set.
    /// </summary>
    public DateTime? SettingValueDatetimeUtc { get; init; }

    /// <summary>
    /// Data type indicator: 'text', 'int', 'bool', 'decimal', 'datetime'
    /// Determines which value column should be read.
    /// </summary>
    public string ValueType { get; init; } = "text";

    /// <summary>
    /// User ID of the last person who updated this setting.
    /// Null if never manually updated (only seeded).
    /// </summary>
    public long? UpdatedByUserId { get; init; }

    /// <summary>
    /// UTC timestamp when this setting was last updated.
    /// </summary>
    public DateTime UpdatedUtc { get; init; }
}

/// <summary>
/// Well-known setting keys for configuration management.
/// Use these constants to ensure consistent naming across the codebase.
/// </summary>
public static class ConfigSettingKeys
{
    /// <summary>
    /// Image storage shared folder path override.
    /// Setting Key: "image_storage.shared_folder_path"
    /// Value Type: "text" (string path)
    /// Scope: "all_users" (global setting)
    /// Description: Admin-configured UNC path to the shared network folder for image files.
    ///             When set, overrides the value from appsettings.json.
    /// Example: "X:\Software Development\Live Applications\MTM_Waitlist\Images"
    /// </summary>
    public const string ImageStorageSharedFolderPath = "image_storage.shared_folder_path";

    /// <summary>
    /// Image storage maximum file size override.
    /// Setting Key: "image_storage.max_file_size_bytes"
    /// Value Type: "int" (long - bytes)
    /// Scope: "all_users" (global setting)
    /// Description: Maximum file size in bytes for uploaded images.
    ///             When set, overrides the value from appsettings.json.
    /// Default: 10485760 (10 MB)
    /// </summary>
    public const string ImageStorageMaxFileSizeBytes = "image_storage.max_file_size_bytes";

    /// <summary>
    /// Enable archive versioning for replaced images.
    /// Setting Key: "image_storage.enable_archive_versioning"
    /// Value Type: "bool" (true/false)
    /// Scope: "all_users" (global setting)
    /// Description: When true, old images are renamed with timestamps before replacement.
    /// Default: true (1)
    /// </summary>
    public const string ImageStorageEnableArchiveVersioning = "image_storage.enable_archive_versioning";

    /// <summary>
    /// Archive retention days for old image files.
    /// Setting Key: "image_storage.archive_keep_days"
    /// Value Type: "int" (days)
    /// Scope: "all_users" (global setting)
    /// Description: Number of days to keep archived image files before cleanup.
    /// Default: 30
    /// </summary>
    public const string ImageStorageArchiveKeepDays = "image_storage.archive_keep_days";
}
