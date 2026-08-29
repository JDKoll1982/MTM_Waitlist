using System.Collections.Concurrent;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Waitlist.Services;

public sealed class WaitlistRequestService : IWaitlistRequestService
{
    private const string InforVisualMockDataSettingKey = "Feature.InforVisualMockData";
    private const string RecvMockDataSettingKey = "Feature.RecvMockData";

    public event EventHandler? RequestsChanged;

    private readonly ILocalSettingsService? _localSettingsService;
    private readonly ISampleDataService? _sampleDataService;
    private readonly MySqlHelperServer? _mySqlHelperServer;
    private readonly ConcurrentDictionary<Guid, WaitlistRequest> _requests = new();
    private readonly ConcurrentDictionary<Guid, List<WaitlistRequestAuditEntry>> _auditTrail = new();

    public WaitlistRequestService()
    {
    }

    public WaitlistRequestService(
        ILocalSettingsService? localSettingsService,
        ISampleDataService? sampleDataService,
        MySqlHelperServer? mySqlHelperServer)
    {
        _localSettingsService = localSettingsService;
        _sampleDataService = sampleDataService;
        _mySqlHelperServer = mySqlHelperServer;
    }

    public IReadOnlyList<WaitlistRequest> GetActiveRequests(string? building = null)
    {
        var normalizedBuilding = building?.Trim();
        return _requests.Values
            .Where(request => string.IsNullOrWhiteSpace(normalizedBuilding) || string.Equals(request.Building, normalizedBuilding, StringComparison.OrdinalIgnoreCase))
            .Where(request => string.Equals(request.Status, "Pending", StringComparison.OrdinalIgnoreCase) || string.Equals(request.Status, "Accepted", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(request => request.RequestedUtc)
            .ToArray();
    }

    public WaitlistRequest? GetRequest(Guid requestId)
    {
        return _requests.TryGetValue(requestId, out var request) ? request : null;
    }

    public IReadOnlyList<WaitlistRequestAuditEntry> GetAuditTrail(Guid requestId)
    {
        if (_auditTrail.TryGetValue(requestId, out var auditEntries))
        {
            return auditEntries.OrderBy(item => item.OccurredUtc).ToArray();
        }

        return Array.Empty<WaitlistRequestAuditEntry>();
    }

    public async Task<bool> TransitionStatusAsync(Guid requestId, string status, string? cancellationReason = null, string? canceledByEmployeeNumber = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_requests.TryGetValue(requestId, out var existing))
        {
            return false;
        }

        var normalizedStatus = (status ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedStatus))
        {
            return false;
        }

        var nextStatus = normalizedStatus switch
        {
            "Pending" => "Pending",
            "Accepted" => "Accepted",
            "Completed" => "Completed",
            "Canceled" => "Canceled",
            _ => throw new ArgumentOutOfRangeException(nameof(status), $"Unsupported request status '{status}'.")
        };

        if (!IsValidStatusTransition(existing.Status, nextStatus))
        {
            return false;
        }

        var updated = new WaitlistRequest
        {
            Id = existing.Id,
            Building = existing.Building,
            WorkCenter = existing.WorkCenter,
            RequestType = existing.RequestType,
            Subtype = existing.Subtype,
            InputValue = existing.InputValue,
            ActiveSetupJobId = existing.ActiveSetupJobId,
            WorkCenterName = existing.WorkCenterName,
            RequesterEmployeeNumber = existing.RequesterEmployeeNumber,
            RequesterEmployeeName = existing.RequesterEmployeeName,
            Status = nextStatus,
            RequestedUtc = existing.RequestedUtc,
            TargetTimeUtc = existing.TargetTimeUtc,
            IsOverdue = existing.IsOverdue,
            AssignedMaterialHandler = existing.AssignedMaterialHandler,
            CancellationReason = string.Equals(nextStatus, "Canceled", StringComparison.OrdinalIgnoreCase)
                ? (string.IsNullOrWhiteSpace(cancellationReason) ? existing.CancellationReason : cancellationReason.Trim())
                : null,
            CanceledUtc = string.Equals(nextStatus, "Canceled", StringComparison.OrdinalIgnoreCase) ? DateTimeOffset.UtcNow : null,
            CanceledByEmployeeNumber = string.Equals(nextStatus, "Canceled", StringComparison.OrdinalIgnoreCase)
                ? (string.IsNullOrWhiteSpace(canceledByEmployeeNumber) ? existing.CanceledByEmployeeNumber : canceledByEmployeeNumber.Trim())
                : null,
        };

        _requests[requestId] = updated;
        RecordAuditTrail(requestId, nextStatus, canceledByEmployeeNumber, cancellationReason);
        RequestsChanged?.Invoke(this, EventArgs.Empty);
        await Task.CompletedTask.ConfigureAwait(false);
        return true;
    }

    public void Reset()
    {
        _requests.Clear();
        _auditTrail.Clear();
    }

    private static bool IsValidStatusTransition(string currentStatus, string nextStatus)
    {
        var current = (currentStatus ?? string.Empty).Trim();
        var next = (nextStatus ?? string.Empty).Trim();

        return current switch
        {
            "Pending" => next is "Accepted" or "Canceled" or "Completed",
            "Accepted" => next is "Completed" or "Canceled",
            "Completed" => false,
            "Canceled" => false,
            _ => false,
        };
    }

    public async Task<WaitlistRequestSubmitResult> SubmitAsync(WaitlistRequestDraft draft, bool allowDuplicate, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (draft is null)
        {
            return WaitlistRequestSubmitResult.ValidationFailure("Request details are required.");
        }

        if (string.IsNullOrWhiteSpace(draft.Building) || string.IsNullOrWhiteSpace(draft.WorkCenter) || string.IsNullOrWhiteSpace(draft.RequestType))
        {
            return WaitlistRequestSubmitResult.ValidationFailure("Building, Work Center, and request type are required.");
        }

        if (string.IsNullOrWhiteSpace(draft.ActiveSetupJobId))
        {
            return WaitlistRequestSubmitResult.ValidationFailure("A valid active setup job is required before submitting a waitlist request.");
        }

        if (string.IsNullOrWhiteSpace(draft.WorkCenterName))
        {
            return WaitlistRequestSubmitResult.ValidationFailure("The current workstation name is required before submitting a waitlist request.");
        }

        if (string.IsNullOrWhiteSpace(draft.RequesterEmployeeNumber) || string.IsNullOrWhiteSpace(draft.RequesterEmployeeName))
        {
            return WaitlistRequestSubmitResult.ValidationFailure("The verified requester employee identity is required before submitting a waitlist request.");
        }

        var normalizedDraftInput = draft.InputValue?.Trim();
        var normalizedDraftSubtype = draft.Subtype?.Trim();
        var duplicate = GetActiveRequests(draft.Building).FirstOrDefault(request =>
            string.Equals(request.WorkCenter, draft.WorkCenter.Trim(), StringComparison.OrdinalIgnoreCase)
            && string.Equals(request.RequestType, draft.RequestType.Trim(), StringComparison.OrdinalIgnoreCase)
            && string.Equals(request.Subtype ?? string.Empty, normalizedDraftSubtype ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            && string.Equals(request.InputValue ?? string.Empty, normalizedDraftInput ?? string.Empty, StringComparison.OrdinalIgnoreCase));

        if (duplicate is not null && !allowDuplicate)
        {
            StartupDebugLog.Info("WaitlistRequest", $"Duplicate request detected. Building='{draft.Building}', WorkCenter='{draft.WorkCenter}', RequestType='{draft.RequestType}', Subtype='{draft.Subtype ?? string.Empty}'.");
            return WaitlistRequestSubmitResult.DuplicateWarning(duplicate);
        }

        var request = new WaitlistRequest
        {
            Building = draft.Building.Trim(),
            WorkCenter = draft.WorkCenter.Trim(),
            RequestType = draft.RequestType.Trim(),
            Subtype = string.IsNullOrWhiteSpace(draft.Subtype) ? null : draft.Subtype.Trim(),
            InputValue = string.IsNullOrWhiteSpace(draft.InputValue) ? null : draft.InputValue.Trim(),
            ActiveSetupJobId = draft.ActiveSetupJobId.Trim(),
            WorkCenterName = draft.WorkCenterName.Trim(),
            RequesterEmployeeNumber = draft.RequesterEmployeeNumber.Trim(),
            RequesterEmployeeName = draft.RequesterEmployeeName.Trim(),
            RequestedUtc = draft.RequestedUtc,
            TargetTimeUtc = draft.TargetTimeUtc,
            IsOverdue = draft.IsOverdue,
            AssignedMaterialHandler = string.IsNullOrWhiteSpace(draft.AssignedMaterialHandler) ? null : draft.AssignedMaterialHandler.Trim(),
            CancellationReason = string.IsNullOrWhiteSpace(draft.CancellationReason) ? null : draft.CancellationReason.Trim(),
        };

        if (IsMockDataEnabled())
        {
            _requests[request.Id] = request;
            RecordAuditTrail(request.Id, "Created", request.RequesterEmployeeNumber, request.InputValue);
            RequestsChanged?.Invoke(this, EventArgs.Empty);
            StartupDebugLog.Info("WaitlistRequest", $"Mock request stored in session. Id='{request.Id}', Building='{request.Building}', WorkCenter='{request.WorkCenter}', RequestType='{request.RequestType}', Subtype='{request.Subtype ?? string.Empty}', ActiveJobId='{request.ActiveSetupJobId}', Requester='{request.RequesterEmployeeNumber}'.");
            return WaitlistRequestSubmitResult.Success(request);
        }

        if (_mySqlHelperServer is not null)
        {
            var affectedRows = await _mySqlHelperServer.ExecuteStoredProcedureNonQueryAsync(
                "sp_waitlist_request_insert",
                new Dictionary<string, object?>
                {
                    ["p_building"] = request.Building,
                    ["p_work_center"] = request.WorkCenter,
                    ["p_request_type"] = request.RequestType,
                    ["p_subtype"] = request.Subtype,
                    ["p_input_value"] = request.InputValue,
                    ["p_active_setup_job_id"] = request.ActiveSetupJobId,
                    ["p_work_center_name"] = request.WorkCenterName,
                    ["p_requester_employee_number"] = request.RequesterEmployeeNumber,
                    ["p_requester_employee_name"] = request.RequesterEmployeeName,
                    ["p_status"] = request.Status,
                    ["p_requested_utc"] = request.RequestedUtc.UtcDateTime,
                    ["p_target_time_utc"] = request.TargetTimeUtc?.UtcDateTime,
                    ["p_is_overdue"] = request.IsOverdue,
                    ["p_assigned_material_handler"] = request.AssignedMaterialHandler,
                    ["p_cancellation_reason"] = request.CancellationReason,
                },
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken).ConfigureAwait(false);

            if (affectedRows <= 0)
            {
                StartupDebugLog.Error(
                    "WaitlistRequest",
                    new InvalidOperationException("Production waitlist persistence failed. No rows were affected by stored procedure 'sp_waitlist_request_insert'."),
                    $"Production waitlist persistence failed for request '{request.Id}'. No rows were affected by stored procedure 'sp_waitlist_request_insert'.");
                return WaitlistRequestSubmitResult.PersistenceFailure("Production waitlist persistence is not configured or failed. Re-check the helper-server route and database contract.");
            }
        }

        _requests[request.Id] = request;
        RecordAuditTrail(request.Id, "Created", request.RequesterEmployeeNumber, request.InputValue);
        RequestsChanged?.Invoke(this, EventArgs.Empty);
        StartupDebugLog.Info("WaitlistRequest", $"Request stored in session and production route acknowledged. Id='{request.Id}', Building='{request.Building}', WorkCenter='{request.WorkCenter}', RequestType='{request.RequestType}', Subtype='{request.Subtype ?? string.Empty}', ActiveJobId='{request.ActiveSetupJobId}', Requester='{request.RequesterEmployeeNumber}'.");
        return WaitlistRequestSubmitResult.Success(request);
    }

    private void RecordAuditTrail(Guid requestId, string eventType, string? employeeNumber, string? details)
    {
        var entry = new WaitlistRequestAuditEntry
        {
            RequestId = requestId,
            EventType = eventType,
            OccurredUtc = DateTimeOffset.UtcNow,
            EmployeeNumber = string.IsNullOrWhiteSpace(employeeNumber) ? null : employeeNumber.Trim(),
            Details = string.IsNullOrWhiteSpace(details) ? null : details.Trim(),
        };

        var auditEntries = _auditTrail.GetOrAdd(requestId, _ => new List<WaitlistRequestAuditEntry>());
        lock (auditEntries)
        {
            auditEntries.Add(entry);
        }
    }

    private bool IsMockDataEnabled()
    {
        if (_localSettingsService is null)
        {
            return false;
        }

        var inforVisualValue = _localSettingsService.ReadSettingAsync<bool?>(InforVisualMockDataSettingKey).GetAwaiter().GetResult() ?? false;
        var recvValue = _localSettingsService.ReadSettingAsync<bool?>(RecvMockDataSettingKey).GetAwaiter().GetResult() ?? false;
        return inforVisualValue || recvValue;
    }
}