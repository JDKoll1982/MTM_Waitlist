using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Settings.Services;

namespace MTM_Waitlist.Tests.Module_Settings.Services;

[TestClass]
public sealed class DefectTypeCatalogServiceTests
{
    [TestMethod]
    public void CanManage_AllowsAdminDeveloperAdministrator()
    {
        var service = new DefectTypeCatalogService(new StubMySqlHelperServer());
        Assert.IsTrue(service.CanManage("Admin"));
        Assert.IsTrue(service.CanManage("Developer"));
        Assert.IsTrue(service.CanManage("administrator"));
        Assert.IsTrue(service.CanManage("ADMIN"));
    }

    [TestMethod]
    public void CanManage_DeniesOperatorAndBlank()
    {
        var service = new DefectTypeCatalogService(new StubMySqlHelperServer());
        Assert.IsFalse(service.CanManage("Operator"));
        Assert.IsFalse(service.CanManage("Material Handler"));
        Assert.IsFalse(service.CanManage("   "));
        Assert.IsFalse(service.CanManage(null));
    }

    [TestMethod]
    public async Task GetActiveAsync_MapsRows()
    {
        var helper = new StubMySqlHelperServer(new Dictionary<string, object?>[]
        {
            new()
            {
                ["id"] = 7L,
                ["public_id"] = "abc",
                ["defect_name"] = "Scratch",
                ["description"] = "Surface scratch",
                ["sort_order"] = 2,
                ["is_active"] = (byte)1,
            },
        });
        var service = new DefectTypeCatalogService(helper);

        var result = await service.GetActiveAsync();

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(7L, result[0].Id);
        Assert.AreEqual("Scratch", result[0].Name);
        Assert.AreEqual(2, result[0].SortOrder);
        Assert.IsTrue(result[0].IsActive);
    }

    [TestMethod]
    public async Task AddAsync_DeniedRole_ReturnsFail_NoSpCall()
    {
        var helper = new StubMySqlHelperServer();
        var service = new DefectTypeCatalogService(helper);

        var result = await service.AddAsync("Scratch", null, 0, "Operator");

        Assert.IsFalse(result.Success);
        Assert.AreEqual(0, helper.NonQueryCalls.Count);
    }

    [TestMethod]
    public async Task AddAsync_EmptyName_ReturnsFail_NoSpCall()
    {
        var helper = new StubMySqlHelperServer();
        var service = new DefectTypeCatalogService(helper);

        var result = await service.AddAsync("   ", null, 0, "Admin");

        Assert.IsFalse(result.Success);
        Assert.AreEqual(0, helper.NonQueryCalls.Count);
    }

    [TestMethod]
    public async Task AddAsync_AllowedRole_CallsInsertSp()
    {
        var helper = new StubMySqlHelperServer(affectedRows: 1);
        var service = new DefectTypeCatalogService(helper);

        var result = await service.AddAsync("Scratch", "desc", 3, "Developer");

        Assert.IsTrue(result.Success);
        Assert.AreEqual(1, helper.NonQueryCalls.Count);
        Assert.AreEqual("sp_waitlist_defect_types_insert", helper.NonQueryCalls[0].Procedure);
        Assert.AreEqual("Scratch", helper.NonQueryCalls[0].Parameters["p_defect_name"]);
    }

    [TestMethod]
    public async Task UpdateAsync_DeniedRole_ReturnsFail_NoSpCall()
    {
        var helper = new StubMySqlHelperServer();
        var service = new DefectTypeCatalogService(helper);

        var result = await service.UpdateAsync(1, "New", null, 0, "Production Lead");

        Assert.IsFalse(result.Success);
        Assert.AreEqual(0, helper.NonQueryCalls.Count);
    }

    [TestMethod]
    public async Task DeleteAsync_AllowedRole_CallsDeleteSp()
    {
        var helper = new StubMySqlHelperServer(affectedRows: 1);
        var service = new DefectTypeCatalogService(helper);

        var result = await service.DeleteAsync(5, "Admin");

        Assert.IsTrue(result.Success);
        Assert.AreEqual(1, helper.NonQueryCalls.Count);
        Assert.AreEqual("sp_waitlist_defect_types_delete", helper.NonQueryCalls[0].Procedure);
        Assert.AreEqual(5L, helper.NonQueryCalls[0].Parameters["p_id"]);
    }

    [TestMethod]
    public async Task DeleteAsync_NotFound_AffectedZero_ReturnsFail()
    {
        var helper = new StubMySqlHelperServer(affectedRows: 0);
        var service = new DefectTypeCatalogService(helper);

        var result = await service.DeleteAsync(999, "Admin");

        Assert.IsFalse(result.Success);
        Assert.AreEqual(1, helper.NonQueryCalls.Count);
    }

    private sealed record Call(string Procedure, IReadOnlyDictionary<string, object?> Parameters);

    private sealed class StubMySqlHelperServer : IMySqlHelperServer
    {
        private readonly IReadOnlyList<Dictionary<string, object?>> _rows;
        private readonly int _affectedRows;

        public StubMySqlHelperServer(
            IReadOnlyList<Dictionary<string, object?>>? rows = null,
            int affectedRows = 0)
        {
            _rows = rows ?? Array.Empty<Dictionary<string, object?>>();
            _affectedRows = affectedRows;
        }

        public List<Call> NonQueryCalls { get; } = new();

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => Task.FromResult(_rows);

        public Task<int> ExecuteStoredProcedureNonQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
        {
            NonQueryCalls.Add(new Call(storedProcedureName, parameters));
            return Task.FromResult(_affectedRows);
        }

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteSqlQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => Task.FromResult(_rows);

        public Task<int> ExecuteSqlNonQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => Task.FromResult(0);
    }
}
