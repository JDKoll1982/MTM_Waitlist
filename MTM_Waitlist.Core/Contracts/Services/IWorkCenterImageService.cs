namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// Minimal work-center image contract used by modules that need to resolve the
/// effective image path for a work center. Implemented by the Settings-owned
/// <c>ImageLocationService</c> (composition root) so feature modules never need
/// to reference the Settings module.
/// </summary>
public interface IWorkCenterImageService
{
    /// <summary>
    /// Gets a value indicating whether the underlying image location service has
    /// been initialized (requires a prior <c>InitializeAsync</c>).
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Makes the image location service ready, initializing it when it is not, so a caller can resolve a
    /// picture without having to know whether anything else has got there first.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for initialization.</param>
    /// <returns>
    /// <see langword="true"/> when the service is ready to resolve pictures; <see langword="false"/> when it
    /// could not be initialized, in which case the caller draws the application's no-image placeholder instead
    /// of failing.
    /// </returns>
    /// <remarks>
    /// Nothing initializes this service during startup, so a screen that only asked
    /// <see cref="IsInitialized"/> drew its placeholder pictures for the whole first visit — even for Items
    /// whose picture somebody had configured. Initialization is idempotent and lock-guarded, so awaiting this at
    /// the point of use is what makes the first render deterministic.
    /// </remarks>
    Task<bool> EnsureInitializedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the effective image path for a work center.
    /// Resolution order: database override → default asset.
    /// </summary>
    /// <param name="workCenterId">The numeric work center ID.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The resolved image path, falling back to the work center default asset when needed.</returns>
    Task<string> ResolveWorkCenterImagePathAsync(string workCenterId, CancellationToken cancellationToken = default);
}
