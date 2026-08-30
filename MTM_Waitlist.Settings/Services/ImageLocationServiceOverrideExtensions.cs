using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// Extension methods for accessing image override read operations through IImageLocationService.
/// Provides convenient integration of the read service into the main image location service.
/// </summary>
public static class ImageLocationServiceOverrideExtensions
{
    /// <summary>
    /// Gets an active image override for a specific scope and scope item.
    /// Requires the read service to be registered in DI (via AddImageLocationServices).
    /// </summary>
    /// <param name="service">The image location service (unused, only for extension method syntax)</param>
    /// <param name="readService">The override read service (injected separately)</param>
    /// <param name="scope">The scope type: request_type, request_subtype, or work_center</param>
    /// <param name="scopeItemId">The stable identifier within scope</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>ImageOverride if found and active; null if not found or inactive</returns>
    /// <exception cref="ArgumentNullException">If service or readService is null</exception>
    public static Task<ImageOverride?> GetOverrideAsync(
        this IImageLocationService service,
        IImageOverrideReadService readService,
        string scope,
        string scopeItemId,
        CancellationToken cancellationToken = default)
    {
        if (service == null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        if (readService == null)
        {
            throw new ArgumentNullException(nameof(readService));
        }

        return readService.GetOverrideAsync(scope, scopeItemId, cancellationToken);
    }

    /// <summary>
    /// Gets all active image overrides for a specific scope.
    /// Requires the read service to be registered in DI (via AddImageLocationServices).
    /// </summary>
    /// <param name="service">The image location service (unused, only for extension method syntax)</param>
    /// <param name="readService">The override read service (injected separately)</param>
    /// <param name="scope">The scope type: request_type, request_subtype, or work_center</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Collection of active overrides for this scope; empty list if none found</returns>
    /// <exception cref="ArgumentNullException">If service or readService is null</exception>
    public static Task<IReadOnlyList<ImageOverride>> GetOverridesByScopeAsync(
        this IImageLocationService service,
        IImageOverrideReadService readService,
        string scope,
        CancellationToken cancellationToken = default)
    {
        if (service == null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        if (readService == null)
        {
            throw new ArgumentNullException(nameof(readService));
        }

        return readService.GetOverridesByScopeAsync(scope, cancellationToken);
    }

    /// <summary>
    /// Checks if an active override exists for the given scope and item.
    /// Requires the read service to be registered in DI (via AddImageLocationServices).
    /// </summary>
    /// <param name="service">The image location service (unused, only for extension method syntax)</param>
    /// <param name="readService">The override read service (injected separately)</param>
    /// <param name="scope">The scope type</param>
    /// <param name="scopeItemId">The stable identifier within scope</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>True if an active override exists; false otherwise</returns>
    /// <exception cref="ArgumentNullException">If service or readService is null</exception>
    public static Task<bool> HasOverrideAsync(
        this IImageLocationService service,
        IImageOverrideReadService readService,
        string scope,
        string scopeItemId,
        CancellationToken cancellationToken = default)
    {
        if (service == null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        if (readService == null)
        {
            throw new ArgumentNullException(nameof(readService));
        }

        return readService.HasOverrideAsync(scope, scopeItemId, cancellationToken);
    }

    /// <summary>
    /// Counts the total number of active image overrides in the database.
    /// Requires the read service to be registered in DI (via AddImageLocationServices).
    /// </summary>
    /// <param name="service">The image location service (unused, only for extension method syntax)</param>
    /// <param name="readService">The override read service (injected separately)</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>The count of active override records</returns>
    /// <exception cref="ArgumentNullException">If service or readService is null</exception>
    public static Task<int> CountAllActiveOverridesAsync(
        this IImageLocationService service,
        IImageOverrideReadService readService,
        CancellationToken cancellationToken = default)
    {
        if (service == null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        if (readService == null)
        {
            throw new ArgumentNullException(nameof(readService));
        }

        return readService.CountAllActiveOverridesAsync(cancellationToken);
    }
}
