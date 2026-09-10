using MTM_Waitlist.Mock.Models;

namespace MTM_Waitlist.Mock.Service.Contracts;

/// <summary>
/// Replaces a read shape's live mirror with a complete new result, atomically.
/// </summary>
/// <remarks>
/// Implementations call the shape's <c>sp_visual_&lt;shape&gt;_refresh</c> procedure, which performs
/// the truncate, the staged load, the validation, and the atomic three-name <c>RENAME</c> swap. The
/// caller never writes mirror rows directly — C# must not contain MySQL statement text
/// (inline-SQL audit, task T098).
/// </remarks>
public interface IMockMirrorRefreshWriter
{
    /// <summary>
    /// Loads the payload into the shape's stage twin and swaps it into the live mirror.
    /// </summary>
    /// <param name="shape">The catalog shape to refresh.</param>
    /// <param name="jsonPayload">The complete result payload produced by the shape's payload source.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of rows the procedure loaded into the new snapshot.</returns>
    Task<int> RefreshAsync(VisualReadShape shape, string jsonPayload, CancellationToken cancellationToken = default);
}
