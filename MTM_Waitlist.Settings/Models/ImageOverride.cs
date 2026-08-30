using System;

namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// Represents a single image override record from the config_images_locations table.
/// Immutable; used for reading overrides from the database.
/// </summary>
public sealed class ImageOverride
{
    /// <summary>
    /// Database row ID (BIGINT AUTO_INCREMENT).
    /// Not used for persistence (public_id is used for external reference).
    /// </summary>
    public long RecordId { get; init; }

    /// <summary>
    /// Public identifier (UUID CHAR(36)).
    /// Used for external API references and public linking.
    /// </summary>
    public string PublicId { get; init; } = string.Empty;

    /// <summary>
    /// Scope type: request_type, request_subtype, or work_center.
    /// </summary>
    public string Scope { get; init; } = string.Empty;

    /// <summary>
    /// Stable identifier within scope.
    /// For request types/subtypes: GUID string (from waitlist-request-types.json)
    /// For work centers: numeric ID string (from setup_workstations_catalog.id)
    /// </summary>
    public string ScopeItemId { get; init; } = string.Empty;

    /// <summary>
    /// File system path to the copied image.
    /// Examples: \\server\share\images\request_type_pickup_2026-08-18.png
    /// </summary>
    public string ImagePath { get; init; } = string.Empty;

    /// <summary>
    /// Soft-delete flag; inactive rows are ignored during resolution.
    /// Set to 0 when an override is logically deleted (kept for audit trail).
    /// </summary>
    public bool IsActive { get; init; } = true;

    /// <summary>
    /// User ID who created this override.
    /// Nullable; may be null if created by system or batch process.
    /// </summary>
    public long? CreatedByUserId { get; init; }

    /// <summary>
    /// User ID who last modified this override.
    /// Nullable; may be null if created by system or batch process.
    /// </summary>
    public long? UpdatedByUserId { get; init; }

    /// <summary>
    /// UTC timestamp when override was created.
    /// </summary>
    public DateTime CreatedUtc { get; init; }

    /// <summary>
    /// UTC timestamp when override was last updated.
    /// </summary>
    public DateTime UpdatedUtc { get; init; }

    /// <summary>
    /// Indicates how this override was loaded (for diagnostics).
    /// </summary>
    public string LoadedFrom { get; init; } = "database";
}

/// <summary>
/// Result of a query for an image override.
/// Includes the override data and metadata about the query result.
/// </summary>
public sealed class ImageOverrideQueryResult
{
    /// <summary>
    /// The override record, if found and active.
    /// Null if not found or inactive.
    /// </summary>
    public ImageOverride? Override { get; init; }

    /// <summary>
    /// Indicates if the query succeeded (database was reachable).
    /// False if there was a connection or query error.
    /// </summary>
    public bool Success { get; init; } = true;

    /// <summary>
    /// Error message if the query failed.
    /// Null if successful.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// The scope that was queried.
    /// </summary>
    public string? QueriedScope { get; init; }

    /// <summary>
    /// The scope item ID that was queried.
    /// </summary>
    public string? QueriedScopeItemId { get; init; }
}
