using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Module_Settings;

/// <summary>
/// Opt-in live-DB round-trip for the Phase 5 NCM defect CRUD stored procedures
/// (<c>sp_waitlist_defect_types_*</c> on <c>waitlist_defect_types</c>, mtm_waitlist).
///
/// Requires MTM_WAITLIST_TEST_DB_CONNECTION_STRING to point at a schema-provisioned MySQL instance.
/// Without it the test reports inconclusive so the suite stays green offline.
/// </summary>
[TestClass]
public sealed class DefectTypesCrudIntegrationTests
{
    private const string ConnectionStringVariable = "MTM_WAITLIST_TEST_DB_CONNECTION_STRING";

    private MySqlHelperServer _helper = null!;
    private string _defectName = string.Empty;

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

        _defectName = $"IT-DEFECT-{Guid.NewGuid():N}";
    }

    [TestCleanup]
    public async Task TestCleanup()
    {
        if (_helper is null || string.IsNullOrEmpty(_defectName))
        {
            return;
        }

        await _helper.ExecuteSqlNonQueryAsync(
            "DELETE FROM waitlist_defect_types WHERE defect_name LIKE 'IT-DEFECT-%';",
            new Dictionary<string, object?>(),
            MySqlDatabaseTarget.MtmWaitlist);
    }

    [TestMethod]
    public async Task CrudRoundTrip_InsertListUpdateDelete()
    {
        // Insert via the SP.
        await _helper.ExecuteStoredProcedureNonQueryAsync(
            "sp_waitlist_defect_types_insert",
            new Dictionary<string, object?>
            {
                ["p_defect_name"] = _defectName,
                ["p_description"] = "Integration test defect.",
                ["p_sort_order"] = 5,
                ["p_created_by_user_id"] = null,
            },
            MySqlDatabaseTarget.MtmWaitlist);

        var listed = await _helper.ExecuteStoredProcedureQueryAsync(
            "sp_waitlist_defect_types_get_all",
            new Dictionary<string, object?>(),
            MySqlDatabaseTarget.MtmWaitlist);

        var row = listed.FirstOrDefault(r =>
            string.Equals(Convert.ToString(r["defect_name"])?.Trim(), _defectName, StringComparison.OrdinalIgnoreCase));
        Assert.IsNotNull(row, "Inserted defect must be returned by the list SP.");
        var id = Convert.ToInt64(row!["id"]);
        Assert.AreEqual(5L, Convert.ToInt64(row["sort_order"]));

        // Update via the SP.
        var updatedName = _defectName + "-UPD";
        await _helper.ExecuteStoredProcedureNonQueryAsync(
            "sp_waitlist_defect_types_update",
            new Dictionary<string, object?>
            {
                ["p_id"] = id,
                ["p_defect_name"] = updatedName,
                ["p_description"] = "Edited.",
                ["p_sort_order"] = 9,
                ["p_updated_by_user_id"] = null,
            },
            MySqlDatabaseTarget.MtmWaitlist);

        var relisted = await _helper.ExecuteStoredProcedureQueryAsync(
            "sp_waitlist_defect_types_get_all",
            new Dictionary<string, object?>(),
            MySqlDatabaseTarget.MtmWaitlist);
        var updated = relisted.FirstOrDefault(r =>
            string.Equals(Convert.ToString(r["defect_name"])?.Trim(), updatedName, StringComparison.OrdinalIgnoreCase));
        Assert.IsNotNull(updated, "Updated defect must be returned by the list SP.");
        Assert.AreEqual(9L, Convert.ToInt64(updated!["sort_order"]));

        // Delete via the SP.
        await _helper.ExecuteStoredProcedureNonQueryAsync(
            "sp_waitlist_defect_types_delete",
            new Dictionary<string, object?> { ["p_id"] = id },
            MySqlDatabaseTarget.MtmWaitlist);

        var afterDelete = await _helper.ExecuteStoredProcedureQueryAsync(
            "sp_waitlist_defect_types_get_all",
            new Dictionary<string, object?>(),
            MySqlDatabaseTarget.MtmWaitlist);
        Assert.IsFalse(afterDelete.Any(r =>
            string.Equals(Convert.ToString(r["defect_name"])?.Trim(), updatedName, StringComparison.OrdinalIgnoreCase)),
            "Deleted defect must no longer be returned.");
    }
}
