using MTM_Waitlist.GenericRequest.Models;

namespace MTM_Waitlist.GenericRequest.Contracts;

/// <summary>
/// The live Infor Visual die search a generic die request is answered from.
/// </summary>
/// <remarks>
/// It has its own seam for the same reason the part step does: the read answers one question, its shape is what the
/// tests code against, and no step has to know where the answer came from. The search is live only, with no cached
/// mirror, so an unreachable source answers <see cref="GenericRequestPresentation.Unavailable"/> rather than a
/// substitute (020 FR-005, FR-015).
/// </remarks>
public interface IGenericRequestDieData
{
    /// <summary>
    /// Every die the term matches: the die itself when the term is a die number, and every die Infor Visual ties to
    /// the part when the term is a part number.
    /// </summary>
    /// <param name="searchText">What the requester typed, or empty.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <see cref="GenericRequestPresentation.Populated"/> with every match, <see cref="GenericRequestPresentation.Empty"/>
    /// when the read found none, or <see cref="GenericRequestPresentation.Unavailable"/> when the source could not be
    /// asked (020 FR-002, FR-003).
    /// </returns>
    Task<GenericRequestStepResult<GenericRequestDieOption>> ReadDiesAsync(
        string searchText,
        CancellationToken cancellationToken = default);
}
