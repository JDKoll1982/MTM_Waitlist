using MTM_Waitlist.Mock.Models;

namespace MTM_Waitlist.Mock.Contracts;

/// <summary>
/// Asks the on-host service to refresh the mirror immediately.
/// </summary>
/// <remarks>
/// The service is never a hard dependency: a missing service, a missing credential, or an error fails
/// gracefully and the application keeps serving cached content (FR-025, SC-011). Requesting a refresh
/// never changes the application's own read state — the detector observes reality independently.
/// </remarks>
public interface IMockServiceRefreshClient
{
    /// <summary>
    /// Requests an immediate refresh.
    /// </summary>
    /// <param name="shapeKeys">Shapes to refresh, or <see langword="null"/> for all enabled shapes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<RefreshRequestResult> RequestRefreshAsync(
        IReadOnlyCollection<string>? shapeKeys,
        CancellationToken cancellationToken = default);
}
