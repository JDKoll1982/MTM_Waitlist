using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Tests.Module_Setup;

/// <summary>
/// Opt-in live-DB round-trips for the two Phase 2.1 read-back stored procedures:
///   • sp_setup_active_jobs_latest_by_work_center_get  (mtm_waitlist)
///   • sp_receiving_history_average_coil_weight         (mtm_receiving_application)
///
/// Requires MTM_WAITLIST_TEST_DB_CONNECTION_STRING to point at a MySQL instance that
/// hosts both schemas (the shared fallback connection reaches both via the
/// MySqlDatabaseTarget database override). Without it every test reports
/// inconclusive rather than failing, so the suite stays green offline.
///
/// Uses a dedicated variable so MTM_WAITLIST_DB_CONNECTION_STRING resolution is not
/// changed for the whole process (which would break Module_Setup no-backend tests).
/// </summary>
[TestClass]
public sealed class ActiveJobReadBackSpIntegrationTests
{
    private const string ConnectionStringVariable = "MTM_WAITLIST_TEST_DB_CONNECTION_STRING";

    private MySqlHelperServer _helper = null!;
    private string _testWorkCenter = string.Empty;
    private string _testPartId = string.Empty;

    [TestInitialize]
    public void TestInitialize()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Assert.Inconclusive($"{ConnectionStringVariable} is not set; skipping database integration tests.");
        }

        _helper = new MySqlHelperServer(
            Options.Create(new StartupDatabaseOptions { ConnectionString = connectionString! }));

        _testWorkCenter = $"IT-WC-{Guid.NewGuid():N}";
        _testPartId = $"IT-COIL-{Guid.NewGuid():N}";
    }

    [TestCleanup]
    public async Task TestCleanup()
    {
        if (_helper is null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(_testWorkCenter))
        {
            await _helper.ExecuteSqlNonQueryAsync(
                "DELETE FROM setup_active_jobs WHERE work_center = @p_work_center;",
                new Dictionary<string, object?> { ["p_work_center"] = _testWorkCenter },
                MySqlDatabaseTarget.MtmWaitlist);
        }

        if (!string.IsNullOrEmpty(_testPartId))
        {
            await _helper.ExecuteSqlNonQueryAsync(
                "DELETE FROM receiving_history WHERE part_id = @p_part_id;",
                new Dictionary<string, object?>
                {
                    ["p_part_id"] = _testPartId,
                },
                MySqlDatabaseTarget.MtmReceivingApplication);
        }
    }

    [TestMethod]
    public async Task ActiveJobsReadBack_ReturnsJsonColumns_ForSeededWorkCenter()
    {
        var partsJson = "[{\"Category\":\"Coil\",\"PartNumber\":\"MMC0001000\",\"Description\":\"C\",\"Location\":\"Rack A1\"}]";
        var dunnageJson = "[{\"Id\":\"dn-1\",\"PartNumber\":\"DN-STL-4\",\"DisplayName\":\"Steel Rack\"}]";

        var inserted = await _helper.ExecuteSqlNonQueryAsync(
            "INSERT INTO setup_active_jobs (public_id, work_order, part_number, sequence_number, work_center, subordinate_parts_json, selected_dunnage_parts_json, is_active, created_utc, updated_utc) VALUES (@p_public_id, @p_work_order, @p_part_number, @p_sequence, @p_work_center, @p_sub_parts, @p_dunnage, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP());",
            new Dictionary<string, object?>
            {
                ["p_public_id"] = Guid.NewGuid().ToString(),
                ["p_work_order"] = "WO-ITEST",
                ["p_part_number"] = "22-ITEST-001",
                ["p_sequence"] = "30",
                ["p_work_center"] = _testWorkCenter,
                ["p_sub_parts"] = partsJson,
                ["p_dunnage"] = dunnageJson,
            },
            MySqlDatabaseTarget.MtmWaitlist);

        Assert.AreEqual(1, inserted, "Seed row must insert for the read-back SP test.");

        var rows = await _helper.ExecuteStoredProcedureQueryAsync(
            "sp_setup_active_jobs_latest_by_work_center_get",
            new Dictionary<string, object?>(),
            MySqlDatabaseTarget.MtmWaitlist);

        var row = rows.FirstOrDefault(r =>
            string.Equals(Convert.ToString(r["work_center"])?.Trim(), _testWorkCenter, StringComparison.OrdinalIgnoreCase));

        Assert.IsNotNull(row, "The read-back SP must return the seeded work center's latest active row.");
        Assert.IsTrue(Convert.ToString(row!["subordinate_parts_json"])?.Contains("MMC0001000") == true, "subordinate_parts_json must be selected.");
        Assert.IsTrue(Convert.ToString(row["selected_dunnage_parts_json"])?.Contains("DN-STL-4") == true, "selected_dunnage_parts_json must be selected.");
    }

    [TestMethod]
    public async Task AverageCoilWeight_ReturnsRoundedAvg_ForSeededPart()
    {
        const int skidWeight1 = 4800;
        const int skidWeight2 = 5200;
        var loadGuid1 = Guid.NewGuid().ToString();
        var loadGuid2 = Guid.NewGuid().ToString();

        await _helper.ExecuteSqlNonQueryAsync(
            "INSERT INTO receiving_history (load_guid, quantity, part_id, part_type, employee_number, received_date, transaction_date, is_non_po_item, is_quality_hold_required, is_quality_hold_acknowledged, is_reprint) VALUES (@p_load_guid, @p_qty, @p_part_id, 'COIL', 6229, UTC_TIMESTAMP(), CURDATE(), 0, 0, 0, 0);",
            new Dictionary<string, object?>
            {
                ["p_load_guid"] = loadGuid1,
                ["p_qty"] = skidWeight1,
                ["p_part_id"] = _testPartId,
            },
            MySqlDatabaseTarget.MtmReceivingApplication);
        await _helper.ExecuteSqlNonQueryAsync(
            "INSERT INTO receiving_history (load_guid, quantity, part_id, part_type, employee_number, received_date, transaction_date, is_non_po_item, is_quality_hold_required, is_quality_hold_acknowledged, is_reprint) VALUES (@p_load_guid, @p_qty, @p_part_id, 'COIL', 6229, UTC_TIMESTAMP(), CURDATE(), 0, 0, 0, 0);",
            new Dictionary<string, object?>
            {
                ["p_load_guid"] = loadGuid2,
                ["p_qty"] = skidWeight2,
                ["p_part_id"] = _testPartId,
            },
            MySqlDatabaseTarget.MtmReceivingApplication);

        var rows = await _helper.ExecuteStoredProcedureQueryAsync(
            "sp_receiving_history_average_coil_weight",
            new Dictionary<string, object?> { ["p_part_id"] = _testPartId },
            MySqlDatabaseTarget.MtmReceivingApplication);

        Assert.AreEqual(1, rows.Count, "The average-coil-weight SP must return one row.");
        var average = Convert.ToDecimal(rows[0]["AverageWeight"]);
        Assert.AreEqual(5000, average, "(4800 + 5200) / 2 must round to 5000.");
    }
}
