using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.GenericRequest.Models;
using MTM_Waitlist.GenericRequest.Services;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Shared.Models;
using MTM_Waitlist.Module_Shared.Services;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;
using MTM_Waitlist.Tests.Module_GenericRequest;

namespace MTM_Waitlist.Tests.Module_Waitlist;

/// <summary>
/// A request filled from a job is still a generic request: it is raised generic, keeps its own identity, and carries
/// the job as provenance that both request reads return (021 FR-011, FR-013).
/// </summary>
/// <remarks>
/// The proof runs through the two surfaces a reader meets: the write path the queue is actually given, and the read
/// that fills the detail page. Nothing here inspects a private field, and nothing asserts that the request looks like
/// a work-order request, because the point of the rule is that it never does.
/// </remarks>
[TestClass]
public sealed class WaitlistRequestJobFillProvenanceTests
{
    [TestMethod]
    public async Task Service_WaitlistRequestJobFillProvenanceTests_SubmitAsync_WritesProvenanceAndKeepsTheRequestGeneric()
    {
        var helper = new RecordingHelperServer();
        var service = new WaitlistRequestService(helper);

        var result = await service.SubmitAsync(FilledDraft());

        Assert.AreEqual(WaitlistRequestSubmitStatus.Success, result.Status, result.Message);

        var insert = helper.Calls.Single(call => call.Procedure == "sp_waitlist_request_insert");

        Assert.AreEqual("WO-9001", insert.Parameters["p_provenance_work_order"], "the job is stored as provenance");
        Assert.AreEqual(1, insert.Parameters["p_is_generic"], "a filled request is still generic (FR-011)");
        Assert.AreEqual(
            string.Empty,
            insert.Parameters["p_active_setup_job_id"],
            "the job is never written into the identity column (FR-011)");
        Assert.AreEqual(
            "WO-9001",
            result.Request!.ProvenanceWorkOrder,
            "the request the service keeps carries the job it was filled from (FR-013)");
    }

    [TestMethod]
    public void Service_WaitlistRequestJobFillProvenanceTests_MapRowToRequest_ReturnsTheStoredProvenance()
    {
        var request = WaitlistRequestService.MapRowToRequest(new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["public_id"] = "f0000000-00aa-4000-8000-000000000021",
            ["building"] = "Expo Drive",
            ["work_center"] = "WC-1",
            ["category"] = "Pickup",
            ["item"] = "pickup-part",
            ["is_generic"] = 1,
            ["provenance_work_order"] = "WO-9001",
            ["work_center_name"] = "PC-1",
            ["requester_employee_number"] = "6229",
            ["requester_employee_name"] = "John Koll",
            ["status"] = "Pending",
        });

        Assert.IsNotNull(request);
        Assert.AreEqual("WO-9001", request!.ProvenanceWorkOrder, "both request reads answer the same row shape (FR-013)");
        Assert.IsTrue(request.IsGeneric, "provenance never turns the request into a work-order request (FR-011)");
        Assert.AreEqual(string.Empty, request.ActiveSetupJobId);
    }

    [TestMethod]
    public void Service_WaitlistRequestJobFillProvenanceTests_MapRowToRequest_ARequestNoJobFilledCarriesNoProvenance()
    {
        var request = WaitlistRequestService.MapRowToRequest(new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["public_id"] = "f0000000-00aa-4000-8000-000000000022",
            ["building"] = "Expo Drive",
            ["work_center"] = "WC-1",
            ["category"] = "Pickup",
            ["item"] = "pickup-part",
            ["is_generic"] = 1,
            ["work_center_name"] = "PC-1",
            ["requester_employee_number"] = "6229",
            ["requester_employee_name"] = "John Koll",
            ["status"] = "Pending",
        });

        Assert.IsNull(request!.ProvenanceWorkOrder, "a request no job filled says nothing about a job");
    }

    [TestMethod]
    public async Task Service_WaitlistRequestJobFillProvenanceTests_SubmitAsync_TheGenericRaiseCarriesTheJobAsProvenanceOnly()
    {
        var requests = new RecordingWaitlistRequestService();
        var data = new GenericRequestLiveStepData(
            new StubWorkCenterCatalogService(),
            new StubNewRequestPickerService(),
            new StubRequestItemConfigurationService(),
            requests,
            new StubPersonIdentity(),
            new StubEmployeeDirectoryService
            {
                Found = new EmployeeIdentity { EmployeeNumber = "1234", DisplayName = "J. Koll", IsActive = true },
            },
            new StubGenericRequestPartData(),
            new StubGenericRequestDieData(),
            new FakeDunnageCatalogueReader(),
            new FakeLuggerArrangementService());

        var draft = FilledGenericDraft();
        var result = await data.SubmitAsync(draft);

        Assert.IsTrue(result.IsRaised, result.FailureMessage);
        Assert.AreEqual(1, requests.Submitted.Count);

        var recorded = requests.Submitted[0];

        Assert.AreEqual("WO-9001", recorded.ProvenanceWorkOrder, "the raise records the job it was filled from (FR-013)");
        Assert.IsTrue(recorded.IsGeneric, "the fill never makes the request a work-order request (FR-011)");
        Assert.AreEqual(string.Empty, recorded.ActiveSetupJobId, "the job is never the request's identity (FR-011)");
    }

    private static WaitlistRequestDraft FilledDraft() => new()
    {
        Building = "Expo Drive",
        WorkCenter = "WC-1",
        Category = "Pickup",
        Item = "pickup-part",
        IsGeneric = true,
        ProvenanceWorkOrder = "WO-9001",
        WorkCenterName = "PC-1",
        RequesterEmployeeNumber = "6229",
        RequesterEmployeeName = "John Koll",
        RequestedUtc = DateTimeOffset.UtcNow,
    };

    private static GenericRequestDraft FilledGenericDraft()
    {
        var draft = new GenericRequestDraft
        {
            WorkCenter = "WC-1",
            Building = "Expo Drive",
            Category = "Pickup",
            ItemCode = "pickup-part",
            ItemName = "Pickup part",
            JobFill = GenericRequestJobFill.FromJob("WO-9001", "PART-1", "20"),
            JobFillAnswered = true,
        };

        return draft;
    }

    /// <summary>The helper server, recording what each stored-procedure call was carrying.</summary>
    private sealed class RecordingHelperServer : IMySqlHelperServer
    {
        public List<(string Procedure, IReadOnlyDictionary<string, object?> Parameters)> Calls { get; } = new();

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default) =>
            Task.FromResult((IReadOnlyList<Dictionary<string, object?>>)Array.Empty<Dictionary<string, object?>>());

        public Task<int> ExecuteStoredProcedureNonQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
        {
            Calls.Add((storedProcedureName, parameters));
            return Task.FromResult(1);
        }

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteSqlQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default) =>
            Task.FromResult((IReadOnlyList<Dictionary<string, object?>>)Array.Empty<Dictionary<string, object?>>());

        public Task<int> ExecuteSqlNonQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<int> ExecuteSqlNonQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<object?> ExecuteScalarAsync(
            string sql,
            IReadOnlyDictionary<string, object?>? parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<object?>(null);
    }
}
