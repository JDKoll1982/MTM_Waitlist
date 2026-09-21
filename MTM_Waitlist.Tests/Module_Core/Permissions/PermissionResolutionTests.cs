using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Permissions;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Tests.Module_Settings;

namespace MTM_Waitlist.Tests.Module_Core.Permissions;

/// <summary>
/// The resolution order for one person: their own stored row, otherwise their role's baseline, otherwise the
/// shipped fallback (FR-049, FR-050, FR-051, FR-053, FR-060).
/// </summary>
/// <remarks>
/// The store is faked, so what these cases prove is the composition and the cache rather than the SQL. The
/// composition is the part with the rule in it: the order the rows are applied in decides whether a person's own
/// choice survives.
/// </remarks>
[TestClass]
public sealed class PermissionResolutionTests
{
    [TestMethod]
    public async Task HasPermissionAsync_WithNoStoredRowOfTheirOwn_TakesTheRoleBaseline()
    {
        var helper = new FakeMySqlHelperServer();
        helper.EnqueueQueryResult(Row(PermissionKeys.RequestsHandle, "role", "role:production", value: true));
        var service = CreateService(helper);

        Assert.IsTrue(await service.HasPermissionAsync(PermissionKeys.RequestsHandle));
    }

    [TestMethod]
    public async Task HasPermissionAsync_WithNoStoredRowAtAll_FallsBackToTheShippedFallback()
    {
        var helper = new FakeMySqlHelperServer();
        helper.EnqueueEmptyQueryResult();
        var service = CreateService(helper);

        // permission.requests.handle is the one key whose shipped fallback is true, so an unreachable or empty
        // store keeps the shop floor working; every other key answers false, which is the next case.
        Assert.IsTrue(await service.HasPermissionAsync(PermissionKeys.RequestsHandle));
        Assert.IsFalse(await service.HasPermissionAsync(PermissionKeys.SetupWorkCenters));
        Assert.IsFalse(await service.HasPermissionAsync(PermissionKeys.AdminPermissions));
    }

    [TestMethod]
    public async Task HasPermissionAsync_APersonsOwnRowBeatsTheirRoleBaseline()
    {
        var helper = new FakeMySqlHelperServer();

        // The role grants it and the person's own row denies it. Both orders are supplied deliberately: the
        // person must win whichever order the store returned them in, so the rule is not the SELECT's.
        helper.EnqueueQueryResult(
            Row(PermissionKeys.SetupWorkCenters, "role", "role:production", value: true),
            Row(PermissionKeys.SetupWorkCenters, "user", "user:7", value: false));
        var service = CreateService(helper);

        Assert.IsFalse(await service.HasPermissionAsync(PermissionKeys.SetupWorkCenters));
    }

    [TestMethod]
    public async Task HasPermissionAsync_APersonsOwnRowBeatsTheirRoleBaseline_WhateverOrderTheRowsArriveIn()
    {
        var helper = new FakeMySqlHelperServer();
        helper.EnqueueQueryResult(
            Row(PermissionKeys.SetupWorkCenters, "user", "user:7", value: false),
            Row(PermissionKeys.SetupWorkCenters, "role", "role:production", value: true));
        var service = CreateService(helper);

        Assert.IsFalse(await service.HasPermissionAsync(PermissionKeys.SetupWorkCenters));
    }

    [TestMethod]
    public async Task HasPermissionAsync_ARoleWithNoBaselineRowAnswersFromTheShippedFallback()
    {
        var helper = new FakeMySqlHelperServer();
        helper.EnqueueQueryResult(Row(PermissionKeys.RequestsHandle, "role", "role:production", value: true));
        var service = CreateService(helper);

        // A role added to the catalogue after this shipped answers from the fallback rather than failing, so a
        // gate never fails to recognise a person (FR-060).
        Assert.IsFalse(await service.HasPermissionAsync(PermissionKeys.SettingsHotWorkCenters));
    }

    [TestMethod]
    public async Task HasPermissionAsync_WhenTheStoreThrows_AnswersFromTheShippedFallbackRatherThanRefusing()
    {
        var service = CreateService(new ThrowingMySqlHelperServer());

        // A settings failure must not turn every control in the application into a refusal (FR-050). The one key
        // that is operationally load-bearing answers true, and the administrative keys answer false.
        Assert.IsTrue(await service.HasPermissionAsync(PermissionKeys.RequestsHandle));
        Assert.IsFalse(await service.HasPermissionAsync(PermissionKeys.AdminUsers));
    }

    [TestMethod]
    public async Task HasPermissionAsync_WithAnUndeclaredKey_ReportsAFailureRatherThanRefusingSilently()
    {
        var service = CreateService(new FakeMySqlHelperServer());

        // A gate reading a key the declaration does not hold is a failure, not a quiet no (FR-061).
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => service.HasPermissionAsync("permission.not_a_declared_action"));
    }

    [TestMethod]
    public async Task HasPermissionsAsync_AsksTheStoreOnceForSeveralAnswers()
    {
        var helper = new FakeMySqlHelperServer();
        helper.EnqueueEmptyQueryResult();
        var service = CreateService(helper);

        var answers = await service.HasPermissionsAsync(
            new[] { PermissionKeys.RequestsHandle, PermissionKeys.AdminUsers, PermissionKeys.SetupWorkCenters });

        Assert.AreEqual(3, answers.Count);
        Assert.IsTrue(answers[PermissionKeys.RequestsHandle]);
        Assert.IsFalse(answers[PermissionKeys.AdminUsers]);
        Assert.AreEqual(1, helper.ExecutedQueries.Count);
    }

    [TestMethod]
    public async Task HasPermissionAsync_AnswersFromTheSessionCacheUntilItIsInvalidated()
    {
        var helper = new FakeMySqlHelperServer();
        helper.EnqueueQueryResult(Row(PermissionKeys.AdminUsers, "role", "role:production", value: true));
        var service = CreateService(helper);

        Assert.IsTrue(await service.HasPermissionAsync(PermissionKeys.AdminUsers));

        // One read for the session: a second question does not go back to the store.
        Assert.IsTrue(await service.HasPermissionAsync(PermissionKeys.AdminUsers));
        Assert.AreEqual(1, helper.ExecutedQueries.Count);

        helper.EnqueueEmptyQueryResult();
        service.Invalidate();

        Assert.IsFalse(await service.HasPermissionAsync(PermissionKeys.AdminUsers));
        Assert.AreEqual(2, helper.ExecutedQueries.Count);
    }

    [TestMethod]
    public async Task HasPermissionAsync_AfterASignInResolvesADifferentPerson_ReadsAgain()
    {
        var helper = new FakeMySqlHelperServer();
        helper.EnqueueQueryResult(Row(PermissionKeys.AdminUsers, "user", "user:7", value: true));
        var state = new StartupState { UserId = 7, CurrentRoleCode = "production" };
        var service = new PermissionService(helper, state);

        Assert.IsTrue(await service.HasPermissionAsync(PermissionKeys.AdminUsers));

        // A sign-in that resolves somebody else must not hand back the previous person's answers.
        state.UserId = 8;
        state.CurrentRoleCode = "production";
        helper.EnqueueEmptyQueryResult();

        Assert.IsFalse(await service.HasPermissionAsync(PermissionKeys.AdminUsers));
        Assert.AreEqual(2, helper.ExecutedQueries.Count);
    }

    [TestMethod]
    public async Task HasPermissionAsync_BeforeAnyAccountIsResolved_AnswersFromTheFallbackWithoutReading()
    {
        var helper = new FakeMySqlHelperServer();
        var service = CreateService(helper, userId: 0);

        Assert.IsFalse(await service.HasPermissionAsync(PermissionKeys.AdminUsers));
        Assert.AreEqual(0, helper.ExecutedQueries.Count);
    }

    private static PermissionService CreateService(IMySqlHelperServer helper, long userId = 7)
    {
        var state = new StartupState { UserId = userId, CurrentRoleCode = "production" };
        return new PermissionService(helper, state);
    }

    private static Dictionary<string, object?> Row(string key, string scopeType, string scopeKey, bool value) =>
        new()
        {
            ["setting_key"] = key,
            ["scope_type"] = scopeType,
            ["scope_key"] = scopeKey,
            ["setting_value_bool"] = value ? 1 : 0,
            ["updated_utc"] = new DateTime(2026, 9, 21, 0, 0, 0, DateTimeKind.Utc),
        };

    /// <summary>A store that cannot be reached at all.</summary>
    private sealed class ThrowingMySqlHelperServer : IMySqlHelperServer
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
}
