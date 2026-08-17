using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Setup.Models;
using MTM_Waitlist.Module_Core.Helpers;

namespace MTM_Waitlist.Module_Setup.Services;

public sealed class SetupWorkflowService : ISetupWorkflowService
{
    private static readonly string[] s_defaultScrapTypes =
    [
        "Scrap Type Required",
        "3003 Aluminum",
        "5052 aluminum",
        "Galvanized Steel",
        "Steel",
        "Skeleton",
        "Gaylord"
    ];

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

        cancellationToken.ThrowIfCancellationRequested();

        State.PartResults.Clear();

        foreach (var part in lookupResult.Parts)
        {
            cancellationToken.ThrowIfCancellationRequested();
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
        cancellationToken.ThrowIfCancellationRequested();
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
        State.SelectedScrapType = string.Empty;
        State.SelectedDunnageTypeId = string.Empty;
        StartupDebugLog.Info("SetupWorkflow", "Part selection reset scrap state. SelectedScrapType cleared and dunnage selections reset.");

        var sequences = await _lookupService.GetSequencesAsync(State.NormalizedWorkOrder, partNumber, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        foreach (var sequence in sequences)
        {
            cancellationToken.ThrowIfCancellationRequested();
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
        cancellationToken.ThrowIfCancellationRequested();
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

        cancellationToken.ThrowIfCancellationRequested();
        foreach (var subordinatePart in subordinateParts)
        {
            cancellationToken.ThrowIfCancellationRequested();
            State.SubordinateParts.Add(subordinatePart);
        }

        StartupDebugLog.Info("SetupWorkflow", $"Subordinate parts loaded. Count={State.SubordinateParts.Count}.");
        var user8Samples = State.SubordinateParts
            .Select(part => part.User8?.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToArray();
        StartupDebugLog.Info("SetupWorkflow", $"Subordinate USER_8 snapshot. NonEmptyCount={user8Samples.Length}, Samples='{string.Join(" | ", user8Samples)}'.");

        EnsureDefaultScrapTypes();
        StartupDebugLog.Info("SetupWorkflow", $"Preparing scrap rehydrate. WO='{State.NormalizedWorkOrder}', Part='{State.SelectedPartNumber}', Sequence='{sequenceNumber}', CurrentSelectedScrap='{State.SelectedScrapType}', ScrapTypeCount={State.ScrapTypes.Count}.");
        await RehydrateOrSuggestScrapTypeAsync(sequenceNumber, cancellationToken).ConfigureAwait(true);
        StartupDebugLog.Info("SetupWorkflow", $"Scrap selection after rehydrate. SelectedScrapType='{State.SelectedScrapType}', ScrapTypeCount={State.ScrapTypes.Count}, ScrapTypes='{string.Join(" | ", State.ScrapTypes)}'.");

        var dunnageTypes = await _dunnageWorkflowService.GetDunnageTypesAsync(
            State.SelectedPartNumber,
            sequenceNumber,
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
        foreach (var dunnageType in dunnageTypes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            State.DunnageTypes.Add(dunnageType);
        }

        StartupDebugLog.Info("SetupWorkflow", $"Dunnage types loaded. Count={State.DunnageTypes.Count}.");

        var savedAssignments = await _persistenceService
            .LoadSavedDunnageAssignmentsAsync(State.NormalizedWorkOrder, State.SelectedPartNumber, sequenceNumber, cancellationToken)
            .ConfigureAwait(true);

        cancellationToken.ThrowIfCancellationRequested();
        State.SelectedDunnageParts.Clear();
        foreach (var assignment in savedAssignments)
        {
            cancellationToken.ThrowIfCancellationRequested();
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
        StartupDebugLog.Info("SetupWorkflow", $"Scrap state before save. SelectedScrapType='{State.SelectedScrapType}', ScrapTypeCount={State.ScrapTypes.Count}, SubordinatePartCount={State.SubordinateParts.Count}.");
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
            SelectedScrapType = State.SelectedScrapType,
            SubordinateParts = State.SubordinateParts
                .Select(part => new SetupSubordinatePart
                {
                    Category = part.Category,
                    PartNumber = part.PartNumber,
                    Description = part.Description,
                    Location = part.Location,
                    OnHandQuantity = part.OnHandQuantity,
                    IsLowStock = part.IsLowStock,
                    User8 = part.User8,
                    SelectedScrapType = State.SelectedScrapType,
                })
                .ToArray(),
            SelectedDunnageParts = State.SelectedDunnageParts.ToArray()
        };

        State.SelectedWorkCenter = request.WorkCenter;
        var requestScrapValues = request.SubordinateParts
            .Select(part => part.SelectedScrapType?.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        StartupDebugLog.Info("SetupWorkflow", $"Save request scrap payload. RequestSelectedScrapType='{request.SelectedScrapType}', DistinctSubordinateScrapCount={requestScrapValues.Length}, DistinctSubordinateScrapValues='{string.Join(" | ", requestScrapValues)}'.");
        StartupDebugLog.Info("SetupWorkflow", "Save request assembled and dispatched to persistence service.");
        var result = await _persistenceService.SaveAsync(request, forceReplace, cancellationToken).ConfigureAwait(true);
        StartupDebugLog.Info("SetupWorkflow", $"SaveAsync result received. Success={result.Success}, RequiresReplacementConfirmation={result.RequiresReplacementConfirmation}, Message='{result.Message}'.");
        State.HasUnsavedChanges = !(result.Success && !result.RequiresReplacementConfirmation);
        return result;
    }

    private void EnsureDefaultScrapTypes()
    {
        var initialCount = State.ScrapTypes.Count;
        var defaults = s_defaultScrapTypes;
        if (State.ScrapTypes.Count == 0)
        {
            foreach (var value in defaults)
            {
                State.ScrapTypes.Add(value);
            }

            StartupDebugLog.Info("SetupWorkflow", $"Default scrap types initialized. Added={defaults.Length}, FinalCount={State.ScrapTypes.Count}, Values='{string.Join(" | ", State.ScrapTypes)}'.");

            return;
        }

        foreach (var value in defaults)
        {
            if (!State.ScrapTypes.Any(existing => string.Equals(existing, value, StringComparison.OrdinalIgnoreCase)))
            {
                State.ScrapTypes.Add(value);
            }
        }

        var addedCount = State.ScrapTypes.Count - initialCount;
        StartupDebugLog.Info("SetupWorkflow", $"Default scrap type merge completed. Added={addedCount}, FinalCount={State.ScrapTypes.Count}, Values='{string.Join(" | ", State.ScrapTypes)}'.");
    }

    private void SetSuggestedScrapTypeFromUser8()
    {
        var candidates = State.ScrapTypes
            .Where(value => !string.Equals(value, s_defaultScrapTypes[0], StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var user8Sources = State.SubordinateParts
            .Where(part => string.Equals(part.Category, "Coil", StringComparison.OrdinalIgnoreCase)
                || string.Equals(part.Category, "Flatstock", StringComparison.OrdinalIgnoreCase))
            .Select(part => part.User8)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();

        StartupDebugLog.Info("SetupWorkflow", $"USER_8 scrap suggestion started. CandidateCount={candidates.Length}, User8SourceCount={user8Sources.Length}. Candidates='{string.Join(" | ", candidates)}'.");

        string? bestMatch = null;
        var bestDistance = int.MaxValue;

        foreach (var source in user8Sources)
        {
            var match = FindBestScrapTypeMatch(source, candidates);
            if (match.Match is null)
            {
                continue;
            }

            if (match.Distance < bestDistance)
            {
                bestDistance = match.Distance;
                bestMatch = match.Match;
            }
        }

        State.SelectedScrapType = bestMatch ?? s_defaultScrapTypes[0];
        StartupDebugLog.Info("SetupWorkflow", $"USER_8 scrap suggestion completed. BestMatch='{bestMatch}', SelectedScrapType='{State.SelectedScrapType}', Distance={bestDistance}. Sources='{string.Join(" | ", user8Sources)}'.");
    }

    private async Task RehydrateOrSuggestScrapTypeAsync(string sequenceNumber, CancellationToken cancellationToken)
    {
        StartupDebugLog.Info("SetupWorkflow", $"Scrap rehydrate started. WO='{State.NormalizedWorkOrder}', Part='{State.SelectedPartNumber}', Sequence='{sequenceNumber}'.");
        var savedScrapType = await _persistenceService
            .LoadSavedScrapTypeAsync(State.NormalizedWorkOrder, State.SelectedPartNumber, sequenceNumber, cancellationToken)
            .ConfigureAwait(true);

        if (!string.IsNullOrWhiteSpace(savedScrapType))
        {
            var existing = State.ScrapTypes.FirstOrDefault(value => string.Equals(value, savedScrapType, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                State.ScrapTypes.Add(savedScrapType);
                State.SelectedScrapType = savedScrapType;
                StartupDebugLog.Info("SetupWorkflow", $"Saved scrap type not found in list; appended new value. SavedValue='{savedScrapType}'.");
            }
            else
            {
                State.SelectedScrapType = existing;
            }

            StartupDebugLog.Info("SetupWorkflow", $"Scrap type rehydrated from saved metadata. Sequence='{sequenceNumber}', Value='{State.SelectedScrapType}'.");

            return;
        }

        State.SelectedScrapType = s_defaultScrapTypes[0];
        StartupDebugLog.Info("SetupWorkflow", $"No saved scrap type found. Defaulting to required placeholder. Sequence='{sequenceNumber}', Value='{State.SelectedScrapType}'.");
    }

    private static (string? Match, int Distance) FindBestScrapTypeMatch(string source, IReadOnlyList<string> candidates)
    {
        var normalizedSource = NormalizeForFuzzy(source);
        if (string.IsNullOrWhiteSpace(normalizedSource))
        {
            return (null, int.MaxValue);
        }

        string? bestMatch = null;
        var bestDistance = int.MaxValue;

        foreach (var candidate in candidates)
        {
            var normalizedCandidate = NormalizeForFuzzy(candidate);
            if (string.IsNullOrWhiteSpace(normalizedCandidate))
            {
                continue;
            }

            if (string.Equals(normalizedSource, normalizedCandidate, StringComparison.OrdinalIgnoreCase)
                || normalizedSource.Contains(normalizedCandidate, StringComparison.OrdinalIgnoreCase)
                || normalizedCandidate.Contains(normalizedSource, StringComparison.OrdinalIgnoreCase))
            {
                return (candidate, 0);
            }

            var distance = ComputeLevenshteinDistance(normalizedSource, normalizedCandidate);
            var threshold = Math.Max(2, (int)Math.Ceiling(Math.Max(normalizedSource.Length, normalizedCandidate.Length) * 0.35));
            if (distance <= threshold && distance < bestDistance)
            {
                bestDistance = distance;
                bestMatch = candidate;
            }
        }

        return (bestMatch, bestDistance);
    }

    private static string NormalizeForFuzzy(string value)
    {
        var chars = value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray();
        return new string(chars);
    }

    private static int ComputeLevenshteinDistance(string left, string right)
    {
        if (left.Length == 0)
        {
            return right.Length;
        }

        if (right.Length == 0)
        {
            return left.Length;
        }

        var costs = new int[right.Length + 1];
        for (var j = 0; j <= right.Length; j++)
        {
            costs[j] = j;
        }

        for (var i = 1; i <= left.Length; i++)
        {
            var previousDiagonal = costs[0];
            costs[0] = i;

            for (var j = 1; j <= right.Length; j++)
            {
                var previousCost = costs[j];
                var substitutionCost = left[i - 1] == right[j - 1] ? 0 : 1;
                costs[j] = Math.Min(
                    Math.Min(costs[j] + 1, costs[j - 1] + 1),
                    previousDiagonal + substitutionCost);
                previousDiagonal = previousCost;
            }
        }

        return costs[right.Length];
    }
}