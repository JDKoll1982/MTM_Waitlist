using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Core.Services;

/// <summary>
/// Pure urgency math for the file-08 "deadlines and handler list ordering" work. Given a request's created time
/// and its sub-type max-allotted time, computes due / remaining / overdue; and orders requests most-urgent-first
/// (overdue first, then least remaining time). Deterministic and unit-testable.
/// </summary>
public static class UrgencyCalculator
{
    public static UrgencyState Compute(DateTimeOffset createdUtc, TimeSpan maxAllotted, DateTimeOffset now)
    {
        var due = createdUtc + maxAllotted;
        var remaining = due - now;
        return new UrgencyState
        {
            CreatedUtc = createdUtc,
            DueUtc = due,
            Remaining = remaining,
            IsOverdue = remaining < TimeSpan.Zero,
        };
    }

    /// <summary>
    /// Returns <paramref name="source"/> ordered most-urgent-first: overdue items first, then by least remaining
    /// time. <paramref name="stateOf"/> extracts each item's <see cref="UrgencyState"/>.
    /// </summary>
    public static IOrderedEnumerable<T> OrderMostUrgentFirst<T>(IEnumerable<T> source, Func<T, UrgencyState> stateOf)
        => source.OrderByDescending(item => stateOf(item).IsOverdue)
                 .ThenBy(item => stateOf(item).Remaining);
}
