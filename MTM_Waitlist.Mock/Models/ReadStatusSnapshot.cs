namespace MTM_Waitlist.Mock.Models;

/// <summary>
/// An immutable view of the current read state, exposed by <c>IReadStatusProvider</c>.
/// </summary>
/// <remarks>
/// Drives the non-interactive shell indicator only. Data is never refused based on age (FR-022).
/// </remarks>
public sealed record ReadStatusSnapshot
{
    /// <summary>The detector's current state.</summary>
    public required VisualReadStatus Status { get; init; }

    /// <summary>
    /// <see langword="true"/> exactly when <see cref="Status"/> is
    /// <see cref="VisualReadStatus.Cached"/>; drives indicator visibility (FR-005).
    /// </summary>
    public bool IsCachedDataInUse => Status == VisualReadStatus.Cached;

    /// <summary>
    /// How old the served cached snapshot is, or <see langword="null"/> when only seed content exists
    /// because no refresh has ever succeeded (FR-017, FR-022).
    /// </summary>
    public TimeSpan? CachedDataAgeUtc { get; init; }

    /// <summary>
    /// <see langword="true"/> before the first successful refresh, when the mirror still holds only its
    /// baseline seed rows (FR-017).
    /// </summary>
    public bool IsSeedContentOnly { get; init; }

    /// <summary>Per-shape last successful refresh time, for operator visibility (FR-013).</summary>
    public IReadOnlyDictionary<string, DateTimeOffset> PerShapeLastRefreshUtc { get; init; }
        = new Dictionary<string, DateTimeOffset>(StringComparer.Ordinal);
}
