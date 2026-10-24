namespace MTM_Waitlist.Module_Settings.Models;

using MTM_Waitlist.Module_Shared.Helpers;

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
    public long? ComputerId { get; init; }

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
    /// Description: IT-configured path to the shared folder the application's pictures are copied into.
    ///             When set, overrides the value from appsettings.json.
    /// Default: \\mtmanu-fs01\Expo Drive\MH_RESOURCE\Material_Handler\MTM Applications\MTM Waitlist Application\Images
    /// </summary>
    public const string ImageStorageSharedFolderPath = "image_storage.shared_folder_path";

    /// <summary>
    /// The folder the hopper reference pictures sit in, relative to the picture folder (or absolute, in which case
    /// the picture folder is not consulted).
    /// <para>
    /// Setting Key: "image_storage.hopper_folder_path"
    /// </para>
    /// <para>
    /// Description: IT-configured location of the three hopper size reference pictures, one file per size named
    /// after the size word. Owned by the storage screen (Settings, the storage panel) rather than compiled in, so
    /// where the hopper artwork lives is the owner's to change without a new release (FR-078). When set, overrides
    /// the value from appsettings.json. The naming contract for the files inside it is
    /// <c>docs/guides/guides-pictures-image-naming.md</c>.
    /// </para>
    /// Default: <see cref="ImageStorageHopperFolderPathDefault"/>
    /// (<c>\\mtmanu-fs01\Expo Drive\MH_RESOURCE\…\Images\Waitlist\hopper</c>).
    /// </summary>
    public const string ImageStorageHopperFolderPath = "image_storage.hopper_folder_path";

    /// <summary>
    /// The hopper picture folder this application ships with, given as the share's own path, and the one place the
    /// figure is written: the storage screen's box, the folder dialog and the compiled fallback all read it from
    /// here.
    /// </summary>
    /// <remarks>
    /// Absolute rather than relative to the picture folder: this is the folder the hopper artwork already lives in,
    /// so a machine that has never been told otherwise still finds the three size pictures. The spelling of the
    /// path itself belongs to <see cref="AppStoragePaths.HopperFolderDefault"/>.
    /// </remarks>
    public const string ImageStorageHopperFolderPathDefault = AppStoragePaths.HopperFolderDefault;

    /// <summary>
    /// The folder the lugger pictures sit in, one file per lugger.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Setting Key: "image_storage.lugger_folder_path"
    /// </para>
    /// <para>
    /// Description: IT-configured location of the lugger pictures the arrangement's cards draw behind themselves.
    /// The file for a lugger is its place in the arrangement, two digits wide, with a picture extension:
    /// <c>Lugger01.png</c> … <c>Lugger09.png</c>, <c>Lugger10.png</c> … <c>Lugger99.png</c>. There is no Lugger100:
    /// the arrangement holds at most 99 luggers, and a placement past that draws no picture rather than a wrong one.
    /// </para>
    /// <para>
    /// Default: <see cref="ImageStorageLuggerFolderPathDefault"/>
    /// (<c>\\mtmanu-fs01\Expo Drive\MH_RESOURCE\…\Images\Waitlist\lugger</c>).
    /// </para>
    /// </remarks>
    public const string ImageStorageLuggerFolderPath = "image_storage.lugger_folder_path";

    /// <summary>
    /// The lugger picture folder this application ships with, given as the share's own path.
    /// </summary>
    /// <remarks>
    /// Default: <see cref="AppStoragePaths.LuggerFolderDefault"/>.
    /// </remarks>
    public const string ImageStorageLuggerFolderPathDefault = AppStoragePaths.LuggerFolderDefault;

    /// <summary>
    /// The picture drawn wherever a surface has no picture of its own.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Setting Key: "image_storage.no_image_path"
    /// </para>
    /// <para>
    /// Description: the plant's own "no picture" artwork, held once for every computer like the folders above. It
    /// is one file rather than a folder because there is only ever one answer: an item, a work center and a lugger
    /// with no picture all draw the same thing, which is what teaches a person to read it as "no picture". Empty
    /// means the artwork that ships with the application.
    /// </para>
    /// <para>
    /// Default: <see cref="ImageStorageNoImagePathDefault"/>
    /// (<c>Assets/Placeholders/default-no-image.png</c>, the shipped artwork).
    /// </para>
    /// </remarks>
    public const string ImageStorageNoImagePath = "image_storage.no_image_path";

    /// <summary>
    /// The no-picture artwork this application ships with, relative to the deployment folder.
    /// </summary>
    /// <remarks>
    /// Default: <see cref="ImagePicturePolicy.NoImagePath"/>, which is also what every surface drew before this
    /// setting existed, so a plant that never changes it sees no difference.
    /// </remarks>
    public const string ImageStorageNoImagePathDefault = ImagePicturePolicy.NoImagePath;

    /// <summary>
    /// The root the dunnage pictures sit under, one folder per dunnage type and part beneath it.
    /// Setting Key: "dunnage.root_path"
    /// Value Type: "text" (string path)
    /// Scope: "all_users" (global setting)
    /// Description: IT-configured path to the shared folder the dunnage pictures are read from. It is held once
    ///             for the whole plant so no computer captures a folder of its own (FR-040). When set, overrides
    ///             the value from appsettings.json.
    /// Default: \\mtmanu-fs01\Expo Drive\Software Development\Live Applications\Shared\Images\Dunnage
    /// </summary>
    public const string DunnageRootPath = "dunnage.root_path";

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
    ///             <b>Owned by the storage screen</b> (Settings, the storage panel), not compiled in: the panel
    ///             writes this row and the cleanup reads it, so the size of the archive is the owner's to bound
    ///             without a new release (FR-037).
    /// Default: <see cref="ImageStorageArchiveKeepDaysDefault"/> (ninety days, which is what appsettings.json
    ///             ships and what the screen's box starts at).
    /// </summary>
    public const string ImageStorageArchiveKeepDays = "image_storage.archive_keep_days";

    /// <summary>
    /// The retention period this application ships with, in days, and the one place the figure is written: the
    /// shipped configuration, the storage screen's box and the compiled fallback all read it from here.
    /// </summary>
    /// <remarks>
    /// A name rather than a literal in three files. The period is a setting rather than a constant (FR-037), so
    /// the only thing that may be fixed at build time is the figure a machine uses before anybody changes it.
    /// </remarks>
    public const int ImageStorageArchiveKeepDaysDefault = 90;

    /// <summary>
    /// The folder on this computer that pictures are mirrored into.
    /// Setting Key: "image_cache.folder_path"
    /// Value Type: "text" (string path)
    /// Scope: "all_users" (global setting)
    /// Description: IT-configured local folder for the picture cache. When set, overrides appsettings.json.
    /// Default: %LOCALAPPDATA%\MTM_Waitlist\ImageCache
    /// </summary>
    public const string ImageCacheFolderPath = "image_cache.folder_path";

    /// <summary>
    /// Whether startup mirrors the picture roots onto this computer.
    /// Setting Key: "image_cache.enabled"
    /// Value Type: "bool" (true/false)
    /// Scope: "all_users" (global setting)
    /// Description: When false, no picture is copied locally and every screen reads from the share.
    /// Default: true
    /// </summary>
    public const string ImageCacheEnabled = "image_cache.enabled";

    /// <summary>
    /// How much the log store keeps, as a severity name from the store's own vocabulary.
    /// Setting Key: "logging.store_minimum_level"
    /// Value Type: "text" (a <c>LogSeverity</c> name: Debug, Info, Warning, Error or Critical)
    /// Scope: "all_users" (global setting)
    /// Description: the least severe entry the store keeps. Held once for the plant so one decision covers every
    ///             computer, and changed from Settings by a person holding the diagnostics entitlement. The shipped
    ///             default is <c>Warning</c> — errors and warnings are kept and ordinary progress lines are dropped
    ///             — and <c>LogStore:MinimumLevel</c> in <c>appsettings.json</c> is what a machine uses until
    ///             somebody changes it. A value that names no severity answers the default rather than silencing
    ///             the store.
    /// Default: Warning
    /// </summary>
    public const string LogStoreMinimumLevel = "logging.store_minimum_level";

    /// <summary>
    /// Whether clicking a picture shows it enlarged.
    /// Setting Key: "display.enlarge_pictures_on_click"
    /// Value Type: "bool" (true/false)
    /// Scope: "user" (the person's own preference, scope key "user:&lt;id&gt;") — the only setting stored this way
    ///          as a flag; the alternative is a workstation value that two people sharing a computer would share.
    /// Description: When false, pictures are drawn as before and clicking one does nothing.
    /// Default: true
    /// </summary>
    public const string EnlargePicturesOnClick = "display.enlarge_pictures_on_click";
}
