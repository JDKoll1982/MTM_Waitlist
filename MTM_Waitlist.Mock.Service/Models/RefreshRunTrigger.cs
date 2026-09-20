namespace MTM_Waitlist.Mock.Service.Models;

/// <summary>
/// What caused a shape refresh to run.
/// </summary>
/// <remarks>
/// This exists so the reliability history can be read honestly. <b>SC-007 is stated over scheduled
/// cycles</b>, and a caller who asks for a refresh — the API's on-demand endpoint, a settings save, the
/// tray — runs a cycle that is not one of them. Without this distinction an on-demand run that succeeds
/// would sit in the same tally as a scheduled one that failed, and the success rate could be improved by
/// asking for more refreshes. The record carries the trigger so a reader can exclude them.
/// </remarks>
public enum RefreshRunTrigger
{
    /// <summary>
    /// The scheduled loop ran it because the shape's interval had elapsed. This is the unit SC-007
    /// measures.
    /// </summary>
    Scheduled,

    /// <summary>An operator or API caller asked for it, or a single shape was refreshed directly.</summary>
    OnDemand
}
