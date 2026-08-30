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
    /// Resolves the effective image path for a work center.
    /// Resolution order: database override → default asset.
    /// </summary>
    /// <param name="workCenterId">The numeric work center ID.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The resolved image path, falling back to the work center default asset when needed.</returns>
    Task<string> ResolveWorkCenterImagePathAsync(string workCenterId, CancellationToken cancellationToken = default);
}
