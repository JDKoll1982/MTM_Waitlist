namespace MTM_Waitlist.Module_Waitlist.Models;

/// <summary>
/// Outcome of a requester's attempt to cancel their own request before it is handled.
/// </summary>
public enum WaitlistRequestCancelStatus
{
    /// <summary>The request was cancelled and recorded (never merely dropped from memory).</summary>
    Success,

    /// <summary>No request with the supplied id exists in the current set.</summary>
    NotFound,

    /// <summary>The request exists but was created by a different requester.</summary>
    NotOwnedByRequester,

    /// <summary>The request exists and is owned by the requester but is not in a cancelable (Waiting) state.</summary>
    NotInCancelableState,

    /// <summary>The transition failed for an unexpected reason.</summary>
    Failed,
}

/// <summary>Carries the status of a requester cancel-own attempt.</summary>
public sealed class WaitlistRequestCancelResult
{
    private WaitlistRequestCancelResult(WaitlistRequestCancelStatus status, WaitlistRequest? request)
    {
        Status = status;
        Request = request;
    }

    public WaitlistRequestCancelStatus Status { get; }

    /// <summary>The cancelled request when <see cref="Status"/> is <see cref="WaitlistRequestCancelStatus.Success"/>.</summary>
    public WaitlistRequest? Request { get; }

    public static WaitlistRequestCancelResult Success(WaitlistRequest request) => new(WaitlistRequestCancelStatus.Success, request);

    public static WaitlistRequestCancelResult NotFound() => new(WaitlistRequestCancelStatus.NotFound, null);

    public static WaitlistRequestCancelResult NotOwnedByRequester() => new(WaitlistRequestCancelStatus.NotOwnedByRequester, null);

    public static WaitlistRequestCancelResult NotInCancelableState() => new(WaitlistRequestCancelStatus.NotInCancelableState, null);

    public static WaitlistRequestCancelResult Failed() => new(WaitlistRequestCancelStatus.Failed, null);
}
