using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Waitlist.Services;

public interface IWaitlistRequestService
{
    event EventHandler? RequestsChanged;

    IReadOnlyList<WaitlistRequest> GetActiveRequests(string? building = null);

    WaitlistRequest? GetRequest(Guid requestId);

    IReadOnlyList<WaitlistRequestAuditEntry> GetAuditTrail(Guid requestId);

    void Reset();

    Task<WaitlistRequestSubmitResult> SubmitAsync(WaitlistRequestDraft draft, bool allowDuplicate, CancellationToken cancellationToken = default);

    Task<bool> TransitionStatusAsync(Guid requestId, string status, string? cancellationReason = null, string? canceledByEmployeeNumber = null, CancellationToken cancellationToken = default);
}