namespace MTM_Waitlist.Module_Core.Models;

/// <summary>
/// One lugger: its identity, the status the lead sets and the place it takes in the set's display order.
/// </summary>
/// <remarks>
/// The eight members below are the eight columns of <c>sp_waitlist_lugger_locations_list</c>:
/// <c>id</c>, <c>public_id</c>, <c>lugger_name</c>, <c>status</c>, <c>status_reason</c>,
/// <c>display_position</c>, <c>created_utc</c> and <c>updated_utc</c>.
/// <c>sp_waitlist_lugger_arrangement_list</c> carries the five lugger columns only
/// (<c>lugger_public_id</c>, <c>lugger_name</c>, <c>status</c>, <c>status_reason</c>,
/// <c>lugger_display_position</c>), so a lugger read through the arrangement leaves
/// <see cref="Id"/>, <see cref="CreatedUtc"/> and <see cref="UpdatedUtc"/> at their defaults.
/// </remarks>
public sealed class Lugger
{
    public long Id { get; init; }

    public string PublicId { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// One of the four <see cref="LuggerStatus"/> constants. <see cref="LuggerStatus.Parse"/> is what a
    /// reader uses to reject anything else.
    /// </summary>
    public string Status { get; init; } = LuggerStatus.Backup;

    /// <summary>
    /// Why the lugger is <see cref="LuggerStatus.DoNotUse"/>; null for the other three statuses.
    /// </summary>
    public string? StatusReason { get; init; }

    public int DisplayPosition { get; init; }

    public DateTime CreatedUtc { get; init; }

    public DateTime UpdatedUtc { get; init; }

    public bool CanReceiveMaterial => LuggerStatus.CanReceiveMaterial(Status);

    public bool RequiresReason => LuggerStatus.RequiresReason(Status);
}
