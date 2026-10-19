namespace MTM_Waitlist.Module_Core.Models;

/// <summary>
/// One recorded change to the lugger arrangement: what was changed, against which lugger and scrap type, the value
/// before and after, why, and who made it and when (FR-032, FR-035).
/// </summary>
/// <remarks>
/// <para>
/// The row is both the audit trail and the source of the notice the waitlist strip shows (FR-033), so
/// <see cref="LuggerName"/> and <see cref="ScrapType"/> are text rather than references: the record outlives the
/// lugger and the assignment it describes.
/// </para>
/// <para>
/// The read carries the actor's name and nothing else about them; the employee number the table also holds is not
/// part of this shape.
/// </para>
/// </remarks>
public sealed record ArrangementChange
{
    /// <summary><c>id</c>: the row's number, and the notice poll's watermark.</summary>
    public long Id { get; init; }

    /// <summary><c>public_id</c>: the row's public identifier.</summary>
    public string PublicId { get; init; } = string.Empty;

    /// <summary><c>change_kind</c>: one of the pinned kind strings, held in <see cref="Kinds"/>.</summary>
    public string ChangeKind { get; init; } = string.Empty;

    /// <summary><c>lugger_name</c>: the name the lugger holds after the change, or held when it was removed.</summary>
    public string LuggerName { get; init; } = string.Empty;

    /// <summary><c>scrap_type</c>: the affected scrap type as it was spelled, null when no assignment was touched.</summary>
    public string? ScrapType { get; init; }

    /// <summary><c>previous_value</c>: the value before the change, read according to <see cref="ChangeKind"/>.</summary>
    public string? PreviousValue { get; init; }

    /// <summary><c>new_value</c>: the value after the change, read according to <see cref="ChangeKind"/>.</summary>
    public string? NewValue { get; init; }

    /// <summary><c>change_reason</c>: the reason recorded with a <c>Do Not Use</c> status.</summary>
    public string? ChangeReason { get; init; }

    /// <summary><c>changed_by_employee_name</c>: who made the change.</summary>
    public string ChangedByEmployeeName { get; init; } = string.Empty;

    /// <summary><c>changed_utc</c>: when the change was made.</summary>
    public DateTime ChangedUtc { get; init; }

    /// <summary>
    /// The change kinds the procedures write. Each is the literal stored in <c>change_kind</c>, so a notice
    /// resolves its wording from the stored value without a second copy of the spelling.
    /// </summary>
    public static class Kinds
    {
        /// <summary>A lugger was added.</summary>
        public const string LuggerAdded = "lugger_added";

        /// <summary>A lugger was renamed.</summary>
        public const string LuggerRenamed = "lugger_renamed";

        /// <summary>A lugger was removed.</summary>
        public const string LuggerRemoved = "lugger_removed";

        /// <summary>A lugger's status was set.</summary>
        public const string StatusChanged = "status_changed";

        /// <summary>The dump point for a scrap type moved to another lugger.</summary>
        public const string DumpPointPromoted = "dump_point_promoted";

        /// <summary>A lugger was assigned to a scrap type.</summary>
        public const string AssignmentAdded = "assignment_added";

        /// <summary>A lugger was unassigned from a scrap type.</summary>
        public const string AssignmentRemoved = "assignment_removed";

        /// <summary>An assignment's position in a scrap type's try order changed.</summary>
        public const string AssignmentReordered = "assignment_reordered";

        /// <summary>
        /// A lugger was moved to another place in the display order. It is recorded because it is an edit like any
        /// other, and it is <b>not</b> announced: no handler is sent anywhere differently by it (FR-033).
        /// </summary>
        public const string LuggerReordered = "lugger_reordered";
    }
}
