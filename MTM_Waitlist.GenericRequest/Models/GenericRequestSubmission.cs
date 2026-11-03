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
        string requestId,
        DateTimeOffset attemptedAtUtc,
        string failureMessage)
    {
        IsRaised = isRaised;
        RequestId = requestId;
        AttemptedAtUtc = attemptedAtUtc;
        FailureMessage = failureMessage;
    }

    /// <summary>Whether the store accepted the request.</summary>
    public bool IsRaised { get; }

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
        new(true, requestId ?? string.Empty, attemptedAtUtc, string.Empty);

    /// <summary>The store refused the request, or could not be reached.</summary>
    public static GenericRequestSubmission Failed(string failureMessage, DateTimeOffset attemptedAtUtc) =>
        new(false, string.Empty, attemptedAtUtc, failureMessage ?? string.Empty);
}
