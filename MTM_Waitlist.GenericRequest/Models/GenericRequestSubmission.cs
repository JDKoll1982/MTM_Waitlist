namespace MTM_Waitlist.GenericRequest.Models;

/// <summary>
/// What happened when the flow asked the queue to raise the request.
/// </summary>
/// <remarks>
/// One value rather than a boolean and a separate failure string, so a raised request can never also read as failed
/// and the result step always has exactly one thing to draw. A failure carries the instant of the attempt, which is
/// what the unavailable presentation shows beside its retry.
/// </remarks>
public sealed class GenericRequestSubmission
{
    private GenericRequestSubmission(
        bool isRaised,
        bool isDuplicate,
        string requestId,
        DateTimeOffset attemptedAtUtc,
        string failureMessage)
    {
        IsRaised = isRaised;
        IsDuplicate = isDuplicate;
        RequestId = requestId;
        AttemptedAtUtc = attemptedAtUtc;
        FailureMessage = failureMessage;
    }

    /// <summary>Whether the store accepted the request.</summary>
    public bool IsRaised { get; }

    /// <summary>
    /// Whether the queue refused it because an active request for the same work center, item and detail already
    /// exists, rather than because the write failed. A refusal is an answer, not a fault: it is shown as its own
    /// outcome with the request it collided with, and no retry can raise it (FR-018).
    /// </summary>
    public bool IsDuplicate { get; }

    /// <summary>The raised request's public identifier, or empty when the write failed.</summary>
    public string RequestId { get; }

    /// <summary>When the write was attempted, in UTC.</summary>
    public DateTimeOffset AttemptedAtUtc { get; }

    /// <summary>
    /// Why the write did not land, in plain language, or empty when it did. Already worded for a person: the seam
    /// that knows the store's answer is the only place that can say what went wrong.
    /// </summary>
    public string FailureMessage { get; }

    /// <summary>The store accepted the request.</summary>
    public static GenericRequestSubmission Raised(string requestId, DateTimeOffset attemptedAtUtc) =>
        new(true, false, requestId ?? string.Empty, attemptedAtUtc, string.Empty);

    /// <summary>The queue already holds an active request for the same thing, so this one was not raised.</summary>
    public static GenericRequestSubmission Duplicate(string message, DateTimeOffset attemptedAtUtc) =>
        new(false, true, string.Empty, attemptedAtUtc, message ?? string.Empty);

    /// <summary>The store refused the request, or could not be reached.</summary>
    public static GenericRequestSubmission Failed(string failureMessage, DateTimeOffset attemptedAtUtc) =>
        new(false, false, string.Empty, attemptedAtUtc, failureMessage ?? string.Empty);
}
