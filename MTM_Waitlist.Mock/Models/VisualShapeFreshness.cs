namespace MTM_Waitlist.Mock.Models;

/// <summary>
/// How current one shape's live mirror is, as reported by <c>sp_visual_read_shape_freshness_get</c>.
/// </summary>
/// <remarks>
/// The freshness report exists because the shape's own <c>sp_visual_&lt;shape&gt;_get</c> may project only the
/// live read's columns — adding <c>refreshed_utc</c> to it would break the structural identity the fallback
/// guarantees at FR-004 — and the metadata procedure reports artifact existence, not data age. Neither can
/// carry the cached-data age the status surface states (FR-017, FR-022).
/// </remarks>
public sealed record VisualShapeFreshness
{
    /// <summary>The shape's catalog key.</summary>
    public required string ShapeKey { get; init; }

    /// <summary>When the live mirror last received a refreshed snapshot; <see langword="null"/> when empty.</summary>
    public DateTime? RefreshedUtc { get; init; }

    /// <summary>
    /// <see langword="true"/> when no refreshed row is present — the mirror is empty or still holds only its
    /// baseline seed content, so no age can be reported (FR-017).
    /// </summary>
    public bool IsSeedContentOnly { get; init; }

    /// <summary>Number of rows currently in the live mirror.</summary>
    public int RowCount { get; init; }
}
