using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// The one seam the scrap-lugger arrangement is read and maintained through. Every method names a stored procedure;
/// none takes statement text (constitution III, FR-014).
/// </summary>
/// <remarks>
/// <para>
/// The card path reads <see cref="GetSnapshotAsync"/> once per list load. The Settings page reads the luggers, one
/// scrap type's ordered assignments and the change history, and writes through the seven methods below. Every write
/// carries the signed-in actor, because the store records who made each change in the same transaction that makes
/// it (FR-032).
/// </para>
/// <para>
/// A write answers the stored procedure's <c>affected_rows</c>: 0 means nothing matched, so a lost race is
/// distinguishable from a success. A refusal the store raises arrives as
/// <see cref="LuggerArrangementRefusalException"/> carrying the store's own plain sentence, which is what the page
/// shows; a store that cannot be reached arrives as any other exception and is reported as the unavailable state.
/// </para>
/// </remarks>
public interface ILuggerArrangementService
{
    /// <summary>
    /// Reads the whole arrangement once, through <c>sp_waitlist_lugger_arrangement_list</c>. A store that cannot be
    /// reached answers an empty snapshot, so a card shows no lugger rather than a stale or invented one (FR-023).
    /// </summary>
    Task<LuggerArrangementSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);

    /// <summary>Reads every lugger in display order, through <c>sp_waitlist_lugger_locations_list</c>.</summary>
    Task<IReadOnlyList<Lugger>> GetLuggersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads one scrap type's luggers in try order with their current statuses, through
    /// <c>sp_waitlist_lugger_scrap_assignments_list</c>. An unassigned scrap type answers no row.
    /// </summary>
    /// <param name="scrapTypeKey">The normalised scrap type, as the assignment row holds it.</param>
    Task<IReadOnlyList<ScrapAssignment>> GetAssignmentsAsync(
        string scrapTypeKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the current highest change id through <c>sp_waitlist_lugger_arrangement_changes_list</c> with a null
    /// cursor, which is the watermark a session starts from (FR-037).
    /// </summary>
    Task<long> GetChangeWatermarkAsync(CancellationToken cancellationToken = default);

    /// <summary>Reads the changes made after <paramref name="afterId"/>, in id order (FR-033).</summary>
    Task<IReadOnlyList<ArrangementChange>> GetChangesAfterAsync(
        long afterId,
        CancellationToken cancellationToken = default);

    /// <summary>Adds a lugger at a display position, through <c>sp_waitlist_lugger_locations_insert</c>.</summary>
    Task<int> AddLuggerAsync(
        string luggerName,
        int displayPosition,
        string actorEmployeeNumber,
        string actorEmployeeName,
        CancellationToken cancellationToken = default);

    /// <summary>Renames a lugger, through <c>sp_waitlist_lugger_locations_rename</c>.</summary>
    Task<int> RenameLuggerAsync(
        string luggerPublicId,
        string luggerName,
        string actorEmployeeNumber,
        string actorEmployeeName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a lugger to a place in the display order and renumbers the rest behind it, through
    /// <c>sp_waitlist_lugger_locations_position_set</c>. The requested place is clamped to the arrangement that
    /// exists, and a move that asks for the place the lugger already holds changes nothing.
    /// </summary>
    /// <param name="luggerPublicId">The lugger to move.</param>
    /// <param name="displayPosition">The 1-based place it should hold.</param>
    /// <param name="actorEmployeeNumber">Who moved it.</param>
    /// <param name="actorEmployeeName">The same person's name.</param>
    Task<int> SetLuggerPositionAsync(
        string luggerPublicId,
        int displayPosition,
        string actorEmployeeNumber,
        string actorEmployeeName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a lugger and its assignments, through <c>sp_waitlist_lugger_locations_delete</c>, recording the scrap
    /// types it took and promoting another lugger where one can take the role (FR-005, FR-026).
    /// </summary>
    Task<int> RemoveLuggerAsync(
        string luggerPublicId,
        string actorEmployeeNumber,
        string actorEmployeeName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a lugger's status, through <c>sp_waitlist_lugger_locations_status_set</c>. <paramref name="reason"/> is
    /// required for <c>Do Not Use</c> and cleared for the other three statuses (FR-019).
    /// </summary>
    Task<int> SetStatusAsync(
        string luggerPublicId,
        string status,
        string? reason,
        string actorEmployeeNumber,
        string actorEmployeeName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Makes a lugger the dump point for one scrap type, through <c>sp_waitlist_lugger_locations_promote</c>.
    /// </summary>
    Task<int> PromoteLuggerAsync(
        string luggerPublicId,
        string scrapTypeKey,
        string actorEmployeeNumber,
        string actorEmployeeName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns a scrap type to a lugger at a position, through <c>sp_waitlist_lugger_scrap_assignments_set</c>. The
    /// entered spelling and its normalised key are written together (FR-009, FR-011).
    /// </summary>
    Task<int> AssignScrapTypeAsync(
        string scrapType,
        string luggerPublicId,
        int sortPosition,
        string actorEmployeeNumber,
        string actorEmployeeName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes one pairing, through <c>sp_waitlist_lugger_scrap_assignments_delete</c>, giving the role to the next
    /// usable lugger when it held it (FR-005, FR-013).
    /// </summary>
    Task<int> UnassignScrapTypeAsync(
        string scrapTypeKey,
        string luggerPublicId,
        string actorEmployeeNumber,
        string actorEmployeeName,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// A write the arrangement's own stored procedure refused. <see cref="Exception.Message"/> is the store's plain
/// sentence, shown to the person as it stands rather than reworded here.
/// </summary>
public sealed class LuggerArrangementRefusalException : Exception
{
    public LuggerArrangementRefusalException(string message)
        : base(message)
    {
    }

    public LuggerArrangementRefusalException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// The store does not hold an object this build needs, so the change could not be made at all. It is a deployment
/// gap rather than a refusal or a store that is down, and the page says so instead of asking the reader to try a
/// change that cannot succeed until the schema is updated.
/// </summary>
public sealed class LuggerArrangementStoreOutOfDateException : Exception
{
    public LuggerArrangementStoreOutOfDateException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
