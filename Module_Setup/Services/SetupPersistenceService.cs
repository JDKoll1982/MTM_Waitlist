using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Setup.Models;
using System.Text.Json;

namespace MTM_Waitlist.Module_Setup.Services;

public sealed class SetupPersistenceService : ISetupPersistenceService
{
    private readonly IActiveJobCoordinatorService _activeJobCoordinatorService;
    private readonly MySqlHelperServer _mySqlHelperServer;

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
                Message = string.Format("Setup_Review.Confirmation.ReplacePrompt".GetLocalized(), request.WorkCenter)
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

    private static Task<SetupSaveResult> SaveMockAsync()
    {
        StartupDebugLog.Info("SetupPersistence", "SaveMockAsync executed.");
        return Task.FromResult(new SetupSaveResult
        {
            Success = true,
            Message = "Setup_Review.Status.MockSaved".GetLocalized()
        });
    }

    private async Task<SetupSaveResult> SaveBackendAsync(SetupSaveRequest request, CancellationToken cancellationToken)
    {
        StartupDebugLog.Info("SetupPersistence", "SaveBackendAsync started. Loading waitlist SQL script and executing stored procedure.");
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

        var affectedRows = await _mySqlHelperServer.ExecuteStoredProcedureNonQueryAsync(
            "sp_setup_save_setup",
            parameters,
            MySqlDatabaseTarget.MtmWaitlist,
            cancellationToken).ConfigureAwait(false);

        StartupDebugLog.Info("SetupPersistence", $"sp_setup_save_setup completed. AffectedRows={affectedRows}.");

        await _activeJobCoordinatorService.RegisterActiveJobAsync(request, cancellationToken).ConfigureAwait(false);
        StartupDebugLog.Info("SetupPersistence", "Active job coordinator registration completed.");

        return new SetupSaveResult
        {
            Success = true,
            Message = "Setup_Review.Status.Saved".GetLocalized()
        };
    }
}