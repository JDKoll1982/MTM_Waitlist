using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Core.Services;

/// <summary>
/// Pure urgency math for the file-08 "deadlines and handler list ordering" work. Given a request's created time
/// and its sub-type max-allotted time, computes due / remaining / overdue; and orders requests most-urgent-first
/// (overdue first, then least remaining time). Deterministic and unit-testable.
/// <para>
/// Since the sort work it is also the one home for every order the list can be shown in (§D8): the four
/// additional keys sit beside <see cref="OrderMostUrgentFirst"/>, which stays the default path. Every key reads
/// a value the row already carries for the card, so no order adds a data read.
/// </para>
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

    /// <summary>
    /// The values one order key reads from a row. Every one of them is already on the row for the card — its
    /// urgency plus the metadata the card shows — so ordering never adds a query (§D8).
    /// </summary>
    public readonly record struct SortValues(
        UrgencyState Urgency,
        DateTimeOffset? RequestedUtc,
        string Press,
        string RequestedBy,
        string Status);

    /// <summary>
    /// Returns <paramref name="source"/> in the order <paramref name="sortOrder"/> names (FR-011), which is
    /// most-urgent-first when it names none (FR-010). <paramref name="valuesOf"/> extracts each row's
    /// <see cref="SortValues"/>.
    /// </summary>
    /// <remarks>
    /// Every order ends with the urgency rule, so an overdue row leads its own group and the most overdue stay
    /// findable whatever the list is ordered by (FR-012).
    /// </remarks>
    public static IOrderedEnumerable<T> OrderBy<T>(
        IEnumerable<T> source,
        string? sortOrder,
        Func<T, SortValues> valuesOf)
        => WaitlistSortOrder.Normalize(sortOrder) switch
        {
            WaitlistSortOrder.LongestWaiting => ThenMostUrgentFirst(
                source.OrderBy(item => valuesOf(item).RequestedUtc ?? DateTimeOffset.MaxValue), valuesOf),
            WaitlistSortOrder.Press => ThenMostUrgentFirst(
                source.OrderBy(item => valuesOf(item).Press, StringComparer.OrdinalIgnoreCase), valuesOf),
            WaitlistSortOrder.RequestedBy => ThenMostUrgentFirst(
                source.OrderBy(item => valuesOf(item).RequestedBy, StringComparer.OrdinalIgnoreCase), valuesOf),
            WaitlistSortOrder.Status => ThenMostUrgentFirst(
                source.OrderBy(item => StatusRank(valuesOf(item).Status)), valuesOf),
            _ => OrderMostUrgentFirst(source, item => valuesOf(item).Urgency),
        };

    /// <summary>
    /// The lifecycle order the list works through: work still waiting to be claimed, then work already claimed,
    /// then anything else. A status the list does not expect is ordered last rather than dropped.
    /// </summary>
    private static int StatusRank(string? status) => status?.Trim().ToLowerInvariant() switch
    {
        "pending" => 0,
        "accepted" => 1,
        _ => 2,
    };

    /// <summary>
    /// Second-level ordering for the non-urgency keys: inside a group, the most urgent row leads.
    /// </summary>
    private static IOrderedEnumerable<T> ThenMostUrgentFirst<T>(
        IOrderedEnumerable<T> source,
        Func<T, SortValues> valuesOf)
        => source.ThenByDescending(item => valuesOf(item).Urgency.IsOverdue)
                 .ThenBy(item => valuesOf(item).Urgency.Remaining);
}
