using System.Text.Json;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;

namespace MTM_Waitlist.Tests.Module_Waitlist.Services;

/// <summary>
/// Coil availability is read from the saved Work Center Setup record (FR-019, T099): the coil on the job is
/// taken from the active job's subordinate parts, and nothing is assumed when the work center has no saved
/// job. No sample catalog and no mock toggle is consulted (FR-014).
/// </summary>
[TestClass]
public sealed class CoilAvailabilityServiceTests
{
    private const string WorkCenter = "100-3";

    [TestMethod]
    public async Task GetCoilForJobAsync_ReadsTheSavedActiveJob_AndReportsTheRealCoil()
    {
        var helper = new StubMySqlHelperServer(
            ActiveJobRow("MMC0001000", "COIL 0.5 X 12.0", 4_200m));
        var weights = new StubAverageCoilWeightService("5,000 lb");
        var service = new CoilAvailabilityService(helper, weights);

        var coil = await service.GetCoilForJobAsync(WorkCenter);

        Assert.IsTrue(coil.HasCoil);
        Assert.AreEqual("MMC0001000", coil.CoilNumber);
        Assert.AreEqual("4200", coil.QuantityOnHand);
        Assert.AreEqual("COIL 0.5 X 12.0", coil.Description);
        Assert.AreEqual("5,000 lb", coil.AverageWeight);
        Assert.AreEqual("sp_setup_active_jobs_latest_by_work_center_get", helper.LastStoredProcedureName);
        Assert.AreEqual(MySqlDatabaseTarget.MtmWaitlist, helper.LastDatabaseTarget);
        Assert.AreEqual(0, helper.SqlCallCount, "The active job must be read through its procedure (FR-015).");
        Assert.AreEqual("MMC0001000", weights.LastPartNumber, "The average skid weight is asked for the resolved coil.");
    }

    [TestMethod]
    public async Task GetCoilForJobAsync_WhenNoActiveJobIsSaved_ReportsNoCoil()
    {
        var helper = new StubMySqlHelperServer();
        var service = new CoilAvailabilityService(helper, new StubAverageCoilWeightService("5,000 lb"));

        var coil = await service.GetCoilForJobAsync(WorkCenter);

        Assert.IsFalse(coil.HasCoil, "A work center with no saved job has no coil to report.");
        Assert.AreEqual(string.Empty, coil.CoilNumber);
    }

    [TestMethod]
    public async Task GetCoilForJobAsync_WhenTheSavedJobIsForAnotherWorkCenter_ReportsNoCoil()
    {
        var helper = new StubMySqlHelperServer(ActiveJobRow("MMC0001000", "COIL", 10m, workCenter: "999-9"));
        var service = new CoilAvailabilityService(helper, new StubAverageCoilWeightService("5,000 lb"));

        var coil = await service.GetCoilForJobAsync(WorkCenter);

        Assert.IsFalse(coil.HasCoil);
    }

    [TestMethod]
    public async Task GetCoilForJobAsync_WhenTheJobCarriesNoCoilPart_ReportsNoCoil()
    {
        // A flatstock/die-only job: subordinate parts exist but none carries the MMC coil prefix.
        var helper = new StubMySqlHelperServer(ActiveJobRow("MMF0001154", "FLATSTOCK", 12m));
        var service = new CoilAvailabilityService(helper, new StubAverageCoilWeightService("5,000 lb"));

        var coil = await service.GetCoilForJobAsync(WorkCenter);

        Assert.IsFalse(coil.HasCoil);
    }

    [TestMethod]
    public async Task GetCoilForJobAsync_ToleratesAMalformedSubordinatePayload_WithoutAssumingACoil()
    {
        var helper = new StubMySqlHelperServer(new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["work_center"] = WorkCenter,
            ["subordinate_parts_json"] = "{ not json",
        });
        var service = new CoilAvailabilityService(helper, new StubAverageCoilWeightService("5,000 lb"));

        var coil = await service.GetCoilForJobAsync(WorkCenter);

        Assert.IsFalse(coil.HasCoil);
    }

    [TestMethod]
    public async Task GetCoilForJobAsync_WithoutAWorkCenter_ReadsNothingAndReportsNoCoil()
    {
        var helper = new StubMySqlHelperServer(ActiveJobRow("MMC0001000", "COIL", 10m));
        var service = new CoilAvailabilityService(helper, new StubAverageCoilWeightService("5,000 lb"));

        var coil = await service.GetCoilForJobAsync(null);

        Assert.IsFalse(coil.HasCoil);
        Assert.AreEqual(0, helper.QueryCallCount, "Without a work center there is nothing to resolve.");
    }

    [TestMethod]
    public async Task GetCoilForJobAsync_HonorsCancellation()
    {
        var service = new CoilAvailabilityService(
            new StubMySqlHelperServer(),
            new StubAverageCoilWeightService("5,000 lb"));
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsExceptionAsync<OperationCanceledException>(
            () => service.GetCoilForJobAsync(WorkCenter, cts.Token));
    }

    private static Dictionary<string, object?> ActiveJobRow(
        string partNumber,
        string description,
        decimal onHandQuantity,
        string workCenter = WorkCenter)
    {
        var parts = new[]
        {
            new
            {
                Category = "Coil",
                PartNumber = partNumber,
                Description = description,
                Location = "R-1",
                OnHandQuantity = onHandQuantity,
                User8 = string.Empty,
                SelectedScrapType = string.Empty,
                IsLowStock = false,
            },
        };

        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["work_center"] = workCenter,
            ["work_order"] = "WO-1",
            ["part_number"] = "PART-1",
            ["sequence_number"] = "10",
            ["subordinate_parts_json"] = JsonSerializer.Serialize(parts),
            ["selected_dunnage_parts_json"] = "[]",
        };
    }

    private sealed class StubAverageCoilWeightService : IAverageCoilWeightService
    {
        private readonly string _text;

        public StubAverageCoilWeightService(string text) => _text = text;

        public string? LastPartNumber { get; private set; }

        public Task<string> ResolveAverageCoilWeightTextAsync(string partId, CancellationToken cancellationToken = default)
        {
            LastPartNumber = partId;
            return Task.FromResult(_text);
        }
    }

    private sealed class StubMySqlHelperServer : IMySqlHelperServer
    {
        private readonly IReadOnlyList<Dictionary<string, object?>> _rows;

        public StubMySqlHelperServer(params Dictionary<string, object?>[] rows) => _rows = rows;

        public int QueryCallCount { get; private set; }

        /// <summary>Reads issued as raw statement text — must stay 0 (FR-015, constitution III).</summary>
        public int SqlCallCount { get; private set; }

        public MySqlDatabaseTarget? LastDatabaseTarget { get; private set; }

        public string? LastStoredProcedureName { get; private set; }

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            QueryCallCount++;
            LastStoredProcedureName = storedProcedureName;
            LastDatabaseTarget = databaseTarget;
            return Task.FromResult(_rows);
        }

        public Task<int> ExecuteStoredProcedureNonQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteSqlQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
        {
            SqlCallCount++;
            return Task.FromResult<IReadOnlyList<Dictionary<string, object?>>>(Array.Empty<Dictionary<string, object?>>());
        }

        public Task<int> ExecuteSqlNonQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
        {
            SqlCallCount++;
            return Task.FromResult(0);
        }
    }
}
