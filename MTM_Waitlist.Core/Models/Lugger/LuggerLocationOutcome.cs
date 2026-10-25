namespace MTM_Waitlist.Module_Core.Models;

/// <summary>
/// What a card learns when it asks the arrangement where a job's scrap goes.
/// </summary>
public enum LuggerLocationOutcome
{
    /// <summary>A lugger was named, and <see cref="LuggerLocationResolution.LuggerLocation"/> carries it.</summary>
    Resolved,

    /// <summary>
    /// The job recorded no scrap decision: the value is blank, <c>Scrap Type Required</c> or <c>No Scrap</c>.
    /// These are not scrap types, so there is no lugger and no configuration gap to report (FR-010, FR-025).
    /// </summary>
    NoScrapDecision,

    /// <summary>
    /// The scrap type is real and no lugger is assigned to it. Nothing is named and the caller reports the gap
    /// once, because the arrangement is incomplete rather than the card being wrong (FR-026).
    /// </summary>
    NotConfigured,
}
