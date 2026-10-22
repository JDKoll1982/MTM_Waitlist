using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Waitlist.Services;

public interface IWaitlistRequestService
{
    event EventHandler? RequestsChanged;

    IReadOnlyList<WaitlistRequest> GetActiveRequests(string? building = null);

    WaitlistRequest? GetRequest(Guid requestId);

    /// <summary>Requests submitted by a given requester (all statuses, optionally building-scoped), newest first.</summary>
    IReadOnlyList<WaitlistRequest> GetMyRequests(string requesterEmployeeNumber, string? building = null);

    IReadOnlyList<WaitlistRequestAuditEntry> GetAuditTrail(Guid requestId);

    /// <summary>
    /// Reads the request's persisted audit entries from the store and merges them with the ones this session
    /// already holds, so the request page can show the history that was recorded before the app started.
    /// Idempotent: re-reading never duplicates an entry. Returns the merged trail, oldest first; a failed read
    /// leaves the in-memory trail intact rather than reporting an empty history.
    /// </summary>
    Task<IReadOnlyList<WaitlistRequestAuditEntry>> LoadAuditTrailAsync(Guid requestId, CancellationToken cancellationToken = default);

    void Reset();

    Task<int> RefreshFromDatabaseAsync(string? building = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Raises the request. A request that matches one still active on building, work centre, item and captured
    /// answer is never raised: the refusal comes back as
    /// <see cref="WaitlistRequestSubmitStatus.DuplicateWarningRequired"/> and there is no override.
    /// </summary>
    Task<WaitlistRequestSubmitResult> SubmitAsync(WaitlistRequestDraft draft, CancellationToken cancellationToken = default);

    Task<bool> TransitionStatusAsync(Guid requestId, string status, string? cancellationReason = null, string? canceledByEmployeeNumber = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lets the request's own requester cancel it while it is still Waiting (Pending).
    /// Gated to the creator identity and the Waiting state only; a cancelled request is
    /// recorded (never removed from memory) and persisted through the status-update path.
    /// </summary>
    Task<WaitlistRequestCancelResult> CancelOwnRequestAsync(Guid requestId, string requesterEmployeeNumber, string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the short handler note on a request (mock + production). Persists the note through the status-update
    /// path (status unchanged) and records an audit entry naming the author. Returns the updated request, or null
    /// when not found.
    /// </summary>
    Task<WaitlistRequest?> UpdateNoteAsync(
        Guid requestId,
        string? note,
        string? actorEmployeeNumber = null,
        string? actorEmployeeName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// A Material-Handler-or-above handler claims an available (Pending) request: auto-assigns it to the given
    /// handler (employee number) and moves it to Accepted ("In Progress"). Gated to the available state — a taken,
    /// completed, or cancelled request returns null. Returns the updated request, or null when not found/denied.
    /// </summary>
    Task<WaitlistRequest?> AcceptAsync(Guid requestId, string handlerEmployeeNumber, string? handlerEmployeeName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// The assigned handler marks a taken request Complete ("Done"). Gated to the assigned handler and a taken,
    /// not-done state (file 07). Returns the updated request, or null when not found/denied.
    /// </summary>
    Task<WaitlistRequest?> CompleteAsync(Guid requestId, string handlerEmployeeNumber, string? handlerEmployeeName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// The assigned handler releases a taken request back onto the open list: status returns to Pending, the
    /// assignee is cleared, and ReleasedUtc is stamped. Release is NOT a cancellation (file 07). Gated to the
    /// assigned handler and a taken, not-done state. Returns the updated request, or null when not found/denied.
    /// </summary>
    Task<WaitlistRequest?> ReleaseAsync(Guid requestId, string handlerEmployeeNumber, string? handlerEmployeeName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the condition the row's own material handler reported from its card, and hands back the affected-row
    /// count so the caller can tell a write from a lost race rather than being shown a success it did not get
    /// (FR-070, FR-073, FR-076).
    /// </summary>
    /// <param name="requestId">The request the condition belongs to.</param>
    /// <param name="handlerStatus">
    /// One of the four verbatim conditions, or something else to be refused. A value that is not a condition is
    /// never written: the vocabulary is closed (<see cref="Helpers.HandlerStatusCatalog"/>), and the workspace
    /// offers no path that clears a condition back to none.
    /// </param>
    /// <param name="expectedMaterialHandler">
    /// The handler the caller read on the row. The write is conditional on it, so only the row's own handler can
    /// set its condition and a caller whose expected handler is blank affects nothing (FR-076).
    /// </param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>
    /// The store's own affected-row count: 1 when the condition was written, 0 when nothing was affected — a
    /// handler who no longer holds the request, a caller that is not that handler, or a value that is not one of
    /// the four conditions.
    /// </returns>
    Task<int> SetHandlerStatusAsync(Guid requestId, string? handlerStatus, string expectedMaterialHandler, CancellationToken cancellationToken = default);
}