using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Settings.Services;

/// <summary>
/// The one catalogue read and the service over it (FR-010, FR-016).
/// </summary>
/// <remarks>
/// The store is faked for the behaviour cases, so what they prove is the ordering and the cache rather than the
/// SQL. The shipped procedure's own text and, when a live store is reachable, its own answer are checked too,
/// because "the retired role is absent" is a claim about the procedure rather than about this service.
/// </remarks>
[TestClass]
public sealed class RoleCatalogServiceTests
{
    private const string ConnectionStringVariable = "MTM_WAITLIST_TEST_DB_CONNECTION_STRING";

    private const string CatalogueProcedure = "sp_auth_roles_list";

    private const string RetiredRoleCode = "admin";

    /// <summary>The nine codes the specification pins, once the rename has run.</summary>
    private static readonly string[] PinnedRoleCodes =
    {
        "developer", "it_department", "plant_manager", "production_lead", "setup_lead",
        "material_handler_lead", "material_handler", "production", "setup",
    };

    /// <summary>Every code the catalogue holds while the retired role is still in it.</summary>
    private static readonly string[] LiveRoleCodes =
    {
        "developer", "plant_manager", "production_lead", "setup_lead",
        "material_handler_lead", "material_handler", "production", "setup",
    };

    [TestMethod]
    public async Task GetRolesAsync_ReadsTheOneCatalogueProcedure()
    {
        var helper = new FakeMySqlHelperServer();
        helper.EnqueueQueryResult(Row(1, "developer", "Developer", 100));
        var service = new RoleCatalogService(helper);

        await service.GetRolesAsync();

        Assert.AreEqual(1, helper.ExecutedQueries.Count);
        Assert.AreEqual(CatalogueProcedure, helper.ExecutedQueries[0].Sql);
        Assert.AreEqual(0, helper.ExecutedQueries[0].Parameters.Count);
    }

    [TestMethod]
    public async Task GetRolesAsync_OrdersByRungDescendingThenCode()
    {
        var helper = new FakeMySqlHelperServer();
        helper.EnqueueQueryResult(
            Row(1, "setup", "Setup", 10),
            Row(2, "developer", "Developer", 100),
            Row(3, "production", "Production", 10),
            Row(4, "material_handler", "Material Handler", 10),
            Row(5, "plant_manager", "Plant Manager", 80));
        var service = new RoleCatalogService(helper);

        var roles = await service.GetRolesAsync();

        CollectionAssert.AreEqual(
            new[] { "developer", "plant_manager", "material_handler", "production", "setup" },
            roles.Select(entry => entry.RoleCode).ToArray());
    }

    [TestMethod]
    public async Task GetRolesAsync_ReadsTheCatalogueOnceForTheSessionAndAgainAfterInvalidation()
    {
        var helper = new FakeMySqlHelperServer();
        helper.EnqueueQueryResult(Row(1, "developer", "Developer", 100));
        var service = new RoleCatalogService(helper);

        await service.GetRolesAsync();
        await service.GetRolesAsync();

        Assert.AreEqual(1, helper.ExecutedQueries.Count, "The catalogue must be read once for the session.");

        service.Invalidate();
        await service.GetRolesAsync();

        Assert.AreEqual(2, helper.ExecutedQueries.Count);
    }

    [TestMethod]
    public async Task GetRolesAsync_WithACatalogueThatCannotBeReached_OffersNoRoleRatherThanFailing()
    {
        var service = new RoleCatalogService(new UnreachableMySqlHelperServer());

        var roles = await service.GetRolesAsync();

        Assert.AreEqual(0, roles.Count, "A catalogue that cannot be read is empty, never guessed at.");
    }

    [TestMethod]
    public async Task GetRolesAsync_AfterAFailedRead_AsksAgainRatherThanAnsweringEmptyForTheSession()
    {
        var helper = new FlakyMySqlHelperServer();
        helper.EnqueueQueryResult(Row(1, "developer", "Developer", 100));
        var service = new RoleCatalogService(helper);

        var unreached = await service.GetRolesAsync();

        Assert.AreEqual(0, unreached.Count, "A catalogue that cannot be read is empty, never guessed at.");

        helper.Recover();

        var reached = await service.GetRolesAsync();

        Assert.AreEqual(
            2,
            helper.Attempts,
            "A failed read is deliberately not cached: caching it would tell every later screen for the rest of the " +
            "session that the plant has no roles, which empties the picker and makes the rank rule answer for nobody.");
        Assert.AreEqual(1, reached.Count, "And the retry answers from the store rather than from the failure.");
        Assert.AreEqual("developer", reached[0].RoleCode);
    }

    [TestMethod]
    public async Task GetRolesAsync_IgnoresARowWithNoRoleCode()
    {
        var helper = new FakeMySqlHelperServer();
        helper.EnqueueQueryResult(Row(1, string.Empty, string.Empty, 0), Row(2, "developer", "Developer", 100));
        var service = new RoleCatalogService(helper);

        var roles = await service.GetRolesAsync();

        Assert.AreEqual(1, roles.Count);
        Assert.AreEqual("developer", roles[0].RoleCode);
    }

    [TestMethod]
    public void TheShippedCatalogueRead_ExcludesTheRetiredRoleCode()
    {
        var procedure = File.ReadAllText(Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "Database",
            "StoredProcedures",
            CatalogueProcedure,
            "create.sql"));

        StringAssert.Contains(
            procedure,
            "role_code <> '" + RetiredRoleCode + "'",
            "The catalogue read must exclude the retired role, so a picker cannot offer it.");

        StringAssert.Contains(procedure, "ORDER BY r.role_rank DESC, r.role_code ASC");
    }

    /// <summary>
    /// The live catalogue read, in whichever state the store is in.
    /// </summary>
    /// <remarks>
    /// This check is deliberately state-agnostic about the retired role. Before Phase 4's rename runs, the
    /// catalogue holds it and the read is what excludes it; after the rename has been applied against this store,
    /// the row is gone and the read has nothing left to exclude. Both are correct, so asserting that the row is
    /// present would make this check fail for a reason that has nothing to do with the read. What is asserted is
    /// the read's own contract: every catalogue row except the retired one, the nine pinned codes, no duplicates,
    /// and never the retired code.
    /// </remarks>
    [TestMethod]
    public async Task LiveCatalogue_ReturnsEveryCatalogueRoleExceptTheRetiredOne()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Assert.Inconclusive($"{ConnectionStringVariable} is not set; skipping the live catalogue check.");
        }

        var helper = new MySqlHelperServer(
            Options.Create(new StartupDatabaseOptions { ConnectionString = connectionString! }));
        var service = new RoleCatalogService(helper);

        var roles = await service.GetRolesAsync();

        var catalogueRows = await helper.ExecuteSqlQueryAsync(
            "SELECT role_code FROM auth_roles_catalog;",
            new Dictionary<string, object?>(),
            MySqlDatabaseTarget.MtmWaitlist);

        var catalogueCodes = catalogueRows
            .Select(row => Convert.ToString(row["role_code"])?.Trim() ?? string.Empty)
            .ToArray();

        var returnedCodes = roles.Select(entry => entry.RoleCode).ToArray();

        CollectionAssert.DoesNotContain(returnedCodes, RetiredRoleCode);
        Assert.AreEqual(
            catalogueCodes.Count(code => !string.Equals(code, RetiredRoleCode, StringComparison.Ordinal)),
            returnedCodes.Length,
            "The read returns every catalogue row except the retired one.");
        Assert.AreEqual(returnedCodes.Length, returnedCodes.Distinct(StringComparer.Ordinal).Count());

        foreach (var code in LiveRoleCodes)
        {
            CollectionAssert.Contains(returnedCodes, code, $"The catalogue read must return '{code}'.");
        }

        // Every code it returns is one of the pinned nine, so a typo cannot slip in as a role.
        foreach (var code in returnedCodes)
        {
            CollectionAssert.Contains(PinnedRoleCodes.ToArray(), code, $"'{code}' is not one of the pinned role codes.");
        }
    }

    private static Dictionary<string, object?> Row(long roleId, string roleCode, string roleName, int roleRank) => new()
    {
        ["role_id"] = roleId,
        ["role_code"] = roleCode,
        ["role_name"] = roleName,
        ["role_rank"] = roleRank,
    };

    /// <summary>A store that cannot be reached at all.</summary>
    private sealed class UnreachableMySqlHelperServer : IMySqlHelperServer
    {
        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteSqlQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default) => throw new InvalidOperationException("The store is unreachable.");

        public Task<int> ExecuteSqlNonQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default) => throw new InvalidOperationException("The store is unreachable.");

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default) => throw new InvalidOperationException("The store is unreachable.");

        public Task<int> ExecuteStoredProcedureNonQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default) => throw new InvalidOperationException("The store is unreachable.");
    }

    /// <summary>A store that cannot be reached until it recovers, and counts how often it was asked.</summary>
    private sealed class FlakyMySqlHelperServer : IMySqlHelperServer
    {
        private readonly List<Dictionary<string, object?>> _rows = [];

        private bool _recovered;

        internal int Attempts { get; private set; }

        internal void EnqueueQueryResult(params Dictionary<string, object?>[] rows) => _rows.AddRange(rows);

        internal void Recover() => _recovered = true;

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
        {
            Attempts++;

            if (!_recovered)
            {
                throw new InvalidOperationException("The store is unreachable.");
            }

            return Task.FromResult<IReadOnlyList<Dictionary<string, object?>>>(_rows);
        }

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteSqlQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<int> ExecuteSqlNonQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<int> ExecuteStoredProcedureNonQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
