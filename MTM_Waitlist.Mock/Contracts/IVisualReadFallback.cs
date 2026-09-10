using MTM_Waitlist.Mock.Models;

namespace MTM_Waitlist.Mock.Contracts;

/// <summary>
/// The live-then-cached read seam for one Infor Visual read shape.
/// </summary>
/// <typeparam name="TRequest">The shape's input.</typeparam>
/// <typeparam name="TRow">The shape's single row type.</typeparam>
/// <remarks>
/// <para>Every implementation follows the same algorithm (FR-024):</para>
/// <list type="number">
///   <item><description>attempt the live read;</description></item>
///   <item><description>on unreachability read the mirror's <c>sp_visual_&lt;shape&gt;_get</c>;</description></item>
///   <item><description>on a successful live read return exactly those rows, including an empty set;</description></item>
///   <item><description>on any other error surface it rather than substituting cache.</description></item>
/// </list>
/// <para>
/// <see cref="ReadWithProvenanceAsync"/> exists only for the status surface. Ordinary callers use
/// <see cref="ReadAsync"/> so they cannot distinguish which source answered.
/// </para>
/// </remarks>
public interface IVisualReadFallback<TRequest, TRow>
{
    /// <summary>Reads the shape without exposing which source answered.</summary>
    Task<IReadOnlyList<TRow>> ReadAsync(TRequest request, CancellationToken cancellationToken = default);

    /// <summary>Reads the shape and reports the source and the mirror snapshot time.</summary>
    Task<CachedReadResult<IReadOnlyList<TRow>>> ReadWithProvenanceAsync(TRequest request, CancellationToken cancellationToken = default);
}
