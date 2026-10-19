namespace MTM_Waitlist.Module_Core.Models;

/// <summary>
/// One assignment row: a lugger paired with a scrap type, carrying the lugger's status at read time and the
/// position the pairing holds in that scrap type's try order (FR-006, FR-013).
/// </summary>
/// <remarks>
/// <para>
/// The same shape serves both reads. <c>sp_waitlist_lugger_scrap_assignments_list</c> returns the assignments of one
/// scrap type, so its <c>id</c> and <c>public_id</c> are the assignment's own; <c>sp_waitlist_lugger_arrangement_list</c>
/// returns one row per lugger and assignment pairing and names the same two facts
/// <c>assignment_public_id</c> and no numeric <c>id</c>, so <see cref="AssignmentPublicId"/> is the only assignment
/// identity both carry. A lugger with no assignment appears once with the assignment columns null (FR-026), which is
/// why those members are nullable.
/// </para>
/// <para>
/// The scrap type is kept as it was entered and as the normalised key the procedures match on, so a caller never
/// derives one from the other: the two are written together and can only disagree through a write outside the
/// procedures (FR-009).
/// </para>
/// </remarks>
public sealed record ScrapAssignment
{
    /// <summary><c>id</c>: the assignment row's number. The arrangement read carries no numeric id.</summary>
    public long? AssignmentId { get; init; }

    /// <summary><c>public_id</c>, or <c>assignment_public_id</c> on the arrangement read.</summary>
    public string? AssignmentPublicId { get; init; }

    /// <summary><c>lugger_public_id</c>: the lugger this scrap type is assigned to.</summary>
    public string LuggerPublicId { get; init; } = string.Empty;

    /// <summary><c>lugger_name</c>: the lugger's name as it stood when the read was made.</summary>
    public string LuggerName { get; init; } = string.Empty;

    /// <summary><c>status</c>: the lugger's current status, one of the four pinned words.</summary>
    public string LuggerStatus { get; init; } = string.Empty;

    /// <summary><c>status_reason</c>: why the lugger was blocked, present only while it is <c>Do Not Use</c>.</summary>
    public string? LuggerStatusReason { get; init; }

    /// <summary>
    /// <c>lugger_display_position</c>: where the lugger sits in the arrangement's own order. The assignments read
    /// returns its rows in <c>sort_position</c> order instead, so this is null there.
    /// </summary>
    public int? LuggerDisplayPosition { get; init; }

    /// <summary><c>scrap_type</c>: the scrap type as the lead entered it, spelling preserved.</summary>
    public string? ScrapType { get; init; }

    /// <summary><c>scrap_type_key</c>: the normalised key, written with <see cref="ScrapType"/> in one statement.</summary>
    public string? ScrapTypeKey { get; init; }

    /// <summary><c>is_dump_point</c>: whether this lugger is the one the handlers walk to for the scrap type.</summary>
    public bool? IsDumpPoint { get; init; }

    /// <summary>
    /// <c>sort_position</c> from the assignments read, or <c>assignment_sort_position</c> from the arrangement read:
    /// the position this lugger holds in the scrap type's try order.
    /// </summary>
    public int? SortPosition { get; init; }

    /// <summary>Whether this lugger admits material in its current status (FR-013).</summary>
    public bool CanReceiveMaterial =>
        global::MTM_Waitlist.Module_Core.Models.LuggerStatus.CanReceiveMaterial(LuggerStatus);
}
