using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Setup.Models;
using Microsoft.UI.Dispatching;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MTM_Waitlist.Module_Setup.Services;

public sealed class SetupPersistenceService : ISetupPersistenceService
{
    private readonly IActiveJobCoordinatorService _activeJobCoordinatorService;
    private readonly MySqlHelperServer _mySqlHelperServer;

    private static string LocalizeOrDefault(string key, string fallback)
    {
        DispatcherQueue? dispatcherQueue;
        try
        {
            dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        }
        catch (COMException)
        {
            return fallback;
        }
        catch (FileNotFoundException)
        {
            return fallback;
        }

        if (dispatcherQueue is null || !dispatcherQueue.HasThreadAccess)
        {
            // Setup persistence frequently runs off the UI thread; avoid WinRT resource-loader COM calls there.
            return fallback;
        }

        var localized = key.GetLocalized();
        return string.Equals(localized, key, StringComparison.Ordinal) ? fallback : localized;
    }

    public SetupPersistenceService(IActiveJobCoordinatorService activeJobCoordinatorService, MySqlHelperServer mySqlHelperServer)
    {
        _activeJobCoordinatorService = activeJobCoordinatorService;
        _mySqlHelperServer = mySqlHelperServer;
    }

    public async Task<SetupSaveResult> SaveAsync(SetupSaveRequest request, bool forceReplace = false, CancellationToken cancellationToken = default)
    {
        StartupDebugLog.Info("SetupPersistence", $"SaveAsync started. ForceReplace={forceReplace}, WorkCenter='{request.WorkCenter}', WO='{request.WorkOrder}', Part='{request.PartNumber}', Sequence='{request.SequenceNumber}'.");
        if (await _activeJobCoordinatorService.HasActiveJobAsync(request.WorkCenter, cancellationToken).ConfigureAwait(false) && !forceReplace)
        {
            StartupDebugLog.Info("SetupPersistence", $"Active job exists for WorkCenter='{request.WorkCenter}'. Replacement confirmation required.");
            return new SetupSaveResult
            {
                Success = false,
                RequiresReplacementConfirmation = true,
                Message = string.Format(LocalizeOrDefault("Setup_Review.Confirmation.ReplacePrompt", "Work center '{0}' already has an active setup. Replace it?"), request.WorkCenter)
            };
        }

        var result = await _mySqlHelperServer.ExecuteReadWriteAsync(
            "Setup.Save",
            request.WorkOrder,
            () => SaveMockAsync(),
            () => SaveBackendAsync(request, cancellationToken)).ConfigureAwait(false);

        StartupDebugLog.Info("SetupPersistence", $"SaveAsync completed. Success={result.Success}, RequiresReplacementConfirmation={result.RequiresReplacementConfirmation}, Message='{result.Message}'.");
        return result;
    }

    public async Task<IReadOnlyList<SetupDunnagePart>> LoadSavedDunnageAssignmentsAsync(string workOrder, string partNumber, string sequenceNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workOrder)
            || string.IsNullOrWhiteSpace(partNumber)
            || string.IsNullOrWhiteSpace(sequenceNumber))
        {
            return Array.Empty<SetupDunnagePart>();
        }

        var rows = await _mySqlHelperServer.ExecuteStoredProcedureQueryAsync(
            "sp_setup_active_job_dunnage_assignments_get",
            new Dictionary<string, object?>
            {
                ["p_work_order"] = workOrder.Trim(),
                ["p_part_number"] = partNumber.Trim(),
                ["p_sequence_number"] = sequenceNumber.Trim(),
            },
            MySqlDatabaseTarget.MtmWaitlist,
            cancellationToken).ConfigureAwait(false);

        if (rows.Count == 0)
        {
            return Array.Empty<SetupDunnagePart>();
        }

        if (!rows[0].TryGetValue("selected_dunnage_parts_json", out var jsonValue) || jsonValue is null)
        {
            return Array.Empty<SetupDunnagePart>();
        }

        var json = Convert.ToString(jsonValue);
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<SetupDunnagePart>();
        }

        try
        {
            var payload = JsonSerializer.Deserialize<List<PersistedDunnagePart>>(json) ?? new List<PersistedDunnagePart>();
            return payload
                .Where(item => !string.IsNullOrWhiteSpace(item.Id) && !string.IsNullOrWhiteSpace(item.PartNumber))
                .Select(item => new SetupDunnagePart
                {
                    Id = item.Id ?? string.Empty,
                    TypeId = item.TypeId ?? string.Empty,
                    PartNumber = item.PartNumber ?? string.Empty,
                    DisplayName = string.IsNullOrWhiteSpace(item.DisplayName) ? item.PartNumber ?? string.Empty : item.DisplayName!,
                    ImagePath = item.ImagePath ?? string.Empty,
                    Metadata = item.Metadata ?? string.Empty,
                })
                .ToArray();
        }
        catch
        {
            return Array.Empty<SetupDunnagePart>();
        }
    }

    public async Task<string?> LoadSavedScrapTypeAsync(string workOrder, string partNumber, string sequenceNumber, CancellationToken cancellationToken = default)
    {
        StartupDebugLog.Info("SetupPersistence", $"LoadSavedScrapTypeAsync started. WO='{workOrder}', Part='{partNumber}', Sequence='{sequenceNumber}'.");
        if (string.IsNullOrWhiteSpace(workOrder)
            || string.IsNullOrWhiteSpace(partNumber)
            || string.IsNullOrWhiteSpace(sequenceNumber))
        {
            StartupDebugLog.Info("SetupPersistence", "LoadSavedScrapTypeAsync skipped due to missing required key(s).");
            return null;
        }

        var exactPairScrapType = await LoadSavedScrapTypeInternalAsync(
            workOrder,
            partNumber,
            sequenceNumber,
            cancellationToken).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(exactPairScrapType))
        {
            StartupDebugLog.Info("SetupPersistence", $"LoadSavedScrapTypeAsync matched exact WO+Part+Sequence history entry. ScrapType='{exactPairScrapType}'.");
            return exactPairScrapType;
        }

        StartupDebugLog.Info("SetupPersistence", "LoadSavedScrapTypeAsync found no exact WO+Part+Sequence history scrap type.");
        return null;
    }

    private async Task<string?> LoadSavedScrapTypeInternalAsync(
        string workOrder,
        string partNumber,
        string sequenceNumber,
        CancellationToken cancellationToken)
    {
        StartupDebugLog.Info("SetupPersistence", $"LoadSavedScrapTypeInternalAsync started. Source=setup_part_sequence_custom_data, Scope=Part+Sequence, Part='{partNumber}', Sequence='{sequenceNumber}'.");
                var rows = await _mySqlHelperServer.ExecuteStoredProcedureQueryAsync(
                        "sp_setup_job_history_scrap_type_get",
            new Dictionary<string, object?>
            {
                ["p_work_order"] = workOrder.Trim(),
                ["p_part_number"] = partNumber.Trim(),
                ["p_sequence_number"] = sequenceNumber.Trim(),
            },
            MySqlDatabaseTarget.MtmWaitlist,
            cancellationToken).ConfigureAwait(false);

        StartupDebugLog.Info("SetupPersistence", $"LoadSavedScrapTypeInternalAsync query completed. Source=setup_part_sequence_custom_data, RowCount={rows.Count}.");

        if (rows.Count == 0)
        {
            return null;
        }

        if (!rows[0].TryGetValue("selected_scrap_type", out var scrapValue) || scrapValue is null)
        {
            StartupDebugLog.Info("SetupPersistence", "LoadSavedScrapTypeInternalAsync found row but selected_scrap_type was null/missing.");
            return null;
        }

        var selectedScrapType = Convert.ToString(scrapValue)?.Trim();
        if (string.IsNullOrWhiteSpace(selectedScrapType))
        {
            StartupDebugLog.Info("SetupPersistence", "LoadSavedScrapTypeInternalAsync found row but selected_scrap_type was empty.");
            return null;
        }

        StartupDebugLog.Info("SetupPersistence", $"LoadSavedScrapTypeInternalAsync resolved selected_scrap_type='{selectedScrapType}'.");
        return selectedScrapType;
    }

    private static Task<SetupSaveResult> SaveMockAsync()
    {
        StartupDebugLog.Info("SetupPersistence", "SaveMockAsync executed.");
        return Task.FromResult(new SetupSaveResult
        {
            Success = true,
            Message = LocalizeOrDefault("Setup_Review.Status.MockSaved", "Setup was saved successfully in mock mode.")
        });
    }

    private async Task<SetupSaveResult> SaveBackendAsync(SetupSaveRequest request, CancellationToken cancellationToken)
    {
        StartupDebugLog.Info("SetupPersistence", "SaveBackendAsync started. Loading waitlist SQL script and executing stored procedure.");
        var distinctRequestScrapValues = request.SubordinateParts
            .Select(part => part.SelectedScrapType?.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        StartupDebugLog.Info("SetupPersistence", $"SaveBackendAsync scrap payload snapshot. RequestSelectedScrapType='{request.SelectedScrapType}', SubordinatePartCount={request.SubordinateParts.Count}, DistinctSubordinateScrapCount={distinctRequestScrapValues.Length}, DistinctSubordinateScrapValues='{string.Join(" | ", distinctRequestScrapValues)}'.");
        _ = await SetupWaitlistMySqlScriptStore.LoadAsync("create.sql", cancellationToken).ConfigureAwait(false);

        var parameters = new Dictionary<string, object?>
        {
            ["p_work_order"] = request.WorkOrder,
            ["p_part_number"] = request.PartNumber,
            ["p_sequence_number"] = request.SequenceNumber,
            ["p_work_center"] = request.WorkCenter,
            ["p_selected_dunnage_type_id"] = request.SelectedDunnageTypeId,
            ["p_selected_dunnage_part_id"] = request.SelectedDunnagePartId,
            ["p_subordinate_parts_json"] = JsonSerializer.Serialize(request.SubordinateParts),
            ["p_selected_dunnage_parts_json"] = JsonSerializer.Serialize(request.SelectedDunnageParts),
            ["p_saved_by_user_id"] = null,
        };
        StartupDebugLog.Info("SetupPersistence", $"SaveBackendAsync procedure parameters assembled. SubordinateJsonLength={Convert.ToString(parameters["p_subordinate_parts_json"])?.Length ?? 0}, SelectedDunnageJsonLength={Convert.ToString(parameters["p_selected_dunnage_parts_json"])?.Length ?? 0}." );

        var affectedRows = await _mySqlHelperServer.ExecuteStoredProcedureNonQueryAsync(
            "sp_setup_save_setup",
            parameters,
            MySqlDatabaseTarget.MtmWaitlist,
            cancellationToken).ConfigureAwait(false);

        StartupDebugLog.Info("SetupPersistence", $"sp_setup_save_setup completed. AffectedRows={affectedRows}.");

        if (affectedRows <= 0)
        {
            StartupDebugLog.Info("SetupPersistence", "SaveBackendAsync failed because stored procedure did not persist any row.");
            return new SetupSaveResult
            {
                Success = false,
                Message = "Setup save failed: no rows were written to setup_active_jobs."
            };
        }

        var customDataRows = await _mySqlHelperServer.ExecuteStoredProcedureNonQueryAsync(
            "sp_setup_part_sequence_custom_data_upsert",
            new Dictionary<string, object?>
            {
                ["p_part_number"] = request.PartNumber,
                ["p_sequence_number"] = request.SequenceNumber,
                ["p_selected_scrap_type"] = request.SelectedScrapType,
                ["p_selected_dunnage_type_id"] = request.SelectedDunnageTypeId,
                ["p_selected_dunnage_part_id"] = request.SelectedDunnagePartId,
                ["p_subordinate_parts_json"] = JsonSerializer.Serialize(request.SubordinateParts),
                ["p_selected_dunnage_parts_json"] = JsonSerializer.Serialize(request.SelectedDunnageParts),
                ["p_updated_by_user_id"] = null,
            },
            MySqlDatabaseTarget.MtmWaitlist,
            cancellationToken).ConfigureAwait(false);

        StartupDebugLog.Info("SetupPersistence", $"sp_setup_part_sequence_custom_data_upsert completed. AffectedRows={customDataRows}.");

        var historyRows = await _mySqlHelperServer.ExecuteStoredProcedureNonQueryAsync(
            "sp_setup_job_history_insert_for_pair",
            new Dictionary<string, object?>
            {
                ["p_event_action"] = "save",
                ["p_changed_by_user_id"] = null,
                ["p_work_order"] = request.WorkOrder,
                ["p_part_number"] = request.PartNumber,
                ["p_sequence_number"] = request.SequenceNumber,
            },
            MySqlDatabaseTarget.MtmWaitlist,
            cancellationToken).ConfigureAwait(false);

        StartupDebugLog.Info("SetupPersistence", $"setup_job_history insert completed. AffectedRows={historyRows}.");

        if (historyRows <= 0)
        {
            return new SetupSaveResult
            {
                Success = false,
                Message = "Setup save failed: job history was not recorded."
            };
        }

        var touchRows = await _mySqlHelperServer.ExecuteStoredProcedureNonQueryAsync(
            "sp_setup_workstations_touch",
            new Dictionary<string, object?>
            {
                ["p_work_center"] = request.WorkCenter,
                ["p_updated_by_user_id"] = null,
            },
            MySqlDatabaseTarget.MtmWaitlist,
            cancellationToken).ConfigureAwait(false);

        StartupDebugLog.Info("SetupPersistence", $"sp_setup_workstations_touch completed. WorkCenter='{request.WorkCenter}', AffectedRows={touchRows}.");

        await _activeJobCoordinatorService.RegisterActiveJobAsync(request, cancellationToken).ConfigureAwait(false);
        StartupDebugLog.Info("SetupPersistence", "Active job coordinator registration completed.");

        return new SetupSaveResult
        {
            Success = affectedRows > 0,
            Message = LocalizeOrDefault("Setup_Review.Status.Saved", "Setup was saved successfully.")
        };
    }

    private sealed class PersistedDunnagePart
    {
        [JsonPropertyName("Id")]
        public string? Id { get; set; }

        [JsonPropertyName("TypeId")]
        public string? TypeId { get; set; }

        [JsonPropertyName("PartNumber")]
        public string? PartNumber { get; set; }

        [JsonPropertyName("DisplayName")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("ImagePath")]
        public string? ImagePath { get; set; }

        [JsonPropertyName("Metadata")]
        public string? Metadata { get; set; }
    }

    private sealed class PersistedSubordinatePart
    {
        [JsonPropertyName("SelectedScrapType")]
        public string? SelectedScrapType { get; set; }
    }
}