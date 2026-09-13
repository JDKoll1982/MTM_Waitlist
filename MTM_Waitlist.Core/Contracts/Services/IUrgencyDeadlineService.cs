using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// Resolves the urgency deadline for a request: due = created + the <b>Item's</b> allotted minutes (from
/// <see cref="IUrgencySettingsService"/>), then remaining/overdue via <c>UrgencyCalculator</c>. This is the
/// single compute entry the New-Request flow and the handler list use so <c>TargetTimeUtc</c>/<c>IsOverdue</c>
/// stay consistent.
/// </summary>
/// <remarks>
/// The window comes from the Item and from nothing else (FR-016): a request type or a subtype is not an input
/// to this service, and an Item with no configured minutes is measured by the labelled 15-minute default
/// rather than dropped from the order (FR-017, SC-007).
/// </remarks>
public interface IUrgencyDeadlineService
{
    /// <summary>
    /// Computes the urgency state for a request created at <paramref name="createdUtc"/> for the given
    /// <paramref name="item"/>, evaluated at <paramref name="now"/>. The allotted window defaults to the
    /// labelled 15 minutes when the Item has no configured allotment.
    /// </summary>
    Task<UrgencyState> ComputeAsync(DateTimeOffset createdUtc, string? item, DateTimeOffset now, CancellationToken cancellationToken = default);

    /// <summary>Resolves the allotted duration for an Item (the labelled 15-minute default when unset).</summary>
    Task<TimeSpan> GetMaxAllottedAsync(string? item, CancellationToken cancellationToken = default);
}
