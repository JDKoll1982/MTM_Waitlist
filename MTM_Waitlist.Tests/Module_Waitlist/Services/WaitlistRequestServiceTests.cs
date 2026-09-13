using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;
using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Tests.Module_Waitlist.Services;

[TestClass]
public sealed class WaitlistRequestServiceTests
{
    [TestMethod]
    public async Task SubmitAsync_PersistsRequestAndReturnsSuccessAsync()
    {
        var service = new WaitlistRequestService();
        var draft = CreateDraft();

        var result = await service.SubmitAsync(draft, allowDuplicate: false);

        Assert.AreEqual(WaitlistRequestSubmitStatus.Success, result.Status);
        Assert.IsNotNull(result.Request);
        var requests = service.GetActiveRequests("Expo Drive");
        Assert.AreEqual(1, requests.Count);
        Assert.AreEqual("deliver-wrong-coil", requests[0].Item);
        Assert.AreEqual("Press 12", requests[0].WorkCenter);
    }

    [TestMethod]
    public async Task SubmitAsync_DerivesUrgencyDeadlineWhenDraftHasNone()
    {
        var settings = new InMemoryLocalSettingsService(new Dictionary<string, object>());
        var deadline = new UrgencyDeadlineService(new UrgencySettingsService(settings));
        var service = new WaitlistRequestService(null, null, deadline);

        var result = await service.SubmitAsync(DeadlineLessDraft(), allowDuplicate: false);

        Assert.AreEqual(WaitlistRequestSubmitStatus.Success, result.Status);
        Assert.IsNotNull(result.Request!.TargetTimeUtc, "A deadline should be derived when the draft has none.");
        var span = result.Request.TargetTimeUtc.Value - result.Request.RequestedUtc;
        Assert.IsTrue(span >= TimeSpan.FromMinutes(14) && span <= TimeSpan.FromMinutes(16), $"expected ~15 min, got {span}.");
        Assert.IsFalse(result.Request.IsOverdue);
    }

    [TestMethod]
    public async Task SubmitAsync_PreservesExplicitDeadline()
    {
        var settings = new InMemoryLocalSettingsService(new Dictionary<string, object>());
        var deadline = new UrgencyDeadlineService(new UrgencySettingsService(settings));
        var service = new WaitlistRequestService(null, null, deadline);

        // CreateDraft already carries an explicit TargetTimeUtc; it must be preserved, not overwritten.
        var result = await service.SubmitAsync(CreateDraft(), allowDuplicate: false);

        Assert.AreEqual(WaitlistRequestSubmitStatus.Success, result.Status);
        Assert.IsNotNull(result.Request!.TargetTimeUtc);
    }

    [TestMethod]
    public async Task UpdateNote_SetsNoteOnRequestAndRecordsAudit()
    {
        var service = new WaitlistRequestService();
        var submit = await service.SubmitAsync(CreateDraft(), allowDuplicate: false);
        var id = submit.Request!.Id;

        var updated = await service.UpdateNoteAsync(id, "Forklift ordered for pickup");

        Assert.IsNotNull(updated);
        Assert.AreEqual("Forklift ordered for pickup", updated!.Note);
        Assert.AreEqual("Forklift ordered for pickup", service.GetRequest(id)!.Note);
        Assert.IsTrue(service.GetAuditTrail(id).Any(entry => entry.EventType == "NoteUpdated"));
    }

    [TestMethod]
    public async Task UpdateNote_EmptyClearsNote()
    {
        var service = new WaitlistRequestService();
        var submit = await service.SubmitAsync(CreateDraft(), allowDuplicate: false);
        var id = submit.Request!.Id;

        await service.UpdateNoteAsync(id, "a note");
        var cleared = await service.UpdateNoteAsync(id, "   ");

        Assert.IsNotNull(cleared);
        Assert.IsNull(cleared!.Note);
        Assert.IsNull(service.GetRequest(id)!.Note);
    }

    [TestMethod]
    public async Task UpdateNote_NotFound_ReturnsNull()
    {
        var service = new WaitlistRequestService();

        var result = await service.UpdateNoteAsync(Guid.NewGuid(), "note");

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task SubmitAsync_AlwaysPersistsNewRequestToDatabase()
    {
        var helper = new StubMySqlHelperServer(Array.Empty<Dictionary<string, object?>>());
        var service = new WaitlistRequestService(helper);

        var result = await service.SubmitAsync(CreateDraft(), allowDuplicate: false);

        Assert.AreEqual(WaitlistRequestSubmitStatus.Success, result.Status);
        Assert.IsNotNull(result.Request);
        // The app's own store is always written: no setting can short-circuit the insert (FR-001, SC-002).
        Assert.IsTrue(helper.NonQueryProcedures.Contains("sp_waitlist_request_insert"),
            "A new request must be saved to the mtm_waitlist database on every submit.");
    }

    [TestMethod]
    public async Task SubmitAsync_ReturnsDuplicateWarningThenAllowsOverrideAsync()
    {
        var service = new WaitlistRequestService();
        var draft = CreateDraft();

        await service.SubmitAsync(draft, allowDuplicate: false);
        var warning = await service.SubmitAsync(draft, allowDuplicate: false);
        var overrideResult = await service.SubmitAsync(draft, allowDuplicate: true);

        Assert.AreEqual(WaitlistRequestSubmitStatus.DuplicateWarningRequired, warning.Status);
        Assert.AreEqual(WaitlistRequestSubmitStatus.Success, overrideResult.Status);
        Assert.AreEqual(2, service.GetActiveRequests("Expo Drive").Count);
    }

    [TestMethod]
    public async Task SubmitAsync_OnlyFlagsExactDuplicateRequests()
    {
        var service = new WaitlistRequestService();
        var draft = CreateDraft();
        var differentDetailDraft = new WaitlistRequestDraft
        {
            Building = draft.Building,
            WorkCenter = draft.WorkCenter,
            Category = draft.Category,
            Item = draft.Item,
            InputValue = "Different reason for the same request",
            ActiveSetupJobId = draft.ActiveSetupJobId,
            WorkCenterName = draft.WorkCenterName,
            RequesterEmployeeNumber = draft.RequesterEmployeeNumber,
            RequesterEmployeeName = draft.RequesterEmployeeName,
            RequestedUtc = DateTimeOffset.UtcNow,
            TargetTimeUtc = draft.TargetTimeUtc,
            IsOverdue = draft.IsOverdue,
            AssignedMaterialHandler = draft.AssignedMaterialHandler,
        };

        var firstResult = await service.SubmitAsync(draft, allowDuplicate: false);
        var secondResult = await service.SubmitAsync(differentDetailDraft, allowDuplicate: false);

        Assert.AreEqual(WaitlistRequestSubmitStatus.Success, firstResult.Status);
        Assert.AreEqual(WaitlistRequestSubmitStatus.Success, secondResult.Status);
        Assert.AreEqual(2, service.GetActiveRequests("Expo Drive").Count);
    }

    [TestMethod]
    public async Task SubmitAsync_RejectsIncompleteDraftAsync()
    {
        var service = new WaitlistRequestService();

        var result = await service.SubmitAsync(new WaitlistRequestDraft(), allowDuplicate: false);

        Assert.AreEqual(WaitlistRequestSubmitStatus.ValidationFailure, result.Status);
        Assert.AreEqual(0, service.GetActiveRequests().Count);
    }

    [TestMethod]
    public async Task SubmitAsync_WhenTheInsertAffectsNoRows_ReturnsPersistenceFailureAsync()
    {
        // The production route reports "not configured or failed" when sp_waitlist_request_insert affects no
        // rows, which is the same branch a store that cannot answer takes. Driven by a stub so the premise is
        // deterministic: the previous version stood up the real helper server and, on a host with a configured
        // connection, both failed the assertion and wrote rows into the operational store (T153/T155).
        var mySqlHelperServer = new StubMySqlHelperServer(Array.Empty<Dictionary<string, object?>>(), affectedRows: 0);
        var service = new WaitlistRequestService(mySqlHelperServer);

        var result = await service.SubmitAsync(CreateDraft(), allowDuplicate: false);

        Assert.AreEqual(WaitlistRequestSubmitStatus.PersistenceFailure, result.Status);
        Assert.AreEqual("Production waitlist persistence is not configured or failed. Re-check the helper-server route and database contract.", result.Message);
        Assert.AreEqual(0, service.GetActiveRequests().Count);
    }

    [TestMethod]
    public async Task RefreshFromDatabaseAsync_LoadsOpenRequestsFromDb()
    {
        var helper = new StubMySqlHelperServer(
        [
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["public_id"] = "f0000000-00aa-4000-8000-0000000000aa",
                ["building"] = "Expo Drive",
                ["work_center"] = "100-3",
                ["category"] = "Pickup",
                ["item"] = "pickup-coil",
                ["input_value"] = null,
                ["active_setup_job_id"] = "100-3",
                ["work_center_name"] = "100-3",
                ["requester_employee_number"] = "6229",
                ["requester_employee_name"] = "John Koll",
                ["status"] = "Pending",
                ["requested_utc"] = new DateTime(2026, 9, 5, 15, 0, 0, DateTimeKind.Unspecified),
                ["target_time_utc"] = null,
                ["is_overdue"] = 0,
                ["assigned_material_handler"] = null,
                ["cancellation_reason"] = null,
                ["canceled_utc"] = null,
                ["canceled_by_employee_number"] = null,
                ["note"] = "db note",
                ["accepted_utc"] = null,
                ["completed_utc"] = null,
                ["released_utc"] = null,
            },
        ]);
        var service = new WaitlistRequestService(helper);

        var count = await service.RefreshFromDatabaseAsync("Expo Drive");

        Assert.AreEqual(1, count);
        Assert.AreEqual(1, helper.QueryCallCount);
        var request = service.GetActiveRequests("Expo Drive").SingleOrDefault();
        Assert.IsNotNull(request);
        Assert.AreEqual("Pending", request!.Status);
        Assert.AreEqual("100-3", request.WorkCenter);
        Assert.AreEqual("db note", request.Note);
    }

    [TestMethod]
    public async Task RefreshFromDatabaseAsync_AlwaysQueriesTheDatabase()
    {
        var helper = new StubMySqlHelperServer(Array.Empty<Dictionary<string, object?>>());
        var service = new WaitlistRequestService(helper);

        var count = await service.RefreshFromDatabaseAsync("Expo Drive");

        Assert.AreEqual(0, count);
        Assert.AreEqual(1, helper.QueryCallCount, "The app's own store is always queried (FR-001).");
        Assert.AreEqual(0, service.GetActiveRequests("Expo Drive").Count);
    }

    [TestMethod]
    public async Task TransitionStatusAsync_PersistsStatusUpdateToDb()
    {
        var helper = new StubMySqlHelperServer(Array.Empty<Dictionary<string, object?>>());
        var service = new WaitlistRequestService(helper);

        var submit = await service.SubmitAsync(CreateDraft(), allowDuplicate: false);
        Assert.AreEqual(WaitlistRequestSubmitStatus.Success, submit.Status);
        helper.NonQueryProcedures.Clear();

        var transitioned = await service.TransitionStatusAsync(submit.Request!.Id, "Accepted");

        Assert.IsTrue(transitioned);
        Assert.IsTrue(helper.NonQueryProcedures.Contains("sp_waitlist_request_status_update"));
    }

    [TestMethod]
    public async Task AuditTrail_RecordsFromToStatusAcrossLifecycle()
    {
        var service = new WaitlistRequestService();
        var submit = await service.SubmitAsync(CreateDraft(), allowDuplicate: false);
        var requestId = submit.Request!.Id;

        await service.TransitionStatusAsync(requestId, "Accepted");
        await service.TransitionStatusAsync(requestId, "Completed");

        var audit = service.GetAuditTrail(requestId).OrderBy(item => item.OccurredUtc).ToArray();
        Assert.IsTrue(audit.Any(item => item.EventType == "Created"));
        var accepted = audit.First(item => item.EventType == "Accepted");
        var completed = audit.First(item => item.EventType == "Completed");
        Assert.AreEqual("Pending", accepted.FromStatus);
        Assert.AreEqual("Accepted", accepted.ToStatus);
        Assert.AreEqual("Accepted", completed.FromStatus);
        Assert.AreEqual("Completed", completed.ToStatus);
    }

    [TestMethod]
    public async Task AuditTrail_PersistsAuditEntriesToDb()
    {
        var helper = new StubMySqlHelperServer(Array.Empty<Dictionary<string, object?>>());
        var service = new WaitlistRequestService(helper);

        var submit = await service.SubmitAsync(CreateDraft(), allowDuplicate: false);
        Assert.AreEqual(WaitlistRequestSubmitStatus.Success, submit.Status);
        helper.NonQueryProcedures.Clear();

        await service.TransitionStatusAsync(submit.Request!.Id, "Accepted");

        Assert.IsTrue(helper.NonQueryProcedures.Contains("sp_waitlist_request_audit_insert"));
        Assert.IsTrue(helper.NonQueryProcedures.Contains("sp_waitlist_request_status_update"));
    }

    private sealed class StubMySqlHelperServer : IMySqlHelperServer
    {
        private readonly IReadOnlyList<Dictionary<string, object?>> _rows;
        private readonly int _affectedRows;

        public int QueryCallCount { get; private set; }

        public List<string> NonQueryProcedures { get; } = new();

        public StubMySqlHelperServer(IReadOnlyList<Dictionary<string, object?>> rows, int affectedRows = 1)
        {
            _rows = rows;
            _affectedRows = affectedRows;
        }

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
        {
            QueryCallCount++;
            return Task.FromResult(_rows);
        }

        public Task<int> ExecuteStoredProcedureNonQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
        {
            NonQueryProcedures.Add(storedProcedureName);
            return Task.FromResult(_affectedRows);
        }

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteSqlQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<Dictionary<string, object?>>)Array.Empty<Dictionary<string, object?>>());

        public Task<int> ExecuteSqlNonQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default) => Task.FromResult(0);
    }

    [TestMethod]
    public async Task SubmitAsync_PreservesActiveJobAndRequesterMetadataAsync()
    {
        var service = new WaitlistRequestService();
        var targetTime = DateTimeOffset.UtcNow.AddMinutes(15);
        var draft = new WaitlistRequestDraft
        {
            Building = "Expo Drive",
            WorkCenter = "Press 12",
            Category = "Deliver",
            Item = "deliver-wrong-coil",
            InputValue = "Wrong material at press",
            ActiveSetupJobId = "JOB-1001",
            WorkCenterName = "Press 12",
            RequesterEmployeeNumber = "6229",
            RequesterEmployeeName = "John Koll",
            TargetTimeUtc = targetTime,
            IsOverdue = false,
            AssignedMaterialHandler = "M. Lewis",
            CancellationReason = null,
        };

        var result = await service.SubmitAsync(draft, allowDuplicate: false);

        Assert.AreEqual(WaitlistRequestSubmitStatus.Success, result.Status);
        Assert.IsNotNull(result.Request);
        Assert.AreEqual("JOB-1001", result.Request.ActiveSetupJobId);
        Assert.AreEqual("Press 12", result.Request.WorkCenterName);
        Assert.AreEqual("6229", result.Request.RequesterEmployeeNumber);
        Assert.AreEqual("John Koll", result.Request.RequesterEmployeeName);
        Assert.AreEqual(targetTime, result.Request.TargetTimeUtc);
        Assert.IsFalse(result.Request.IsOverdue);
        Assert.AreEqual("M. Lewis", result.Request.AssignedMaterialHandler);
        Assert.IsNull(result.Request.CancellationReason);
    }

    [TestMethod]
    public async Task SubmitAsync_RequiresActiveSetupJobForSubmissionAsync()
    {
        var service = new WaitlistRequestService();
        var draft = new WaitlistRequestDraft
        {
            Building = "Expo Drive",
            WorkCenter = "Press 12",
            Category = "Deliver",
            Item = "deliver-wrong-coil",
            InputValue = "Wrong material at press",
            ActiveSetupJobId = string.Empty,
            WorkCenterName = "Press 12",
            RequesterEmployeeNumber = "6229",
            RequesterEmployeeName = "John Koll",
        };

        var result = await service.SubmitAsync(draft, allowDuplicate: false);

        Assert.AreEqual(WaitlistRequestSubmitStatus.ValidationFailure, result.Status);
        Assert.AreEqual("A valid active setup job is required before submitting a waitlist request.", result.Message);
    }

    [TestMethod]
    public void NewRequestFlowRules_VerifyEmployeeIdentity_ReturnsActiveEmployeeResult()
    {
        var result = NewRequestFlowRules.VerifyEmployeeIdentity("6229");

        Assert.IsTrue(result.IsValid);
        Assert.IsTrue(result.IsActive);
        Assert.AreEqual("6229", result.EmployeeNumber);
        Assert.AreEqual("John Koll", result.EmployeeName);
    }

    [TestMethod]
    public void NewRequestFlowRules_VerifyEmployeeIdentity_RejectsInactiveOrUnknownEmployee()
    {
        var unknownResult = NewRequestFlowRules.VerifyEmployeeIdentity("999999");
        var inactiveResult = NewRequestFlowRules.VerifyEmployeeIdentity("0000");

        Assert.IsFalse(unknownResult.IsValid);
        Assert.IsFalse(inactiveResult.IsValid);
        Assert.IsFalse(inactiveResult.IsActive);
    }

    [TestMethod]
    public void NewRequestFlowRules_ValidateSelectedWorkCenter_RequiresActiveSelection()
    {
        var validResult = NewRequestFlowRules.ValidateSelectedWorkCenter("Press 12");
        var invalidResult = NewRequestFlowRules.ValidateSelectedWorkCenter(string.Empty);
        var blockedResult = NewRequestFlowRules.ValidateSelectedWorkCenter("No active job");

        Assert.IsTrue(validResult.IsValid);
        Assert.IsFalse(invalidResult.IsValid);
        Assert.IsFalse(blockedResult.IsValid);
    }

    [TestMethod]
    public void NewRequestFlowRules_ValidateCurrentJobState_RequiresRestart_WhenWorkCenterIsNoLongerActive()
    {
        var valid = NewRequestFlowRules.ValidateCurrentJobState("Press 12", "JOB-1001");
        var stale = NewRequestFlowRules.ValidateCurrentJobState("No active job", "JOB-1001");
        var missingJob = NewRequestFlowRules.ValidateCurrentJobState("Press 12", string.Empty);

        Assert.IsTrue(valid.IsValid);
        Assert.IsFalse(stale.IsValid);
        Assert.IsFalse(missingJob.IsValid);
    }

    [TestMethod]
    public void NewRequestFlowRules_ValidateActiveJobsForWorkCenter_RejectsMultipleActiveJobs()
    {
        var valid = NewRequestFlowRules.ValidateActiveJobsForWorkCenter("Press 12", new[] { "JOB-1001" });
        var multiple = NewRequestFlowRules.ValidateActiveJobsForWorkCenter("Press 12", new[] { "JOB-1001", "JOB-1002" });
        var missing = NewRequestFlowRules.ValidateActiveJobsForWorkCenter("Press 12", Array.Empty<string>());

        Assert.IsTrue(valid.IsValid);
        Assert.IsFalse(multiple.IsValid);
        Assert.IsFalse(missing.IsValid);
    }

    [TestMethod]
    public void NewRequestFlowRules_GetNextStepType_RequiresAChosenItem()
    {
        // The step order is a property of the chosen Item's stored configuration, so a state with no Item has no
        // next step to resolve — the type/subtype steps that used to decide this are gone (FR-003).
        var state = new NewRequestFlowState { WorkCenter = "Press 12" };

        Assert.ThrowsException<ArgumentNullException>(() => NewRequestFlowRules.GetNextStepType(state));
    }

    [TestMethod]
    public async Task GetMyRequests_FiltersToRequesterAcrossStatusesAndBuilding()
    {
        var service = new WaitlistRequestService();
        var minePending = await service.SubmitAsync(CreateDraft(), allowDuplicate: false);
        Assert.AreEqual(WaitlistRequestSubmitStatus.Success, minePending.Status);

        // Accept then submit a second of our own that we cancel, plus another requester's row.
        await service.TransitionStatusAsync(minePending.Request!.Id, "Accepted");
        var mineCanceled = await service.SubmitAsync(
            new WaitlistRequestDraft
            {
                Building = "Expo Drive",
                WorkCenter = "Press 9",
                Category = "Deliver",
                Item = "deliver-coil",
                ActiveSetupJobId = "JOB-2002",
                WorkCenterName = "Press 9",
                RequesterEmployeeNumber = "6229",
                RequesterEmployeeName = "John Koll",
                RequestedUtc = DateTimeOffset.UtcNow,
            },
            allowDuplicate: false);
        Assert.AreEqual(WaitlistRequestSubmitStatus.Success, mineCanceled.Status);
        await service.CancelOwnRequestAsync(mineCanceled.Request!.Id, "6229", "No longer needed");

        await service.SubmitAsync(
            new WaitlistRequestDraft
            {
                Building = "Expo Drive",
                WorkCenter = "Press 15",
                Category = "Deliver",
                Item = "deliver-coil",
                ActiveSetupJobId = "JOB-3003",
                WorkCenterName = "Press 15",
                RequesterEmployeeNumber = "5000",
                RequesterEmployeeName = "Other User",
                RequestedUtc = DateTimeOffset.UtcNow,
            },
            allowDuplicate: false);

        var mine = service.GetMyRequests("6229", "Expo Drive");
        Assert.AreEqual(2, mine.Count);
        Assert.IsTrue(mine.All(item => item.RequesterEmployeeNumber == "6229"));

        // Other building scope excludes ours.
        Assert.AreEqual(0, service.GetMyRequests("6229", "VITS").Count);
    }

    [TestMethod]
    public async Task GetMyRequests_UnknownRequesterReturnsEmpty()
    {
        var service = new WaitlistRequestService();
        await service.SubmitAsync(CreateDraft(), allowDuplicate: false);

        Assert.AreEqual(0, service.GetMyRequests("999999").Count);
    }

    [TestMethod]
    public void WaitlistViewViewModel_CreatesSessionOrder_CarriesRequestIdAndRequesterMetadata()
    {
        var request = new WaitlistRequest
        {
            Id = Guid.NewGuid(),
            Building = "Expo Drive",
            WorkCenter = "Press 12",
            Category = "Deliver",
            Item = "deliver-coil",
            Status = "Pending",
            RequesterEmployeeNumber = "6229",
            RequesterEmployeeName = "John Koll",
            TargetTimeUtc = DateTimeOffset.UtcNow.AddMinutes(5),
        };

        var order = WaitlistViewViewModel.CreateSessionOrder(request);

        Assert.AreEqual(request.Id, order.RequestId);
        Assert.AreEqual("6229", order.RequesterEmployeeNumber);
        Assert.AreEqual("John Koll", order.RequestedByName);
        Assert.IsTrue(WaitlistViewViewModel.IsRequesterOrder(order, "6229"));
        Assert.IsFalse(WaitlistViewViewModel.IsRequesterOrder(order, "5000"));
        Assert.IsTrue(WaitlistViewViewModel.CanRequesterCancel(order));
    }

    [TestMethod]
    public void WaitlistViewViewModel_CanRequesterCancel_FalseForAcceptedOrStaticRows()
    {
        var accepted = new WaitlistRequest
        {
            Id = Guid.NewGuid(),
            Building = "Expo Drive",
            WorkCenter = "Press 12",
            Category = "Deliver",
            Item = "deliver-coil",
            Status = "Accepted",
            RequesterEmployeeNumber = "6229",
        };
        var acceptedOrder = WaitlistViewViewModel.CreateSessionOrder(accepted);
        Assert.IsFalse(WaitlistViewViewModel.CanRequesterCancel(acceptedOrder));

        var staticRow = new SampleOrder { Id = 1, Title = "Static", Status = string.Empty };
        Assert.IsFalse(WaitlistViewViewModel.CanRequesterCancel(staticRow));
    }

    [TestMethod]
    public void WaitlistViewViewModel_CreatesSessionOrder_WithSpecificItemRules_ForPickupWrongCoilAndScrapEmpty()
    {
        var wrongCoil = new WaitlistRequest
        {
            Id = Guid.NewGuid(),
            Building = "Expo Drive",
            WorkCenter = "Press 12",
            Category = "Deliver",
            Item = "deliver-wrong-coil",
            InputValue = "Wrong material at press",
            Status = "Pending",
            TargetTimeUtc = DateTimeOffset.UtcNow.AddMinutes(8),
            IsOverdue = false,
        };

        var wrongCoilOrder = WaitlistViewViewModel.CreateSessionOrder(wrongCoil);

        // Both the coil identifier and its average weight come from a coil lookup this surface does not
        // perform. The card therefore carries the Item it was raised with and the typed detail, and no
        // stand-in for a coil attribute it cannot support (FR-001/FR-002, FR-004/FR-005).
        Assert.AreEqual("deliver-wrong-coil", wrongCoilOrder.ItemCode);
        Assert.AreEqual("Wrong material at press", wrongCoilOrder.Fields.First(item => item.Label == "Request details").Value);
        Assert.IsFalse(wrongCoilOrder.Fields.Any(item => string.Equals(item.Label, "Requested coil", StringComparison.Ordinal)));

        var normalCoil = new WaitlistRequest
        {
            Id = Guid.NewGuid(),
            Building = "Expo Drive",
            WorkCenter = "100-3",
            Category = "Deliver",
            Item = "deliver-coil",
            Status = "Pending",
            TargetTimeUtc = DateTimeOffset.UtcNow.AddMinutes(5),
        };
        var normalCoilOrder = WaitlistViewViewModel.CreateSessionOrder(normalCoil);

        Assert.IsFalse(normalCoilOrder.Fields.Any(item => string.Equals(item.Label, "Requested coil", StringComparison.Ordinal)));
        Assert.IsFalse(normalCoilOrder.Fields.Any(item => string.Equals(item.Label, "Average coil weight", StringComparison.Ordinal)));

        // A "Bring" request is the deliver-coil Item itself — the action lives in the Item code, not in a
        // second, derived subtype the card would have to render.
        var bringCoil = new WaitlistRequest
        {
            Id = Guid.NewGuid(),
            Building = "Expo Drive",
            WorkCenter = "100-6",
            Category = "Deliver",
            Item = "deliver-coil",
            Status = "Pending",
            TargetTimeUtc = DateTimeOffset.UtcNow.AddMinutes(7),
        };
        var bringCoilOrder = WaitlistViewViewModel.CreateSessionOrder(bringCoil);

        // A coil identifier would have to come from the coil lookup, so the card never renders a stand-in
        // coil either.
        Assert.AreEqual("deliver-coil", bringCoilOrder.ItemCode);
        Assert.IsFalse(bringCoilOrder.Fields.Any(item => string.Equals(item.Label, "Requested coil", StringComparison.Ordinal)));

        var statusMappings = new[]
        {
            ("Pending", "Waiting"),
            ("Accepted", "In Progress"),
            ("Completed", "Done"),
            ("Canceled", "Cancelled"),
        };
        foreach (var (status, expectedBadge) in statusMappings)
        {
            var statusRequest = new WaitlistRequest
            {
                Id = Guid.NewGuid(),
                Building = "Expo Drive",
                WorkCenter = "100-3",
                Category = "Deliver",
                Item = "deliver-coil",
                Status = status,
                TargetTimeUtc = DateTimeOffset.UtcNow.AddMinutes(5),
            };
            var order = WaitlistViewViewModel.CreateSessionOrder(statusRequest);
            Assert.AreEqual(expectedBadge, order.StatusBadgeText, $"Unexpected badge for status '{status}'.");
        }

        var pickupOther = new WaitlistRequest
        {
            Id = Guid.NewGuid(),
            Building = "Expo Drive",
            WorkCenter = "Press 12",
            Category = "Other",
            Item = "other",
            InputValue = "Need an outside service",
            Status = "Pending",
            TargetTimeUtc = DateTimeOffset.UtcNow.AddMinutes(12),
            IsOverdue = false,
        };

        var pickupOtherOrder = WaitlistViewViewModel.CreateSessionOrder(pickupOther);
        Assert.AreEqual("other", pickupOtherOrder.ItemCode);

        var scrapEmpty = new WaitlistRequest
        {
            Id = Guid.NewGuid(),
            Building = "Expo Drive",
            WorkCenter = "Press 12",
            Category = "Pickup",
            Item = "pickup-scrap",
            InputValue = "Scrap cart empty",
            Status = "Pending",
            TargetTimeUtc = DateTimeOffset.UtcNow.AddMinutes(6),
            IsOverdue = false,
        };

        var scrapOrder = WaitlistViewViewModel.CreateSessionOrder(scrapEmpty);
        Assert.AreEqual("pickup-scrap", scrapOrder.ItemCode);
    }

    /// <summary>
    /// Superseded by US2 (FR-006/FR-007): the card draws no per-Item field, so a session row is no longer
    /// padded into the five slots the deleted per-type templates bound. The honest check is the reverse — a
    /// row carries exactly the request's own carried values, and no invented filler.
    /// </summary>
    [TestMethod]
    public void WaitlistViewViewModel_SessionOrderFields_CarryNoPaddingSlots()
    {
        var forklift = new WaitlistRequest
        {
            Id = Guid.NewGuid(),
            Building = "Expo Drive",
            WorkCenter = "Press 12",
            Category = "Other",
            Item = "other",
            InputValue = "HELP ME!!!",
            Status = "Pending",
        };
        var flatstock = new WaitlistRequest
        {
            Id = Guid.NewGuid(),
            Building = "Expo Drive",
            WorkCenter = "Press 12",
            Category = "Deliver",
            Item = "deliver-flatstock",
            Status = "Pending",
        };
        var other = new WaitlistRequest
        {
            Id = Guid.NewGuid(),
            Building = "Expo Drive",
            WorkCenter = "Press 12",
            Category = "Other",
            Item = "other",
            InputValue = "Please assist",
            Status = "Pending",
        };
        var pickupOther = new WaitlistRequest
        {
            Id = Guid.NewGuid(),
            Building = "Expo Drive",
            WorkCenter = "Press 12",
            Category = "Other",
            Item = "other",
            Status = "Pending",
        };

        Assert.AreEqual(
            0,
            new[] { forklift, flatstock, other, pickupOther }
                .Select(WaitlistViewViewModel.CreateSessionOrder)
                .SelectMany(order => order.Fields)
                .Count(field => string.IsNullOrWhiteSpace(field.Label) && string.IsNullOrWhiteSpace(field.Value)),
            "A session row still carries empty padding slots, which only a per-type template's fixed slot count needed.");
    }

    [TestMethod]
    public void WaitlistViewViewModel_CreatesSessionOrder_ForAPickupFgRequest_CarriesNoUnsourcedMaterialAttribute()
    {
        var request = new WaitlistRequest
        {
            Id = Guid.NewGuid(),
            Building = "Expo Drive",
            WorkCenter = "Press 12",
            Category = "Pickup",
            Item = "pickup-fg",
            InputValue = "Finished goods request",
            Status = "Pending",
            TargetTimeUtc = DateTimeOffset.UtcNow.AddMinutes(8),
            IsOverdue = false,
        };

        var order = WaitlistViewViewModel.CreateSessionOrder(request);

        // The part, description, remaining quantity, customer and packlist all come from an item lookup
        // this surface does not perform, so none of those rows appears (FR-001/FR-002).
        string[] unsourcedLabels = ["Part number", "Part description", "Quantity remaining", "Customer", "Packlist"];

        foreach (var label in unsourcedLabels)
        {
            Assert.IsFalse(
                order.Fields.Any(item => string.Equals(item.Label, label, StringComparison.Ordinal)),
                $"A Pickup FG card still renders '{label}', which no source on the request produces.");
        }

        Assert.AreEqual("pickup-fg", order.ItemCode);
        Assert.AreEqual("Finished goods request", order.Fields.First(item => item.Label == "Request details").Value);
    }

    [TestMethod]
    public void WaitlistViewViewModel_CreatesSessionOrder_WithProperRemainingTimeAndOverdueState()
    {
        var request = new WaitlistRequest
        {
            Id = Guid.NewGuid(),
            Building = "Expo Drive",
            WorkCenter = "Press 12",
            Category = "Deliver",
            Item = "deliver-wrong-coil",
            InputValue = "Wrong material at press",
            Status = "Pending",
            TargetTimeUtc = DateTimeOffset.UtcNow.AddMinutes(8),
            IsOverdue = false,
        };

        var order = WaitlistViewViewModel.CreateSessionOrder(request);

        Assert.AreEqual("00:08", order.RemainingTimeText);
        Assert.IsFalse(order.IsOverdue);

        var overdueRequest = new WaitlistRequest
        {
            Id = Guid.NewGuid(),
            Building = "Expo Drive",
            WorkCenter = "Press 12",
            Category = "Other",
            Item = "other",
            InputValue = "Late submission",
            Status = "Accepted",
            TargetTimeUtc = DateTimeOffset.UtcNow.AddMinutes(-3),
            IsOverdue = true,
        };

        var overdueOrder = WaitlistViewViewModel.CreateSessionOrder(overdueRequest);

        Assert.AreEqual("Overdue", overdueOrder.RemainingTimeText);
        Assert.IsTrue(overdueOrder.IsOverdue);
    }

    [TestMethod]
    public void WaitlistViewViewModel_OverdueRequestsRemainActiveInTheActiveList()
    {
        var activeOverdue = new WaitlistRequest
        {
            Id = Guid.NewGuid(),
            Building = "Expo Drive",
            WorkCenter = "Press 12",
            Category = "Pickup",
            Item = "pickup-coil",
            InputValue = "Late demand",
            Status = "Accepted",
            TargetTimeUtc = DateTimeOffset.UtcNow.AddMinutes(-5),
            IsOverdue = true,
        };

        var order = WaitlistViewViewModel.CreateSessionOrder(activeOverdue);
        Assert.AreEqual("Overdue", order.RemainingTimeText);
        Assert.IsTrue(order.IsOverdue);
        Assert.AreEqual("Accepted", order.Status);
    }

    [TestMethod]
    public async Task TransitionStatusAsync_ChangesPendingToAcceptedCompletedAndCanceled()
    {
        var service = new WaitlistRequestService();
        var submitResult = await service.SubmitAsync(CreateDraft(), allowDuplicate: false);
        var request = submitResult.Request!;

        var accepted = await service.TransitionStatusAsync(request.Id, "Accepted");
        var acceptedRow = service.GetActiveRequests("Expo Drive").FirstOrDefault(item => item.Id == request.Id);

        Assert.IsTrue(accepted);
        Assert.IsNotNull(acceptedRow);
        Assert.AreEqual("Accepted", acceptedRow!.Status);

        var completed = await service.TransitionStatusAsync(request.Id, "Completed");
        var completedRow = service.GetActiveRequests("Expo Drive").FirstOrDefault(item => item.Id == request.Id);
        var canceled = await service.TransitionStatusAsync(request.Id, "Canceled", "No longer needed");

        Assert.IsTrue(completed);
        Assert.IsNull(completedRow);
        Assert.IsFalse(canceled);
    }

    [TestMethod]
    public async Task AcceptAsync_AssignsToHandlerAndMarksInProgress()
    {
        var service = new WaitlistRequestService();
        var submitResult = await service.SubmitAsync(CreateDraft(), allowDuplicate: false);
        var request = submitResult.Request!;

        var accepted = await service.AcceptAsync(request.Id, "9001", "Hana Handler");

        Assert.IsNotNull(accepted);
        Assert.AreEqual("Accepted", accepted!.Status);
        Assert.AreEqual("9001", accepted.AssignedMaterialHandler);
        Assert.IsNotNull(accepted.AcceptedUtc);
        Assert.IsNull(accepted.ReleasedUtc);
        // An accepted job stays on the shared (active) list.
        Assert.IsTrue(service.GetActiveRequests("Expo Drive").Any(item => item.Id == request.Id));
        Assert.IsTrue(service.GetAuditTrail(request.Id).Any(entry => entry.EventType == "Accepted"));
    }

    [TestMethod]
    public async Task AcceptAsync_NotAvailable_ReturnsNull()
    {
        var service = new WaitlistRequestService();
        var submitResult = await service.SubmitAsync(CreateDraft(), allowDuplicate: false);
        var request = submitResult.Request!;

        var accepted = await service.AcceptAsync(request.Id, "9001");
        var secondAccept = await service.AcceptAsync(request.Id, "9002");

        Assert.IsNotNull(accepted);
        Assert.IsNull(secondAccept, "An already-taken request cannot be accepted by another handler.");
    }

    [TestMethod]
    public async Task CompleteAsync_OnlyAssignedHandlerCanComplete()
    {
        var service = new WaitlistRequestService();
        var submitResult = await service.SubmitAsync(CreateDraft(), allowDuplicate: false);
        var request = submitResult.Request!;
        await service.AcceptAsync(request.Id, "9001");

        var wrongHandler = await service.CompleteAsync(request.Id, "9002");
        var completed = await service.CompleteAsync(request.Id, "9001");

        Assert.IsNull(wrongHandler, "Only the assigned handler may complete a request.");
        Assert.IsNotNull(completed);
        Assert.AreEqual("Completed", completed!.Status);
        Assert.IsNotNull(completed.CompletedUtc);
        Assert.IsFalse(service.GetActiveRequests("Expo Drive").Any(item => item.Id == request.Id), "A completed request leaves the active list.");
        Assert.IsTrue(service.GetAuditTrail(request.Id).Any(entry => entry.EventType == "Completed"));
    }

    [TestMethod]
    public async Task ReleaseAsync_ReturnsToOpenList_NotACancellation()
    {
        var service = new WaitlistRequestService();
        var submitResult = await service.SubmitAsync(CreateDraft(), allowDuplicate: false);
        var request = submitResult.Request!;
        await service.AcceptAsync(request.Id, "9001");

        var released = await service.ReleaseAsync(request.Id, "9001");

        Assert.IsNotNull(released);
        Assert.AreEqual("Pending", released!.Status, "Release returns the request to the open (Waiting) state.");
        Assert.IsNull(released.AssignedMaterialHandler, "Release clears the assignee so another handler can accept.");
        Assert.IsNotNull(released.ReleasedUtc);
        Assert.IsNull(released.CanceledUtc, "Release must NOT be treated as a cancellation.");
        Assert.IsNull(released.CancellationReason);
        Assert.IsFalse(service.GetAuditTrail(request.Id).Any(entry => entry.EventType == "Canceled"), "Release must not record a Canceled audit event.");
        Assert.IsTrue(service.GetAuditTrail(request.Id).Any(entry => entry.EventType == "Released"));

        // The claim the handler gave up is gone from the stored row, and the request is back on the very list
        // the screen reads — not merely marked available in memory (FR-007).
        Assert.IsTrue(
            service.GetActiveRequests("Expo Drive").Any(item => item.Id == request.Id),
            "A released request must be back on the open list the screen reads.");
        Assert.IsNull(service.GetRequest(request.Id)!.AssignedMaterialHandler, "The stored row must carry no assignee after a release.");

        // After release the request is available again to any handler.
        var reaccepted = await service.AcceptAsync(request.Id, "9002");
        Assert.IsNotNull(reaccepted);
        Assert.AreEqual("9002", reaccepted!.AssignedMaterialHandler);
    }

    [TestMethod]
    public async Task ReleaseAsync_NonAssignee_ReturnsNull()
    {
        var service = new WaitlistRequestService();
        var submitResult = await service.SubmitAsync(CreateDraft(), allowDuplicate: false);
        var request = submitResult.Request!;
        await service.AcceptAsync(request.Id, "9001");

        var wrongHandler = await service.ReleaseAsync(request.Id, "9002");

        Assert.IsNull(wrongHandler);
        Assert.AreEqual("Accepted", service.GetRequest(request.Id)!.Status);
    }

    [TestMethod]
    public void GetWaitingForText_FormatsWaitingAge()
    {
        var now = DateTimeOffset.UtcNow;
        Assert.AreEqual("Waiting < 1m", WaitlistViewViewModel.GetWaitingForText(now));
        Assert.IsTrue(WaitlistViewViewModel.GetWaitingForText(now.AddMinutes(-35)).StartsWith("Waiting 35m", StringComparison.Ordinal));
        Assert.IsTrue(WaitlistViewViewModel.GetWaitingForText(now.AddMinutes(-95)).Contains("1h", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task TransitionStatusAsync_SetsAcceptedAndCompletedTimestamps()
    {
        var service = new WaitlistRequestService();
        var submitResult = await service.SubmitAsync(CreateDraft(), allowDuplicate: false);
        var request = submitResult.Request!;

        var accepted = await service.TransitionStatusAsync(request.Id, "Accepted");
        var acceptedRow = service.GetRequest(request.Id);

        Assert.IsTrue(accepted);
        Assert.IsNotNull(acceptedRow);
        Assert.AreEqual("Accepted", acceptedRow!.Status);
        Assert.IsNotNull(acceptedRow.AcceptedUtc);
        Assert.IsNull(acceptedRow.CompletedUtc);

        var completed = await service.TransitionStatusAsync(request.Id, "Completed");
        var completedRow = service.GetRequest(request.Id);

        Assert.IsTrue(completed);
        Assert.IsNotNull(completedRow);
        Assert.AreEqual("Completed", completedRow!.Status);
        Assert.IsNotNull(completedRow.CompletedUtc);
        Assert.IsTrue(completedRow!.CompletedUtc!.Value >= acceptedRow!.AcceptedUtc!.Value);
    }

    [TestMethod]
    public async Task SubmitAsync_PersistsDraftNote()
    {
        var service = new WaitlistRequestService();
        var draft = new WaitlistRequestDraft
        {
            Building = "Expo Drive",
            WorkCenter = "Press 12",
            Category = "Deliver",
            Item = "deliver-coil",
            InputValue = "1",
            ActiveSetupJobId = "Press 12",
            WorkCenterName = "Press 12",
            RequesterEmployeeNumber = "6229",
            RequesterEmployeeName = "John Koll",
            Note = "Handler follow-up note",
        };

        var result = await service.SubmitAsync(draft, allowDuplicate: false);

        Assert.AreEqual(WaitlistRequestSubmitStatus.Success, result.Status, result.Message);
        Assert.IsNotNull(result.Request);
        Assert.AreEqual("Handler follow-up note", result.Request!.Note);
    }

    [TestMethod]
    public async Task TransitionStatusAsync_WhenCanceled_RecordsCancellationMetadata()
    {
        var service = new WaitlistRequestService();
        var submitResult = await service.SubmitAsync(CreateDraft(), allowDuplicate: false);
        var request = submitResult.Request!;

        var accepted = await service.TransitionStatusAsync(request.Id, "Accepted");
        var canceled = await service.TransitionStatusAsync(request.Id, "Canceled", "No longer needed", "6229");

        Assert.IsTrue(accepted);
        Assert.IsTrue(canceled);
        Assert.AreEqual(0, service.GetActiveRequests("Expo Drive").Count(item => item.Id == request.Id));

        var updated = service.GetRequest(request.Id);
        Assert.IsNotNull(updated);
        Assert.AreEqual("Canceled", updated!.Status);
        Assert.AreEqual("No longer needed", updated.CancellationReason);
        Assert.IsNotNull(updated.CanceledUtc);
        Assert.AreEqual("6229", updated.CanceledByEmployeeNumber);
    }

    [TestMethod]
    public async Task TransitionStatusAsync_WhenCanceled_RecordsMaterialHandlerNotificationPayload()
    {
        var service = new WaitlistRequestService();
        var submitResult = await service.SubmitAsync(CreateDraft(), allowDuplicate: false);
        var request = submitResult.Request!;

        await service.TransitionStatusAsync(request.Id, "Accepted");
        var canceled = await service.TransitionStatusAsync(request.Id, "Canceled", "No longer needed", "6229");

        Assert.IsTrue(canceled);
        var updated = service.GetRequest(request.Id);
        Assert.IsNotNull(updated);
        Assert.AreEqual("Canceled", updated!.Status);
        Assert.AreEqual("No longer needed", updated.CancellationReason);
        Assert.AreEqual("6229", updated.CanceledByEmployeeNumber);
        Assert.IsNotNull(updated.CanceledUtc);
    }

    [TestMethod]
    public async Task RequestsChanged_IsRaised_WhenRequestIsSubmittedOrStatusTransitions()
    {
        var service = new WaitlistRequestService();
        var changeCount = 0;
        service.RequestsChanged += (_, _) => changeCount++;

        var submitResult = await service.SubmitAsync(CreateDraft(), allowDuplicate: false);
        var request = submitResult.Request!;

        Assert.AreEqual(1, changeCount);

        _ = await service.TransitionStatusAsync(request.Id, "Accepted");
        Assert.AreEqual(2, changeCount);
    }

    [TestMethod]
    public async Task GetAuditTrail_RecordsRequestCreationAndLifecycleTransitions()
    {
        var service = new WaitlistRequestService();
        var submitResult = await service.SubmitAsync(CreateDraft(), allowDuplicate: false);
        var request = submitResult.Request!;

        await service.TransitionStatusAsync(request.Id, "Accepted");
        await service.TransitionStatusAsync(request.Id, "Canceled", "No longer needed", "6229");

        var auditTrail = service.GetAuditTrail(request.Id);
        Assert.AreEqual(3, auditTrail.Count);
        Assert.IsTrue(auditTrail.Any(item => item.EventType == "Created"));
        Assert.IsTrue(auditTrail.Any(item => item.EventType == "Accepted"));
        Assert.IsTrue(auditTrail.Any(item => item.EventType == "Canceled"));
    }

    [TestMethod]
    public async Task WaitlistViewViewModel_ReflectsTheSelectedBuilding_WhenBuildingChanges()
    {
        var buildingSelectionService = new StubBuildingSelectionService("Expo Drive");
        var requestService = new WaitlistRequestService();
        var viewModel = new WaitlistViewViewModel(new NoOpNavigationService(), buildingSelectionService, requestService);

        await requestService.SubmitAsync(CreateDraft(), allowDuplicate: true);
        var expoTask = InvokeLoad(viewModel, "Expo Drive");
        buildingSelectionService.SelectedBuilding = "VITS";
        var vitsTask = InvokeLoad(viewModel, "VITS");

        await Task.WhenAll(expoTask, vitsTask);

        // The Expo Drive request must not leak into the VITS list: a stale load result is discarded.
        Assert.AreEqual(0, viewModel.Source.Count);
    }

    [TestMethod]
    public async Task WaitlistViewViewModel_RefreshesActiveList_WhenRequestIsSubmitted()
    {
        var buildingSelectionService = new StubBuildingSelectionService("Expo Drive");
        var requestService = new WaitlistRequestService();
        var viewModel = new WaitlistViewViewModel(new NoOpNavigationService(), buildingSelectionService, requestService);

        // The view model only refreshes on RequestsChanged once it subscribes in
        // OnNavigatedTo; without this it never picks up the submitted request.
        viewModel.OnNavigatedTo(null!);
        await viewModel.RefreshAsync();
        var submitResult = await requestService.SubmitAsync(CreateDraft(), allowDuplicate: false);
        await Task.Delay(50);

        Assert.AreEqual(WaitlistRequestSubmitStatus.Success, submitResult.Status);
        Assert.IsTrue(viewModel.Source.Any(item => item.Title.Contains("Wrong Coil", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void WaitlistViewViewModel_FilterToMyRequests_NarrowsToSignedInUserRows()
    {
        var mine = WaitlistViewViewModel.CreateSessionOrder(new WaitlistRequest
        {
            Id = Guid.NewGuid(),
            Building = "Expo Drive",
            WorkCenter = "Press 12",
            Category = "Deliver",
            Item = "deliver-coil",
            Status = "Pending",
            RequesterEmployeeNumber = "6229",
            RequesterEmployeeName = "John Koll",
        });
        var other = WaitlistViewViewModel.CreateSessionOrder(new WaitlistRequest
        {
            Id = Guid.NewGuid(),
            Building = "Expo Drive",
            WorkCenter = "Press 15",
            Category = "Deliver",
            Item = "deliver-coil",
            Status = "Pending",
            RequesterEmployeeNumber = "5000",
            RequesterEmployeeName = "Other User",
        });
        var staticRow = new SampleOrder { Id = 1, Title = "Static", RequestedByName = "Current user" };

        var filtered = WaitlistViewViewModel.FilterToMyRequests(new[] { mine, other, staticRow }, "6229");

        Assert.AreEqual(1, filtered.Count);
        Assert.AreEqual("6229", filtered[0].RequesterEmployeeNumber);
    }

    [TestMethod]
    public async Task WaitlistViewViewModel_ShowMyRequestsOnly_NarrowsLoadedSourceToCurrentUser()
    {
        var buildingSelectionService = new StubBuildingSelectionService("Expo Drive");
        var requestService = new WaitlistRequestService();
        var startupState = new MTM_Waitlist.Module_Core.Models.StartupState { EmployeeNumber = "6229" };
        var viewModel = new WaitlistViewViewModel(new NoOpNavigationService(), buildingSelectionService, requestService, startupState: startupState);

        viewModel.OnNavigatedTo(null!);
        await viewModel.RefreshAsync();
        var myDraft = CreateDraft();
        var submitMine = await requestService.SubmitAsync(myDraft, allowDuplicate: false);
        Assert.AreEqual(WaitlistRequestSubmitStatus.Success, submitMine.Status);
        var submitOther = await requestService.SubmitAsync(new WaitlistRequestDraft
        {
            Building = "Expo Drive",
            WorkCenter = "Press 15",
            Category = "Deliver",
            Item = "deliver-coil",
            ActiveSetupJobId = "JOB-3003",
            WorkCenterName = "Press 15",
            RequesterEmployeeNumber = "5000",
            RequesterEmployeeName = "Other User",
        }, allowDuplicate: false);
        Assert.AreEqual(WaitlistRequestSubmitStatus.Success, submitOther.Status);
        await Task.Delay(50);

        viewModel.ShowMyRequestsOnly = true;
        await Task.Delay(50);

        Assert.IsTrue(viewModel.Source.Count > 0);
        Assert.IsTrue(viewModel.Source.All(order => order.RequesterEmployeeNumber == "6229"));
    }

    [TestMethod]
    public async Task Reset_ClearsSessionRequestsAsync()
    {
        var service = new WaitlistRequestService();

        await service.SubmitAsync(CreateDraft(), allowDuplicate: false);
        service.Reset();

        Assert.AreEqual(0, service.GetActiveRequests().Count);
    }

    [TestMethod]
    public async Task CancelOwnRequestAsync_CreatorCancelsOwnWaitingRequest_SucceedsAndRecordsCancelled()
    {
        var service = new WaitlistRequestService();
        var submit = await service.SubmitAsync(CreateDraft(), allowDuplicate: false);
        Assert.AreEqual(WaitlistRequestSubmitStatus.Success, submit.Status);

        var result = await service.CancelOwnRequestAsync(submit.Request!.Id, "6229", "No longer needed");

        Assert.AreEqual(WaitlistRequestCancelStatus.Success, result.Status);
        Assert.IsNotNull(result.Request);
        Assert.AreEqual("Canceled", result.Request.Status);
        Assert.AreEqual("6229", result.Request.CanceledByEmployeeNumber);
        Assert.IsNotNull(result.Request.CanceledUtc);
        Assert.AreEqual("No longer needed", result.Request.CancellationReason);

        // The cancelled request is recorded, not merely removed from memory.
        Assert.IsNotNull(service.GetRequest(submit.Request.Id));
        Assert.IsFalse(service.GetActiveRequests("Expo Drive").Any(item => item.Id == submit.Request.Id));
    }

    [TestMethod]
    public async Task CancelOwnRequestAsync_NonCreatorIsDenied()
    {
        var service = new WaitlistRequestService();
        var submit = await service.SubmitAsync(CreateDraft(), allowDuplicate: false);

        var result = await service.CancelOwnRequestAsync(submit.Request!.Id, "5000");

        Assert.AreEqual(WaitlistRequestCancelStatus.NotOwnedByRequester, result.Status);
        Assert.AreEqual("Pending", service.GetRequest(submit.Request.Id)!.Status);
    }

    [TestMethod]
    public async Task CancelOwnRequestAsync_AfterAcceptIsDenied()
    {
        var service = new WaitlistRequestService();
        var submit = await service.SubmitAsync(CreateDraft(), allowDuplicate: false);
        await service.TransitionStatusAsync(submit.Request!.Id, "Accepted");

        var result = await service.CancelOwnRequestAsync(submit.Request!.Id, "6229");

        Assert.AreEqual(WaitlistRequestCancelStatus.NotInCancelableState, result.Status);
        Assert.AreEqual("Accepted", service.GetRequest(submit.Request.Id)!.Status);
    }

    [TestMethod]
    public async Task CancelOwnRequestAsync_UnknownRequestReturnsNotFound()
    {
        var service = new WaitlistRequestService();

        var result = await service.CancelOwnRequestAsync(Guid.NewGuid(), "6229");

        Assert.AreEqual(WaitlistRequestCancelStatus.NotFound, result.Status);
    }

    [TestMethod]
    public async Task CancelOwnRequestAsync_PersistsCancelledRowToDb()
    {
        var helper = new StubMySqlHelperServer(Array.Empty<Dictionary<string, object?>>());
        var service = new WaitlistRequestService(helper);

        var submit = await service.SubmitAsync(CreateDraft(), allowDuplicate: false);
        Assert.AreEqual(WaitlistRequestSubmitStatus.Success, submit.Status);
        helper.NonQueryProcedures.Clear();

        var result = await service.CancelOwnRequestAsync(submit.Request!.Id, "6229", "Changed my mind");

        Assert.AreEqual(WaitlistRequestCancelStatus.Success, result.Status);
        Assert.IsTrue(helper.NonQueryProcedures.Contains("sp_waitlist_request_status_update"));
        Assert.IsTrue(helper.NonQueryProcedures.Contains("sp_waitlist_request_audit_insert"));
        Assert.AreEqual("Canceled", service.GetRequest(submit.Request.Id)!.Status);
    }

    [TestMethod]
    public async Task MyRequestsFiltersToSignedInUserAndCancelOwnBlockedForNonCreator()
    {
        var service = new WaitlistRequestService();

        var mine = await service.SubmitAsync(CreateDraft(), allowDuplicate: false);
        Assert.AreEqual(WaitlistRequestSubmitStatus.Success, mine.Status);
        var other = await service.SubmitAsync(new WaitlistRequestDraft
        {
            Building = "Expo Drive",
            WorkCenter = "Press 15",
            Category = "Deliver",
            Item = "deliver-coil",
            ActiveSetupJobId = "JOB-3003",
            WorkCenterName = "Press 15",
            RequesterEmployeeNumber = "5000",
            RequesterEmployeeName = "Other User",
        }, allowDuplicate: false);
        Assert.AreEqual(WaitlistRequestSubmitStatus.Success, other.Status);

        // My Requests narrows to the signed-in user only.
        var mineOnly = service.GetMyRequests("6229", "Expo Drive");
        Assert.AreEqual(1, mineOnly.Count);
        Assert.AreEqual("6229", mineOnly[0].RequesterEmployeeNumber);

        // A non-creator cannot cancel the signed-in user's Waiting request.
        var denied = await service.CancelOwnRequestAsync(mine.Request!.Id, "5000");
        Assert.AreEqual(WaitlistRequestCancelStatus.NotOwnedByRequester, denied.Status);
        Assert.AreEqual("Pending", service.GetRequest(mine.Request.Id)!.Status);

        // The creator can cancel their own Waiting request.
        var success = await service.CancelOwnRequestAsync(mine.Request.Id, "6229", "No longer needed");
        Assert.AreEqual(WaitlistRequestCancelStatus.Success, success.Status);
        Assert.AreEqual("Canceled", service.GetRequest(mine.Request.Id)!.Status);
    }

    private static async Task InvokeLoad(WaitlistViewViewModel viewModel, string building)
    {
        await viewModel.RefreshAsync();
        await Task.Delay(10);
    }

    [TestMethod]
    public async Task SubmitAsync_InvokesNewRequestAlertNotifier()
    {
        var notifier = new FakeNewRequestAlertNotifier();
        var service = new WaitlistRequestService(null, notifier);

        var submit = await service.SubmitAsync(CreateDraft(), allowDuplicate: false);

        Assert.AreEqual(WaitlistRequestSubmitStatus.Success, submit.Status);
        Assert.IsNotNull(submit.Request);
        Assert.AreEqual(1, notifier.Notified.Count);
        Assert.AreEqual(submit.Request!.Id, notifier.Notified[0]);
    }

    [TestMethod]
    public async Task SubmitAsync_ValidationFailure_DoesNotInvokeNotifier()
    {
        var notifier = new FakeNewRequestAlertNotifier();
        var service = new WaitlistRequestService(null, notifier);

        var submit = await service.SubmitAsync(new WaitlistRequestDraft(), allowDuplicate: false);

        Assert.AreEqual(WaitlistRequestSubmitStatus.ValidationFailure, submit.Status);
        Assert.AreEqual(0, notifier.Notified.Count);
    }

    private sealed class FakeNewRequestAlertNotifier : INewRequestAlertNotifier
    {
        public List<Guid> Notified { get; } = new();

        public Task<bool> NotifyNewRequestAsync(Guid requestId, string title, string body, bool isPackaged, CancellationToken cancellationToken = default)
        {
            Notified.Add(requestId);
            return Task.FromResult(true);
        }
    }

    private static WaitlistRequestDraft DeadlineLessDraft() => new()
    {
        Building = "Expo Drive",
        WorkCenter = "Press 12",
        Category = "Deliver",
        Item = "deliver-wrong-coil",
        InputValue = "Wrong material at press",
        ActiveSetupJobId = "JOB-1001",
        WorkCenterName = "Press 12",
        RequesterEmployeeNumber = "6229",
        RequesterEmployeeName = "John Koll",
    };

    private static WaitlistRequestDraft CreateDraft() => new()
    {
        Building = "Expo Drive",
        WorkCenter = "Press 12",
        Category = "Deliver",
        Item = "deliver-wrong-coil",
        InputValue = "Wrong material at press",
        ActiveSetupJobId = "JOB-1001",
        WorkCenterName = "Press 12",
        RequesterEmployeeNumber = "6229",
        RequesterEmployeeName = "John Koll",
        TargetTimeUtc = DateTimeOffset.UtcNow.AddMinutes(15),
        IsOverdue = false,
        AssignedMaterialHandler = "M. Lewis",
    };

    private sealed class NoOpNavigationService : INavigationService
    {
        public event Microsoft.UI.Xaml.Navigation.NavigatedEventHandler? Navigated
        {
            add { }
            remove { }
        }

        public bool CanGoBack => false;

        public Microsoft.UI.Xaml.Controls.Frame? Frame { get; set; }

        public bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false) => true;

        public bool GoBack() => true;

        public void SetListDataItemForNextConnectedAnimation(object item)
        {
        }
    }

    private sealed class StubBuildingSelectionService : IBuildingSelectionService
    {
        public StubBuildingSelectionService(string selectedBuilding)
        {
            SelectedBuilding = selectedBuilding;
        }

        public event EventHandler? BuildingChanged
        {
            add { }
            remove { }
        }

        public IReadOnlyList<string> Buildings => new[] { "Expo Drive", "VITS" };

        public string SelectedBuilding { get; set; }
    }

    private sealed class InMemoryLocalSettingsService : ILocalSettingsService
    {
        private readonly Dictionary<string, object> _settings;

        public InMemoryLocalSettingsService(Dictionary<string, object> settings)
        {
            _settings = settings;
        }

        public Task<T?> ReadSettingAsync<T>(string key)
        {
            if (_settings.TryGetValue(key, out var value))
            {
                return Task.FromResult((T?)value);
            }

            return Task.FromResult(default(T));
        }

        public Task SaveSettingAsync<T>(string key, T value)
        {
            _settings[key] = value!;
            return Task.CompletedTask;
        }

        public Task ResetSettingAsync(string key, CancellationToken cancellationToken = default)
        {
            _settings.Remove(key);
            return Task.CompletedTask;
        }

        public Task ResetAsync()
        {
            _settings.Clear();
            return Task.CompletedTask;
        }

        public Task CorruptForTestAsync() => Task.CompletedTask;
    }
}
