namespace MTM_Waitlist.Module_Waitlist.Models;

/// <summary>
/// How one fuzzy incorrect-part search ended (<c>data-model.md</c> §11, FR-083, FR-085).
/// </summary>
public enum PartMatchSearchStatus
{
    /// <summary>Nothing was searched: the operator had not typed a term.</summary>
    NothingToSearch,

    /// <summary>The term matched a part number exactly, so that part is the answer (FR-083).</summary>
    Resolved,

    /// <summary>Nothing matched exactly, and the parts whose numbers contain the term are offered (FR-083).</summary>
    Contains,

    /// <summary>The plant system could not be reached, so no list can be offered and the typed value stands (FR-085).</summary>
    Unavailable,

    /// <summary>The read could not be completed — a missing script or a refused connection (FR-084).</summary>
    Failed,
}
