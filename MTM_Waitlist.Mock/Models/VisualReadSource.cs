namespace MTM_Waitlist.Mock.Models;

/// <summary>
/// Which source actually answered a fallback read.
/// </summary>
/// <remarks>
/// Available only to the status surface. Callers that cannot distinguish the source are what enforces
/// "structurally identical whichever source answered" (FR-004, SC-004).
/// </remarks>
public enum VisualReadSource
{
    /// <summary>The live Infor Visual read answered.</summary>
    ServedFromLive,

    /// <summary>The <c>mtm_mock</c> mirror answered because Infor Visual was unreachable.</summary>
    ServedFromCache
}
