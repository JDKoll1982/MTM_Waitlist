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

public interface ISetupWorkCenterService
{
    Task<IReadOnlyList<SetupWorkCenter>> GetWorkCentersAsync(CancellationToken cancellationToken = default);

    Task<SetupSelectionResult> AddWorkCenterAsync(string workstationName, string building, CancellationToken cancellationToken = default);

    Task<SetupSelectionResult> UpdateWorkCenterAsync(string workstationId, string workstationName, string building, CancellationToken cancellationToken = default);

    Task<SetupSelectionResult> RemoveWorkCenterAsync(string workstationId, CancellationToken cancellationToken = default);
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

    Task<SetupSelectionResult> AddDunnageTypeAsync(string typeName, string currentUserRole, CancellationToken cancellationToken = default);

    Task<SetupSelectionResult> AddDunnagePartAsync(string dunnageTypeId, string partName, string currentUserRole, CancellationToken cancellationToken = default);
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