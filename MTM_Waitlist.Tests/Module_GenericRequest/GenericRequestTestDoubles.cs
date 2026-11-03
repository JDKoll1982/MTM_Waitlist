using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Models;

namespace MTM_Waitlist.Tests.Module_GenericRequest;

/// <summary>
/// The answers a generic request step talks to, held in memory, so a test can say what a store would answer without
/// a database.
/// </summary>
/// <remarks>
/// Every read is a settable result rather than a fixed value: what a step does with an unavailable read, an empty one
/// and a populated one is the behaviour under test, and a double that could only answer "populated" could not show it.
/// </remarks>
internal sealed class StubGenericRequestStepData : IGenericRequestStepData
{
    public GenericRequestStepResult<GenericRequestWorkCenterOption> WorkCenters { get; set; } =
        GenericRequestStepResult<GenericRequestWorkCenterOption>.Empty(DateTimeOffset.UtcNow);

    public GenericRequestStepResult<GenericRequestCategoryOption> Categories { get; set; } =
        GenericRequestStepResult<GenericRequestCategoryOption>.Empty(DateTimeOffset.UtcNow);

    public GenericRequestStepResult<GenericRequestItemOption> Items { get; set; } =
        GenericRequestStepResult<GenericRequestItemOption>.Empty(DateTimeOffset.UtcNow);

    public GenericRequestStepAvailability Availability { get; set; } =
        GenericRequestStepAvailability.Ready(DateTimeOffset.UtcNow);

    /// <summary>What the queue answers, and what it is told, when a draft is submitted.</summary>
    public Func<GenericRequestDraft, GenericRequestSubmission> SubmitResult { get; set; } =
        draft => GenericRequestSubmission.Raised("ba1d2c3e-0000-4000-8000-000000000001", DateTimeOffset.UtcNow);

    public GenericRequestDraft? SubmittedDraft { get; private set; }

    public int SubmitCount { get; private set; }

    public Task<GenericRequestStepResult<GenericRequestWorkCenterOption>> ReadWorkCentersAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(WorkCenters);

    public Task<GenericRequestStepResult<GenericRequestCategoryOption>> ReadCategoriesAsync(string workCenter, CancellationToken cancellationToken = default) =>
        Task.FromResult(Categories);

    public Task<GenericRequestStepResult<GenericRequestItemOption>> ReadItemsAsync(string category, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items);

    public Task<GenericRequestStepResult<GenericRequestPartOption>> ReadPartsAsync(string itemCode, CancellationToken cancellationToken = default) =>
        Task.FromResult(GenericRequestStepResult<GenericRequestPartOption>.Unavailable(DateTimeOffset.UtcNow));

    public Task<GenericRequestStepResult<GenericRequestSequenceOption>> ReadSequencesAsync(string partNumber, CancellationToken cancellationToken = default) =>
        Task.FromResult(GenericRequestStepResult<GenericRequestSequenceOption>.Unavailable(DateTimeOffset.UtcNow));

    public Task<GenericRequestStepResult<GenericRequestDunnageOption>> ReadDunnageAsync(string itemCode, CancellationToken cancellationToken = default) =>
        Task.FromResult(GenericRequestStepResult<GenericRequestDunnageOption>.Unavailable(DateTimeOffset.UtcNow));

    public Task<GenericRequestStepResult<GenericRequestScrapOption>> ReadScrapTypesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(GenericRequestStepResult<GenericRequestScrapOption>.Unavailable(DateTimeOffset.UtcNow));

    public Task<GenericRequestStepResult<GenericRequestDieOption>> ReadDiesAsync(string searchText, CancellationToken cancellationToken = default) =>
        Task.FromResult(GenericRequestStepResult<GenericRequestDieOption>.Unavailable(DateTimeOffset.UtcNow));

    public Task<GenericRequestStepAvailability> ReadStepAvailabilityAsync(GenericRequestStep step, CancellationToken cancellationToken = default) =>
        Task.FromResult(Availability);

    public Task<GenericRequestSubmission> SubmitAsync(GenericRequestDraft draft, CancellationToken cancellationToken = default)
    {
        SubmitCount++;
        SubmittedDraft = draft;
        return Task.FromResult(SubmitResult(draft));
    }
}

/// <summary>
/// The flow's own navigation, recorded rather than followed, so a test can see which step a step asked for.
/// </summary>
internal sealed class StubGenericRequestNavigator : IGenericRequestNavigator
{
    public GenericRequestDraft Draft { get; } = new();

    public GenericRequestStep CurrentStep { get; private set; } = GenericRequestStep.WorkCenter;

    public List<GenericRequestStep> Visited { get; } = new();

    public void GoTo(GenericRequestStep step)
    {
        CurrentStep = step;
        Visited.Add(step);
    }
}
