namespace MTM_Waitlist.Module_Core.Models;

/// <summary>
/// Computed urgency for one request: its due time, how much time remains, and whether it is overdue. Drives the
/// file-08 "most-urgent-first" handler ordering and the overdue/remaining display.
/// </summary>
public sealed record UrgencyState
{
    public DateTimeOffset CreatedUtc { get; init; }

    public DateTimeOffset DueUtc { get; init; }

    /// <summary>Time remaining until due (<see cref="TimeSpan.Zero"/> or negative when overdue).</summary>
    public TimeSpan Remaining { get; init; }

    public bool IsOverdue { get; init; }

    public string RemainingLabel => IsOverdue
        ? "Overdue"
        : $"{(int)Remaining.TotalMinutes} min";
}
