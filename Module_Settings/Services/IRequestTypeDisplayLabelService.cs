namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// Service for managing request type display label mappings and tracking display name changes.
/// Ensures that renaming a request type (e.g., "Pickup" → "Parts Pickup") never orphans stored image overrides.
/// 
/// Implementation Strategy:
/// - All database queries use stable GUID IDs, never display names
/// - Display names are tracked for audit and diagnostics only
/// - Display name changes are logged but don't affect stored overrides
/// - Provides lookups for both current and historical display names
/// 
/// Error Handling:
/// - ArgumentNullException: If configuration or JSON is null
/// - ArgumentException: If requested ID is not found in inventory
/// - InvalidOperationException: If JSON load fails or inventory is corrupted
/// </summary>
public interface IRequestTypeDisplayLabelService
{
    /// <summary>
    /// Gets the current display name for a request type by its stable ID.
    /// </summary>
    /// <param name="requestTypeId">The stable GUID identifier</param>
    /// <returns>The current display name (from JSON)</returns>
    /// <exception cref="ArgumentException">If requestTypeId is not found in inventory</exception>
    string GetCurrentDisplayName(Guid requestTypeId);

    /// <summary>
    /// Gets the stable ID for a request type by its current display name.
    /// Note: Use with caution; if a display name has been renamed, this will return null.
    /// For persistence, always use GetCurrentDisplayName() instead of storing display names.
    /// </summary>
    /// <param name="displayName">The display name to look up</param>
    /// <returns>The stable GUID, or null if not found (e.g., if the name was recently changed)</returns>
    Guid? GetIdByCurrentDisplayName(string displayName);

    /// <summary>
    /// Detects if a request type display name has changed since application startup.
    /// Compares the JSON configuration against the last known state.
    /// </summary>
    /// <param name="requestTypeId">The stable GUID identifier</param>
    /// <returns>True if the display name has changed; false otherwise</returns>
    bool HasDisplayNameChanged(Guid requestTypeId);

    /// <summary>
    /// Gets the previous display name(s) for a request type.
    /// Useful for migration or audit logging.
    /// </summary>
    /// <param name="requestTypeId">The stable GUID identifier</param>
    /// <returns>The previous display name, or null if the name has never changed</returns>
    string? GetPreviousDisplayName(Guid requestTypeId);

    /// <summary>
    /// Registers all request type display names from the current JSON configuration.
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
/// Represents a single request type display label record with change history.
/// </summary>
public sealed class RequestTypeDisplayLabelRecord
{
    /// <summary>
    /// Stable GUID identifier (never changes).
    /// </summary>
    public Guid RequestTypeId { get; init; }

    /// <summary>
    /// Current display name from the JSON configuration.
    /// This is what users see in the UI.
    /// </summary>
    public string CurrentDisplayName { get; init; } = string.Empty;

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
