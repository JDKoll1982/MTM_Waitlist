using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Permissions;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Settings.Services;

namespace MTM_Waitlist.Tests.Module_Settings.Services;

[TestClass]
public sealed class DefectTypeCatalogServiceTests
{
    [TestMethod]
    public async Task CanManage_ReadsTheDeclaredPermission_AndAnswersItsValue()
    {
        var holding = PermissionStub.Holding(PermissionKeys.SettingsDefectTypes);
        Assert.IsTrue(await new DefectTypeCatalogService(new StubMySqlHelperServer(), holding).CanManageAsync());
        CollectionAssert.Contains(
            holding.RequestedKeys.ToArray(),
            PermissionKeys.SettingsDefectTypes,
            "The catalogue's gate reads the key the declaration names for it.");

        var holdingNothing = PermissionStub.Holding();
        Assert.IsFalse(await new DefectTypeCatalogService(new StubMySqlHelperServer(), holdingNothing).CanManageAsync());
    }

    [TestMethod]
    public async Task CanManage_DoesNotAnswerFromAnyRoleName()
    {
        // The retired list admitted the display names Admin, administrator and Developer. None of them is read
        // any more, so a service that still decided from a role would have to be handed one to do it — and this
        // constructor takes none (FR-054).
        var service = new DefectTypeCatalogService(new StubMySqlHelperServer(), PermissionStub.Holding());

        Assert.IsFalse(await service.CanManageAsync());
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
        var service = new DefectTypeCatalogService(helper, PermissionStub.Holding(PermissionKeys.SettingsDefectTypes));

        var result = await service.GetActiveAsync();

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(7L, result[0].Id);
        Assert.AreEqual("Scratch", result[0].Name);
        Assert.AreEqual(2, result[0].SortOrder);
        Assert.IsTrue(result[0].IsActive);
    }

    [TestMethod]
    public async Task AddAsync_Denied_ReturnsFail_NoSpCall()
    {
        var helper = new StubMySqlHelperServer();
        var service = new DefectTypeCatalogService(helper, PermissionStub.Holding());

        var result = await service.AddAsync("Scratch", null, 0);

        Assert.IsFalse(result.Success);
        Assert.AreEqual(0, helper.NonQueryCalls.Count);
    }

    [TestMethod]
    public async Task AddAsync_EmptyName_ReturnsFail_NoSpCall()
    {
        var helper = new StubMySqlHelperServer();
        var service = new DefectTypeCatalogService(helper, PermissionStub.Holding(PermissionKeys.SettingsDefectTypes));

        var result = await service.AddAsync("   ", null, 0);

        Assert.IsFalse(result.Success);
        Assert.AreEqual(0, helper.NonQueryCalls.Count);
    }

    [TestMethod]
    public async Task AddAsync_Permitted_CallsInsertSp()
    {
        var helper = new StubMySqlHelperServer(affectedRows: 1);
        var service = new DefectTypeCatalogService(helper, PermissionStub.Holding(PermissionKeys.SettingsDefectTypes));

        var result = await service.AddAsync("Scratch", "desc", 3);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(1, helper.NonQueryCalls.Count);
        Assert.AreEqual("sp_waitlist_defect_types_insert", helper.NonQueryCalls[0].Procedure);
        Assert.AreEqual("Scratch", helper.NonQueryCalls[0].Parameters["p_defect_name"]);
    }

    [TestMethod]
    public async Task UpdateAsync_Denied_ReturnsFail_NoSpCall()
    {
        var helper = new StubMySqlHelperServer();
        var service = new DefectTypeCatalogService(helper, PermissionStub.Holding());

        var result = await service.UpdateAsync(1, "New", null, 0);

        Assert.IsFalse(result.Success);
        Assert.AreEqual(0, helper.NonQueryCalls.Count);
    }

    [TestMethod]
    public async Task DeleteAsync_Permitted_CallsDeleteSp()
    {
        var helper = new StubMySqlHelperServer(affectedRows: 1);
        var service = new DefectTypeCatalogService(helper, PermissionStub.Holding(PermissionKeys.SettingsDefectTypes));

        var result = await service.DeleteAsync(5);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(1, helper.NonQueryCalls.Count);
        Assert.AreEqual("sp_waitlist_defect_types_delete", helper.NonQueryCalls[0].Procedure);
        Assert.AreEqual(5L, helper.NonQueryCalls[0].Parameters["p_id"]);
    }

    [TestMethod]
    public async Task DeleteAsync_NotFound_AffectedZero_ReturnsFail()
    {
        var helper = new StubMySqlHelperServer(affectedRows: 0);
        var service = new DefectTypeCatalogService(helper, PermissionStub.Holding(PermissionKeys.SettingsDefectTypes));

        var result = await service.DeleteAsync(999);

        Assert.IsFalse(result.Success);
        Assert.AreEqual(1, helper.NonQueryCalls.Count);
    }

    /// <summary>A permission service that answers whether one key is held, and records what it was asked.</summary>
    private sealed class PermissionStub : IPermissionService
    {
        private readonly HashSet<string> _held;

        private PermissionStub(IEnumerable<string> held) =>
            _held = new HashSet<string>(held, StringComparer.Ordinal);

        internal static PermissionStub Holding(params string[] heldPermissionKeys) => new(heldPermissionKeys);

        internal List<string> RequestedKeys { get; } = [];

        public Task<bool> HasPermissionAsync(string permissionKey, CancellationToken cancellationToken = default)
        {
            RequestedKeys.Add(permissionKey);
            return Task.FromResult(_held.Contains(permissionKey));
        }

        public Task<IReadOnlyDictionary<string, bool>> HasPermissionsAsync(
            IEnumerable<string> permissionKeys,
            CancellationToken cancellationToken = default)
        {
            var keys = permissionKeys.ToArray();
            RequestedKeys.AddRange(keys);

            return Task.FromResult<IReadOnlyDictionary<string, bool>>(
                keys.ToDictionary(key => key, _held.Contains, StringComparer.Ordinal));
        }

        public void Invalidate()
        {
        }
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
