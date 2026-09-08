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
    private readonly IMySqlHelperServer? _mySqlHelperServer;
    private readonly INewRequestAlertNotifier? _newRequestAlertNotifier;
    private readonly IUrgencyDeadlineService? _urgencyDeadlineService;
    private readonly ConcurrentDictionary<Guid, WaitlistRequest> _requests = new();
    private readonly ConcurrentDictionary<Guid, List<WaitlistRequestAuditEntry>> _auditTrail = new();

    public WaitlistRequestService()
    {
    }

    public WaitlistRequestService(
        ILocalSettingsService? localSettingsService,
        ISampleDataService? sampleDataService,
        IMySqlHelperServer? mySqlHelperServer,
        INewRequestAlertNotifier? newRequestAlertNotifier = null,
        IUrgencyDeadlineService? urgencyDeadlineService = null)
    {
        _localSettingsService = localSettingsService;
        _sampleDataService = sampleDataService;
        _mySqlHelperServer = mySqlHelperServer;
        _newRequestAlertNotifier = newRequestAlertNotifier;
        _urgencyDeadlineService = urgencyDeadlineService;
    }

    public async Task<int> RefreshFromDatabaseAsync(string? building = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (IsMockDataEnabled())
        {
            StartupDebugLog.Info("WaitlistRequest", "RefreshFromDatabaseAsync skipped: mock data is enabled.");
            return 0;
        }

        if (_mySqlHelperServer is null)
        {
            StartupDebugLog.Info("WaitlistRequest", "RefreshFromDatabaseAsync skipped: no MySQL helper is configured.");
            return 0;
        }

        var rows = await _mySqlHelperServer.ExecuteStoredProcedureQueryAsync(
            "sp_waitlist_request_list",
            new Dictionary<string, object?>
            {
                ["p_building"] = building,
                ["p_include_resolved"] = 0,
            },
            MySqlDatabaseTarget.MtmWaitlist,
            cancellationToken).ConfigureAwait(false);

        var loaded = new List<WaitlistRequest>(rows.Count);
        foreach (var row in rows)
        {
            var mapped = MapRowToRequest(row);
            if (mapped is not null)
            {
                loaded.Add(mapped);
            }
        }

        // The DB is the authoritative source for open requests when mock data is OFF:
        // replace the in-memory set with the rows read back so the list reflects reality.
        _requests.Clear();
        foreach (var request in loaded)
        {
            _requests[request.Id] = request;
        }

        StartupDebugLog.Info("WaitlistRequest", $"RefreshFromDatabaseAsync loaded {loaded.Count} open request(s) from DB. Building='{building ?? "all"}'.");
        return loaded.Count;
    }

    private static WaitlistRequest? MapRowToRequest(IReadOnlyDictionary<string, object?> row)
    {
        var publicId = ReadString(row, "public_id");
        if (!Guid.TryParse(publicId, out var requestId))
        {
            return null;
        }

        return new WaitlistRequest
        {
            Id = requestId,
            Building = ReadString(row, "building"),
            WorkCenter = ReadString(row, "work_center"),
            RequestType = ReadString(row, "request_type"),
            Subtype = ReadNullableString(row, "subtype"),
            InputValue = ReadNullableString(row, "input_value"),
            ActiveSetupJobId = ReadString(row, "active_setup_job_id"),
            WorkCenterName = ReadString(row, "work_center_name"),
            RequesterEmployeeNumber = ReadString(row, "requester_employee_number"),
            RequesterEmployeeName = ReadString(row, "requester_employee_name"),
            Status = ReadString(row, "status"),
            RequestedUtc = ReadDateTimeUtc(row, "requested_utc") ?? DateTimeOffset.UtcNow,
            TargetTimeUtc = ReadDateTimeUtc(row, "target_time_utc"),
            IsOverdue = ReadBool(row, "is_overdue"),
            AssignedMaterialHandler = ReadNullableString(row, "assigned_material_handler"),
            CancellationReason = ReadNullableString(row, "cancellation_reason"),
            CanceledUtc = ReadDateTimeUtc(row, "canceled_utc"),
            CanceledByEmployeeNumber = ReadNullableString(row, "canceled_by_employee_number"),
            Note = ReadNullableString(row, "note"),
            AcceptedUtc = ReadDateTimeUtc(row, "accepted_utc"),
            CompletedUtc = ReadDateTimeUtc(row, "completed_utc"),
            ReleasedUtc = ReadDateTimeUtc(row, "released_utc"),
        };
    }

    private static string ReadString(IReadOnlyDictionary<string, object?> row, string key)
        => row.TryGetValue(key, out var value) ? Convert.ToString(value)?.Trim() ?? string.Empty : string.Empty;

    private static string? ReadNullableString(IReadOnlyDictionary<string, object?> row, string key)
    {
        var value = ReadString(row, key);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static bool ReadBool(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (row.TryGetValue(key, out var value) && value is not null)
        {
            try { return Convert.ToBoolean(value); }
            catch (Exception) { /* fall through */ }
        }
        return false;
    }

    private static DateTimeOffset? ReadDateTimeUtc(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (row.TryGetValue(key, out var value) && value is not null)
        {
            if (value is DateTime dateTime)
            {
                return new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc));
            }
            if (DateTimeOffset.TryParse(Convert.ToString(value), out var parsed))
            {
                return parsed.ToUniversalTime();
            }
        }
        return null;
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

    /// <summary>
    /// Returns the requests submitted by a given requester (across all statuses so the "My
    /// Requests" view can show Waiting, In Progress, Done, and Cancelled together), optionally
    /// scoped to a building, newest first.
    /// </summary>
    public IReadOnlyList<WaitlistRequest> GetMyRequests(string requesterEmployeeNumber, string? building = null)
    {
        var normalizedRequester = (requesterEmployeeNumber ?? string.Empty).Trim();
        var normalizedBuilding = building?.Trim();
        return _requests.Values
            .Where(request => string.Equals(request.RequesterEmployeeNumber, normalizedRequester, StringComparison.OrdinalIgnoreCase))
            .Where(request => string.IsNullOrWhiteSpace(normalizedBuilding) || string.Equals(request.Building, normalizedBuilding, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(request => request.RequestedUtc)
            .ToArray();
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
            AcceptedUtc = string.Equals(nextStatus, "Accepted", StringComparison.OrdinalIgnoreCase)
                ? (existing.AcceptedUtc ?? DateTimeOffset.UtcNow)
                : existing.AcceptedUtc,
            CompletedUtc = string.Equals(nextStatus, "Completed", StringComparison.OrdinalIgnoreCase)
                ? (existing.CompletedUtc ?? DateTimeOffset.UtcNow)
                : existing.CompletedUtc,
            ReleasedUtc = existing.ReleasedUtc,
            Note = existing.Note,
        };

        _requests[requestId] = updated;
        StartupDebugLog.Info(
            "WaitlistRequest",
            $"Request '{requestId}' transitioned from '{existing.Status}' to '{nextStatus}'. AcceptedUtc='{updated.AcceptedUtc}', CompletedUtc='{updated.CompletedUtc}'.");
        await RecordAuditAsync(requestId, existing.Status, nextStatus, nextStatus, canceledByEmployeeNumber, null, cancellationReason, cancellationToken);

        // Persist the transition to the MySQL DB when not in mock mode and a helper is present.
        // The in-memory request id equals the DB public_id (client-supplied on insert / DB row id
        // on load), so the status update targets the correct row.
        if (!IsMockDataEnabled() && _mySqlHelperServer is not null)
        {
            var rowsAffected = await _mySqlHelperServer.ExecuteStoredProcedureNonQueryAsync(
                "sp_waitlist_request_status_update",
                new Dictionary<string, object?>
                {
                    ["p_public_id"] = requestId.ToString(),
                    ["p_status"] = nextStatus,
                    ["p_assigned_material_handler"] = updated.AssignedMaterialHandler,
                    ["p_cancellation_reason"] = string.Equals(nextStatus, "Canceled", StringComparison.OrdinalIgnoreCase) ? cancellationReason : null,
                    ["p_canceled_by_employee_number"] = string.Equals(nextStatus, "Canceled", StringComparison.OrdinalIgnoreCase) ? canceledByEmployeeNumber : null,
                    ["p_note"] = updated.Note,
                },
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken).ConfigureAwait(false);
            StartupDebugLog.Info("WaitlistRequest", $"Status transition '{nextStatus}' persisted to DB for request '{requestId}'. RowsAffected={rowsAffected}.");
        }

        RequestsChanged?.Invoke(this, EventArgs.Empty);
        await Task.CompletedTask.ConfigureAwait(false);
        return true;
    }

    public async Task<WaitlistRequestCancelResult> CancelOwnRequestAsync(
        Guid requestId,
        string requesterEmployeeNumber,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_requests.TryGetValue(requestId, out var existing))
        {
            StartupDebugLog.Info("WaitlistRequest", $"Cancel-own skipped: request '{requestId}' not found.");
            return WaitlistRequestCancelResult.NotFound();
        }

        var normalizedRequester = (requesterEmployeeNumber ?? string.Empty).Trim();
        if (!string.Equals(normalizedRequester, existing.RequesterEmployeeNumber, StringComparison.OrdinalIgnoreCase))
        {
            StartupDebugLog.Info(
                "WaitlistRequest",
                $"Cancel-own denied: requester '{normalizedRequester}' is not the creator of request '{requestId}' (creator '{existing.RequesterEmployeeNumber}').");
            return WaitlistRequestCancelResult.NotOwnedByRequester();
        }

        if (!string.Equals(existing.Status, "Pending", StringComparison.OrdinalIgnoreCase))
        {
            StartupDebugLog.Info(
                "WaitlistRequest",
                $"Cancel-own denied: request '{requestId}' is in state '{existing.Status}', not Waiting (Pending).");
            return WaitlistRequestCancelResult.NotInCancelableState();
        }

        var transitioned = await TransitionStatusAsync(requestId, "Canceled", reason, normalizedRequester, cancellationToken).ConfigureAwait(false);
        if (!transitioned)
        {
            return WaitlistRequestCancelResult.Failed();
        }

        _requests.TryGetValue(requestId, out var cancelled);
        StartupDebugLog.Info("WaitlistRequest", $"Requester '{normalizedRequester}' cancelled their own request '{requestId}'. Reason='{(reason ?? "null")}'.");
        return cancelled is null ? WaitlistRequestCancelResult.Failed() : WaitlistRequestCancelResult.Success(cancelled);
    }

    public void Reset()
    {
        _requests.Clear();
        _auditTrail.Clear();
    }

    public async Task<WaitlistRequest?> UpdateNoteAsync(Guid requestId, string? note, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_requests.TryGetValue(requestId, out var existing))
        {
            StartupDebugLog.Info("WaitlistRequest", $"Update note skipped: request '{requestId}' not found.");
            return null;
        }

        var normalizedNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (string.Equals(existing.Note ?? string.Empty, normalizedNote ?? string.Empty, StringComparison.Ordinal))
        {
            return existing; // no change
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
            Status = existing.Status,
            RequestedUtc = existing.RequestedUtc,
            TargetTimeUtc = existing.TargetTimeUtc,
            IsOverdue = existing.IsOverdue,
            AssignedMaterialHandler = existing.AssignedMaterialHandler,
            CancellationReason = existing.CancellationReason,
            CanceledUtc = existing.CanceledUtc,
            CanceledByEmployeeNumber = existing.CanceledByEmployeeNumber,
            AcceptedUtc = existing.AcceptedUtc,
            CompletedUtc = existing.CompletedUtc,
            ReleasedUtc = existing.ReleasedUtc,
            Note = normalizedNote,
        };

        _requests[requestId] = updated;
        StartupDebugLog.Info("WaitlistRequest", $"Note updated for request '{requestId}'. Status='{updated.Status}'.");
        await RecordAuditAsync(requestId, existing.Status, updated.Status, "NoteUpdated", null, null, normalizedNote, cancellationToken).ConfigureAwait(false);

        // Persist the note through the status-update path (status unchanged) when not in mock mode.
        if (!IsMockDataEnabled() && _mySqlHelperServer is not null)
        {
            try
            {
                await _mySqlHelperServer.ExecuteStoredProcedureNonQueryAsync(
                    "sp_waitlist_request_status_update",
                    new Dictionary<string, object?>
                    {
                        ["p_public_id"] = requestId.ToString(),
                        ["p_status"] = updated.Status,
                        ["p_assigned_material_handler"] = updated.AssignedMaterialHandler,
                        ["p_cancellation_reason"] = updated.CancellationReason,
                        ["p_canceled_by_employee_number"] = updated.CanceledByEmployeeNumber,
                        ["p_note"] = updated.Note,
                    },
                    MySqlDatabaseTarget.MtmWaitlist,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StartupDebugLog.Error("WaitlistRequest", ex, $"Failed to persist note for request '{requestId}'.");
            }
        }

        RequestsChanged?.Invoke(this, EventArgs.Empty);
        return updated;
    }

    public async Task<WaitlistRequest?> AcceptAsync(
        Guid requestId,
        string handlerEmployeeNumber,
        string? handlerEmployeeName = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_requests.TryGetValue(requestId, out var existing))
        {
            return null;
        }

        var handler = (handlerEmployeeNumber ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(handler))
        {
            return null;
        }

        // Only an available (Pending / unclaimed) request may be accepted.
        if (!RequestActionPolicy.IsAvailable(existing.Status))
        {
            return null;
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
            Status = "Accepted",
            RequestedUtc = existing.RequestedUtc,
            TargetTimeUtc = existing.TargetTimeUtc,
            IsOverdue = existing.IsOverdue,
            AssignedMaterialHandler = handler,
            CancellationReason = null,
            CanceledUtc = null,
            CanceledByEmployeeNumber = null,
            AcceptedUtc = existing.AcceptedUtc ?? DateTimeOffset.UtcNow,
            CompletedUtc = existing.CompletedUtc,
            ReleasedUtc = existing.ReleasedUtc,
            Note = existing.Note,
        };

        await PersistHandlerActionAsync(requestId, existing.Status, updated, "Accepted", handler, handlerEmployeeName, handler, cancellationToken).ConfigureAwait(false);
        StartupDebugLog.Info("WaitlistRequest", $"Request '{requestId}' accepted by handler '{handler}'. Status 'Pending' -> 'Accepted'.");
        return updated;
    }

    public async Task<WaitlistRequest?> CompleteAsync(
        Guid requestId,
        string handlerEmployeeNumber,
        string? handlerEmployeeName = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_requests.TryGetValue(requestId, out var existing))
        {
            return null;
        }

        var handler = (handlerEmployeeNumber ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(handler)
            || !RequestActionPolicy.CanViewerCompleteOrRelease(existing.Status, existing.AssignedMaterialHandler, handler))
        {
            return null;
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
            Status = "Completed",
            RequestedUtc = existing.RequestedUtc,
            TargetTimeUtc = existing.TargetTimeUtc,
            IsOverdue = existing.IsOverdue,
            AssignedMaterialHandler = existing.AssignedMaterialHandler,
            CancellationReason = existing.CancellationReason,
            CanceledUtc = existing.CanceledUtc,
            CanceledByEmployeeNumber = existing.CanceledByEmployeeNumber,
            AcceptedUtc = existing.AcceptedUtc,
            CompletedUtc = existing.CompletedUtc ?? DateTimeOffset.UtcNow,
            ReleasedUtc = existing.ReleasedUtc,
            Note = existing.Note,
        };

        await PersistHandlerActionAsync(requestId, existing.Status, updated, "Completed", handler, handlerEmployeeName, null, cancellationToken).ConfigureAwait(false);
        StartupDebugLog.Info("WaitlistRequest", $"Request '{requestId}' completed by handler '{handler}'. Status 'Accepted' -> 'Completed'.");
        return updated;
    }

    public async Task<WaitlistRequest?> ReleaseAsync(
        Guid requestId,
        string handlerEmployeeNumber,
        string? handlerEmployeeName = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_requests.TryGetValue(requestId, out var existing))
        {
            return null;
        }

        var handler = (handlerEmployeeNumber ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(handler)
            || !RequestActionPolicy.CanViewerCompleteOrRelease(existing.Status, existing.AssignedMaterialHandler, handler))
        {
            return null;
        }

        // Release returns the job to the OPEN list: status -> Pending, assignee cleared, ReleasedUtc stamped.
        // It is NOT a cancellation and must never touch the cancellation metadata.
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
            Status = "Pending",
            RequestedUtc = existing.RequestedUtc,
            TargetTimeUtc = existing.TargetTimeUtc,
            IsOverdue = existing.IsOverdue,
            AssignedMaterialHandler = null,
            CancellationReason = existing.CancellationReason,
            CanceledUtc = existing.CanceledUtc,
            CanceledByEmployeeNumber = existing.CanceledByEmployeeNumber,
            AcceptedUtc = existing.AcceptedUtc,
            CompletedUtc = existing.CompletedUtc,
            ReleasedUtc = DateTimeOffset.UtcNow,
            Note = existing.Note,
        };

        await PersistHandlerActionAsync(requestId, existing.Status, updated, "Released", handler, handlerEmployeeName, null, cancellationToken).ConfigureAwait(false);
        StartupDebugLog.Info("WaitlistRequest", $"Request '{requestId}' released by handler '{handler}'. Status 'Accepted' -> 'Pending' (not a cancellation).");
        return updated;
    }

    /// <summary>
    /// Stores a handler-action transition, records its audit entry, persists it through the status-update
    /// path (mock OFF), and raises <see cref="RequestsChanged"/>.
    /// </summary>
    private async Task PersistHandlerActionAsync(
        Guid requestId,
        string fromStatus,
        WaitlistRequest updated,
        string eventType,
        string? actorNumber,
        string? actorName,
        string? details,
        CancellationToken cancellationToken)
    {
        _requests[requestId] = updated;
        await RecordAuditAsync(requestId, fromStatus, updated.Status, eventType, actorNumber, actorName, details, cancellationToken).ConfigureAwait(false);

        if (!IsMockDataEnabled() && _mySqlHelperServer is not null)
        {
            await _mySqlHelperServer.ExecuteStoredProcedureNonQueryAsync(
                "sp_waitlist_request_status_update",
                new Dictionary<string, object?>
                {
                    ["p_public_id"] = requestId.ToString(),
                    ["p_status"] = updated.Status,
                    ["p_assigned_material_handler"] = updated.AssignedMaterialHandler,
                    ["p_cancellation_reason"] = updated.CancellationReason,
                    ["p_canceled_by_employee_number"] = updated.CanceledByEmployeeNumber,
                    ["p_note"] = updated.Note,
                },
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken).ConfigureAwait(false);
        }

        RequestsChanged?.Invoke(this, EventArgs.Empty);
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

        // Workflow 08: if the flow did not already set a due time, derive one from the request's created time and
        // its sub-type's max-allotted time (UrgencyDeadlineService) so every submitted request carries a deadline.
        var resolvedTargetUtc = draft.TargetTimeUtc;
        if (resolvedTargetUtc is null && _urgencyDeadlineService is not null)
        {
            try
            {
                var urgency = await _urgencyDeadlineService
                    .ComputeAsync(draft.RequestedUtc, draft.Subtype, DateTimeOffset.UtcNow, cancellationToken)
                    .ConfigureAwait(false);
                resolvedTargetUtc = urgency.DueUtc;
            }
            catch (Exception ex)
            {
                StartupDebugLog.Error("WaitlistRequest", ex, "Failed to derive the urgency deadline for a new request.");
            }
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
            TargetTimeUtc = resolvedTargetUtc,
            IsOverdue = draft.IsOverdue,
            AssignedMaterialHandler = string.IsNullOrWhiteSpace(draft.AssignedMaterialHandler) ? null : draft.AssignedMaterialHandler.Trim(),
            CancellationReason = string.IsNullOrWhiteSpace(draft.CancellationReason) ? null : draft.CancellationReason.Trim(),
            Note = string.IsNullOrWhiteSpace(draft.Note) ? null : draft.Note.Trim(),
        };

        // Waitlist requests are REAL mtm_waitlist data and are persisted regardless of the Infor Visual / receiving
        // mock toggles (which only short-circuit external lookups to sample data). Persist whenever a helper server
        // is configured so a new request added in mock mode still saves to the database.
        if (_mySqlHelperServer is not null)
        {
            var affectedRows = await _mySqlHelperServer.ExecuteStoredProcedureNonQueryAsync(
                "sp_waitlist_request_insert",
                new Dictionary<string, object?>
                {
                    ["p_public_id"] = request.Id.ToString(),
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
                    ["p_note"] = request.Note,
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
        await RecordAuditAsync(request.Id, null, null, "Created", request.RequesterEmployeeNumber, request.RequesterEmployeeName, request.InputValue, cancellationToken);
        RequestsChanged?.Invoke(this, EventArgs.Empty);
        StartupDebugLog.Info("WaitlistRequest", $"Request stored in session and production route acknowledged. Id='{request.Id}', Building='{request.Building}', WorkCenter='{request.WorkCenter}', RequestType='{request.RequestType}', Subtype='{request.Subtype ?? string.Empty}', ActiveJobId='{request.ActiveSetupJobId}', Requester='{request.RequesterEmployeeNumber}'.");
        await NotifyRequestCreatedAsync(request).ConfigureAwait(false);
        return WaitlistRequestSubmitResult.Success(request);
    }

    private async Task RecordAuditAsync(
        Guid requestId,
        string? fromStatus,
        string? toStatus,
        string eventType,
        string? employeeNumber,
        string? employeeName,
        string? details,
        CancellationToken cancellationToken)
    {
        var entry = new WaitlistRequestAuditEntry
        {
            RequestId = requestId,
            FromStatus = string.IsNullOrWhiteSpace(fromStatus) ? null : fromStatus.Trim(),
            ToStatus = string.IsNullOrWhiteSpace(toStatus) ? null : toStatus.Trim(),
            EventType = (eventType ?? string.Empty).Trim(),
            OccurredUtc = DateTimeOffset.UtcNow,
            EmployeeNumber = string.IsNullOrWhiteSpace(employeeNumber) ? null : employeeNumber.Trim(),
            EmployeeName = string.IsNullOrWhiteSpace(employeeName) ? null : employeeName.Trim(),
            Details = string.IsNullOrWhiteSpace(details) ? null : details.Trim(),
        };

        var auditEntries = _auditTrail.GetOrAdd(requestId, _ => new List<WaitlistRequestAuditEntry>());
        lock (auditEntries)
        {
            auditEntries.Add(entry);
        }

        // Persist the audit entry to the DB (mock OFF only); cancelled records are never purged.
        if (!IsMockDataEnabled() && _mySqlHelperServer is not null)
        {
            await _mySqlHelperServer.ExecuteStoredProcedureNonQueryAsync(
                "sp_waitlist_request_audit_insert",
                new Dictionary<string, object?>
                {
                    ["p_request_public_id"] = requestId.ToString(),
                    ["p_from_status"] = entry.FromStatus,
                    ["p_to_status"] = entry.ToStatus,
                    ["p_event_type"] = entry.EventType,
                    ["p_actor_employee_number"] = entry.EmployeeNumber,
                    ["p_actor_employee_name"] = entry.EmployeeName,
                    ["p_details"] = entry.Details,
                },
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken).ConfigureAwait(false);
            StartupDebugLog.Info("WaitlistRequest", $"Audit entry persisted for request '{requestId}'. EventType='{entry.EventType}', From='{(entry.FromStatus ?? "null")}', To='{(entry.ToStatus ?? "null")}'.");
        }
    }

    private async Task NotifyRequestCreatedAsync(WaitlistRequest request)
    {
        if (_newRequestAlertNotifier is null)
        {
            return;
        }

        try
        {
            var title = "NewRequestAlert_Title".GetLocalized();
            var body = string.Format(
                "NewRequestAlert_Body".GetLocalized(),
                request.WorkCenter,
                request.RequestType);
            await _newRequestAlertNotifier
                .NotifyNewRequestAsync(request.Id, title, body, RuntimeHelper.IsMSIX)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // A toast failure must never break a successful request submission.
            StartupDebugLog.Error("WaitlistRequest", ex, "Failed to raise the new-request alert.");
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