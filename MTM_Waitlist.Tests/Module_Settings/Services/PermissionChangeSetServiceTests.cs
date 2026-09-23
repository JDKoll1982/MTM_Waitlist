using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Permissions;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Module_Settings.Services;

/// <summary>
/// The change set proved against a live store (T058, FR-026, FR-059, FR-068, FR-070, FR-071, FR-072, SC-009).
/// </summary>
/// <remarks>
/// <para>
/// <b>One run, one sequence, because the steps share one state.</b> A save of several permissions, then its
/// reversal, then a refused change, then an empty set: each step needs what the one before it left, and a run that
/// stopped early would leave the store in a state the next step cannot describe.
/// </para>
/// <para>
/// <b>Environment-gated on <c>MTM_WAITLIST_TEST_DB_CONNECTION_STRING</c>.</b> Without a live <c>mtm_waitlist</c>
/// this test reports inconclusive, recorded as skipped and never as passing.
/// </para>
/// <para>
/// The fixture is this test's own: two accounts it creates and removes, so it never depends on the shape of the
/// shipped roster and never leaves a permission row behind for somebody else.
/// </para>
/// </remarks>
[TestClass]
public sealed class PermissionChangeSetServiceTests
{
    private const string ConnectionStringVariable = "MTM_WAITLIST_TEST_DB_CONNECTION_STRING";

    private const string ActorSignInName = "ZZ.PERMS.ACTOR";
    private const string TargetSignInName = "ZZ.PERMS.TARGET";

    private const string ActorRoleCode = "developer";
    private const string TargetRoleCode = "setup";

    /// <summary>Two permissions the set changes together, so "the whole save reverses" has something to reverse.</summary>
    private static readonly string[] ChangedKeys =
    [
        PermissionKeys.SettingsHotWorkCenters,
        PermissionKeys.SettingsIgnoredLocations,
    ];

    private MySqlHelperServer? _helper;
    private PermissionAdministrationService? _service;
    private long _actorUserId;
    private long _targetUserId;

    [TestInitialize]
    public async Task TestInitializeAsync()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Assert.Inconclusive($"{ConnectionStringVariable} is not set; skipping the live change-set proof.");
        }

        _helper = new MySqlHelperServer(
            Options.Create(new StartupDatabaseOptions { ConnectionString = connectionString! }));

        await DeleteFixturesAsync().ConfigureAwait(false);

        _actorUserId = await CreateFixtureAsync(ActorSignInName, "Zed Actor", ActorRoleCode).ConfigureAwait(false);
        _targetUserId = await CreateFixtureAsync(TargetSignInName, "Tess Target", TargetRoleCode).ConfigureAwait(false);

        var state = new StartupState { UserId = _actorUserId, Username = ActorSignInName, CurrentRoleCode = ActorRoleCode };
        _service = new PermissionAdministrationService(
            Options.Create(new StartupDatabaseOptions { ConnectionString = connectionString }),
            new RecordingPermissionService(),
            state);
    }

    [TestCleanup]
    public async Task TestCleanupAsync()
    {
        if (_helper is not null)
        {
            await DeleteFixturesAsync().ConfigureAwait(false);
        }
    }

    [TestMethod]
    public async Task TheChangeSet_IsWrittenReversedAndRefusedExactlyAsTheContractSays()
    {
        var service = _service!;
        var helper = _helper!;

        var before = await HistoryCountAsync().ConfigureAwait(false);

        // What is in force before anything is written: for a person with no rows of their own, their role's
        // baseline. The save writes the opposite of it, so every entry is a real change rather than a no-op the
        // procedure skips.
        var beforeSave = await service.GetForPersonAsync(_targetUserId).ConfigureAwait(false);
        var baseline = ChangedKeys.ToDictionary(
            key => key,
            key => beforeSave.Single(candidate => candidate.Key == key).Value,
            StringComparer.Ordinal);

        // ---- An empty set writes nothing at all (FR-068).
        var empty = await service.ApplyAsync(_targetUserId, []).ConfigureAwait(false);
        Assert.IsTrue(empty.IsSuccess);
        Assert.AreEqual(before, await HistoryCountAsync().ConfigureAwait(false), "An empty set writes no history record.");

        // ---- A save of several permissions writes exactly its own number of history rows (FR-072).
        var save = await service.ApplyAsync(
            _targetUserId,
            ChangedKeys.Select(key => new PermissionChange(key, From: null, To: !baseline[key])).ToArray()).ConfigureAwait(false);

        Assert.IsTrue(save.IsSuccess, $"The save must land: {save.MessageKey}.");
        Assert.AreEqual(
            before + ChangedKeys.Length,
            await HistoryCountAsync().ConfigureAwait(false),
            "One history row per changed permission, and no more.");

        var recorded = await HistoryRowsAsync().ConfigureAwait(false);
        Assert.AreEqual(ChangedKeys.Length, recorded.Count, "Exactly the keys that changed are recorded.");
        CollectionAssert.AreEquivalent(ChangedKeys, recorded.Select(row => row.Key).ToArray());
        Assert.IsTrue(recorded.All(row => row.ActorUserId == _actorUserId), "Each row names the acting user (FR-072).");
        Assert.IsTrue(recorded.All(row => row.PreviousValue is null), "The value before the first save is nothing.");
        Assert.IsTrue(
            recorded.All(row => row.ChangedValue == (baseline[row.Key] ? 0 : 1)),
            "Each row records the value that was written.");

        var afterSave = await service.GetForPersonAsync(_targetUserId).ConfigureAwait(false);
        foreach (var key in ChangedKeys)
        {
            var row = afterSave.Single(candidate => candidate.Key == key);
            Assert.AreEqual(!baseline[key], row.Value, "The value in force is what was saved.");
            Assert.AreEqual(PermissionProvenance.Chosen, row.Provenance, "It is a choice made for this person, not a baseline.");
        }

        var untouched = afterSave.First(candidate => !ChangedKeys.Contains(candidate.Key));
        Assert.AreEqual(
            PermissionProvenance.Inherited,
            untouched.Provenance,
            "A key the save did not touch still answers from the person's role baseline (FR-051).");

        // ---- The whole save reverses, and restores every one of them (FR-071).
        var reversed = await service.ReverseLastSaveAsync(_targetUserId).ConfigureAwait(false);
        Assert.IsTrue(reversed.IsSuccess, $"The reversal must land: {reversed.MessageKey}.");
        Assert.AreEqual(
            before + (ChangedKeys.Length * 2),
            await HistoryCountAsync().ConfigureAwait(false),
            "The reversal records its own rows rather than erasing the save's.");

        var afterReversal = await service.GetForPersonAsync(_targetUserId).ConfigureAwait(false);
        foreach (var key in ChangedKeys)
        {
            var row = afterReversal.Single(candidate => candidate.Key == key);
            Assert.AreEqual(
                baseline[key],
                row.Value,
                "The reversal restores the value that was in force before the save.");

            // The one observable difference, recorded rather than hidden: the restored value is carried by the
            // person's own row, because this history table names the value row it changed and the foreign key
            // refuses a row naming one that has been deleted. Restoring the absence of a row would be restoring
            // something the table cannot record.
            Assert.AreEqual(
                PermissionProvenance.Chosen,
                row.Provenance,
                "The value is restored; the row that carries it is the person's own rather than the role's.");
        }

        // ---- A value that has moved is refused, and the key that moved is named (FR-070).
        await service.ApplyAsync(
            _targetUserId,
            [new PermissionChange(ChangedKeys[0], From: baseline[ChangedKeys[0]], To: !baseline[ChangedKeys[0]])]).ConfigureAwait(false);

        var afterSecondSave = await HistoryCountAsync().ConfigureAwait(false);

        // The `from` is now stale: the store holds the opposite of it.
        var moved = await service.ApplyAsync(
            _targetUserId,
            [new PermissionChange(ChangedKeys[0], From: baseline[ChangedKeys[0]], To: baseline[ChangedKeys[0]])]).ConfigureAwait(false);

        Assert.AreEqual(PermissionChangeOutcomeKind.ValueMoved, moved.Kind, "A value that moved is refused rather than overwritten.");
        Assert.AreEqual(ChangedKeys[0], moved.MovedKey, "Which key moved is reported, not just that something did.");
        Assert.AreEqual(
            afterSecondSave,
            await HistoryCountAsync().ConfigureAwait(false),
            "A refused change writes no history row, because nothing changed (FR-026).");

        // ---- The one fixed row is refused (FR-059).
        var fixedRow = await service.ApplyAsync(
            _targetUserId,
            [new PermissionChange(PermissionKeys.AdminPermissions, From: null, To: true)]).ConfigureAwait(false);

        Assert.AreEqual(PermissionChangeOutcomeKind.GateFixed, fixedRow.Kind);
        Assert.AreEqual(PermissionKeys.AdminPermissions, fixedRow.MovedKey, "The row the refusal is about is named.");

        // ---- A key outside the permission namespace is refused.
        var outsideNamespace = await service.ApplyAsync(
            _targetUserId,
            [new PermissionChange("admin.users.list_filter", From: null, To: true)]).ConfigureAwait(false);

        Assert.AreEqual(PermissionChangeOutcomeKind.KeyInvalid, outsideNamespace.Kind);
        Assert.AreEqual(
            afterSecondSave,
            await HistoryCountAsync().ConfigureAwait(false),
            "Neither refusal wrote anything.");

        // ---- And nothing about the person is left changed by the refusals.
        var afterRefusals = await service.GetForPersonAsync(_targetUserId).ConfigureAwait(false);
        Assert.AreEqual(!baseline[ChangedKeys[0]], afterRefusals.Single(candidate => candidate.Key == ChangedKeys[0]).Value);
    }

    /// <summary>One audit-free fixture account, created directly so the actor can exist before anything acts.</summary>
    private async Task<long> CreateFixtureAsync(string signInName, string displayName, string roleCode)
    {
        var helper = _helper!;

        await helper.ExecuteSqlNonQueryAsync(
            """
            INSERT INTO core_users_profiles
                (public_id, username_normalized, first_name, last_name, password_hash, password_salt,
                 require_password_change, temporary_credential_failed_attempts, display_name,
                 employee_identifier, is_active, created_utc, updated_utc)
            VALUES (UUID(), @p_username, @p_first, @p_last, 'x', NULL, 0, 0, @p_display, '9999', 1,
                    UTC_TIMESTAMP(), UTC_TIMESTAMP());
            """,
            new Dictionary<string, object?>
            {
                ["p_username"] = signInName,
                ["p_first"] = displayName.Split(' ')[0],
                ["p_last"] = displayName.Split(' ')[1],
                ["p_display"] = displayName,
            },
            MySqlDatabaseTarget.MtmWaitlist).ConfigureAwait(false);

        var created = await helper.ExecuteSqlQueryAsync(
            "SELECT id FROM core_users_profiles WHERE username_normalized = @p_username;",
            new Dictionary<string, object?> { ["p_username"] = signInName },
            MySqlDatabaseTarget.MtmWaitlist).ConfigureAwait(false);

        var userId = Convert.ToInt64(created.Single().Values.First(), System.Globalization.CultureInfo.InvariantCulture);

        await helper.ExecuteSqlNonQueryAsync(
            """
            INSERT INTO auth_roles_assignments (public_id, user_id, role_id, assigned_utc)
            SELECT UUID(), @p_user_id, r.id, UTC_TIMESTAMP() FROM auth_roles_catalog r WHERE r.role_code = @p_role_code;
            """,
            new Dictionary<string, object?> { ["p_user_id"] = userId, ["p_role_code"] = roleCode },
            MySqlDatabaseTarget.MtmWaitlist).ConfigureAwait(false);

        return userId;
    }

    private async Task DeleteFixturesAsync()
    {
        var helper = _helper!;

        // The permission rows and their history go first: the history carries a foreign key to the value row.
        await helper.ExecuteSqlNonQueryAsync(
            """
            DELETE h FROM config_settings_history h
            INNER JOIN core_users_profiles u
                    ON h.scope_key = CONCAT('user:', u.id)
            WHERE u.username_normalized IN (@p_actor, @p_target);

            DELETE v FROM config_settings_values v
            INNER JOIN core_users_profiles u
                    ON v.scope_key = CONCAT('user:', u.id)
            WHERE u.username_normalized IN (@p_actor, @p_target);

            DELETE ra FROM auth_roles_assignments ra
            INNER JOIN core_users_profiles u ON u.id = ra.user_id
            WHERE u.username_normalized IN (@p_actor, @p_target);

            DELETE FROM core_users_profiles WHERE username_normalized IN (@p_actor, @p_target);
            """,
            new Dictionary<string, object?> { ["p_actor"] = ActorSignInName, ["p_target"] = TargetSignInName },
            MySqlDatabaseTarget.MtmWaitlist).ConfigureAwait(false);
    }

    private async Task<long> HistoryCountAsync()
    {
        var rows = await _helper!.ExecuteSqlQueryAsync(
            """
            SELECT COUNT(*) FROM config_settings_history h
            INNER JOIN core_users_profiles u ON h.scope_key = CONCAT('user:', u.id)
            WHERE u.username_normalized = @p_username AND h.setting_key LIKE 'permission.%';
            """,
            new Dictionary<string, object?> { ["p_username"] = TargetSignInName },
            MySqlDatabaseTarget.MtmWaitlist).ConfigureAwait(false);

        return Convert.ToInt64(rows.Single().Values.First(), System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>Every history row this person has, read back through the store rather than through the service.</summary>
    private async Task<IReadOnlyList<(string Key, long? ActorUserId, int? PreviousValue, int? ChangedValue)>> HistoryRowsAsync()
    {
        var rows = await _helper!.ExecuteSqlQueryAsync(
            """
            SELECT h.setting_key, h.changed_by_user_id, h.previous_setting_value_bool, h.changed_setting_value_bool
            FROM config_settings_history h
            INNER JOIN core_users_profiles u ON h.scope_key = CONCAT('user:', u.id)
            WHERE u.username_normalized = @p_username AND h.setting_key LIKE 'permission.%'
            ORDER BY h.id;
            """,
            new Dictionary<string, object?> { ["p_username"] = TargetSignInName },
            MySqlDatabaseTarget.MtmWaitlist).ConfigureAwait(false);

        return rows
            .Select(row => (
                Key: Convert.ToString(row["setting_key"], System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
                ActorUserId: row["changed_by_user_id"] is null or DBNull ? (long?)null : Convert.ToInt64(row["changed_by_user_id"], System.Globalization.CultureInfo.InvariantCulture),
                PreviousValue: row["previous_setting_value_bool"] is null or DBNull ? (int?)null : Convert.ToInt32(row["previous_setting_value_bool"], System.Globalization.CultureInfo.InvariantCulture),
                ChangedValue: row["changed_setting_value_bool"] is null or DBNull ? (int?)null : Convert.ToInt32(row["changed_setting_value_bool"], System.Globalization.CultureInfo.InvariantCulture)))
            .ToArray();
    }

    /// <summary>
    /// The resolution cache the service invalidates. It answers nothing here: this suite is about what reaches the
    /// store, and the cache's own behaviour is proved by the resolution suite.
    /// </summary>
    private sealed class RecordingPermissionService : IPermissionService
    {
        internal int Invalidations { get; private set; }

        public Task<bool> HasPermissionAsync(string permissionKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<IReadOnlyDictionary<string, bool>> HasPermissionsAsync(
            IEnumerable<string> permissionKeys,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<string, bool>>(
                permissionKeys.ToDictionary(key => key, _ => false, StringComparer.Ordinal));

        public void Invalidate() => Invalidations++;
    }
}
