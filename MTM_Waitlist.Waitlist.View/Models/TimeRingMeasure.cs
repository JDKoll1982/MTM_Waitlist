namespace MTM_Waitlist.Module_Waitlist.Models;

/// <summary>
/// Which of the two things a waitlist card's time ring is drawing (FR-015, FR-016).
/// </summary>
/// <remarks>
/// The ring looks the same either way, so the card states which one it is rather than leaving a person to work out
/// whether the ring beside a finished request is counting up or down. The ring's accessible name says the same
/// thing.
/// </remarks>
public enum TimeRingMeasure
{
    /// <summary>Time left: the request is live and has not used its whole allotment yet.</summary>
    TimeLeft,

    /// <summary>Time taken: the request has closed, and the ring is held at the moment it did (FR-015).</summary>
    TimeTaken,
}
