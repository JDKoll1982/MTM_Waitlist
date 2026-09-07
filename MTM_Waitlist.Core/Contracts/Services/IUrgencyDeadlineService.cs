using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// Resolves the file-08 urgency deadline for a request: due = created + the request sub-type's max-allotted time
/// (from <see cref="IUrgencySettingsService"/>), then remaining/overdue via <c>UrgencyCalculator</c>. This is the
/// single compute entry the New-Request flow and the handler list use so <c>TargetTimeUtc</c>/<c>IsOverdue</c>
/// stay consistent.
/// </summary>
public interface IUrgencyDeadlineService
{
    /// <summary>
    /// Computes the urgency state for a request created at <paramref name="createdUtc"/> of the given sub-type,
    /// evaluated at <paramref name="now"/>. Max-allotted defaults to 30 min when the sub-type has no stored override.
    /// </summary>
    Task<UrgencyState> ComputeAsync(DateTimeOffset createdUtc, string? subtype, DateTimeOffset now, CancellationToken cancellationToken = default);

    /// <summary>Resolves the max-allotted duration for a sub-type (default 30 min when unset).</summary>
    Task<TimeSpan> GetMaxAllottedAsync(string? subtype, CancellationToken cancellationToken = default);
}
