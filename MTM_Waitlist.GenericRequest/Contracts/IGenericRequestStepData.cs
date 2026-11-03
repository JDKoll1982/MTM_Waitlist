using MTM_Waitlist.GenericRequest.Models;

namespace MTM_Waitlist.GenericRequest.Contracts;

/// <summary>
/// Every data need of a generic request, one method each.
/// </summary>
/// <remarks>
/// The interface is the seam the whole feature is built on: a view model asks one of these questions and never a
/// store, a service or a file directly. The preview application supplies one implementation and a later chunk
/// supplies the live one, which is why no view has to change when the data arrives.
/// </remarks>
public interface IGenericRequestStepData
{
    Task<GenericRequestStepResult<GenericRequestWorkCenterOption>> ReadWorkCentersAsync(CancellationToken cancellationToken = default);

    Task<GenericRequestStepResult<GenericRequestCategoryOption>> ReadCategoriesAsync(string workCenter, CancellationToken cancellationToken = default);

    Task<GenericRequestStepResult<GenericRequestItemOption>> ReadItemsAsync(string category, CancellationToken cancellationToken = default);

    Task<GenericRequestStepResult<GenericRequestPartOption>> ReadPartsAsync(string itemCode, CancellationToken cancellationToken = default);

    Task<GenericRequestStepResult<GenericRequestSequenceOption>> ReadSequencesAsync(string partNumber, CancellationToken cancellationToken = default);

    Task<GenericRequestStepResult<GenericRequestDunnageOption>> ReadDunnageAsync(string itemCode, CancellationToken cancellationToken = default);

    Task<GenericRequestStepResult<GenericRequestScrapOption>> ReadScrapTypesAsync(CancellationToken cancellationToken = default);

    Task<GenericRequestStepResult<GenericRequestDieOption>> ReadDiesAsync(string searchText, CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether a step that reads no rows can be shown. The details, summary and result steps ask this so every
    /// screen reports the same three presentations.
    /// </summary>
    Task<GenericRequestStepAvailability> ReadStepAvailabilityAsync(GenericRequestStep step, CancellationToken cancellationToken = default);

    /// <summary>
    /// Raise the request the draft describes.
    /// </summary>
    /// <remarks>
    /// The one write this feature makes, and it goes through the same stored-procedure path every other request
    /// uses (FR-009, FR-018). The result says whether the store accepted it; a failure is reported to the requester
    /// on the summary step with the attempt time and a retry rather than being swallowed (FR-029).
    /// </remarks>
    Task<GenericRequestSubmission> SubmitAsync(GenericRequestDraft draft, CancellationToken cancellationToken = default);
}
