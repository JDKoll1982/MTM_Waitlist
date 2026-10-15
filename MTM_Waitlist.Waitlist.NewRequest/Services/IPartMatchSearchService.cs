using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Waitlist.Services;

/// <summary>
/// The fuzzy incorrect-part search's own seam: the step asks this, and this is what knows the read is live and
/// where the containing-parts rule lives (FR-082 to FR-085).
/// </summary>
/// <remarks>
/// The seam exists so the step can be exercised without a plant system: its own tests supply a reader that answers
/// an exact match, a list, or an outage, and the step's behaviour is the same in every case.
/// </remarks>
public interface IPartMatchSearchService
{
    /// <summary>
    /// Searches the plant system for the part the operator is typing.
    /// </summary>
    /// <param name="term">What the operator typed. It is put through the search's input rule first (FR-082).</param>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>
    /// The exact match, the containing parts, or the reason no list can be offered — never a thrown failure, because
    /// the step degrades rather than blocking the request (FR-085).
    /// </returns>
    Task<PartMatchSearchResult> FindMatchesAsync(string? term, CancellationToken cancellationToken = default);
}
