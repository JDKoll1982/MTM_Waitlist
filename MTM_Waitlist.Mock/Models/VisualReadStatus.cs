namespace MTM_Waitlist.Mock.Models;

/// <summary>
/// The observed read state of the Infor Visual source.
/// </summary>
/// <remarks>
/// Derived only from reachability-probe results; no public API can set it (FR-003). Flapping is
/// damped by the detector's hysteresis (see <c>IVisualReachabilityDetector</c>).
/// </remarks>
public enum VisualReadStatus
{
    /// <summary>No probe has completed yet, so the source state is not known.</summary>
    Unknown,

    /// <summary>Infor Visual answered; reads are served live.</summary>
    Live,

    /// <summary>Infor Visual is unreachable; reads are served from the <c>mtm_mock</c> mirror.</summary>
    Cached
}
