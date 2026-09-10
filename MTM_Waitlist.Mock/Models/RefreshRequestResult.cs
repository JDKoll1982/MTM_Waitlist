namespace MTM_Waitlist.Mock.Models;

/// <summary>
/// The outcome of an on-demand refresh request sent to the on-host service.
/// </summary>
public sealed record RefreshRequestResult
{
    /// <summary>Whether the service accepted and completed the request.</summary>
    public required bool Succeeded { get; init; }

    /// <summary>
    /// Per-shape outcome text keyed by shape key, including <c>skippedSourceUnreachable</c> entries;
    /// empty when the request never reached the service.
    /// </summary>
    public IReadOnlyDictionary<string, string> ShapeOutcomes { get; init; }
        = new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>A human-readable explanation when <see cref="Succeeded"/> is <see langword="false"/>.</summary>
    public string? Message { get; init; }

    /// <summary>Creates a graceful failure result for an absent or refusing service.</summary>
    public static RefreshRequestResult Unavailable(string message) =>
        new() { Succeeded = false, Message = message };
}
