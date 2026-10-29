using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Models;

namespace MTM_Waitlist.GenericRequest.Temp;

/// <summary>
/// The seam the screens are built against, answered from memory instead of from a store.
/// </summary>
/// <remarks>
/// Nothing here opens a connection, reads a file or writes anything. The review scenario decides which of the three
/// presentations a step gets, and whether the rows carry the awkward values the plant really types, so the layout
/// can be judged against both without a database existing at all.
/// </remarks>
public sealed class TempGenericRequestStepData : IGenericRequestStepData
{
    /// <summary>The presentation the reviewer has chosen.</summary>
    public TempPreviewScenario Scenario { get; set; } = TempPreviewScenario.All[0];

    public Task<GenericRequestStepResult<GenericRequestWorkCenterOption>> ReadWorkCentersAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Answer(TempPreviewValues.WorkCenters(Scenario)));

    public Task<GenericRequestStepResult<GenericRequestCategoryOption>> ReadCategoriesAsync(string workCenter, CancellationToken cancellationToken = default) =>
        Task.FromResult(Answer(TempPreviewValues.Categories()));

    public Task<GenericRequestStepResult<GenericRequestItemOption>> ReadItemsAsync(string category, CancellationToken cancellationToken = default) =>
        Task.FromResult(Answer(TempPreviewValues.Items()));

    public Task<GenericRequestStepResult<GenericRequestPartOption>> ReadPartsAsync(string itemCode, CancellationToken cancellationToken = default) =>
        Task.FromResult(Answer(TempPreviewValues.Parts(Scenario)));

    public Task<GenericRequestStepResult<GenericRequestSequenceOption>> ReadSequencesAsync(string partNumber, CancellationToken cancellationToken = default) =>
        Task.FromResult(Answer(TempPreviewValues.Sequences()));

    public Task<GenericRequestStepResult<GenericRequestDunnageOption>> ReadDunnageAsync(string itemCode, CancellationToken cancellationToken = default) =>
        Task.FromResult(Answer(TempPreviewValues.Dunnage()));

    public Task<GenericRequestStepResult<GenericRequestScrapOption>> ReadScrapTypesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Answer(TempPreviewValues.Scrap(Scenario)));

    public Task<GenericRequestStepResult<GenericRequestDieOption>> ReadDiesAsync(string searchText, CancellationToken cancellationToken = default) =>
        Task.FromResult(Answer(TempPreviewValues.Dies(searchText, Scenario)));

    public Task<GenericRequestStepAvailability> ReadStepAvailabilityAsync(GenericRequestStep step, CancellationToken cancellationToken = default) =>
        Task.FromResult(Scenario.Presentation switch
        {
            GenericRequestPresentation.Unavailable => GenericRequestStepAvailability.Unavailable(DateTimeOffset.UtcNow),
            GenericRequestPresentation.Empty => GenericRequestStepAvailability.Empty(DateTimeOffset.UtcNow),
            _ => GenericRequestStepAvailability.Ready(DateTimeOffset.UtcNow),
        });

    private GenericRequestStepResult<T> Answer<T>(IReadOnlyList<T> rows)
    {
        var now = DateTimeOffset.UtcNow;

        return Scenario.Presentation switch
        {
            GenericRequestPresentation.Unavailable => GenericRequestStepResult<T>.Unavailable(now),
            GenericRequestPresentation.Empty => GenericRequestStepResult<T>.Empty(now),
            _ => GenericRequestStepResult<T>.Populated(rows, now),
        };
    }
}
