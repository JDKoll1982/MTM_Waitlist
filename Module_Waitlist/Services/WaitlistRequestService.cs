using System.Collections.Concurrent;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Waitlist.Services;

public sealed class WaitlistRequestService : IWaitlistRequestService
{
    private readonly ConcurrentDictionary<Guid, WaitlistRequest> _requests = new();

    public IReadOnlyList<WaitlistRequest> GetActiveRequests(string? building = null)
    {
        var normalizedBuilding = building?.Trim();
        return _requests.Values
            .Where(request => string.IsNullOrWhiteSpace(normalizedBuilding) || string.Equals(request.Building, normalizedBuilding, StringComparison.OrdinalIgnoreCase))
            .Where(request => string.Equals(request.Status, "Pending", StringComparison.OrdinalIgnoreCase) || string.Equals(request.Status, "Accepted", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(request => request.RequestedUtc)
            .ToArray();
    }

    public void Reset()
    {
        _requests.Clear();
    }

    public Task<WaitlistRequestSubmitResult> SubmitAsync(WaitlistRequestDraft draft, bool allowDuplicate, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (draft is null)
        {
            return Task.FromResult(WaitlistRequestSubmitResult.ValidationFailure("Request details are required."));
        }

        if (string.IsNullOrWhiteSpace(draft.Building) || string.IsNullOrWhiteSpace(draft.WorkCenter) || string.IsNullOrWhiteSpace(draft.RequestType))
        {
            return Task.FromResult(WaitlistRequestSubmitResult.ValidationFailure("Building, Work Center, and request type are required."));
        }

        var duplicate = GetActiveRequests(draft.Building).FirstOrDefault(request =>
            string.Equals(request.WorkCenter, draft.WorkCenter.Trim(), StringComparison.OrdinalIgnoreCase)
            && string.Equals(request.RequestType, draft.RequestType.Trim(), StringComparison.OrdinalIgnoreCase)
            && string.Equals(request.Subtype ?? string.Empty, draft.Subtype?.Trim() ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            && string.Equals(request.InputValue ?? string.Empty, draft.InputValue?.Trim() ?? string.Empty, StringComparison.OrdinalIgnoreCase));

        if (duplicate is not null && !allowDuplicate)
        {
            StartupDebugLog.Info("WaitlistRequest", $"Duplicate request detected. Building='{draft.Building}', WorkCenter='{draft.WorkCenter}', RequestType='{draft.RequestType}', Subtype='{draft.Subtype ?? string.Empty}'.");
            return Task.FromResult(WaitlistRequestSubmitResult.DuplicateWarning(duplicate));
        }

        var request = new WaitlistRequest
        {
            Building = draft.Building.Trim(),
            WorkCenter = draft.WorkCenter.Trim(),
            RequestType = draft.RequestType.Trim(),
            Subtype = string.IsNullOrWhiteSpace(draft.Subtype) ? null : draft.Subtype.Trim(),
            InputValue = string.IsNullOrWhiteSpace(draft.InputValue) ? null : draft.InputValue.Trim(),
            RequestedUtc = draft.RequestedUtc,
        };

        _requests[request.Id] = request;
        StartupDebugLog.Info("WaitlistRequest", $"Request stored in session. Id='{request.Id}', Building='{request.Building}', WorkCenter='{request.WorkCenter}', RequestType='{request.RequestType}', Subtype='{request.Subtype ?? string.Empty}'.");
        return Task.FromResult(WaitlistRequestSubmitResult.Success(request));
    }
}