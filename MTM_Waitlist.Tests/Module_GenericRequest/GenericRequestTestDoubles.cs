using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Models;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Shared.Models;
using MTM_Waitlist.Module_Shared.Services;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;

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

    public GenericRequestStepResult<GenericRequestScrapOption> ScrapTypes { get; set; } =
        GenericRequestStepResult<GenericRequestScrapOption>.Unavailable(DateTimeOffset.UtcNow);

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

    public Task<GenericRequestStepResult<GenericRequestScrapOption>> ReadScrapTypesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(ScrapTypes);

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

/// <summary>
/// The plant's dunnage catalogue, held in memory, so a test can say what the receiving store would answer.
/// </summary>
/// <remarks>
/// Three shapes, because they are the three the flows must tell apart: parts in hand, a read that succeeded and
/// found none, and a read that failed. <see cref="Failure"/> is what makes the third, and
/// <see cref="ReadCount"/> lets a test prove the catalogue was read once rather than per surface.
/// </remarks>
internal sealed class FakeDunnageCatalogueReader : IDunnageCatalogueReader
{
    public IReadOnlyList<DunnageCataloguePart> Parts { get; set; } = Array.Empty<DunnageCataloguePart>();

    public Exception? Failure { get; set; }

    public int ReadCount { get; private set; }

    public Task<IReadOnlyList<DunnageCataloguePart>> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        ReadCount++;

        return Failure is null
            ? Task.FromResult(Parts)
            : Task.FromException<IReadOnlyList<DunnageCataloguePart>>(Failure);
    }
}

/// <summary>One dunnage part as the catalogue holds it, for the doubles below to return.</summary>
internal static class FakeDunnageCataloguePart
{
    public static DunnageCataloguePart Of(string partNumber, string typeName = "Rack", string imagePath = "") =>
        new()
        {
            Id = $"ID-{partNumber}",
            TypeId = "TYPE-1",
            PartNumber = partNumber,
            DisplayName = partNumber,
            DunnageTypeName = typeName,
            HomeLocation = "ROW-1",
            ImagePath = imagePath,
        };
}

/// <summary>The work-center catalog, reduced to the one member the raise path reads.</summary>
internal sealed class StubWorkCenterCatalogService : IWorkCenterCatalogService
{
    public string ComputerName { get; set; } = "WCTEST";

    public string GetCurrentComputerName() => ComputerName;

    public Task<IReadOnlyList<ComputerOption>> GetAvailableComputersAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ComputerOption>>(Array.Empty<ComputerOption>());

    public Task<WorkCenterCatalogResult> GetCatalogAsync(string workstationName, CancellationToken cancellationToken = default) =>
        Task.FromResult(new WorkCenterCatalogResult());

    public Task<IReadOnlyList<string>> GetHotWorkCenterNamesAsync(string workstationName, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

    public Task<string?> SaveHotWorkCentersAsync(
        string workstationName,
        IReadOnlyCollection<string> hotWorkCenters,
        CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
}

/// <summary>The item picker, reduced to the reads the generic flow makes.</summary>
internal sealed class StubNewRequestPickerService : INewRequestPickerService
{
    /// <summary>The definitions the picker holds for every category, unless a test says otherwise.</summary>
    public IReadOnlyList<RequestItemDefinition> Items { get; set; } = Array.Empty<RequestItemDefinition>();

    public IReadOnlyList<RequestCategory> GetCategoriesInOrder() => Array.Empty<RequestCategory>();

    public IReadOnlyList<RequestItemDefinition> GetItems(RequestCategory category) => Items;

    public IReadOnlyList<RequestItemDefinition> GetVisibleItems(RequestCategory category, RequestJobPartAvailability availability) =>
        Array.Empty<RequestItemDefinition>();

    public IReadOnlyList<RequestCategory> GetVisibleCategories(RequestJobPartAvailability availability) =>
        Array.Empty<RequestCategory>();

    public IReadOnlyList<RequestItemDefinition> GetAllVisible(RequestJobPartAvailability availability) =>
        Array.Empty<RequestItemDefinition>();
}

/// <summary>The item configuration, reduced to a read that returns nothing configured.</summary>
internal sealed class StubRequestItemConfigurationService : IRequestItemConfigurationService
{
    /// <summary>The rows the store holds, unless a test says otherwise.</summary>
    public RequestItemConfigurationSet Configurations { get; set; } =
        RequestItemConfigurationSet.From(Array.Empty<RequestItemConfiguration>());

    public Task<RequestItemConfigurationSet> GetConfigurationsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Configurations);

    public Task<RequestItemConfiguration> GetConfigurationAsync(string itemCode, CancellationToken cancellationToken = default) =>
        Task.FromResult(new RequestItemConfiguration { Item = itemCode });
}

/// <summary>The part data seam, answering nothing, so a test never reaches a plant system.</summary>
internal sealed class StubGenericRequestPartData : IGenericRequestPartData
{
    public Task<GenericRequestStepResult<GenericRequestPartOption>> ReadCompletionsAsync(
        string searchText,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(GenericRequestStepResult<GenericRequestPartOption>.Unavailable(DateTimeOffset.UtcNow));

    public Task<GenericRequestStepResult<GenericRequestSequenceOption>> ReadSequencesAsync(
        string partNumber,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(GenericRequestStepResult<GenericRequestSequenceOption>.Unavailable(DateTimeOffset.UtcNow));

    public Task<GenericRequestQuantityReading> ReadQuantityAsync(
        string partNumber,
        string sequence,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(GenericRequestQuantityReading.Unavailable(DateTimeOffset.UtcNow));
}

/// <summary>The signed-in person, as a session would report them.</summary>
internal sealed class StubPersonIdentity : IPersonIdentity
{
    public long UserId { get; set; } = 7;

    public string SignInName { get; set; } = "JKOLL";

    public string DisplayName { get; set; } = "J. Koll";

    public string? EmployeeNumber { get; set; } = "1234";

    public string CurrentRoleCode { get; set; } = "MATERIAL_HANDLER";

    public IReadOnlyList<string> HeldRoleCodes { get; set; } = ["MATERIAL_HANDLER"];

    public bool IsSignedIn { get; set; } = true;

    public bool Holds(string roleCode) => HeldRoleCodes.Contains(roleCode, StringComparer.OrdinalIgnoreCase);
}

/// <summary>The account records, answering one active person for the signed-in employee number.</summary>
internal sealed class StubEmployeeDirectoryService : IEmployeeDirectoryService
{
    public EmployeeIdentity? Found { get; set; }

    public Task<EmployeeIdentity?> FindByEmployeeIdentifierAsync(
        string employeeIdentifier,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Found);
}

/// <summary>The queue's write door, recording every draft it is handed.</summary>
internal sealed class RecordingWaitlistRequestService : IWaitlistRequestService
{
    public List<WaitlistRequestDraft> Submitted { get; } = new();

    public event EventHandler? RequestsChanged
    {
        add { }
        remove { }
    }

    public IReadOnlyList<WaitlistRequest> GetActiveRequests(string? building = null) => [];

    public WaitlistRequest? GetRequest(Guid requestId) => null;

    public IReadOnlyList<WaitlistRequest> GetMyRequests(string requesterEmployeeNumber, string? building = null) => [];

    public IReadOnlyList<WaitlistRequestAuditEntry> GetAuditTrail(Guid requestId) => [];

    public Task<IReadOnlyList<WaitlistRequestAuditEntry>> LoadAuditTrailAsync(
        Guid requestId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<WaitlistRequestAuditEntry>>([]);

    public void Reset()
    {
    }

    public Task<int> RefreshFromDatabaseAsync(string? building = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(0);

    public Task<WaitlistRequestSubmitResult> SubmitAsync(
        WaitlistRequestDraft draft,
        CancellationToken cancellationToken = default)
    {
        Submitted.Add(draft);

        return Task.FromResult(WaitlistRequestSubmitResult.Success(new WaitlistRequest
        {
            Id = Guid.NewGuid(),
            Building = draft.Building,
            WorkCenter = draft.WorkCenter,
            Category = draft.Category,
            Item = draft.Item,
            InputValue = draft.InputValue,
        }));
    }

    public Task<bool> TransitionStatusAsync(
        Guid requestId,
        string status,
        string? cancellationReason = null,
        string? canceledByEmployeeNumber = null,
        CancellationToken cancellationToken = default) => Task.FromResult(false);

    public Task<WaitlistRequestCancelResult> CancelOwnRequestAsync(
        Guid requestId,
        string requesterEmployeeNumber,
        string? reason = null,
        CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<WaitlistRequest?> UpdateNoteAsync(
        Guid requestId,
        string? note,
        string? handlerEmployeeNumber,
        string employeeNumber,
        CancellationToken cancellationToken = default) => Task.FromResult<WaitlistRequest?>(null);

    public Task<WaitlistRequest?> AcceptAsync(
        Guid requestId,
        string handlerEmployeeNumber,
        string? handlerEmployeeName = null,
        CancellationToken cancellationToken = default) => Task.FromResult<WaitlistRequest?>(null);

    public Task<WaitlistRequest?> CompleteAsync(
        Guid requestId,
        string handlerEmployeeNumber,
        string? handlerEmployeeName = null,
        CancellationToken cancellationToken = default) => Task.FromResult<WaitlistRequest?>(null);

    public Task<WaitlistRequest?> ReleaseAsync(
        Guid requestId,
        string handlerEmployeeNumber,
        string? handlerEmployeeName = null,
        CancellationToken cancellationToken = default) => Task.FromResult<WaitlistRequest?>(null);

    public Task<int> SetHandlerStatusAsync(
        Guid requestId,
        string? handlerStatus,
        string expectedMaterialHandler,
        CancellationToken cancellationToken = default) => Task.FromResult(0);
}

/// <summary>
/// The lugger arrangement, held in memory, so a test can say what the scrap read would answer without a store.
/// </summary>
/// <remarks>
/// Three shapes, because they are the three the flow must tell apart: a snapshot in hand, a read that never
/// answered (<see cref="LuggerArrangementSnapshot.Empty"/>), and a read that failed. <see cref="Failure"/> makes
/// the third, and a snapshot is in hand when <see cref="Snapshot"/> is set to a
/// <see cref="LuggerArrangementSnapshot.Create"/> result rather than the unread <c>Empty</c>.
/// </remarks>
internal sealed class FakeLuggerArrangementService : ILuggerArrangementService
{
    public LuggerArrangementSnapshot Snapshot { get; set; } = LuggerArrangementSnapshot.Empty;

    public Exception? Failure { get; set; }

    /// <summary>The luggers the arrangement offers to whoever is adding a scrap type, in display order.</summary>
    public IReadOnlyList<Lugger> Luggers { get; set; } = Array.Empty<Lugger>();

    /// <summary>Every assignment this fake was asked to write, in the order it was asked.</summary>
    public List<(string ScrapType, string LuggerPublicId, int SortPosition, string ActorEmployeeNumber, string ActorEmployeeName)> Assignments { get; } = new();

    /// <summary>What a write raises instead of succeeding, or null.</summary>
    public Exception? AssignFailure { get; set; }

    /// <summary>Runs after a write is recorded, so a test can make the next read answer what the store now holds.</summary>
    public Action? OnAssign { get; set; }

    public Task<LuggerArrangementSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default) =>
        Failure is null
            ? Task.FromResult(Snapshot)
            : Task.FromException<LuggerArrangementSnapshot>(Failure);

    public Task<IReadOnlyList<Lugger>> GetLuggersAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Luggers);

    public Task<IReadOnlyList<ScrapAssignment>> GetAssignmentsAsync(
        string scrapTypeKey,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<long> GetChangeWatermarkAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<IReadOnlyList<ArrangementChange>> GetChangesAfterAsync(
        long afterId,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<int> AddLuggerAsync(
        string luggerName,
        int displayPosition,
        string actorEmployeeNumber,
        string actorEmployeeName,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<int> RenameLuggerAsync(
        string luggerPublicId,
        string luggerName,
        string actorEmployeeNumber,
        string actorEmployeeName,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<int> SetLuggerPositionAsync(
        string luggerPublicId,
        int displayPosition,
        string actorEmployeeNumber,
        string actorEmployeeName,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<int> RemoveLuggerAsync(
        string luggerPublicId,
        string actorEmployeeNumber,
        string actorEmployeeName,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<int> SetStatusAsync(
        string luggerPublicId,
        string status,
        string? reason,
        string actorEmployeeNumber,
        string actorEmployeeName,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<int> PromoteLuggerAsync(
        string luggerPublicId,
        string scrapTypeKey,
        string actorEmployeeNumber,
        string actorEmployeeName,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<int> AssignScrapTypeAsync(
        string scrapType,
        string luggerPublicId,
        int sortPosition,
        string actorEmployeeNumber,
        string actorEmployeeName,
        CancellationToken cancellationToken = default)
    {
        if (AssignFailure is { } failure)
        {
            return Task.FromException<int>(failure);
        }

        Assignments.Add((scrapType, luggerPublicId, sortPosition, actorEmployeeNumber, actorEmployeeName));
        OnAssign?.Invoke();
        return Task.FromResult(1);
    }

    public Task<int> UnassignScrapTypeAsync(
        string scrapTypeKey,
        string luggerPublicId,
        string actorEmployeeNumber,
        string actorEmployeeName,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
