using MTM_Waitlist.Mock.Models;

namespace MTM_Waitlist.Mock.Service.Contracts;

/// <summary>
/// Produces the complete result payload a read shape's refresh should load.
/// </summary>
/// <remarks>
/// <para>
/// The Infor Visual read is external (SQL Server) and its reads are parameterized, so the service —
/// not the database procedure — owns the driver inputs and produces the complete result. The refresh
/// procedure then owns the staging load and the atomic swap, keeping the multi-table
/// <c>RENAME</c> inside the database (constitution III).
/// </para>
/// <para>
/// The payload is a JSON array whose objects use the shape's live projection names as keys, matching
/// what the corresponding <c>sp_visual_&lt;shape&gt;_refresh</c> procedure reads.
/// </para>
/// </remarks>
public interface IVisualShapePayloadSource
{
    /// <summary>
    /// Builds the complete result payload for one shape.
    /// </summary>
    /// <param name="shape">The catalog shape to read.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A JSON array of result objects; <c>[]</c> when the source legitimately returned no rows.</returns>
    /// <exception cref="VisualSourceUnreachableException">
    /// The source could not be reached. Callers must skip the refresh and leave the live snapshot intact.
    /// </exception>
    Task<string> BuildRefreshPayloadAsync(VisualReadShape shape, CancellationToken cancellationToken = default);
}
