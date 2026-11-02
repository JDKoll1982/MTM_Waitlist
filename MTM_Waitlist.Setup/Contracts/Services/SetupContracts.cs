using MTM_Waitlist.Module_Setup.Models;

namespace MTM_Waitlist.Module_Setup.Contracts.Services;

public interface IWorkOrderValidationService
{
    bool TryNormalize(string input, out string normalizedWorkOrder, out string validationMessage);
}

public interface IInforVisualLookupService
{
    Task<SetupLookupResult> LookupWorkOrderAsync(string normalizedWorkOrder, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SetupSequenceResult>> GetSequencesAsync(string normalizedWorkOrder, string partNumber, CancellationToken cancellationToken = default);
}

public interface ISubordinatePartService
{
    Task<IReadOnlyList<SetupSubordinatePart>> GetSubordinatePartsAsync(string normalizedWorkOrder, string partNumber, string sequenceNumber, CancellationToken cancellationToken = default);
}

/// <summary>
/// Reads the open work orders a setup run can start from. It is a separate seam from
/// <see cref="IInforVisualLookupService"/> because it answers a question that is not about one order: the
/// operator does not know the number yet, which is why the dialog exists.
/// </summary>
public interface IWorkOrderSearchService
{
    /// <summary>
    /// Finds the open, addressable work orders matching the given fragments. An empty fragment means "no
    /// constraint" on that column, and all three empty means "the first two hundred open orders".
    /// </summary>
    /// <param name="workOrderText">What the operator typed for the work order; matched fuzzily.</param>
    /// <param name="partFilter">A part-number fragment.</param>
    /// <param name="customerFilter">A customer-name fragment; a name, never an id.</param>
    Task<IReadOnlyList<SetupWorkOrderSearchResult>> SearchWorkOrdersAsync(
        string workOrderText,
        string partFilter,
        string customerFilter,
        CancellationToken cancellationToken = default);
}

public interface ISetupWorkCenterService
{    Task<IReadOnlyList<SetupWorkCenter>> GetWorkCentersAsync(CancellationToken cancellationToken = default);

    Task<SetupSelectionResult> AddWorkCenterAsync(string workstationName, string building, CancellationToken cancellationToken = default);

    Task<SetupSelectionResult> UpdateWorkCenterAsync(string workstationId, string workstationName, string building, CancellationToken cancellationToken = default);

    Task<SetupSelectionResult> RemoveWorkCenterAsync(string workstationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks or clears one work center's generic designation — whether requests raised there use the generic
    /// request flow instead of a work order (016-generic-wc-request-foundation, FR-001).
    /// </summary>
    /// <remarks>
    /// Its own write rather than a parameter on the update: the designation is a separate decision under its own
    /// rule, and setting it must not be able to rename a work center or move it to another building. The caller is
    /// responsible for the permission; this method only performs the write, and reports 0 affected rows as a
    /// refusal rather than a success.
    /// </remarks>
    Task<SetupSelectionResult> SetWorkCenterGenericAsync(string workstationId, bool isGeneric, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SetupWorkCenterHistoryEntry>> GetWorkCenterHistoryAsync(string workCenterName, CancellationToken cancellationToken = default);
}

/// <summary>
/// Starts the generic request preview host, the developer tool that walks the generic request flow against
/// in-memory values (015-generic-wc-ui-shell).
/// </summary>
/// <remarks>
/// <para>
/// The preview is a separate program on a developer's machine, never part of this application's build, and it
/// reads nothing — no database, no queue, no file. This contract exists so the setup screen can open it for the work
/// center the reviewer just marked, which is the only way to look at the flow before the live reads land in 016 to
/// 021. Nothing else in the application may depend on it.
/// </para>
/// <para>
/// A machine without the preview reports that plainly. There is no fallback to a substitute and no silent failure,
/// because an operator screen that pretends to preview something would be worse than one that says it cannot.
/// </para>
/// </remarks>
public interface IGenericRequestPreviewLauncher
{
    /// <summary>Where the preview host is on this machine, or <see langword="null"/> when it is not present.</summary>
    string? ResolvePreviewExecutable();

    /// <summary>
    /// Start the preview, telling it which work center the reviewer marked so its window says so.
    /// </summary>
    /// <param name="workCenterName">The work center whose generic flow is being previewed.</param>
    /// <param name="cancellationToken">Cancels the wait for the start, not the preview itself.</param>
    Task<SetupSelectionResult> LaunchAsync(string workCenterName, CancellationToken cancellationToken = default);
}

public interface IDunnageWorkflowService
{
    Task<IReadOnlyList<SetupDunnageType>> GetDunnageTypesAsync(string partNumber, string sequenceNumber, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SetupDunnagePart>> GetDunnagePartsAsync(string dunnageTypeId, string partNumber, string sequenceNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the app-wide dunnage part catalog (across all types) for the
    /// image-search entry point. Parts are returned whether or not they have an
    /// image path so the dialog's "Show all / Images only" toggle can filter them.
    /// </summary>
    Task<IReadOnlyList<SetupDunnagePart>> GetAllDunnagePartsAsync(CancellationToken cancellationToken = default);

    Task<SetupSelectionResult> AddDunnageTypeAsync(string typeName, CancellationToken cancellationToken = default);

    Task<SetupSelectionResult> AddDunnagePartAsync(string dunnageTypeId, string partName, CancellationToken cancellationToken = default);
}

public interface IActiveJobCoordinatorService
{
    Task<bool> HasActiveJobAsync(string workCenter, CancellationToken cancellationToken = default);

    Task RegisterActiveJobAsync(SetupSaveRequest request, CancellationToken cancellationToken = default);
}

public interface ISetupPersistenceService
{
    Task<SetupSaveResult> SaveAsync(SetupSaveRequest request, bool forceReplace = false, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SetupDunnagePart>> LoadSavedDunnageAssignmentsAsync(string workOrder, string partNumber, string sequenceNumber, CancellationToken cancellationToken = default);

    Task<string?> LoadSavedScrapTypeAsync(string workOrder, string partNumber, string sequenceNumber, CancellationToken cancellationToken = default);
}

public interface ISetupWorkflowService
{
    SetupWorkflowState State { get; }

    bool HasUnsavedChanges { get; }

    Task ResetAsync(CancellationToken cancellationToken = default);

    Task<SetupLookupResult> SearchWorkOrderAsync(string workOrderInput, CancellationToken cancellationToken = default);

    Task<SetupSelectionResult> SelectPartAsync(string partNumber, CancellationToken cancellationToken = default);

    Task<SetupSelectionResult> SelectSequenceAsync(string sequenceNumber, CancellationToken cancellationToken = default);

    Task<SetupSelectionResult> SelectDunnageTypeAsync(string dunnageTypeId, CancellationToken cancellationToken = default);

    Task<SetupSelectionResult> SelectDunnagePartAsync(string dunnagePartId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a dunnage part (from the image-search catalog) directly to the current
    /// pair's assignments without requiring the type-selection step.
    /// </summary>
    Task<SetupSelectionResult> AddDunnagePartToPairAsync(SetupDunnagePart part, CancellationToken cancellationToken = default);

    Task<SetupSelectionResult> RemoveDunnagePartAsync(string dunnagePartId, CancellationToken cancellationToken = default);

    Task<SetupSelectionResult> RemoveAllDunnageForTypeAsync(string dunnageTypeId, CancellationToken cancellationToken = default);

    Task<SetupSelectionResult> ClearAllDunnageForPairAsync(CancellationToken cancellationToken = default);

    Task<SetupSaveResult> SaveAsync(bool forceReplace = false, CancellationToken cancellationToken = default);
}

/// <summary>
/// Resolves the latest active setup job's Category/Item data for a work center from
/// <c>mtm_waitlist.setup_active_jobs</c>. Feeds the Waitlist New-Request picker and
/// card render: job-item list (coil/flatstock/die/component from
/// <c>subordinate_parts_json</c>), assigned dunnage (from
/// <c>selected_dunnage_parts_json</c>), the <c>{Sequence}</c> (<c>sequence_number</c>),
/// and the die's location (from the die subordinate row's <c>Location</c>).
/// </summary>
public interface IActiveJobItemResolverService
{
    /// <summary>
    /// Returns the active job snapshot for <paramref name="workCenter"/>, or <c>null</c>
    /// when the work center has no active setup job. Subordinate part categories are
    /// canonicalized (MMC→Coil, MMF→Flatstock, FGT→Die, else Component) so legacy or
    /// mis-tagged rows still land in the correct bucket.
    /// </summary>
    Task<SetupActiveJobSnapshot?> ResolveAsync(string workCenter, CancellationToken cancellationToken = default);
}