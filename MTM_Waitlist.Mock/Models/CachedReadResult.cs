namespace MTM_Waitlist.Mock.Models;

/// <summary>
/// A fallback read result that carries its provenance.
/// </summary>
/// <typeparam name="T">The row collection type the shape returns.</typeparam>
/// <remarks>
/// Returned only by <c>ReadWithProvenanceAsync</c> on the status surface. Ordinary callers use the
/// provenance-free read so they cannot branch on which source answered (FR-004, SC-004).
/// </remarks>
public sealed record CachedReadResult<T>
{
    /// <summary>The rows, identical in shape and order whichever source answered.</summary>
    public required T Value { get; init; }

    /// <summary>Which source supplied <see cref="Value"/>.</summary>
    public required VisualReadSource Source { get; init; }

    /// <summary>
    /// The mirror snapshot's <c>refreshed_utc</c> when the cache answered; <see langword="null"/> for a
    /// live read.
    /// </summary>
    public DateTimeOffset? RefreshedUtc { get; init; }
}
