using MTM_Waitlist.Mock.Service.Api;

namespace MTM_Waitlist.Mock.Service.Services;

/// <summary>
/// The result of one API operation: a status code plus either a payload or the error model.
/// </summary>
/// <typeparam name="TPayload">The success payload type.</typeparam>
/// <remarks>
/// The operations are deliberately decoupled from HTTP so every branch — an unknown shape, a refresh
/// already running, an unavailable backup tool — is unit-testable without a listener.
/// </remarks>
public sealed record ServiceApiOutcome<TPayload>
{
    /// <summary>The HTTP status code the endpoint must return.</summary>
    public required int StatusCode { get; init; }

    /// <summary>The success payload, or <see langword="null"/> when the operation failed.</summary>
    public TPayload? Payload { get; init; }

    /// <summary>The error model, or <see langword="null"/> when the operation succeeded.</summary>
    public ServiceApiContracts.ApiErrorPayload? Error { get; init; }

    /// <summary>Whether the operation produced a payload.</summary>
    public bool Succeeded => Error is null;

    /// <summary>Creates a successful outcome.</summary>
    /// <param name="payload">The payload to return.</param>
    /// <param name="statusCode">The status code; `200` unless a caller needs otherwise.</param>
    public static ServiceApiOutcome<TPayload> Ok(TPayload payload, int statusCode = 200) =>
        new() { StatusCode = statusCode, Payload = payload };

    /// <summary>Creates a failed outcome carrying the error model.</summary>
    /// <param name="statusCode">The status code to return.</param>
    /// <param name="error">The machine-readable error code.</param>
    /// <param name="message">A human-readable, secret-free detail.</param>
    public static ServiceApiOutcome<TPayload> Fail(int statusCode, string error, string? message = null) =>
        new()
        {
            StatusCode = statusCode,
            Error = new ServiceApiContracts.ApiErrorPayload(error, message)
        };
}
