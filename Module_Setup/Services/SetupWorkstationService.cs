using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Setup.Models;

namespace MTM_Waitlist.Module_Setup.Services;

public sealed class SetupWorkstationService : ISetupWorkstationService
{
    private readonly MySqlHelperServer _mySqlHelperServer;

    public SetupWorkstationService(MySqlHelperServer mySqlHelperServer)
    {
        _mySqlHelperServer = mySqlHelperServer;
    }

    public async Task<IReadOnlyList<SetupWorkstation>> GetWorkstationsAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _mySqlHelperServer.ExecuteStoredProcedureQueryAsync(
            "sp_setup_workstations_get_all",
            new Dictionary<string, object?>(),
            MySqlDatabaseTarget.MtmWaitlist,
            cancellationToken).ConfigureAwait(false);

        var activeJobs = await _mySqlHelperServer.ExecuteStoredProcedureQueryAsync(
            "sp_setup_active_jobs_latest_by_work_center_get",
            new Dictionary<string, object?>(),
            MySqlDatabaseTarget.MtmWaitlist,
            cancellationToken).ConfigureAwait(false);

        var jobsByWorkCenter = activeJobs
            .Where(row => !string.IsNullOrWhiteSpace(GetValue(row, "work_center")))
            .ToDictionary(
                row => GetValue(row, "work_center"),
                row => row,
                StringComparer.OrdinalIgnoreCase);

        return rows
            .Select(row =>
            {
                var workstationName = GetValue(row, "workstation_name");
                jobsByWorkCenter.TryGetValue(workstationName, out var activeJobRow);

                return new SetupWorkstation
                {
                    Id = GetValue(row, "id"),
                    Name = workstationName,
                    Building = GetValue(row, "building"),
                    IsActive = string.Equals(GetValue(row, "is_active"), "1", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(GetValue(row, "is_active"), "true", StringComparison.OrdinalIgnoreCase),
                    CurrentWorkOrder = activeJobRow is null ? string.Empty : GetValue(activeJobRow, "work_order"),
                    CurrentPartNumber = activeJobRow is null ? string.Empty : GetValue(activeJobRow, "part_number"),
                    CurrentSequenceNumber = activeJobRow is null ? string.Empty : GetValue(activeJobRow, "sequence_number"),
                };
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Id) && !string.IsNullOrWhiteSpace(item.Name))
            .ToArray();
    }

    public async Task<SetupSelectionResult> AddWorkstationAsync(string workstationName, string building, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workstationName))
        {
            return new SetupSelectionResult { Success = false, Message = "Workstation name is required." };
        }

        if (string.IsNullOrWhiteSpace(building))
        {
            return new SetupSelectionResult { Success = false, Message = "Building is required." };
        }

        var affectedRows = await _mySqlHelperServer.ExecuteStoredProcedureNonQueryAsync(
            "sp_setup_workstations_upsert",
            new Dictionary<string, object?>
            {
                ["p_workstation_id"] = null,
                ["p_workstation_name"] = workstationName.Trim(),
                ["p_building"] = building.Trim(),
                ["p_modified_by_user_id"] = null,
            },
            MySqlDatabaseTarget.MtmWaitlist,
            cancellationToken).ConfigureAwait(false);

        return new SetupSelectionResult
        {
            Success = affectedRows > 0,
            Message = affectedRows > 0 ? "Workstation added." : "Unable to add workstation."
        };
    }

    public async Task<SetupSelectionResult> UpdateWorkstationAsync(string workstationId, string workstationName, string building, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workstationId) || string.IsNullOrWhiteSpace(workstationName) || string.IsNullOrWhiteSpace(building))
        {
            return new SetupSelectionResult { Success = false, Message = "Workstation ID, name, and building are required." };
        }

        var affectedRows = await _mySqlHelperServer.ExecuteStoredProcedureNonQueryAsync(
            "sp_setup_workstations_upsert",
            new Dictionary<string, object?>
            {
                ["p_workstation_id"] = workstationId.Trim(),
                ["p_workstation_name"] = workstationName.Trim(),
                ["p_building"] = building.Trim(),
                ["p_modified_by_user_id"] = null,
            },
            MySqlDatabaseTarget.MtmWaitlist,
            cancellationToken).ConfigureAwait(false);

        return new SetupSelectionResult
        {
            Success = affectedRows > 0,
            Message = affectedRows > 0 ? "Workstation updated." : "Unable to update workstation."
        };
    }

    public async Task<SetupSelectionResult> RemoveWorkstationAsync(string workstationId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workstationId))
        {
            return new SetupSelectionResult { Success = false, Message = "Workstation ID is required." };
        }

        var affectedRows = await _mySqlHelperServer.ExecuteStoredProcedureNonQueryAsync(
            "sp_setup_workstations_delete",
            new Dictionary<string, object?>
            {
                ["p_workstation_id"] = workstationId.Trim(),
            },
            MySqlDatabaseTarget.MtmWaitlist,
            cancellationToken).ConfigureAwait(false);

        return new SetupSelectionResult
        {
            Success = affectedRows > 0,
            Message = affectedRows > 0 ? "Workstation removed." : "Unable to remove workstation."
        };
    }

    private static string GetValue(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null)
        {
            return string.Empty;
        }

        return Convert.ToString(value)?.Trim() ?? string.Empty;
    }
}
