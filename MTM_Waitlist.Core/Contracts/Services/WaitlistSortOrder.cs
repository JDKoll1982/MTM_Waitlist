namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// The five orders the waitlist can be shown in, as the stable keys a viewer's choice is remembered under
/// (FR-011). Most urgent is the default: a viewer who has never chosen anything, and a stored value that no
/// longer names one of these keys, both resolve to it (FR-010).
/// </summary>
/// <remarks>
/// The keys are stored verbatim, so they are data: renaming one silently resets every viewer who had chosen it
/// to the default rather than breaking their list. The ordering rules themselves live in
/// <see cref="Services.UrgencyCalculator"/>, which keeps most-urgent-first as its default path (§D8).
/// </remarks>
public static class WaitlistSortOrder
{
    /// <summary>Overdue first, then the least time remaining — the order the list ships with.</summary>
    public const string MostUrgent = "most-urgent";

    /// <summary>The oldest request first, whatever its urgency.</summary>
    public const string LongestWaiting = "longest-waiting";

    /// <summary>By work centre, then most urgent inside each centre.</summary>
    public const string Press = "press";

    /// <summary>By the person who asked, then most urgent inside each name.</summary>
    public const string RequestedBy = "requested-by";

    /// <summary>
    /// By lifecycle status — work still waiting to be claimed before work already claimed — then most urgent
    /// inside each status.
    /// </summary>
    public const string Status = "status";

    /// <summary>Every order, in the order the shell's control offers them.</summary>
    public static readonly string[] All = [MostUrgent, LongestWaiting, Press, RequestedBy, Status];

    /// <summary>
    /// The order <paramref name="sortOrder"/> names, or <see cref="MostUrgent"/> when it names none — including
    /// when it is null, which is what a viewer who has never chosen has.
    /// </summary>
    public static string Normalize(string? sortOrder)
    {
        var candidate = sortOrder?.Trim();
        if (string.IsNullOrEmpty(candidate))
        {
            return MostUrgent;
        }

        return All.FirstOrDefault(key => string.Equals(key, candidate, StringComparison.OrdinalIgnoreCase))
            ?? MostUrgent;
    }
}
