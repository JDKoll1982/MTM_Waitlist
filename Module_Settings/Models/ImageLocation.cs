namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// Represents a single image location override or resolved image path.
/// Used for both database overrides and resolved final paths.
/// </summary>
public sealed class ImageLocation
{
    /// <summary>
    /// The scope type: request_type, request_subtype, or work_center.
    /// </summary>
    public ImageLocationScope Scope { get; init; }

    /// <summary>
    /// Stable identifier within scope.
    /// For request types/subtypes: GUID (from waitlist-request-types.json)
    /// For work centers: numeric ID (from setup_workstations_catalog.id)
    /// </summary>
    public string ScopeItemId { get; init; } = string.Empty;

    /// <summary>
    /// Display name of the item (for UI purposes only, not for persistence).
    /// For request types: The requestType string (e.g., "Pickup")
    /// For subtypes: The subtype name (e.g., "Pickup Other")
    /// For work centers: The workstation_name (e.g., "Press 1")
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// The effective image path after cascade resolution.
    /// Always a valid path (never null). May be default asset if no override/config exists.
    /// </summary>
    public string ResolvedImagePath { get; init; } = string.Empty;

    /// <summary>
    /// Indicates the source level where this path was resolved.
    /// Useful for UI warnings and diagnostics.
    /// </summary>
    public ImagePathResolutionLevel ResolutionLevel { get; init; }

    /// <summary>
    /// For subtypes only: the GUID of the parent request type.
    /// Null for request types and work centers.
    /// </summary>
    public string? ParentRequestTypeId { get; init; }

    /// <summary>
    /// Indicates if a warning should be shown (e.g., stored file not found).
    /// </summary>
    public string? ResolutionWarning { get; init; }

    /// <summary>
    /// Indicates if the resolved file actually exists on the file system.
    /// False if the file was moved, deleted, or the share is unreachable.
    /// </summary>
    public bool FileExists { get; init; } = true;
}

/// <summary>
/// Indicates the cascade resolution level where an image path was resolved.
/// Used for diagnostics and inline warnings in the UI.
/// </summary>
public enum ImagePathResolutionLevel
{
    /// <summary>
    /// Resolved from database override (config_images_locations with is_active=1)
    /// </summary>
    DatabaseOverride,

    /// <summary>
    /// Resolved from JSON configuration (request_type.imagePath or subtype.imagePath)
    /// </summary>
    JsonConfiguration,

    /// <summary>
    /// Resolved from parent request type (for subtypes only; cascades to parent resolution)
    /// </summary>
    ParentRequestType,

    /// <summary>
    /// Resolved to scope default asset (final fallback; always available)
    /// </summary>
    ScopeDefault
}

/// <summary>
/// Represents a request type image location mapping.
/// Captures the stable ID, display name, and default image for a request type.
/// </summary>
public sealed class RequestTypeImageMapping
{
    /// <summary>
    /// Stable GUID identifier from waitlist-request-types.json
    /// Never changes, even if display name is renamed.
    /// </summary>
    public string RequestTypeId { get; init; } = string.Empty;

    /// <summary>
    /// Display name of the request type (e.g., "Pickup", "Coil", "Scrap")
    /// This can change without affecting stored overrides (because we use stable ID).
    /// </summary>
    public string RequestTypeName { get; init; } = string.Empty;

    /// <summary>
    /// Default image path when no override or JSON config exists.
    /// </summary>
    public string DefaultImagePath { get; init; } = ImageLocationDefaults.RequestTypeDefaultPath;

    /// <summary>
    /// Optional image path from JSON configuration (waitlist-request-types.json)
    /// Null if not configured in JSON.
    /// </summary>
    public string? JsonConfiguredImagePath { get; init; }

    /// <summary>
    /// Currently active database override, if any.
    /// Null if no override is stored.
    /// </summary>
    public string? DatabaseOverridePath { get; init; }
}

/// <summary>
/// Represents a request subtype image location mapping.
/// Captures the stable ID, parent reference, display name, and default image for a subtype.
/// </summary>
public sealed class RequestSubtypeImageMapping
{
    /// <summary>
    /// Stable globally-unique GUID identifier from waitlist-request-types.json
    /// Never changes, even if display name is renamed.
    /// </summary>
    public string SubtypeId { get; init; } = string.Empty;

    /// <summary>
    /// Stable GUID of the parent request type.
    /// Used to resolve inherited images when subtype has no override.
    /// </summary>
    public string ParentRequestTypeId { get; init; } = string.Empty;

    /// <summary>
    /// Display name of the parent request type (for UI organization).
    /// </summary>
    public string ParentRequestTypeName { get; init; } = string.Empty;

    /// <summary>
    /// Display name of the subtype (e.g., "Pickup Other", "Pickup NCM", "Bring", etc.)
    /// Not globally unique; multiple subtypes may share the same name across parents.
    /// </summary>
    public string SubtypeName { get; init; } = string.Empty;

    /// <summary>
    /// Default image path when no override, subtype JSON config, or parent image exists.
    /// Same as request type default.
    /// </summary>
    public string DefaultImagePath { get; init; } = ImageLocationDefaults.RequestSubtypeDefaultPath;

    /// <summary>
    /// Optional image path from JSON configuration (subtype.imagePath in waitlist-request-types.json)
    /// Null if not configured in JSON.
    /// </summary>
    public string? JsonConfiguredImagePath { get; init; }

    /// <summary>
    /// Currently active database override, if any.
    /// Null if no override is stored.
    /// </summary>
    public string? DatabaseOverridePath { get; init; }

    /// <summary>
    /// Indicates if this subtype inherits its image from the parent request type.
    /// True if no subtype-specific override or JSON config exists.
    /// </summary>
    public bool InheritsFromParent =>
        string.IsNullOrEmpty(DatabaseOverridePath) && string.IsNullOrEmpty(JsonConfiguredImagePath);
}

/// <summary>
/// Represents a work center image location mapping.
/// Captures the stable ID, display name, and default image for a work center.
/// Note: Work centers have no JSON configuration; only database overrides.
/// </summary>
public sealed class WorkCenterImageMapping
{
    /// <summary>
    /// Numeric ID from setup_workstations_catalog.id
    /// </summary>
    public long WorkCenterId { get; init; }

    /// <summary>
    /// Display name of the work center (e.g., "Press 1", "Brake 3")
    /// From setup_workstations_catalog.workstation_name
    /// </summary>
    public string WorkCenterName { get; init; } = string.Empty;

    /// <summary>
    /// Building location of the work center.
    /// From setup_workstations_catalog.building
    /// </summary>
    public string Building { get; init; } = string.Empty;

    /// <summary>
    /// Default image path when no override exists.
    /// </summary>
    public string DefaultImagePath { get; init; } = ImageLocationDefaults.WorkCenterDefaultPath;

    /// <summary>
    /// Currently active database override, if any.
    /// Null if no override is stored.
    /// Note: Work centers have no JSON configuration, only database overrides.
    /// </summary>
    public string? DatabaseOverridePath { get; init; }

    /// <summary>
    /// Indicates if this work center has a custom image override.
    /// </summary>
    public bool HasCustomImage => !string.IsNullOrEmpty(DatabaseOverridePath);
}
