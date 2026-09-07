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
}