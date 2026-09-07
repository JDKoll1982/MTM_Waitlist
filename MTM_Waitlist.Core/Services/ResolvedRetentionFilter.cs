namespace MTM_Waitlist.Module_Core.Services;

/// <summary>
/// Pure retention-window logic for the file-11 "resolved requests retained, aged ones hidden from the active
/// list but preserved" work. Uses <c>waitlist.resolved_retention_days</c> (default 90): a resolved request within
/// the window is shown on the active list; one older than the window is hidden (but never purged). Deterministic
/// and unit-testable.
/// </summary>
public static class ResolvedRetentionFilter
{
    public const int DefaultResolvedRetentionDays = 90;

    /// <summary>
    /// True if a request resolved at <paramref name="resolvedUtc"/> should still appear on the active list as of
    /// <paramref name="now"/> (age &lt;= <paramref name="retentionDays"/>). Oldest-first boundary: a request exactly
    /// <paramref name="retentionDays"/> old is still within retention.
    /// </summary>
    public static bool IsWithinRetention(DateTimeOffset resolvedUtc, DateTimeOffset now, int retentionDays = DefaultResolvedRetentionDays)
        => now - resolvedUtc <= TimeSpan.FromDays(retentionDays);

    /// <summary>Inverse of <see cref="IsWithinRetention"/>: the request is aged out of the active list but retained.</summary>
    public static bool IsAgedOut(DateTimeOffset resolvedUtc, DateTimeOffset now, int retentionDays = DefaultResolvedRetentionDays)
        => !IsWithinRetention(resolvedUtc, now, retentionDays);

    /// <summary>The cutoff instant before which a resolved request is considered aged out.</summary>
    public static DateTimeOffset RetentionCutoff(DateTimeOffset now, int retentionDays = DefaultResolvedRetentionDays)
        => now - TimeSpan.FromDays(retentionDays);
}
