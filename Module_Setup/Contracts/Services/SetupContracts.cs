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

public interface ISetupWorkstationService
{
    Task<IReadOnlyList<SetupWorkstation>> GetWorkstationsAsync(CancellationToken cancellationToken = default);

    Task<SetupSelectionResult> AddWorkstationAsync(string workstationName, CancellationToken cancellationToken = default);

    Task<SetupSelectionResult> UpdateWorkstationAsync(string workstationId, string workstationName, CancellationToken cancellationToken = default);

    Task<SetupSelectionResult> RemoveWorkstationAsync(string workstationId, CancellationToken cancellationToken = default);
}

public interface IDunnageWorkflowService
{
    Task<IReadOnlyList<SetupDunnageType>> GetDunnageTypesAsync(string partNumber, string sequenceNumber, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SetupDunnagePart>> GetDunnagePartsAsync(string dunnageTypeId, string partNumber, string sequenceNumber, CancellationToken cancellationToken = default);

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

    Task<SetupSelectionResult> RemoveDunnagePartAsync(string dunnagePartId, CancellationToken cancellationToken = default);

    Task<SetupSelectionResult> RemoveAllDunnageForTypeAsync(string dunnageTypeId, CancellationToken cancellationToken = default);

    Task<SetupSelectionResult> ClearAllDunnageForPairAsync(CancellationToken cancellationToken = default);

    Task<SetupSaveResult> SaveAsync(bool forceReplace = false, CancellationToken cancellationToken = default);
}