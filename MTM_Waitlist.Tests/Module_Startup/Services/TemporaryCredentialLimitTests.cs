using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Startup.Services;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Startup.Services;

/// <summary>
/// The attempt limit on a temporary credential, proved end to end against a live store (FR-031, FR-035 to
/// FR-039, FR-043, SC-008).
/// </summary>
/// <remarks>
/// <para>
/// <b>End to end means the real repository, the real procedure and a real account.</b> These cases drive
/// <see cref="StartupSessionRepository.CheckCredentialsAsync"/> and
/// <c>sp_user_management_reset_password</c> against the live <c>mtm_waitlist</c>, so what is proved is the
/// behaviour a person meets rather than a re-typed copy of it. The fixture accounts are this class's own: each
/// test writes one under a per-run name and removes it afterwards.
/// </para>
/// <para>
/// <b>What "the count survives a restart" means here.</b> The count lives on the account row, so a second
/// repository instance reads the same answer. That is asserted, and it is the whole of the claim: there is no
/// in-memory limit to survive.
/// </para>
/// <para>
/// <b>What "waiting does not restore it" means here, and what it does not.</b> Elapsed wall-clock time is not
/// simulated, because a test that slept for a day would not run. What is asserted instead is that nothing in the
/// path consults a clock: the account row holds no expiry column, and the credential read's own text carries no
/// time comparison for the count.
/// </para>
/// <para>
/// Environment-gated on <c>MTM_WAITLIST_TEST_DB_CONNECTION_STRING</c>: without a live <c>mtm_waitlist</c> every
/// case reports inconclusive, recorded as skipped and never as passing.
/// </para>
/// </remarks>
[TestClass]
public sealed class TemporaryCredentialLimitTests
{
    private const string ConnectionStringVariable = "MTM_WAITLIST_TEST_DB_CONNECTION_STRING";

    /// <summary>Prefix for every account this class writes, so a run's rows are recognisable and removable.</summary>
    private const string OwnedSignInPrefix = "ZZ.ATTEMPTLIMIT.";

    private const string CorrectPin = "4321";

    private const string WrongPin = "1111";

    private readonly List<string> _ownedSignInNames = [];

    private MySqlHelperServer? _helper;

    private StartupSessionRepository? _repository;

    private StartupDatabaseOptions? _options;

    private string _runId = string.Empty;

    [TestInitialize]
    public void TestInitialize()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Assert.Inconclusive($"{ConnectionStringVariable} is not set; skipping the live attempt-limit proof.");
        }

        _options = new StartupDatabaseOptions { ConnectionString = connectionString! };
        _helper = new MySqlHelperServer(Options.Create(_options));
        _repository = new StartupSessionRepository(Options.Create(_options));
        _runId = Guid.NewGuid().ToString("N")[..8];
    }

    [TestCleanup]
    public async Task TestCleanupAsync()
    {
        if (_helper is null || _ownedSignInNames.Count == 0)
        {
            return;
        }

        foreach (var signInName in _ownedSignInNames)
        {
            var parameters = new Dictionary<string, object?> { ["p_username"] = signInName };

            await _helper.ExecuteSqlNonQueryAsync(
                "DELETE FROM auth_user_management_audit WHERE target_user_id IN (SELECT id FROM core_users_profiles WHERE username_normalized = @p_username);"
                    + " DELETE FROM auth_sessions_tokens WHERE user_id IN (SELECT id FROM core_users_profiles WHERE username_normalized = @p_username);"
                    + " DELETE ra FROM auth_roles_assignments ra INNER JOIN core_users_profiles u ON u.id = ra.user_id WHERE u.username_normalized = @p_username;"
                    + " DELETE FROM core_users_profiles WHERE username_normalized = @p_username;",
                parameters,
                MySqlDatabaseTarget.MtmWaitlist);
        }
    }

    [TestMethod]
    public async Task CheckCredentialsAsync_AfterFiveWrongAttempts_RefusesTheSixthAttemptThatHoldsTheCorrectValue()
    {
        var account = await CreateTemporaryCredentialAccountAsync("five-wrong");

        for (var attempt = 1; attempt <= StartupSessionRepository.TemporaryCredentialAttemptLimit; attempt++)
        {
            var failed = await _repository!.CheckCredentialsAsync(account.SignInName, WrongPin);

            Assert.IsFalse(failed.IsAuthenticated, $"Attempt {attempt} holds the wrong value and must be refused.");
            Assert.IsTrue(failed.HoldsTemporaryCredential, "The account holds a temporary credential.");
            Assert.AreEqual(attempt, failed.TemporaryCredentialFailedAttempts, "Each wrong attempt moves the count by exactly one.");
            Assert.IsFalse(failed.TemporaryCredentialAttemptLimitReached, $"The limit is not reached after {attempt} of five.");
        }

        // The sixth attempt carries the RIGHT value and is still refused, because the limit is checked before the
        // value is compared (FR-031).
        var refused = await _repository!.CheckCredentialsAsync(account.SignInName, CorrectPin);

        Assert.IsFalse(refused.IsAuthenticated, "The sixth attempt must be refused even with the correct value.");
        Assert.IsTrue(refused.TemporaryCredentialAttemptLimitReached);
        Assert.AreEqual(
            StartupSessionRepository.TemporaryCredentialAttemptLimit,
            refused.TemporaryCredentialFailedAttempts,
            "A refused attempt does not move the count, which is already at its limit (FR-043).");
    }

    [TestMethod]
    public async Task CheckCredentialsAsync_TheCountSurvivesANewRepositoryInstance()
    {
        var account = await CreateTemporaryCredentialAccountAsync("restart");

        for (var attempt = 1; attempt <= StartupSessionRepository.TemporaryCredentialAttemptLimit; attempt++)
        {
            await _repository!.CheckCredentialsAsync(account.SignInName, WrongPin);
        }

        // A second repository stands in for a restarted application: nothing about the limit is held in memory.
        var afterRestart = new StartupSessionRepository(Options.Create(_options!));
        var refused = await afterRestart.CheckCredentialsAsync(account.SignInName, CorrectPin);

        Assert.IsFalse(refused.IsAuthenticated, "Closing and reopening the application must not restore the credential (FR-037).");
        Assert.IsTrue(refused.TemporaryCredentialAttemptLimitReached);
    }

    [TestMethod]
    public async Task NothingInThePathConsultsAClock_SoWaitingCannotRestoreTheCredential()
    {
        var account = await CreateTemporaryCredentialAccountAsync("no-expiry");

        for (var attempt = 1; attempt <= StartupSessionRepository.TemporaryCredentialAttemptLimit; attempt++)
        {
            await _repository!.CheckCredentialsAsync(account.SignInName, WrongPin);
        }

        // Nothing expires the count: the account row holds no expiry column to read.
        var expiryColumns = await _helper!.ExecuteSqlQueryAsync(
            "SELECT column_name FROM information_schema.columns "
                + "WHERE table_schema = DATABASE() AND table_name = 'core_users_profiles' "
                + "AND (column_name LIKE '%expir%' OR column_name LIKE '%reset%' OR column_name LIKE '%window%');",
            new Dictionary<string, object?>(),
            MySqlDatabaseTarget.MtmWaitlist);

        Assert.AreEqual(0, expiryColumns.Count, "The count must not expire with time, so no column may hold an expiry for it (FR-038).");

        // And the read itself computes no window: the count comes straight from the row.
        var credentialsRead = File.ReadAllText(Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "Database",
            "StoredProcedures",
            "sp_auth_credentials_check",
            "create.sql"));

        foreach (var windowConstruct in new[] { "INTERVAL", "DATE_SUB", "DATEDIFF", "NOW()" })
        {
            Assert.IsFalse(
                credentialsRead.Contains(windowConstruct, StringComparison.Ordinal),
                $"The credential read must not compute a window over the count ('{windowConstruct}'): nothing expires it (FR-038).");
        }

        Assert.AreEqual(
            StartupSessionRepository.TemporaryCredentialAttemptLimit,
            await StoredFailedAttemptCountAsync(account.SignInName),
            "The count is still where the failures left it (FR-038).");
    }

    [TestMethod]
    public async Task CheckCredentialsAsync_ASuccessfulSignInWithTheTemporaryValue_ClearsTheCount()
    {
        var account = await CreateTemporaryCredentialAccountAsync("cleared-by-success");

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            await _repository!.CheckCredentialsAsync(account.SignInName, WrongPin);
        }

        Assert.AreEqual(3, await StoredFailedAttemptCountAsync(account.SignInName));

        var succeeded = await _repository!.CheckCredentialsAsync(account.SignInName, CorrectPin);

        Assert.IsTrue(succeeded.IsAuthenticated, "The right value still works below the limit.");
        Assert.AreEqual(0, succeeded.TemporaryCredentialFailedAttempts);
        Assert.AreEqual(0, await StoredFailedAttemptCountAsync(account.SignInName), "A successful sign-in clears the count (FR-039).");
    }

    [TestMethod]
    public async Task CheckCredentialsAsync_TheLegacyMarkerValue_IsLimitedExactlyLikeAFreshPin()
    {
        var account = await CreateTemporaryCredentialAccountAsync("legacy-marker", legacyMarker: true);

        for (var attempt = 1; attempt <= StartupSessionRepository.TemporaryCredentialAttemptLimit; attempt++)
        {
            var failed = await _repository!.CheckCredentialsAsync(account.SignInName, WrongPin);

            Assert.IsFalse(failed.IsAuthenticated, $"Attempt {attempt} with the wrong value must be refused.");
            Assert.IsTrue(failed.HoldsTemporaryCredential, "The legacy marker is a temporary value (FR-031).");
        }

        var refused = await _repository!.CheckCredentialsAsync(account.SignInName, "0000");

        Assert.IsFalse(refused.IsAuthenticated, "The legacy value is limited exactly like a new one (FR-031).");
        Assert.IsTrue(refused.TemporaryCredentialAttemptLimitReached);
    }

    [TestMethod]
    public async Task CheckCredentialsAsync_ASignInNameThatDoesNotExist_IsRefusedWithoutMovingAnyCount()
    {
        var account = await CreateTemporaryCredentialAccountAsync("unknown-name");

        await _repository!.CheckCredentialsAsync(account.SignInName, WrongPin);

        var before = await StoredFailedAttemptCountAsync(account.SignInName);
        var unknown = await _repository!.CheckCredentialsAsync($"{OwnedSignInPrefix}{_runId}.nobody", WrongPin);

        Assert.IsFalse(unknown.IsAuthenticated);
        Assert.IsFalse(unknown.HoldsTemporaryCredential, "A name that does not exist holds no temporary credential, so the limit stays invisible (FR-042).");
        Assert.AreEqual(0, unknown.TemporaryCredentialFailedAttempts);
        Assert.AreEqual(before, await StoredFailedAttemptCountAsync(account.SignInName), "A failure against a name that does not exist moves no count (FR-042).");
    }

    [TestMethod]
    public async Task AFailedAttempt_WritesNoAuditRecordAndTheAnswerCarriesNoIdentityOfTheAttempter()
    {
        var account = await CreateTemporaryCredentialAccountAsync("no-trail");

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            var failed = await _repository!.CheckCredentialsAsync(account.SignInName, WrongPin);

            Assert.IsFalse(failed.IsAuthenticated);
            Assert.AreEqual(attempt, failed.TemporaryCredentialFailedAttempts, "The answer carries the count and nothing about who tried (FR-043).");
        }

        // The count is a guard, not a trail: no audit row is written for a failed sign-in, and nothing in the
        // answer says who tried (FR-043).
        var auditRows = await _helper!.ExecuteSqlQueryAsync(
            "SELECT COUNT(*) FROM auth_user_management_audit a INNER JOIN core_users_profiles u ON u.id = a.target_user_id WHERE u.username_normalized = @p_username;",
            new Dictionary<string, object?> { ["p_username"] = account.SignInName },
            MySqlDatabaseTarget.MtmWaitlist);

        Assert.AreEqual("0", Convert.ToString(auditRows.Single().Values.First()), "A failed sign-in writes no account-change record (FR-043).");
    }

    [TestMethod]
    public async Task ResetPassword_ClearsTheCountAndIssuesAWorkingValue()
    {
        var account = await CreateTemporaryCredentialAccountAsync("reset-clears");

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            await _repository!.CheckCredentialsAsync(account.SignInName, WrongPin);
        }

        const string freshPin = "9876";
        await ResetAsync(account.UserId, freshPin);

        Assert.AreEqual(0, await StoredFailedAttemptCountAsync(account.SignInName), "A fresh reset clears the count (FR-039).");

        var withTheNewValue = await _repository!.CheckCredentialsAsync(account.SignInName, freshPin);

        Assert.IsTrue(withTheNewValue.IsAuthenticated, "The freshly issued value works (SC-008).");
        Assert.IsTrue(withTheNewValue.RequiresPasswordChange, "The new value must still be changed at the next sign-in (FR-029).");

        // And the superseded PIN no longer works, so a PIN that was handed over cannot still be used (FR-032).
        var withTheOldValue = await _repository.CheckCredentialsAsync(account.SignInName, CorrectPin);

        Assert.IsFalse(withTheOldValue.IsAuthenticated, "A second reset replaces the earlier PIN (FR-032).");
    }

    [TestMethod]
    public async Task ResetPassword_LeavesAnOpenSessionRunning()
    {
        var account = await CreateTemporaryCredentialAccountAsync("reset-keeps-session");
        var sessionExpiry = DateTime.UtcNow.AddHours(4);

        await _helper!.ExecuteSqlNonQueryAsync(
            "INSERT INTO auth_sessions_tokens (public_id, user_id, computer_id, token_hash, token_salt, token_version, issued_utc, expires_utc, revoked_utc, is_active, source_label, created_utc) "
                + "VALUES (UUID(), @p_user_id, NULL, @p_token_hash, @p_token_salt, 1, UTC_TIMESTAMP(), @p_expires_utc, NULL, 1, 'login', UTC_TIMESTAMP());",
            new Dictionary<string, object?>
            {
                ["p_user_id"] = account.UserId,
                ["p_token_hash"] = new string('a', 64),
                ["p_token_salt"] = new byte[32],
                ["p_expires_utc"] = sessionExpiry,
            },
            MySqlDatabaseTarget.MtmWaitlist);

        await ResetAsync(account.UserId, "9876");

        var rows = await _helper.ExecuteSqlQueryAsync(
            "SELECT is_active, expires_utc FROM auth_sessions_tokens WHERE user_id = @p_user_id;",
            new Dictionary<string, object?> { ["p_user_id"] = account.UserId },
            MySqlDatabaseTarget.MtmWaitlist);

        Assert.AreEqual(1, rows.Count, "A reset ends no session (FR-035).");
        Assert.AreEqual(
            "True",
            Convert.ToString(rows.Single()["is_active"]),
            "The session is still active after the reset, so the person stays signed in until they close the application (FR-035).");
    }

    /// <summary>
    /// Writes one masked account holding a temporary credential, and gives it the worker rung so the developer
    /// actor used by <see cref="ResetAsync"/> outranks it.
    /// </summary>
    private async Task<Account> CreateTemporaryCredentialAccountAsync(string tag, bool legacyMarker = false)
    {
        var signInName = $"{OwnedSignInPrefix}{_runId}.{tag}";
        var salt = new byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(salt);

        // The legacy marker is stored as the literal `0000`, exactly as the accounts that still hold it are: that
        // value is what makes the read treat the account as temporary.
        var passwordHash = legacyMarker ? "0000" : PasswordSecretHasher.Hash(CorrectPin, salt);

        await _helper!.ExecuteSqlNonQueryAsync(
            "INSERT INTO core_users_profiles (public_id, username_normalized, first_name, last_name, password_hash, password_salt, require_password_change, temporary_credential_failed_attempts, display_name, employee_identifier, is_active, created_utc, updated_utc) "
                + "VALUES (UUID(), @p_username, 'Attempt', 'Limit', @p_password_hash, @p_password_salt, 1, 0, 'Attempt Limit', '0099', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP());",
            new Dictionary<string, object?>
            {
                ["p_username"] = signInName,
                ["p_password_hash"] = passwordHash,
                ["p_password_salt"] = salt,
            },
            MySqlDatabaseTarget.MtmWaitlist);

        _ownedSignInNames.Add(signInName);

        var userIdRows = await _helper.ExecuteSqlQueryAsync(
            "SELECT id FROM core_users_profiles WHERE username_normalized = @p_username;",
            new Dictionary<string, object?> { ["p_username"] = signInName },
            MySqlDatabaseTarget.MtmWaitlist);

        var userId = Convert.ToInt64(userIdRows.Single()["id"], System.Globalization.CultureInfo.InvariantCulture);

        await _helper.ExecuteSqlNonQueryAsync(
            "INSERT INTO auth_roles_assignments (public_id, user_id, role_id, assigned_utc, assigned_by_user_id) "
                + "SELECT UUID(), @p_user_id, r.id, UTC_TIMESTAMP(), @p_user_id FROM auth_roles_catalog r WHERE r.role_code = 'setup';",
            new Dictionary<string, object?> { ["p_user_id"] = userId },
            MySqlDatabaseTarget.MtmWaitlist);

        return new Account(userId, signInName);
    }

    /// <summary>
    /// Issues a fresh PIN for one account through the shipped reset procedure, acted by a developer from the
    /// store so the rank rule admits the change.
    /// </summary>
    private async Task ResetAsync(long userId, string pin)
    {
        var actorRows = await _helper!.ExecuteSqlQueryAsync(
            "SELECT ra.user_id FROM auth_roles_assignments ra INNER JOIN auth_roles_catalog r ON r.id = ra.role_id "
                + "WHERE r.role_code = 'developer' ORDER BY ra.user_id ASC LIMIT 1;",
            new Dictionary<string, object?>(),
            MySqlDatabaseTarget.MtmWaitlist);

        Assert.AreEqual(1, actorRows.Count, "The store must hold a developer to act as the resetting user.");

        var actorUserId = Convert.ToInt64(actorRows.Single()["user_id"], System.Globalization.CultureInfo.InvariantCulture);

        var salt = new byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(salt);

        await _helper.ExecuteStoredProcedureNonQueryAsync(
            "sp_user_management_reset_password",
            new Dictionary<string, object?>
            {
                ["p_user_id"] = userId,
                ["p_actor_user_id"] = actorUserId,
                ["p_password_hash"] = PasswordSecretHasher.Hash(pin, salt),
                ["p_password_salt"] = salt,
                ["p_change_group_id"] = Guid.NewGuid().ToString(),
            },
            MySqlDatabaseTarget.MtmWaitlist);
    }

    private async Task<int> StoredFailedAttemptCountAsync(string signInName)
    {
        var rows = await _helper!.ExecuteSqlQueryAsync(
            "SELECT temporary_credential_failed_attempts FROM core_users_profiles WHERE username_normalized = @p_username;",
            new Dictionary<string, object?> { ["p_username"] = signInName },
            MySqlDatabaseTarget.MtmWaitlist);

        return Convert.ToInt32(rows.Single().Values.First(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private sealed record Account(long UserId, string SignInName);
}
