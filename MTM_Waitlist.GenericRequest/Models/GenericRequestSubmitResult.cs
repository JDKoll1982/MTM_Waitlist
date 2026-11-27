namespace MTM_Waitlist.GenericRequest.Models;

/// <summary>What the queue did with one generic request.</summary>
public enum GenericRequestSubmitStatus
{
    /// <summary>The store accepted the request.</summary>
    Raised,

    /// <summary>An active matching request already exists, so this one was not raised. A refusal, not a fault.</summary>
    DuplicateWarningRequired,

    /// <summary>The request was refused before any write, because it did not describe something raisable.</summary>
    ValidationFailure,

    /// <summary>The store refused the write or could not be reached.</summary>
    PersistenceFailure,
}

/// <summary>
/// What happened when the flow asked the queue to raise one request.
/// </summary>
/// <remarks>
/// One value rather than a boolean and a separate failure string, so a raised request can never also read as failed and
/// the result step always has exactly one thing to draw. The message is already worded for a person: the seam that
/// knows the store's answer is the only place that can say what went wrong.
/// </remarks>
public sealed class GenericRequestSubmitResult
{
    private GenericRequestSubmitResult(GenericRequestSubmitStatus status, string message, Guid? requestId)
    {
        Status = status;
        Message = message;
        RequestId = requestId;
    }

    public GenericRequestSubmitStatus Status { get; }

    /// <summary>The store's own sentence about the attempt, in plain language.</summary>
    public string Message { get; }

    /// <summary>The raised request's public identifier, or null when nothing was raised.</summary>
    public Guid? RequestId { get; }

    /// <summary>The store accepted the request.</summary>
    public static GenericRequestSubmitResult Raised(Guid requestId) =>
        new(GenericRequestSubmitStatus.Raised, "Request submitted.", requestId);

    /// <summary>The queue already holds an active request for the same thing, so this one was not raised.</summary>
    public static GenericRequestSubmitResult DuplicateWarning(string message) =>
        new(GenericRequestSubmitStatus.DuplicateWarningRequired, message ?? string.Empty, null);

    /// <summary>The request was refused before any write.</summary>
    public static GenericRequestSubmitResult ValidationFailure(string message) =>
        new(GenericRequestSubmitStatus.ValidationFailure, message ?? string.Empty, null);

    /// <summary>The store refused the request, or could not be reached.</summary>
    public static GenericRequestSubmitResult PersistenceFailure(string message) =>
        new(GenericRequestSubmitStatus.PersistenceFailure, message ?? string.Empty, null);
}
