using System.Globalization;
using System.Security.Cryptography;

using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MySqlConnector;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Models.UserManagement;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Startup.Services;

namespace MTM_Waitlist.Tests.Module_Settings.Services;

/// <summary>
/// The user-management writes proved against a live store: the create, the correction, the reset and the
/// refusals, each driven through the shipped service and repository (T072, FR-006, FR-008, FR-014, FR-025,
/// FR-035, FR-102, FR-111, SC-016, SC-019, §6 gate 3).
/// </summary>
/// <remarks>
/// <para>
/// <b>Why these cases are store-level.</b> The screen suites drive hand-written fakes, so they prove what a view
/// model does with an answer, not that the store gives that answer. The rank rule, the self-lockout rule, the
/// self-rename rule and the one-role-per-person rule are enforced by the procedures, which is the only place
/// they hold for a caller that never draws a screen (FR-019, FR-025). Every case below calls the service the
/// screens call.
/// </para>
/// <para>
/// <b>Both connection variables must name the same store.</b> The repository prefers
/// <c>MTM_WAITLIST_DB_CONNECTION_STRING</c> over the one it is handed, so a pass that set only the test variable
/// could write one store and read another. <c>TestInitializeAsync</c> refuses that outright rather than
/// reporting a confusing missing row.
/// </para>
/// <para>
/// <b>The fixture is this class's own.</b> Every account is written under <c>ZZ.USERMGMT.&lt;run&gt;.</c> and
/// removed in <c>TestCleanupAsync</c>, so the pass never depends on the shape of the shipped roster and never
/// leaves a row behind.
/// </para>
/// <para>
/// Environment-gated on <c>MTM_WAITLIST_TEST_DB_CONNECTION_STRING</c>: without a live <c>mtm_waitlist</c> every
/// case reports inconclusive, recorded as skipped and never as passing.
/// </para>
/// </remarks>
[TestClass]
public sealed class UserManagementLiveIntegrationTests
{
    private const string ConnectionStringVariable = "MTM_WAITLIST_TEST_DB_CONNECTION_STRING";

    private const string RepositoryConnectionStringVariable = "MTM_WAITLIST_DB_CONNECTION_STRING";

    /// <summary>Prefix for every account this class writes, so a run's rows are recognisable and removable.</summary>
    private const string OwnedSignInPrefix = "ZZ.USERMGMT.";

    /// <summary>One rung above the other, so "outranks" means something in the store.</summary>
    private const string HighRoleCode = "developer";

    private const string LowRoleCode = "setup";

    /// <summary>The lock wait the mid-way rollback is proved with, in seconds.</summary>
    private const int LockWaitSeconds = 2;

    private MySqlHelperServer? _helper;

    private StartupDatabaseOptions? _options;

    private string _connectionString = string.Empty;

    private string _runId = string.Empty;

    private long _highUserId;

    private long _lowUserId;

    private long _peerUserId;

    private string _highSignInName = string.Empty;

    private string _lowSignInName = string.Empty;

    private string _peerSignInName = string.Empty;

    private IUserManagementService? _actingHigh;

    private IUserManagementService? _actingLow;

    [TestInitialize]
    public async Task TestInitializeAsync()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Assert.Inconclusive($"{ConnectionStringVariable} is not set; skipping the live user-management proof.");
        }

        var repositoryConnectionString = Environment.GetEnvironmentVariable(RepositoryConnectionStringVariable);
        if (!string.IsNullOrWhiteSpace(repositoryConnectionString)
            && !string.Equals(repositoryConnectionString, connectionString, StringComparison.Ordinal))
        {
            Assert.Fail(
                $"{RepositoryConnectionStringVariable} and {ConnectionStringVariable} name different stores, so this pass would "
                    + "write one and read the other. Set both to the same live mtm_waitlist.");
        }

        _connectionString = connectionString!;
        _options = new StartupDatabaseOptions { ConnectionString = _connectionString };
        _helper = new MySqlHelperServer(Options.Create(_options));
        _runId = Guid.NewGuid().ToString("N")[..8];

        _highSignInName = $"{OwnedSignInPrefix}{_runId}.HIGH";
        _lowSignInName = $"{OwnedSignInPrefix}{_runId}.LOW";
        _peerSignInName = $"{OwnedSignInPrefix}{_runId}.PEER";

        await DeleteOwnedRowsAsync().ConfigureAwait(false);

        _highUserId = await CreateFixtureAsync(_highSignInName, HighRoleCode).ConfigureAwait(false);
        _lowUserId = await CreateFixtureAsync(_lowSignInName, LowRoleCode).ConfigureAwait(false);
        _peerUserId = await CreateFixtureAsync(_peerSignInName, LowRoleCode).ConfigureAwait(false);

        _actingHigh = BuildService(_highUserId, _highSignInName, HighRoleCode);
        _actingLow = BuildService(_lowUserId, _lowSignInName, LowRoleCode);
    }

    [TestCleanup]
    public async Task TestCleanupAsync()
    {
        if (_helper is not null)
        {
            await DeleteOwnedRowsAsync().ConfigureAwait(false);
        }
    }

    [TestMethod]
    public async Task CreateAsync_AValidPerson_WritesTheProfileAndItsOneRoleAssignmentTogether()
    {
        var signInName = $"{OwnedSignInPrefix}{_runId}.NEW";
        var result = await _actingHigh!
            .CreateAsync(new UserAccountEdit(signInName, "Nora", "Newcomer", "6229", LowRoleCode))
            .ConfigureAwait(false);

        Assert.IsTrue(result.IsSuccess, $"The create must land against the live store: {result.Message}");
        Assert.IsTrue(
            result.TemporaryPin.Length == 4 && result.TemporaryPin.All(char.IsAsciiDigit),
            "A create hands over a four-digit credential, and that is the one time it is readable (FR-030).");

        Assert.AreEqual("1", await ProfileCountAsync(signInName).ConfigureAwait(false), "The profile is written.");
        Assert.AreEqual(
            "1",
            await AssignmentCountAsync(signInName).ConfigureAwait(false),
            "And exactly one role assignment, so a person comes into existence with one role and never with none or two (FR-006, FR-009).");
        Assert.AreEqual(
            LowRoleCode,
            await ScalarAsync(
                "SELECT r.role_code FROM auth_roles_assignments ra INNER JOIN auth_roles_catalog r ON r.id = ra.role_id "
                    + "INNER JOIN core_users_profiles u ON u.id = ra.user_id WHERE u.username_normalized = @p_sign_in;",
                new Dictionary<string, object?> { ["p_sign_in"] = signInName }).ConfigureAwait(false),
            "The assignment names the role that was asked for.");

        // One act reads as one act: every audit row the create wrote shares one change identifier, names the
        // acting person and carries the time (FR-108, FR-110).
        Assert.AreEqual(
            "1",
            await ScalarAsync(
                "SELECT COUNT(DISTINCT a.change_group_id) FROM auth_user_management_audit a "
                    + "INNER JOIN core_users_profiles u ON u.id = a.target_user_id WHERE u.username_normalized = @p_sign_in;",
                new Dictionary<string, object?> { ["p_sign_in"] = signInName }).ConfigureAwait(false),
            "The create's audit rows share ONE change identifier.");

        Assert.AreEqual(
            "0",
            await ScalarAsync(
                "SELECT COUNT(*) FROM auth_user_management_audit a INNER JOIN core_users_profiles u ON u.id = a.target_user_id "
                    + "WHERE u.username_normalized = @p_sign_in AND (a.actor_user_id <> @p_actor OR a.occurred_utc IS NULL);",
                new Dictionary<string, object?> { ["p_sign_in"] = signInName, ["p_actor"] = _highUserId }).ConfigureAwait(false),
            "Every audit row names the acting user and the time it happened (FR-109).");

        // The credential that opened the account is recorded as having happened, with no value on either side
        // (FR-111): there is no readable PIN to write down, before or after.
        Assert.AreEqual(
            "1",
            await ScalarAsync(
                "SELECT COUNT(*) FROM auth_user_management_audit a INNER JOIN core_users_profiles u ON u.id = a.target_user_id "
                    + "WHERE u.username_normalized = @p_sign_in AND a.field_name = 'password_hash' "
                    + "AND a.previous_value IS NULL AND a.changed_value IS NULL;",
                new Dictionary<string, object?> { ["p_sign_in"] = signInName }).ConfigureAwait(false),
            "The initial credential gets its own audit row carrying no value on either side (FR-111).");
    }

    [TestMethod]
    public async Task CreateAsync_ASignInNameAlreadyTaken_ArrivesAsOneTypedAnswerAndWritesNothing()
    {
        var result = await _actingHigh!
            .CreateAsync(new UserAccountEdit(_peerSignInName, "Tess", "Target", "6229", LowRoleCode))
            .ConfigureAwait(false);

        Assert.AreEqual(
            UserManagementOutcomeKind.DuplicateUsername,
            result.Kind,
            "The provider's own 1062 becomes the one typed \"already taken\" answer (FR-008).");
        Assert.AreEqual(UserManagementMessages.DuplicateUsernameKey, result.MessageKey);
        Assert.AreEqual(UserManagementMessages.DuplicateUsername, result.Message);

        Assert.AreEqual("1", await ProfileCountAsync(_peerSignInName).ConfigureAwait(false), "No second profile is written.");
        Assert.AreEqual(
            "1",
            await AssignmentCountAsync(_peerSignInName).ConfigureAwait(false),
            "And no second role assignment: a refused create leaves the account exactly as it was.");
    }

    [TestMethod]
    public async Task CreateAsync_FailingAfterTheProfileIsWritten_RollsTheProfileAndTheRoleBackTogether()
    {
        // The catalogue the cases name must be the catalogue this store holds, or an outranks case would pass
        // for the wrong reason.
        Assert.AreEqual(
            "2",
            await ScalarAsync(
                "SELECT COUNT(*) FROM auth_roles_catalog WHERE role_code IN (@p_high, @p_low);",
                new Dictionary<string, object?> { ["p_high"] = HighRoleCode, ["p_low"] = LowRoleCode }).ConfigureAwait(false),
            "The catalogue holds both roles these cases name.");

        var signInName = $"{OwnedSignInPrefix}{_runId}.ROLLBACK";

        // How the mid-way failure is induced, and why no input reaches one: the create's own order is resolve
        // the role, insert the profile, insert the role assignment, write the audit rows, commit. Every refusal
        // its inputs can provoke is raised before the profile insert, and neither write after it can be made to
        // fail by anything a caller passes. So the failure is provoked from outside instead: another transaction
        // holds the role-assignment table's rows and gaps, the profile insert (a different table) succeeds, and
        // the role insert cannot proceed. The procedure's own EXIT HANDLER then rolls back a write that had
        // already happened.
        await using var holder = new MySqlConnection(_connectionString);
        await holder.OpenAsync().ConfigureAwait(false);

        await using (var hold = new MySqlCommand(
            "START TRANSACTION; SELECT COUNT(*) FROM auth_roles_assignments FOR UPDATE;",
            holder))
        {
            await hold.ExecuteNonQueryAsync().ConfigureAwait(false);
        }

        try
        {
            await using var create = new MySqlConnection(_connectionString);
            await create.OpenAsync().ConfigureAwait(false);

            await using (var wait = new MySqlCommand(
                $"SET SESSION innodb_lock_wait_timeout = {LockWaitSeconds.ToString(CultureInfo.InvariantCulture)};",
                create))
            {
                await wait.ExecuteNonQueryAsync().ConfigureAwait(false);
            }

            await using var call = new MySqlCommand("sp_user_management_create", create)
            {
                CommandType = System.Data.CommandType.StoredProcedure,
                CommandTimeout = 30,
            };
            call.Parameters.AddWithValue("@p_username", signInName);
            call.Parameters.AddWithValue("@p_first_name", "Roll");
            call.Parameters.AddWithValue("@p_last_name", "Back");
            call.Parameters.AddWithValue("@p_employee_identifier", "9999");
            call.Parameters.AddWithValue("@p_role_code", LowRoleCode);
            call.Parameters.AddWithValue("@p_actor_user_id", _highUserId);
            call.Parameters.AddWithValue("@p_password_hash", "rollback-probe-hash");
            call.Parameters.AddWithValue("@p_password_salt", new byte[32]);
            call.Parameters.AddWithValue("@p_change_group_id", Guid.NewGuid().ToString());

            var failure = await Assert
                .ThrowsExceptionAsync<MySqlException>(() => call.ExecuteNonQueryAsync())
                .ConfigureAwait(false);

            Assert.AreEqual(
                1205,
                failure.Number,
                "The failure is the lock wait on the role assignment, which happens after the profile insert and not before it.");
        }
        finally
        {
            await using var release = new MySqlCommand("ROLLBACK;", holder);
            await release.ExecuteNonQueryAsync().ConfigureAwait(false);
        }

        // Both writes are gone: the profile that had already been inserted, and the role assignment that could
        // not be, so a person cannot come into existence as half a person (FR-006).
        Assert.AreEqual("0", await ProfileCountAsync(signInName).ConfigureAwait(false), "The profile is rolled back, not left orphaned.");
        Assert.AreEqual("0", await AssignmentCountAsync(signInName).ConfigureAwait(false), "And no role assignment survives it.");
    }

    [TestMethod]
    public async Task SearchAsync_MatchesEachOfTheFourFacts_AndNarrowsByRole()
    {
        // The four facts the roster searches are the sign-in name, the display name, the employee number and the
        // role's own name, and the store is where they are matched (FR-086, §6 gate 2).
        await AssertFindsThePeerAsync(_peerSignInName, "the sign-in name");
        await AssertFindsThePeerAsync("Fixture Person", "the display name");
        await AssertFindsThePeerAsync("0000", "the employee number");
        await AssertFindsThePeerAsync("Setup", "the role name");

        var byRole = await _actingHigh!.SearchAsync(searchText: null, roleCode: LowRoleCode).ConfigureAwait(false);

        Assert.IsTrue(
            byRole.Any(row => row.UserId == _peerUserId),
            "A role filter narrows the roster to that role's people.");
    }

    [TestMethod]
    public async Task ASignInNameTypedInLowerCase_IsStoredUpperCase_AndSignsInEitherWay()
    {
        var typedName = $"{OwnedSignInPrefix}{_runId}.lowercase";

        var created = await _actingHigh!
            .CreateAsync(new UserAccountEdit(typedName, "Lou", "Lowercase", "4321", LowRoleCode))
            .ConfigureAwait(false);

        Assert.IsTrue(created.IsSuccess, $"The create must land: {created.Message}");
        Assert.AreEqual(
            typedName.ToUpperInvariant(),
            await ScalarAsync(
                "SELECT username_normalized FROM core_users_profiles WHERE username_normalized = @p_sign_in;",
                new Dictionary<string, object?> { ["p_sign_in"] = typedName }).ConfigureAwait(false),
            "A sign-in name is stored in upper case, whatever case it was typed in (FR-002).");

        var sessions = new StartupSessionRepository(Options.Create(_options!));

        var typedLower = await sessions.CheckCredentialsAsync(typedName, created.TemporaryPin).ConfigureAwait(false);
        Assert.IsTrue(typedLower.IsAuthenticated, "The name typed in lower case signs in against the stored upper-case name.");

        var typedUpper = await sessions
            .CheckCredentialsAsync(typedName.ToUpperInvariant(), created.TemporaryPin)
            .ConfigureAwait(false);
        Assert.IsTrue(typedUpper.IsAuthenticated, "And so does the same name typed in upper case, so either case signs in.");
    }

    [TestMethod]
    public async Task CreateAsync_ARoleAboveTheActorsOwnRung_IsRefusedByTheStoreWithNoScreenInvolved()
    {
        var signInName = $"{OwnedSignInPrefix}{_runId}.TOOHIGH";
        var result = await _actingLow!
            .CreateAsync(new UserAccountEdit(signInName, "Toby", "TooHigh", "1111", HighRoleCode))
            .ConfigureAwait(false);

        Assert.AreEqual(
            UserManagementOutcomeKind.RoleDenied,
            result.Kind,
            "A person cannot create somebody above their own rung, and the store is what refuses it (FR-022, FR-025).");
        Assert.AreEqual("0", await ProfileCountAsync(signInName).ConfigureAwait(false), "Nothing is written when the role is refused.");
    }

    [TestMethod]
    public async Task UpdateAsync_AnAccountThatOutranksTheActor_IsRefusedByTheStoreWithNoScreenInvolved()
    {
        var result = await _actingLow!
            .UpdateAsync(
                _highUserId,
                new UserAccountEdit(_highSignInName, "Changed", "Person", "1234", LowRoleCode, IsActive: false))
            .ConfigureAwait(false);

        Assert.AreEqual(
            UserManagementOutcomeKind.TargetOutranksActor,
            result.Kind,
            "A change to an account that outranks the actor is refused for the role, the active state and the name alike (SC-016).");

        Assert.AreEqual(
            "Fixture Person|1|" + HighRoleCode,
            await ScalarAsync(
                "SELECT CONCAT(u.first_name, ' ', u.last_name, '|', u.is_active, '|', COALESCE(r.role_code, '')) "
                    + "FROM core_users_profiles u LEFT JOIN auth_roles_assignments ra ON ra.user_id = u.id "
                    + "LEFT JOIN auth_roles_catalog r ON r.id = ra.role_id WHERE u.id = @p_user_id;",
                new Dictionary<string, object?> { ["p_user_id"] = _highUserId }).ConfigureAwait(false),
            "And the account is left exactly as it was: the refusal is not a partial save.");
    }

    [TestMethod]
    public async Task UpdateAsync_DeactivatingTheActorsOwnAccount_IsRefusedByTheStore()
    {
        var result = await _actingLow!
            .UpdateAsync(
                _lowUserId,
                new UserAccountEdit(_lowSignInName, "Fixture", "Person", "0000", LowRoleCode, IsActive: false))
            .ConfigureAwait(false);

        Assert.AreEqual(
            UserManagementOutcomeKind.SelfDeactivateDenied,
            result.Kind,
            "The store refuses switching off the account the actor is signed in as (FR-023).");
        Assert.AreEqual(
            "1",
            await ScalarAsync(
                "SELECT CAST(is_active AS UNSIGNED) FROM core_users_profiles WHERE id = @p_user_id;",
                new Dictionary<string, object?> { ["p_user_id"] = _lowUserId }).ConfigureAwait(false),
            "And the account is still active afterwards.");
    }

    [TestMethod]
    public async Task UpdateAsync_RenamingTheActorsOwnSignInName_IsRefusedByTheStore()
    {
        var before = await AuditRowCountAsync(_lowUserId).ConfigureAwait(false);

        var result = await _actingLow!
            .UpdateAsync(
                _lowUserId,
                new UserAccountEdit($"{OwnedSignInPrefix}{_runId}.RENAMED", "Fixture", "Person", "0000", LowRoleCode))
            .ConfigureAwait(false);

        Assert.AreEqual(
            UserManagementOutcomeKind.SelfRenameDenied,
            result.Kind,
            "The store refuses changing the sign-in name of the account the actor is signed in as (FR-024).");
        Assert.AreEqual(
            "1",
            await ScalarAsync(
                "SELECT COUNT(*) FROM core_users_profiles WHERE id = @p_user_id AND username_normalized = @p_sign_in;",
                new Dictionary<string, object?> { ["p_user_id"] = _lowUserId, ["p_sign_in"] = _lowSignInName }).ConfigureAwait(false),
            "The stored sign-in name is unchanged.");
        Assert.AreEqual(
            before,
            await AuditRowCountAsync(_lowUserId).ConfigureAwait(false),
            "A refused write records no change, because nothing changed (FR-026).");
    }

    [TestMethod]
    public async Task ResetPasswordAsync_WritesOneAuditRowWithNoValueOnEitherSide()
    {
        var before = await AuditRowCountAsync(_peerUserId).ConfigureAwait(false);

        var result = await _actingHigh!.ResetPasswordAsync(_peerUserId).ConfigureAwait(false);

        Assert.IsTrue(result.IsSuccess, $"The reset must land: {result.Message}");
        Assert.AreEqual(
            before + 1,
            await AuditRowCountAsync(_peerUserId).ConfigureAwait(false),
            "A reset records that it happened, once, and writes no second row (FR-111).");

        Assert.AreEqual(
            "1",
            await ScalarAsync(
                "SELECT COUNT(*) FROM auth_user_management_audit WHERE target_user_id = @p_user_id "
                    + "AND actor_user_id = @p_actor AND occurred_utc IS NOT NULL "
                    + "AND previous_value IS NULL AND changed_value IS NULL;",
                new Dictionary<string, object?> { ["p_user_id"] = _peerUserId, ["p_actor"] = _highUserId }).ConfigureAwait(false),
            "The reset's single row names the acting person, carries the time, and holds no value on either side, "
                + "because the credential is never stored (FR-030, FR-111).");
    }

    [TestMethod]
    public async Task UpdateAsync_DeactivatingAPerson_LeavesTheirExistingSessionsAlone()
    {
        await InsertSessionAsync(_peerUserId).ConfigureAwait(false);

        var sessionBefore = await SessionStateAsync(_peerUserId).ConfigureAwait(false);
        Assert.IsFalse(string.IsNullOrEmpty(sessionBefore), "The fixture must hold a session for this to prove anything.");

        var result = await _actingHigh!
            .UpdateAsync(
                _peerUserId,
                new UserAccountEdit(_peerSignInName, "Fixture", "Person", "0000", LowRoleCode, IsActive: false))
            .ConfigureAwait(false);

        Assert.IsTrue(result.IsSuccess, $"Switching the person off must land: {result.Message}");
        Assert.AreEqual(
            "0",
            await ScalarAsync(
                "SELECT CAST(is_active AS UNSIGNED) FROM core_users_profiles WHERE id = @p_user_id;",
                new Dictionary<string, object?> { ["p_user_id"] = _peerUserId }).ConfigureAwait(false),
            "The account is switched off.");

        Assert.AreEqual(
            sessionBefore,
            await SessionStateAsync(_peerUserId).ConfigureAwait(false),
            "Switching somebody off ends no session: a session already open stays open until they close the "
                + "application, which is what the confirmation says (FR-102, §6 gate 2).");
    }

    [TestMethod]
    public async Task UpdateAsync_OneSaveChangingSeveralFields_SharesOneChangeIdentifierAcrossItsAuditRows()
    {
        var lastIdBefore = await MaxAuditIdAsync().ConfigureAwait(false);
        var wrote = new Dictionary<string, object?> { ["p_user_id"] = _peerUserId, ["p_last_id"] = lastIdBefore };

        var result = await _actingHigh!
            .UpdateAsync(
                _peerUserId,
                new UserAccountEdit(_peerSignInName, "Corinne", "Corrected", "1234", LowRoleCode))
            .ConfigureAwait(false);

        Assert.IsTrue(result.IsSuccess, $"The save must land: {result.Message}");

        Assert.AreEqual(
            "4",
            await ScalarAsync(
                "SELECT COUNT(*) FROM auth_user_management_audit WHERE target_user_id = @p_user_id AND id > @p_last_id;",
                wrote).ConfigureAwait(false),
            "One row per field that actually changed: both name parts change, the derived display name follows "
                + "them, and the employee number changes with them (FR-110).");

        Assert.AreEqual(
            "1",
            await ScalarAsync(
                "SELECT COUNT(DISTINCT change_group_id) FROM auth_user_management_audit WHERE target_user_id = @p_user_id AND id > @p_last_id;",
                wrote).ConfigureAwait(false),
            "The save's records share ONE change identifier, so one act reads as one act (FR-110).");

        Assert.AreEqual(
            "0",
            await ScalarAsync(
                "SELECT COUNT(*) FROM auth_user_management_audit WHERE target_user_id = @p_user_id AND id > @p_last_id "
                    + "AND (actor_user_id <> @p_actor OR occurred_utc IS NULL);",
                new Dictionary<string, object?>
                {
                    ["p_user_id"] = _peerUserId,
                    ["p_last_id"] = lastIdBefore,
                    ["p_actor"] = _highUserId,
                }).ConfigureAwait(false),
            "Every record names the acting person and the time (FR-109).");
    }

    private IUserManagementService BuildService(long userId, string signInName, string roleCode) =>
        new UserManagementService(
            new UserManagementRepository(Options.Create(_options!)),
            new StartupState { UserId = userId, Username = signInName, CurrentRoleCode = roleCode });

    /// <summary>One search fact, and the peer found by it. The store is where the matching happens.</summary>
    private async Task AssertFindsThePeerAsync(string searchText, string what)
    {
        var found = await _actingHigh!.SearchAsync(searchText, roleCode: null).ConfigureAwait(false);

        Assert.IsTrue(
            found.Any(row => row.UserId == _peerUserId),
            $"Searching by {what} ('{searchText}') must find the person (FR-086).");
    }

    private async Task<long> CreateFixtureAsync(string signInName, string roleCode)
    {
        await _helper!.ExecuteSqlNonQueryAsync(
            "INSERT INTO core_users_profiles (public_id, username_normalized, first_name, last_name, password_hash, password_salt, require_password_change, temporary_credential_failed_attempts, display_name, employee_identifier, is_active, created_utc, updated_utc) "
                + "VALUES (UUID(), @p_sign_in, 'Fixture', 'Person', 'fixture-hash', @p_salt, 0, 0, 'Fixture Person', '0000', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP());",
            new Dictionary<string, object?> { ["p_sign_in"] = signInName, ["p_salt"] = new byte[32] },
            MySqlDatabaseTarget.MtmWaitlist).ConfigureAwait(false);

        var created = await ScalarAsync(
            "SELECT id FROM core_users_profiles WHERE username_normalized = @p_sign_in;",
            new Dictionary<string, object?> { ["p_sign_in"] = signInName }).ConfigureAwait(false);

        Assert.IsTrue(
            long.TryParse(created, NumberStyles.Integer, CultureInfo.InvariantCulture, out var userId),
            $"The fixture account must be in the store: {signInName} ({created}).");

        await _helper.ExecuteSqlNonQueryAsync(
            "INSERT INTO auth_roles_assignments (public_id, user_id, role_id, assigned_utc, assigned_by_user_id) "
                + "SELECT UUID(), @p_user_id, r.id, UTC_TIMESTAMP(), @p_user_id FROM auth_roles_catalog r WHERE r.role_code = @p_role_code;",
            new Dictionary<string, object?> { ["p_user_id"] = userId, ["p_role_code"] = roleCode },
            MySqlDatabaseTarget.MtmWaitlist).ConfigureAwait(false);

        return userId;
    }

    private async Task InsertSessionAsync(long userId)
    {
        await _helper!.ExecuteSqlNonQueryAsync(
            "INSERT INTO auth_sessions_tokens (public_id, user_id, computer_id, token_hash, token_salt, token_version, issued_utc, expires_utc, revoked_utc, is_active, source_label, created_utc) "
                + "VALUES (UUID(), @p_user_id, NULL, @p_token_hash, @p_token_salt, 1, UTC_TIMESTAMP(), @p_expires_utc, NULL, 1, 'login', UTC_TIMESTAMP());",
            new Dictionary<string, object?>
            {
                ["p_user_id"] = userId,
                ["p_token_hash"] = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant(),
                ["p_token_salt"] = new byte[32],
                ["p_expires_utc"] = DateTime.UtcNow.AddHours(4),
            },
            MySqlDatabaseTarget.MtmWaitlist).ConfigureAwait(false);
    }

    private async Task DeleteOwnedRowsAsync()
    {
        var parameters = new Dictionary<string, object?> { ["p_prefix"] = $"{OwnedSignInPrefix}%" };

        await _helper!.ExecuteSqlNonQueryAsync(
            "DELETE FROM auth_user_management_audit WHERE target_user_id IN (SELECT id FROM core_users_profiles WHERE username_normalized LIKE @p_prefix);"
                + " DELETE FROM auth_sessions_tokens WHERE user_id IN (SELECT id FROM core_users_profiles WHERE username_normalized LIKE @p_prefix);"
                + " DELETE ra FROM auth_roles_assignments ra INNER JOIN core_users_profiles u ON u.id = ra.user_id WHERE u.username_normalized LIKE @p_prefix;"
                + " DELETE FROM core_users_profiles WHERE username_normalized LIKE @p_prefix;",
            parameters,
            MySqlDatabaseTarget.MtmWaitlist).ConfigureAwait(false);
    }

    private Task<string> ProfileCountAsync(string signInName) =>
        ScalarAsync(
            "SELECT COUNT(*) FROM core_users_profiles WHERE username_normalized = @p_sign_in;",
            new Dictionary<string, object?> { ["p_sign_in"] = signInName });

    private Task<string> AssignmentCountAsync(string signInName) =>
        ScalarAsync(
            "SELECT COUNT(*) FROM auth_roles_assignments ra INNER JOIN core_users_profiles u ON u.id = ra.user_id WHERE u.username_normalized = @p_sign_in;",
            new Dictionary<string, object?> { ["p_sign_in"] = signInName });

    private async Task<long> AuditRowCountAsync(long userId)
    {
        var value = await ScalarAsync(
            "SELECT COUNT(*) FROM auth_user_management_audit WHERE target_user_id = @p_user_id;",
            new Dictionary<string, object?> { ["p_user_id"] = userId }).ConfigureAwait(false);

        return long.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// The stored state of every session one account holds, so "untouched" is compared against what was there
    /// rather than assumed.
    /// </summary>
    private Task<string> SessionStateAsync(long userId) =>
        ScalarAsync(
            "SELECT GROUP_CONCAT(CONCAT(is_active, '|', expires_utc) ORDER BY id ASC SEPARATOR ',') "
                + "FROM auth_sessions_tokens WHERE user_id = @p_user_id;",
            new Dictionary<string, object?> { ["p_user_id"] = userId });

    private async Task<long> MaxAuditIdAsync()
    {
        var value = await ScalarAsync(
            "SELECT COALESCE(MAX(id), 0) FROM auth_user_management_audit;",
            new Dictionary<string, object?>()).ConfigureAwait(false);

        return long.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
    }

    private async Task<string> ScalarAsync(string sql, IReadOnlyDictionary<string, object?> parameters)
    {
        var rows = await _helper!
            .ExecuteSqlQueryAsync(sql, parameters, MySqlDatabaseTarget.MtmWaitlist)
            .ConfigureAwait(false);

        return rows.Count == 0
            ? string.Empty
            : Convert.ToString(rows[0].Values.First(), CultureInfo.InvariantCulture) ?? string.Empty;
    }
}
