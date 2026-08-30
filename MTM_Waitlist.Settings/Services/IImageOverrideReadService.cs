using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// Service for reading image location overrides from the config_images_locations table.
/// Queries overrides by scope and scope_item_id with comprehensive error handling.
/// Does not modify overrides; see IImageOverrideWriteService for write operations.
/// </summary>
public interface IImageOverrideReadService
{
    /// <summary>
    /// Gets an active override for a specific scope and scope item.
    /// Returns null if not found or inactive; propagates database errors.
    /// </summary>
    /// <param name="scope">The scope type: request_type, request_subtype, or work_center</param>
    /// <param name="scopeItemId">The stable identifier within scope (GUID or numeric string)</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>ImageOverride if found and active; null if not found or inactive; throws on database error</returns>
    /// <exception cref="ArgumentNullException">If scope or scopeItemId is null or empty</exception>
    /// <exception cref="ArgumentException">If scope is invalid (not request_type, request_subtype, or work_center)</exception>
    /// <exception cref="InvalidOperationException">If database query fails</exception>
    Task<ImageOverride?> GetOverrideAsync(string scope, string scopeItemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active overrides for a specific scope.
    /// Useful for batch operations and inventory validation.
    /// </summary>
    /// <param name="scope">The scope type: request_type, request_subtype, or work_center</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Collection of active overrides for this scope; empty list if none found</returns>
    /// <exception cref="ArgumentNullException">If scope is null or empty</exception>
    /// <exception cref="ArgumentException">If scope is invalid</exception>
    /// <exception cref="InvalidOperationException">If database query fails</exception>
    Task<IReadOnlyList<ImageOverride>> GetOverridesByScopeAsync(string scope, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an active override exists for the given scope and item.
    /// More efficient than GetOverrideAsync when only existence check is needed.
    /// </summary>
    /// <param name="scope">The scope type</param>
    /// <param name="scopeItemId">The stable identifier within scope</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>True if an active override exists; false otherwise</returns>
    /// <exception cref="ArgumentNullException">If scope or scopeItemId is null or empty</exception>
    /// <exception cref="ArgumentException">If scope is invalid</exception>
    /// <exception cref="InvalidOperationException">If database query fails</exception>
    Task<bool> HasOverrideAsync(string scope, string scopeItemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts the total number of active overrides in the entire table.
    /// Useful for auditing and statistics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>The count of active override records</returns>
    /// <exception cref="InvalidOperationException">If database query fails</exception>
    Task<int> CountAllActiveOverridesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts active overrides for a specific scope.
    /// </summary>
    /// <param name="scope">The scope type</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>The count of active overrides for this scope</returns>
    /// <exception cref="ArgumentNullException">If scope is null or empty</exception>
    /// <exception cref="ArgumentException">If scope is invalid</exception>
    /// <exception cref="InvalidOperationException">If database query fails</exception>
    Task<int> CountActiveOverridesByScopeAsync(string scope, CancellationToken cancellationToken = default);

    /// <summary>
    /// Detects orphaned overrides (records referencing non-existent items).
    /// For example, a work_center override for an ID that's no longer in setup_workstations_catalog.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Collection of orphaned override records</returns>
    /// <exception cref="InvalidOperationException">If database query fails</exception>
    Task<IReadOnlyList<ImageOverride>> DetectOrphanedOverridesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the image path for an override by its public ID.
    /// Useful for external API references.
    /// </summary>
    /// <param name="publicId">The public UUID identifier (CHAR(36))</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>ImageOverride if found and active; null if not found or inactive</returns>
    /// <exception cref="ArgumentNullException">If publicId is null or empty</exception>
    /// <exception cref="InvalidOperationException">If database query fails</exception>
    Task<ImageOverride?> GetOverrideByPublicIdAsync(string publicId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the most recent override records (ordered by updated_utc DESC).
    /// Useful for auditing and activity history.
    /// </summary>
    /// <param name="maxRecordCount">The maximum number of records to return (default 100)</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Collection of recent override records</returns>
    /// <exception cref="ArgumentException">If maxRecordCount is less than 1</exception>
    /// <exception cref="InvalidOperationException">If database query fails</exception>
    Task<IReadOnlyList<ImageOverride>> GetRecentlyUpdatedOverridesAsync(int maxRecordCount = 100, CancellationToken cancellationToken = default);
}
