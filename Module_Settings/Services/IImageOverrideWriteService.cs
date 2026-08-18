using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// Service for writing image location overrides to the config_images_locations table.
/// Handles creation, updating, deletion, and reset operations with transaction support.
/// Does not read overrides; see IImageOverrideReadService for read operations.
/// 
/// Write Operations:
/// - CreateOverrideAsync: Insert new override (fails on duplicate scope/item)
/// - UpdateOverrideAsync: Update path for existing override (fails if not found)
/// - DeleteOverrideAsync: Soft-delete by setting is_active = 0 (preserves audit trail)
/// - ResetOverrideAsync: Delete override for scope/item (convenience method)
/// - DeleteByPublicIdAsync: Delete override by UUID identifier
/// 
/// All operations:
/// - Accept optional userId for audit trail (logged as updated_by_user_id)
/// - Return success/failure result with error details
/// - Validate input (scope, scopeItemId, imagePath)
/// - Enforce unique constraint on (scope, scope_item_id)
/// - Maintain UTC timestamps for created_utc and updated_utc
/// </summary>
public interface IImageOverrideWriteService
{
    /// <summary>
    /// Creates a new image override in the database.
    /// Fails if an override already exists for this scope/item (unique constraint).
    /// </summary>
    /// <param name="scope">The scope type: request_type, request_subtype, or work_center</param>
    /// <param name="scopeItemId">The stable identifier within scope</param>
    /// <param name="imagePath">The file system path to the copied image</param>
    /// <param name="userId">Optional user ID for audit trail (created_by_user_id)</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Result with created override data or error message</returns>
    /// <exception cref="ArgumentNullException">If scope, scopeItemId, or imagePath is null or empty</exception>
    /// <exception cref="ArgumentException">If scope is invalid or imagePath is too long (>500 chars)</exception>
    /// <exception cref="InvalidOperationException">If database query fails</exception>
    Task<ImageOverrideWriteResult> CreateOverrideAsync(
        string scope,
        string scopeItemId,
        string imagePath,
        long? userId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the image path for an existing override.
    /// Fails if no override exists for this scope/item.
    /// </summary>
    /// <param name="scope">The scope type</param>
    /// <param name="scopeItemId">The stable identifier within scope</param>
    /// <param name="newImagePath">The new file system path to the copied image</param>
    /// <param name="userId">Optional user ID for audit trail (updated_by_user_id)</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Result with updated override data or error message</returns>
    /// <exception cref="ArgumentNullException">If scope, scopeItemId, or newImagePath is null or empty</exception>
    /// <exception cref="ArgumentException">If scope is invalid or newImagePath is too long (>500 chars)</exception>
    /// <exception cref="InvalidOperationException">If database query fails</exception>
    Task<ImageOverrideWriteResult> UpdateOverrideAsync(
        string scope,
        string scopeItemId,
        string newImagePath,
        long? userId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes an override by setting is_active = 0.
    /// Preserves the record for audit trail; does not physically delete.
    /// Fails if no override exists for this scope/item.
    /// </summary>
    /// <param name="scope">The scope type</param>
    /// <param name="scopeItemId">The stable identifier within scope</param>
    /// <param name="userId">Optional user ID for audit trail (updated_by_user_id)</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Result indicating success or failure</returns>
    /// <exception cref="ArgumentNullException">If scope or scopeItemId is null or empty</exception>
    /// <exception cref="ArgumentException">If scope is invalid</exception>
    /// <exception cref="InvalidOperationException">If database query fails</exception>
    Task<ImageOverrideWriteResult> DeleteOverrideAsync(
        string scope,
        string scopeItemId,
        long? userId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an override by its public UUID identifier.
    /// Soft-deletes (sets is_active = 0) to preserve audit trail.
    /// Fails if no override exists with this public ID.
    /// </summary>
    /// <param name="publicId">The public UUID identifier (CHAR(36))</param>
    /// <param name="userId">Optional user ID for audit trail</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Result indicating success or failure</returns>
    /// <exception cref="ArgumentNullException">If publicId is null or empty</exception>
    /// <exception cref="InvalidOperationException">If database query fails</exception>
    Task<ImageOverrideWriteResult> DeleteByPublicIdAsync(
        string publicId,
        long? userId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Convenience method to delete an override for a scope/item.
    /// Equivalent to DeleteOverrideAsync but returns bool instead of result object.
    /// </summary>
    /// <param name="scope">The scope type</param>
    /// <param name="scopeItemId">The stable identifier within scope</param>
    /// <param name="userId">Optional user ID for audit trail</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>True if deleted; false if not found</returns>
    /// <exception cref="ArgumentNullException">If scope or scopeItemId is null or empty</exception>
    /// <exception cref="ArgumentException">If scope is invalid</exception>
    /// <exception cref="InvalidOperationException">If database query fails</exception>
    Task<bool> DeleteIfExistsAsync(
        string scope,
        string scopeItemId,
        long? userId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Purges all inactive overrides (is_active = 0) from the database.
    /// This is a destructive operation and should only be run during maintenance.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Count of records deleted</returns>
    /// <exception cref="InvalidOperationException">If database query fails</exception>
    Task<int> PurgeInactiveOverridesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates all overrides for a specific scope.
    /// Useful for bulk resets when scope items are removed or restructured.
    /// </summary>
    /// <param name="scope">The scope type</param>
    /// <param name="userId">Optional user ID for audit trail</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Count of overrides deactivated</returns>
    /// <exception cref="ArgumentNullException">If scope is null or empty</exception>
    /// <exception cref="ArgumentException">If scope is invalid</exception>
    /// <exception cref="InvalidOperationException">If database query fails</exception>
    Task<int> DeactivateAllForScopeAsync(string scope, long? userId = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a write operation.
/// Includes success/failure status, the affected override (if applicable), and error details.
/// </summary>
public sealed class ImageOverrideWriteResult
{
    /// <summary>
    /// Indicates if the operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The override object after the operation (for create/update).
    /// Null for delete operations or on failure.
    /// </summary>
    public ImageOverride? Override { get; init; }

    /// <summary>
    /// Error message if the operation failed.
    /// Null if successful.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Specific error code for programmatic handling.
    /// Examples: "NOT_FOUND", "DUPLICATE_KEY", "INVALID_SCOPE", etc.
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// The type of operation that was performed.
    /// </summary>
    public string OperationType { get; init; } = "unknown";

    /// <summary>
    /// The scope that was affected by the operation.
    /// </summary>
    public string? AffectedScope { get; init; }

    /// <summary>
    /// The scope item ID that was affected.
    /// </summary>
    public string? AffectedScopeItemId { get; init; }
}
