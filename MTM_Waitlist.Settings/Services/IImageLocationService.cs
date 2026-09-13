namespace MTM_Waitlist.Module_Settings.Services;

using MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// Service for managing and resolving image locations across the live scopes.
/// Acts as the primary orchestration point for the image location feature.
/// Coordinates configuration resolution and path resolution.
/// 
/// This service manages the complete lifecycle:
/// 1. **Initialization:** Validate configuration at startup
/// 2. **Querying:** Resolve effective image paths with cascade fallback
/// 3. **Mutation:** Update image overrides in database
/// 4. **Notification:** Notify subscribers when overrides change
/// 
/// The request-type and subtype scopes are retired with the type/subtype vocabulary (FR-023); the live scopes
/// are <c>request_item</c>, <c>request_category</c> and <c>work_center</c>.
/// 
/// Error Handling Strategy:
/// - ArgumentNullException: If dependencies or inputs are null
/// - InvalidOperationException: If service not initialized or configuration missing
/// - OperationCanceledException: If long-running operation is cancelled
/// </summary>
public interface IImageLocationService
{
    /// <summary>
    /// Initializes the image location service.
    /// Must be called once at application startup before using query methods.
    /// Validates the image storage configuration.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    /// <exception cref="InvalidOperationException">If initialization fails</exception>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a value indicating whether the service has been initialized.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Checks if a work center ID is valid and exists in the catalog.
    /// </summary>
    /// <param name="workCenterId">The numeric ID from setup_workstations_catalog</param>
    /// <returns>True if the ID is valid; false otherwise</returns>
    bool IsValidWorkCenterId(long workCenterId);

    /// <summary>
    /// Resolves the effective image path for a work center.
    /// Resolution order: database override → default asset.
    /// </summary>
    /// <param name="workCenterId">The numeric work center ID</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>The resolved image path, falling back to the work center default asset when needed</returns>
    Task<string> ResolveWorkCenterImagePathAsync(string workCenterId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the effective image path for a request Item.
    /// Resolution order: Item override → the Item's Category family → the existing placeholder
    /// (<c>contracts/card-and-identifier.md</c> §4, FR-009/FR-021).
    /// </summary>
    /// <param name="itemCode">The canonical Item code (e.g. <c>pickup-coil</c>)</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>The resolved image path, falling back to the request-item default asset when needed</returns>
    Task<string> ResolveRequestItemImagePathAsync(string itemCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the effective shared folder path for image storage.
    /// Resolution order: Database override → appsettings default → hard-coded fallback.
    /// </summary>
    /// <returns>The effective UNC path for the shared folder</returns>
    /// <exception cref="InvalidOperationException">If configuration cannot be resolved</exception>
    Task<string> GetSharedFolderPathAsync();

    /// <summary>
    /// Detects if display names have changed in the configuration.
    /// Call after a potential JSON redeployment to detect renames.
    /// </summary>
    /// <returns>A task representing the asynchronous operation. Returns the count of detected changes.</returns>
    Task<int> DetectConfigurationChangesAsync();

    /// <summary>
    /// Raises a notification that image locations have been updated.
    /// All subscribed views should refresh their image paths.
    /// </summary>
    /// <param name="scope">The scope that was updated (e.g., "request_item", "request_category", "work_center")</param>
    /// <param name="scopeId">The ID within the scope that was updated</param>
    void RaiseImageLocationUpdated(string scope, string scopeId);

    /// <summary>
    /// Subscribes to image location change notifications.
    /// Called when an image override is updated or deleted.
    /// </summary>
    /// <param name="handler">The handler to call when an update occurs</param>
    /// <returns>An IDisposable that can be used to unsubscribe</returns>
    IDisposable SubscribeToImageLocationChanges(Action<ImageLocationChangedEventArgs> handler);

    /// <summary>
    /// Gets all active work centers for the work-center card in image location settings.
    /// Queries the setup_workstations_catalog table for all rows where is_active=1.
    /// Results are grouped by building and sorted by sort_rank, then work center name.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>A task that returns the active work centers, or null if database is unavailable</returns>
    /// <exception cref="InvalidOperationException">If service not initialized</exception>
    Task<IReadOnlyList<WorkCenterItem>?> GetActiveWorkCentersAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Event arguments for image location change notifications.
/// </summary>
public sealed class ImageLocationChangedEventArgs : EventArgs
{
    /// <summary>
    /// The scope that was updated (e.g., "request_item", "request_category", "work_center").
    /// </summary>
    public string Scope { get; init; } = string.Empty;

    /// <summary>
    /// The ID within the scope that was updated (as string for consistency).
    /// </summary>
    public string ScopeId { get; init; } = string.Empty;

    /// <summary>
    /// The type of change: "updated", "deleted", "reset".
    /// </summary>
    public string ChangeType { get; init; } = "updated";

    /// <summary>
    /// When the change occurred.
    /// </summary>
    public DateTime ChangedAtUtc { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// User ID who made the change (optional).
    /// </summary>
    public long? ChangedByUserId { get; init; }
}
