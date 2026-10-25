namespace MTM_Waitlist.Module_Waitlist.Models;

public enum WaitlistRequestSubmitStatus
{
    Success,
    DuplicateWarningRequired,
    ValidationFailure,
    PersistenceFailure,
}

public sealed class WaitlistRequestSubmitResult
{
    private WaitlistRequestSubmitResult(WaitlistRequestSubmitStatus status, string message, WaitlistRequest? request = null)
    {
        Status = status;
        Message = message;
        Request = request;
    }

    public WaitlistRequestSubmitStatus Status { get; }
    public string Message { get; }
    public WaitlistRequest? Request { get; }

    public static WaitlistRequestSubmitResult Success(WaitlistRequest request) =>
        new(WaitlistRequestSubmitStatus.Success, "Request submitted.", request);

    public static WaitlistRequestSubmitResult DuplicateWarning(WaitlistRequest request) =>
        new(
            WaitlistRequestSubmitStatus.DuplicateWarningRequired,
            "An active matching request already exists. Only one active request is allowed for the same work center, item and detail, so this request was not raised.",
            request);

    public static WaitlistRequestSubmitResult ValidationFailure(string message) =>
        new(WaitlistRequestSubmitStatus.ValidationFailure, message);

    public static WaitlistRequestSubmitResult PersistenceFailure(string message) =>
        new(WaitlistRequestSubmitStatus.PersistenceFailure, message);
}