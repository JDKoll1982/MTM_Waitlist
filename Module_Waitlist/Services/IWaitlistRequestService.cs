using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Waitlist.Services;

public interface IWaitlistRequestService
{
    IReadOnlyList<WaitlistRequest> GetActiveRequests(string? building = null);

    void Reset();

    Task<WaitlistRequestSubmitResult> SubmitAsync(WaitlistRequestDraft draft, bool allowDuplicate, CancellationToken cancellationToken = default);
}