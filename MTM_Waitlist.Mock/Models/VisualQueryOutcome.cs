namespace MTM_Waitlist.Mock.Models;

/// <summary>
/// The classified result of one attempted Infor Visual read.
/// </summary>
/// <remarks>
/// Replaces the retired executors' behaviour of returning an empty list for every failure, which made
/// "unreachable" indistinguishable from "answered with no rows" and would have disabled the fallback
/// entirely (FR-024).
/// </remarks>
public sealed record VisualQueryOutcome
{
    /// <summary>How the attempt ended.</summary>
    public required VisualQueryStatus Status { get; init; }

    /// <summary>The rows read; empty unless <see cref="Status"/> is <see cref="VisualQueryStatus.Ok"/>.</summary>
    public IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows { get; init; } = [];

    /// <summary>Diagnostic detail for a non-<see cref="VisualQueryStatus.Ok"/> outcome.</summary>
    public string? Reason { get; init; }

    /// <summary>Creates a successful outcome, including a legitimately empty result set.</summary>
    public static VisualQueryOutcome Ok(IReadOnlyList<IReadOnlyDictionary<string, object?>> rows) =>
        new() { Status = VisualQueryStatus.Ok, Rows = rows };

    /// <summary>Creates an unreachable outcome, which is the only outcome that permits fallback.</summary>
    public static VisualQueryOutcome Unreachable(string reason) =>
        new() { Status = VisualQueryStatus.Unreachable, Reason = reason };

    /// <summary>Creates a failed outcome, which must surface rather than fall back.</summary>
    public static VisualQueryOutcome Failed(string reason) =>
        new() { Status = VisualQueryStatus.Failed, Reason = reason };
}
