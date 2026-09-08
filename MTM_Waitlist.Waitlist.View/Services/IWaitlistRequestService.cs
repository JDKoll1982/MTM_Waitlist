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

    void Reset();

    Task<int> RefreshFromDatabaseAsync(string? building = null, CancellationToken cancellationToken = default);

    Task<WaitlistRequestSubmitResult> SubmitAsync(WaitlistRequestDraft draft, bool allowDuplicate, CancellationToken cancellationToken = default);

    Task<bool> TransitionStatusAsync(Guid requestId, string status, string? cancellationReason = null, string? canceledByEmployeeNumber = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lets the request's own requester cancel it while it is still Waiting (Pending).
    /// Gated to the creator identity and the Waiting state only; a cancelled request is
    /// recorded (never removed from memory) and persisted through the status-update path.
    /// </summary>
    Task<WaitlistRequestCancelResult> CancelOwnRequestAsync(Guid requestId, string requesterEmployeeNumber, string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the short handler note on a request (mock + production). Persists the note through the status-update
    /// path (status unchanged) and records an audit entry. Returns the updated request, or null when not found.
    /// </summary>
    Task<WaitlistRequest?> UpdateNoteAsync(Guid requestId, string? note, CancellationToken cancellationToken = default);

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
}