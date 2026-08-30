namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// Service for managing request subtype display label mappings and tracking display name changes.
/// Ensures that renaming a subtype (e.g., "Pickup Other" → "Pickup - Other") never orphans stored image overrides.
/// 
/// Key Differences from Request Types:
/// - Subtype display names are NOT globally unique (e.g., "Bring" and "Pickup" appear under multiple parents)
/// - Lookups must include parent request type ID to disambiguate
/// - Display name changes are tracked separately for each parent group
/// 
/// Implementation Strategy:
/// - All database queries use globally unique stable GUID IDs, never display names
/// - Display names are tracked for audit and diagnostics only
/// - Display name changes are logged but don't affect stored overrides
/// - Provides lookups for both current and historical display names within each parent
/// 
/// Error Handling:
/// - ArgumentNullException: If configuration or JSON is null
/// - ArgumentException: If requested ID is not found in inventory
/// - InvalidOperationException: If JSON load fails or inventory is corrupted
/// </summary>
public interface IRequestSubtypeDisplayLabelService
{
    /// <summary>
    /// Gets the current display name for a subtype by its stable ID.
    /// </summary>
    /// <param name="subtypeId">The stable globally-unique GUID identifier</param>
    /// <returns>The current display name (from JSON)</returns>
    /// <exception cref="ArgumentException">If subtypeId is not found in inventory</exception>
    string GetCurrentDisplayName(Guid subtypeId);

    /// <summary>
    /// Gets the parent request type ID for a subtype.
    /// </summary>
    /// <param name="subtypeId">The stable GUID identifier of the subtype</param>
    /// <returns>The stable GUID of the parent request type</returns>
    /// <exception cref="ArgumentException">If subtypeId is not found in inventory</exception>
    Guid GetParentRequestTypeId(Guid subtypeId);

    /// <summary>
    /// Gets the stable subtype ID by parent request type and subtype display names.
    /// Note: Use with caution; if a display name has been renamed, this will return null.
    /// For persistence, always use stable IDs instead of storing display names.
    /// </summary>
    /// <param name="parentRequestTypeId">The stable GUID of the parent request type</param>
    /// <param name="subtypeDisplayName">The subtype display name to look up</param>
    /// <returns>The stable GUID of the subtype, or null if not found</returns>
    Guid? GetIdByDisplayName(Guid parentRequestTypeId, string subtypeDisplayName);

    /// <summary>
    /// Detects if a subtype display name has changed since application startup.
    /// Compares the JSON configuration against the last known state.
    /// </summary>
    /// <param name="subtypeId">The stable GUID identifier</param>
    /// <returns>True if the display name has changed; false otherwise</returns>
    bool HasDisplayNameChanged(Guid subtypeId);

    /// <summary>
    /// Gets the previous display name for a subtype.
    /// Useful for migration or audit logging.
    /// </summary>
    /// <param name="subtypeId">The stable GUID identifier</param>
    /// <returns>The previous display name, or null if the name has never changed</returns>
    string? GetPreviousDisplayName(Guid subtypeId);

    /// <summary>
    /// Registers all subtype display names from the current JSON configuration.
    /// Call this at application startup to establish the baseline.
    /// </summary>
    /// <returns>A task representing the asynchronous operation</returns>
    Task InitializeFromJsonAsync();

    /// <summary>
    /// Checks for display name changes in the JSON configuration and logs them.
    /// Call this to detect if JSON files have been manually edited or redeployed.
    /// </summary>
    /// <returns>A task representing the asynchronous operation. Returns the count of detected changes.</returns>
    Task<int> DetectDisplayNameChangesAsync();
}

/// <summary>
/// Represents a single request subtype display label record with change history.
/// </summary>
public sealed class RequestSubtypeDisplayLabelRecord
{
    /// <summary>
    /// Globally-unique stable GUID identifier (never changes).
    /// </summary>
    public Guid SubtypeId { get; init; }

    /// <summary>
    /// Stable GUID of the parent request type.
    /// Used to disambiguate subtypes with the same display name.
    /// </summary>
    public Guid ParentRequestTypeId { get; init; }

    /// <summary>
    /// Current display name from the JSON configuration.
    /// This is what users see in the UI.
    /// Note: NOT globally unique; multiple subtypes may share the same name under different parents.
    /// </summary>
    public string CurrentDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Previous display name, if the name has changed since application startup.
    /// Null if the name has never changed or never been known.
    /// </summary>
    public string? PreviousDisplayName { get; set; }

    /// <summary>
    /// Timestamp when the display name was last observed to change.
    /// Null if the name has never changed.
    /// </summary>
    public DateTime? LastNameChangeUtc { get; set; }

    /// <summary>
    /// Indicates if this record's display name differs from the last known state.
    /// True if CurrentDisplayName != PreviousDisplayName.
    /// </summary>
    public bool HasChanged => !string.Equals(CurrentDisplayName, PreviousDisplayName, StringComparison.Ordinal);
}
