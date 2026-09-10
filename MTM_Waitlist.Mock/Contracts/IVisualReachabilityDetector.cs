using MTM_Waitlist.Mock.Models;

namespace MTM_Waitlist.Mock.Contracts;

/// <summary>
/// Tracks whether Infor Visual is reachable.
/// </summary>
/// <remarks>
/// Deliberately independent of the retired mock-routing stack (FR-014): it knows nothing about toggles,
/// manual modes, or stored settings. The state is derived only from probe results, and damping lives
/// here rather than in the callers (FR-002, FR-003).
/// </remarks>
public interface IVisualReachabilityDetector
{
    /// <summary>The current state.</summary>
    VisualReadStatus Current { get; }

    /// <summary>Raised whenever <see cref="Current"/> changes.</summary>
    event EventHandler<VisualReadStatus>? StateChanged;

    /// <summary>
    /// Runs one probe and folds the result into the state using hysteresis: two consecutive failures
    /// move to <see cref="VisualReadStatus.Cached"/>, and one success returns to
    /// <see cref="VisualReadStatus.Live"/>.
    /// </summary>
    Task ProbeAsync(CancellationToken cancellationToken = default);
}
