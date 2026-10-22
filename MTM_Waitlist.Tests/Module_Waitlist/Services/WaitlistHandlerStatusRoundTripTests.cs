using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;
using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Tests.Module_Waitlist.Services;

/// <summary>
/// US4's checkpoint driven end to end against a live store: a condition is chosen on a real row, stored, replaced,
/// read back with the list and restored onto the row a restart builds — which is what SC-016 asks for
/// (FR-072, FR-073).
/// </summary>
/// <remarks>
/// <para>
/// Opt-in and environment-gated. It needs <c>MTM_WAITLIST_TEST_DB_CONNECTION_STRING</c> pointing at a MySQL
/// instance carrying the waitlist schema and its stored procedures, and reports inconclusive without one, so the
/// suite stays green offline. A dedicated variable is used so the process-wide connection-string resolution the
/// rest of the suite relies on is not changed.
/// </para>
/// <para>
/// Everything the test creates is its own: it raises one request, claims it as its handler, and deletes both the
/// row and its audit trail afterwards.
/// </para>
/// </remarks>
[TestClass]
public sealed class WaitlistHandlerStatusRoundTripTests
{
    private const string ConnectionStringVariable = "MTM_WAITLIST_TEST_DB_CONNECTION_STRING";
    private const string Building = "Expo Drive";
    private const string HandlerEmployeeNumber = "IT-6331";
    private const string HandlerEmployeeName = "IT Handler";
    private const string RequesterEmployeeNumber = "IT-6229";

    private MySqlHelperServer _helper = null!;
    private WaitlistRequestService _service = null!;
    private Guid _requestId;

    [TestInitialize]
    public void TestInitialize()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Assert.Inconclusive($"{ConnectionStringVariable} is not set; skipping the live handler-status round trip.");
        }

        _helper = new MySqlHelperServer(
            Options.Create(new WaitlistDatabaseOptions { ConnectionString = connectionString! }));
        _service = new WaitlistRequestService(_helper);
    }

    [TestCleanup]
    public async Task TestCleanupAsync()
    {
        if (_helper is null || _requestId == Guid.Empty)
        {
            return;
        }

        // The audit trail names the row, so it goes first; nothing else in the store references this test's row.
        await _helper.ExecuteSqlNonQueryAsync(
            "DELETE FROM waitlist_requests_audit WHERE request_public_id = @p_public_id;",
            new Dictionary<string, object?> { ["p_public_id"] = _requestId.ToString() },
            MySqlDatabaseTarget.MtmWaitlist);
        await _helper.ExecuteSqlNonQueryAsync(
            "DELETE FROM waitlist_requests_queue WHERE public_id = @p_public_id;",
            new Dictionary<string, object?> { ["p_public_id"] = _requestId.ToString() },
            MySqlDatabaseTarget.MtmWaitlist);
    }

    [TestMethod]
    public async Task Database_WaitlistHandlerStatusRoundTripTests_Condition_FollowsTheChoiceThroughTheStoreAndBackOntoTheRow()
    {
        var submitted = await _service.SubmitAsync(
            new WaitlistRequestDraft
            {
                Building = Building,
                WorkCenter = "Press 12",
                Category = "Deliver",
                Item = "deliver-wrong-coil",
                InputValue = "Wrong material at press",
                ActiveSetupJobId = "IT-ROUNDTRIP",
                WorkCenterName = "Press 12",
                RequesterEmployeeNumber = RequesterEmployeeNumber,
                RequesterEmployeeName = "IT Requester",
                TargetTimeUtc = DateTimeOffset.UtcNow.AddMinutes(15),
            });

        Assert.AreEqual(
            WaitlistRequestSubmitStatus.Success,
            submitted.Status,
            "The round trip needs one real request to write a condition against.");
        _requestId = submitted.Request!.Id;

        var claimed = await _service.AcceptAsync(_requestId, HandlerEmployeeNumber, HandlerEmployeeName);
        Assert.IsNotNull(
            claimed,
            "The round trip needs the request held by the handler whose condition it sets (FR-070, FR-076).");

        var reported = await _service.SetHandlerStatusAsync(_requestId, "Product Buried", HandlerEmployeeNumber);
        Assert.AreEqual(1, reported, "The chosen condition must reach the store as an affected row (FR-072).");

        var replaced = await _service.SetHandlerStatusAsync(_requestId, "Changing Propane", HandlerEmployeeNumber);
        Assert.AreEqual(1, replaced, "A second choice replaces the first on the same row (data-model.md §2).");

        // The restart, as far as one process can stage it: nothing this session holds is kept, and the list is read
        // again from the store. What comes back is what a fresh start of the application would read.
        _service.Reset();
        await _service.RefreshFromDatabaseAsync(Building);

        var stored = _service.GetRequest(_requestId);
        Assert.IsNotNull(stored, "The request must come back on the next list read (FR-073).");
        Assert.AreEqual(
            "Changing Propane",
            stored!.HandlerStatus,
            "The stored condition must be read back with the request, so a restart shows the same card (FR-073).");

        var row = WaitlistViewViewModel.CreateSessionOrder(stored);
        Assert.AreEqual(
            "Changing Propane",
            row.HandlerStatus,
            "The condition must be restored onto that row when the list is next read (FR-073, SC-016).");
        Assert.AreEqual("Changing Propane", row.StatusWord, "The restored condition is what row 1 reads (FR-012).");
        Assert.AreEqual(
            StatusColumnTheme.HandlerCondition,
            row.StatusTheme,
            "The restored condition re-themes the whole status column as the lighter handler red (FR-072).");
    }

    [TestMethod]
    public async Task Database_WaitlistHandlerStatusRoundTripTests_Release_ClearsTheConditionInTheStoreSoTheCardReadsWaiting()
    {
        // The store, not the session's own row, is where this has to be true. A store that kept the condition would
        // hand it straight back on the next list read, and the card would then show the previous handler's condition
        // on a job nobody holds. The clear happens inside one UPDATE, so this is the only place the deployed
        // procedure's own behaviour can be observed at all.
        var submitted = await _service.SubmitAsync(
            new WaitlistRequestDraft
            {
                Building = Building,
                WorkCenter = "Press 12",
                Category = "Deliver",
                Item = "deliver-wrong-coil",
                InputValue = "Wrong material at press",
                ActiveSetupJobId = "IT-RELEASE",
                WorkCenterName = "Press 12",
                RequesterEmployeeNumber = RequesterEmployeeNumber,
                RequesterEmployeeName = "IT Requester",
                TargetTimeUtc = DateTimeOffset.UtcNow.AddMinutes(15),
            });

        Assert.AreEqual(
            WaitlistRequestSubmitStatus.Success,
            submitted.Status,
            "The round trip needs one real request to report a condition against.");
        _requestId = submitted.Request!.Id;

        Assert.IsNotNull(
            await _service.AcceptAsync(_requestId, HandlerEmployeeNumber, HandlerEmployeeName),
            "The handler has to hold the job before a condition can be reported on it (FR-076).");
        Assert.AreEqual(
            1,
            await _service.SetHandlerStatusAsync(_requestId, "Product Buried", HandlerEmployeeNumber),
            "The condition has to be in the store before giving the job back can be shown to remove it.");

        var released = await _service.ReleaseAsync(_requestId, HandlerEmployeeNumber, HandlerEmployeeName);
        Assert.IsNotNull(released, "The assignee must be able to give the job back.");
        Assert.IsNull(
            released!.HandlerStatus,
            "The row this session holds carries no condition once the job is back in the pool (FR-106).");

        // The restart, as far as one process can stage it: nothing this session holds is kept, and the list is read
        // again from the store.
        _service.Reset();
        await _service.RefreshFromDatabaseAsync(Building);

        var stored = _service.GetRequest(_requestId);
        Assert.IsNotNull(stored, "The released request must come back on the next list read.");
        Assert.AreEqual("Pending", stored!.Status, "A release returns the job to the open list rather than canceling it.");
        Assert.IsNull(stored.AssignedMaterialHandler, "And to nobody.");
        Assert.IsNull(
            stored.HandlerStatus,
            "The store must clear the condition with the assignee instead of leaving it on a job nobody holds (FR-106).");

        var row = WaitlistViewViewModel.CreateSessionOrder(stored);
        Assert.AreEqual("Waiting", row.StatusWord, "So the card reads Waiting again (FR-012).");
        Assert.AreEqual(
            StatusColumnTheme.Normal,
            row.StatusTheme,
            "And the status column is back to its normal theme rather than themed for a condition nobody still holds.");
    }
}
