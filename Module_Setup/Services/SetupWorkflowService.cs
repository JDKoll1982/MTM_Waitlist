using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Setup.Models;
using MTM_Waitlist.Module_Core.Helpers;

namespace MTM_Waitlist.Module_Setup.Services;

public sealed class SetupWorkflowService : ISetupWorkflowService
{
    private readonly IWorkOrderValidationService _workOrderValidationService;
    private readonly IInforVisualLookupService _lookupService;
    private readonly ISubordinatePartService _subordinatePartService;
    private readonly IDunnageWorkflowService _dunnageWorkflowService;
    private readonly ISetupPersistenceService _persistenceService;

    public SetupWorkflowState State { get; }

    public bool HasUnsavedChanges => State.HasUnsavedChanges;

    public SetupWorkflowService(
        IWorkOrderValidationService workOrderValidationService,
        IInforVisualLookupService lookupService,
        ISubordinatePartService subordinatePartService,
        IDunnageWorkflowService dunnageWorkflowService,
        ISetupPersistenceService persistenceService,
        SetupWorkflowState state)
    {
        _workOrderValidationService = workOrderValidationService;
        _lookupService = lookupService;
        _subordinatePartService = subordinatePartService;
        _dunnageWorkflowService = dunnageWorkflowService;
        _persistenceService = persistenceService;
        State = state;
    }

    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        State.Reset();
        return Task.CompletedTask;
    }

    public async Task<SetupLookupResult> SearchWorkOrderAsync(string workOrderInput, CancellationToken cancellationToken = default)
    {
        StartupDebugLog.Info("SetupWorkflow", $"SearchWorkOrderAsync started. Input='{workOrderInput}'.");
        State.WorkOrderInput = workOrderInput;
        State.ValidationMessage = string.Empty;
        State.StatusMessage = string.Empty;

        if (!_workOrderValidationService.TryNormalize(workOrderInput, out var normalizedWorkOrder, out var validationMessage))
        {
            StartupDebugLog.Info("SetupWorkflow", $"Work order validation failed. Message='{validationMessage}'.");
            State.ValidationMessage = validationMessage;
            State.CurrentStep = SetupWorkflowStep.WorkOrderEntry;
            return new SetupLookupResult
            {
                Success = false,
                Message = validationMessage
            };
        }

        State.NormalizedWorkOrder = normalizedWorkOrder;
        StartupDebugLog.Info("SetupWorkflow", $"Work order normalized to '{normalizedWorkOrder}'. Running lookup.");
        var lookupResult = await _lookupService.LookupWorkOrderAsync(normalizedWorkOrder, cancellationToken);

        if (!lookupResult.Success)
        {
            StartupDebugLog.Info("SetupWorkflow", "Lookup failed or unavailable; returning to work order entry step.");
            State.ValidationMessage = string.IsNullOrWhiteSpace(lookupResult.Message)
                ? "Setup_Error.LookupUnavailable".GetLocalized()
                : lookupResult.Message;
            State.CurrentStep = SetupWorkflowStep.WorkOrderEntry;
            return lookupResult;
        }

        State.PartResults.Clear();

        foreach (var part in lookupResult.Parts)
        {
            State.PartResults.Add(part);
        }

        StartupDebugLog.Info("SetupWorkflow", $"Lookup returned {State.PartResults.Count} part(s).");

        if (State.PartResults.Count == 0)
        {
            StartupDebugLog.Info("SetupWorkflow", "No matching parts found.");
            var noMatchingPartsMessage = "Setup_WorkOrder.Validation.NoMatchingParts".GetLocalized();
            State.ValidationMessage = string.Equals(noMatchingPartsMessage, "Setup_WorkOrder.Validation.NoMatchingParts", StringComparison.Ordinal)
                ? "No parts were found for this work order."
                : noMatchingPartsMessage;
            State.CurrentStep = SetupWorkflowStep.WorkOrderEntry;
            return new SetupLookupResult
            {
                Success = false,
                Message = State.ValidationMessage
            };
        }

        if (State.PartResults.Count == 1)
        {
            StartupDebugLog.Info("SetupWorkflow", "Single part returned; auto-selecting part.");
            await SelectPartAsync(State.PartResults[0].PartNumber, cancellationToken);
        }
        else
        {
            StartupDebugLog.Info("SetupWorkflow", "Multiple parts returned; moving to part selection.");
            State.CurrentStep = SetupWorkflowStep.PartSelection;
        }

        State.HasUnsavedChanges = true;

        return lookupResult;
    }

    public async Task<SetupSelectionResult> SelectPartAsync(string partNumber, CancellationToken cancellationToken = default)
    {
        StartupDebugLog.Info("SetupWorkflow", $"SelectPartAsync started. Part='{partNumber}'.");
        State.SelectedPartNumber = partNumber;
        if (string.IsNullOrWhiteSpace(State.SelectedWorkCenter))
        {
            State.SelectedWorkCenter = State.PartResults.FirstOrDefault(part => string.Equals(part.PartNumber, partNumber, StringComparison.OrdinalIgnoreCase))?.WorkCenter ?? string.Empty;
        }
        State.SequenceResults.Clear();
        State.SelectedSequence = string.Empty;
        State.DunnageTypes.Clear();
        State.DunnageParts.Clear();
        State.SelectedDunnageParts.Clear();
        State.SelectedDunnagePartId = string.Empty;
        State.SelectedDunnageTypeId = string.Empty;

        var sequences = await _lookupService.GetSequencesAsync(State.NormalizedWorkOrder, partNumber, cancellationToken);
        foreach (var sequence in sequences)
        {
            State.SequenceResults.Add(sequence);
        }

        StartupDebugLog.Info("SetupWorkflow", $"Sequences loaded for part '{partNumber}'. Count={State.SequenceResults.Count}.");

        if (State.SequenceResults.Count == 0)
        {
            StartupDebugLog.Info("SetupWorkflow", "No sequences found; staying on part selection.");
            var noMatchingSequencesMessage = "Setup_Sequence.Validation.NoMatchingSequences".GetLocalized();
            State.StatusMessage = string.Equals(noMatchingSequencesMessage, "Setup_Sequence.Validation.NoMatchingSequences", StringComparison.Ordinal)
                ? "No operations were found for the selected part."
                : noMatchingSequencesMessage;
            State.CurrentStep = SetupWorkflowStep.PartSelection;
            return new SetupSelectionResult { Success = false, Message = State.StatusMessage };
        }

        State.CurrentStep = SetupWorkflowStep.SequenceSelection;
        StartupDebugLog.Info("SetupWorkflow", "Part selection completed; moving to sequence selection.");
        State.StatusMessage = string.Empty;
        State.HasUnsavedChanges = true;
        return new SetupSelectionResult { Success = true };
    }

    public async Task<SetupSelectionResult> SelectSequenceAsync(string sequenceNumber, CancellationToken cancellationToken = default)
    {
        StartupDebugLog.Info("SetupWorkflow", $"SelectSequenceAsync started. Sequence='{sequenceNumber}'.");
        State.SelectedSequence = sequenceNumber;
        State.SubordinateParts.Clear();
        State.DunnageTypes.Clear();
        State.DunnageParts.Clear();
        State.SelectedDunnageParts.Clear();
        State.SelectedDunnagePartId = string.Empty;

        var subordinateParts = await _subordinatePartService.GetSubordinatePartsAsync(
            State.NormalizedWorkOrder,
            State.SelectedPartNumber,
            sequenceNumber,
            cancellationToken);

        foreach (var subordinatePart in subordinateParts)
        {
            State.SubordinateParts.Add(subordinatePart);
        }

        StartupDebugLog.Info("SetupWorkflow", $"Subordinate parts loaded. Count={State.SubordinateParts.Count}.");

        var dunnageTypes = await _dunnageWorkflowService.GetDunnageTypesAsync(
            State.SelectedPartNumber,
            sequenceNumber,
            cancellationToken);

        foreach (var dunnageType in dunnageTypes)
        {
            State.DunnageTypes.Add(dunnageType);
        }

        StartupDebugLog.Info("SetupWorkflow", $"Dunnage types loaded. Count={State.DunnageTypes.Count}.");

        var savedAssignments = await _persistenceService
            .LoadSavedDunnageAssignmentsAsync(State.NormalizedWorkOrder, State.SelectedPartNumber, sequenceNumber, cancellationToken)
            .ConfigureAwait(true);

        State.SelectedDunnageParts.Clear();
        foreach (var assignment in savedAssignments)
        {
            State.SelectedDunnageParts.Add(assignment);
        }
        State.UpdateSelectedDunnageSummary();
        StartupDebugLog.Info("SetupWorkflow", $"Saved dunnage assignments rehydrated. Count={State.SelectedDunnageParts.Count}.");

        State.CurrentStep = SetupWorkflowStep.DunnageTypeSelection;
        StartupDebugLog.Info("SetupWorkflow", "Sequence selection completed; moving to dunnage type selection.");
        State.HasUnsavedChanges = true;
        return new SetupSelectionResult { Success = true };
    }

    public async Task<SetupSelectionResult> SelectDunnageTypeAsync(string dunnageTypeId, CancellationToken cancellationToken = default)
    {
        StartupDebugLog.Info("SetupWorkflow", $"SelectDunnageTypeAsync started. DunnageTypeId='{dunnageTypeId}'.");
        State.SelectedDunnageTypeId = dunnageTypeId;
        State.DunnageParts.Clear();

        var dunnageParts = await _dunnageWorkflowService.GetDunnagePartsAsync(
            dunnageTypeId,
            State.SelectedPartNumber,
            State.SelectedSequence,
            cancellationToken);

        foreach (var dunnagePart in dunnageParts)
        {
            State.DunnageParts.Add(dunnagePart);
        }

        StartupDebugLog.Info("SetupWorkflow", $"Dunnage parts loaded for type '{dunnageTypeId}'. Count={State.DunnageParts.Count}.");

        State.CurrentStep = SetupWorkflowStep.DunnagePartSelection;
        StartupDebugLog.Info("SetupWorkflow", "Dunnage type selection completed; moving to dunnage part selection.");
        State.HasUnsavedChanges = true;
        return new SetupSelectionResult { Success = true };
    }

    public Task<SetupSelectionResult> SelectDunnagePartAsync(string dunnagePartId, CancellationToken cancellationToken = default)
    {
        StartupDebugLog.Info("SetupWorkflow", $"SelectDunnagePartAsync started. DunnagePartId='{dunnagePartId}'.");
        State.SelectedDunnagePartId = dunnagePartId;

        var selectedPart = State.DunnageParts.FirstOrDefault(part => string.Equals(part.Id, dunnagePartId, StringComparison.OrdinalIgnoreCase));
        if (selectedPart is not null)
        {
            if (!State.SelectedDunnageParts.Any(part => string.Equals(part.Id, selectedPart.Id, StringComparison.OrdinalIgnoreCase)))
            {
                State.SelectedDunnageParts.Add(selectedPart);
            }
        }

        State.UpdateSelectedDunnageSummary();
        State.CurrentStep = SetupWorkflowStep.DunnageTypeSelection;
        StartupDebugLog.Info("SetupWorkflow", $"Dunnage part selection completed; returning to dunnage pair screen. Summary='{State.SelectedDunnageSummary}'.");
        State.HasUnsavedChanges = true;

        return Task.FromResult(new SetupSelectionResult { Success = true });
    }

    public Task<SetupSelectionResult> RemoveDunnagePartAsync(string dunnagePartId, CancellationToken cancellationToken = default)
    {
        StartupDebugLog.Info("SetupWorkflow", $"RemoveDunnagePartAsync started. DunnagePartId='{dunnagePartId}'.");

        var existingPart = State.SelectedDunnageParts.FirstOrDefault(part => string.Equals(part.Id, dunnagePartId, StringComparison.OrdinalIgnoreCase));
        if (existingPart is null)
        {
            return Task.FromResult(new SetupSelectionResult
            {
                Success = false,
                Message = "Selected dunnage part was not found in the pair assignment."
            });
        }

        _ = State.SelectedDunnageParts.Remove(existingPart);
        if (string.Equals(State.SelectedDunnagePartId, dunnagePartId, StringComparison.OrdinalIgnoreCase))
        {
            State.SelectedDunnagePartId = string.Empty;
        }

        State.UpdateSelectedDunnageSummary();
        State.HasUnsavedChanges = true;
        return Task.FromResult(new SetupSelectionResult { Success = true });
    }

    public Task<SetupSelectionResult> RemoveAllDunnageForTypeAsync(string dunnageTypeId, CancellationToken cancellationToken = default)
    {
        StartupDebugLog.Info("SetupWorkflow", $"RemoveAllDunnageForTypeAsync started. DunnageTypeId='{dunnageTypeId}'.");

        var removedCount = 0;
        for (var index = State.SelectedDunnageParts.Count - 1; index >= 0; index--)
        {
            if (string.Equals(State.SelectedDunnageParts[index].TypeId, dunnageTypeId, StringComparison.OrdinalIgnoreCase))
            {
                State.SelectedDunnageParts.RemoveAt(index);
                removedCount++;
            }
        }

        if (removedCount > 0 && State.DunnageParts.All(part => !string.Equals(part.Id, State.SelectedDunnagePartId, StringComparison.OrdinalIgnoreCase)))
        {
            State.SelectedDunnagePartId = string.Empty;
        }

        State.UpdateSelectedDunnageSummary();
        State.HasUnsavedChanges = true;
        return Task.FromResult(new SetupSelectionResult
        {
            Success = true,
            Message = removedCount == 0
                ? "No assigned dunnage parts were found for this type."
                : $"Removed {removedCount} dunnage assignment(s) for this type."
        });
    }

    public Task<SetupSelectionResult> ClearAllDunnageForPairAsync(CancellationToken cancellationToken = default)
    {
        StartupDebugLog.Info("SetupWorkflow", "ClearAllDunnageForPairAsync started.");

        State.SelectedDunnageParts.Clear();
        State.SelectedDunnagePartId = string.Empty;
        State.UpdateSelectedDunnageSummary();
        State.HasUnsavedChanges = true;

        return Task.FromResult(new SetupSelectionResult
        {
            Success = true,
            Message = "Cleared all dunnage assignments for this part/sequence pair."
        });
    }

    public async Task<SetupSaveResult> SaveAsync(bool forceReplace = false, CancellationToken cancellationToken = default)
    {
        StartupDebugLog.Info("SetupWorkflow", $"SaveAsync started. ForceReplace={forceReplace}. WO='{State.NormalizedWorkOrder}', Part='{State.SelectedPartNumber}', Sequence='{State.SelectedSequence}', WorkCenter='{State.SelectedWorkCenter}'.");
        var selectedPart = State.PartResults.FirstOrDefault(part => string.Equals(part.PartNumber, State.SelectedPartNumber, StringComparison.OrdinalIgnoreCase));
        var request = new SetupSaveRequest
        {
            WorkOrder = State.NormalizedWorkOrder,
            PartNumber = State.SelectedPartNumber,
            SequenceNumber = State.SelectedSequence,
            WorkCenter = string.IsNullOrWhiteSpace(State.SelectedWorkCenter)
                ? (selectedPart?.WorkCenter ?? string.Empty)
                : State.SelectedWorkCenter,
            SelectedDunnageTypeId = State.SelectedDunnageTypeId,
            SelectedDunnagePartId = State.SelectedDunnagePartId,
            SubordinateParts = State.SubordinateParts.ToArray(),
            SelectedDunnageParts = State.SelectedDunnageParts.ToArray()
        };

        State.SelectedWorkCenter = request.WorkCenter;
        StartupDebugLog.Info("SetupWorkflow", "Save request assembled and dispatched to persistence service.");
        var result = await _persistenceService.SaveAsync(request, forceReplace, cancellationToken).ConfigureAwait(true);
        State.HasUnsavedChanges = !(result.Success && !result.RequiresReplacementConfirmation);
        return result;
    }
}