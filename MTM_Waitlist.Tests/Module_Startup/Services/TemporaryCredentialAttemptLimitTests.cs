using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Startup.Services;

namespace MTM_Waitlist.Tests.Module_Startup.Services;

/// <summary>
/// FR-012 and SC-007: an account holding a temporary credential is refused after five failed attempts, including
/// attempts presenting the correct value, and the refusal survives a restart.
/// </summary>
/// <remarks>
/// <para>
/// The double is a hand-written store, as the suite's conventions require, and it does the one thing the
/// requirement is about: it moves the account's wrong-attempt count the way
/// <c>sp_auth_temporary_credential_attempt_record</c> does. That is what makes "it stays refused across a
/// restart" provable rather than asserted: the second attempt is made by a <b>new</b> service over the same
/// account, so nothing the first service held in memory can be doing the refusing.
/// </para>
/// <para>
/// The credential is built with the same hasher the application uses, so a passing test proves the comparison
/// rather than a fake's idea of one.
/// </para>
/// </remarks>
[TestClass]
public sealed class TemporaryCredentialAttemptLimitTests
{
    private const string CredentialReadProcedure = "sp_auth_user_credential_get";
    private const string AttemptRecordProcedure = "sp_auth_temporary_credential_attempt_record";
    private const string SignInName = "JKoll";
    private const string Pin = "8391";

    /// <summary>One random salt for the class, so no case depends on a value written into the repository.</summary>
    private static readonly byte[] s_salt = PasswordSecretHasher.NewSalt();

    [TestMethod]
    public async Task CheckAsync_AfterFiveFailedAttempts_RefusesTheSixthEvenWithTheCorrectValue()
    {
        // Arrange: five failures have already been recorded against the account.
        var store = new StubAccountStore(TemporaryAccount(failedAttempts: 5));

        // Act
        var result = await Check(store).CheckAsync(SignInName, Pin, CancellationToken.None);

        // Assert
        Assert.IsFalse(result.IsAccepted, "the limit is spent, so the attempt is refused (FR-012)");
        Assert.AreEqual(CredentialCheckRefusals.AttemptsExhausted, result.RefusalReason);
        Assert.AreEqual(
            0,
            store.CallCount(AttemptRecordProcedure),
            "a refused attempt is not compared, so it is not counted either");
    }

    [TestMethod]
    public async Task CheckAsync_AfterARestart_StillRefusesTheSixthAttempt()
    {
        // Arrange: five failures, then a brand-new service over the same account, which is what a restart is.
        var account = TemporaryAccount();

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var refused = await Check(new StubAccountStore(account)).CheckAsync(SignInName, "0000", CancellationToken.None);
            Assert.AreEqual(CredentialCheckRefusals.CredentialRefused, refused.RefusalReason, $"attempt {attempt + 1} is a wrong credential");
        }

        // Act: a fresh service, as a restarted application would build.
        var afterRestart = await Check(new StubAccountStore(account)).CheckAsync(SignInName, Pin, CancellationToken.None);

        // Assert: the count lived with the account, not in the process (SC-007).
        Assert.IsFalse(afterRestart.IsAccepted, "the refusal survives a restart (SC-007)");
        Assert.AreEqual(CredentialCheckRefusals.AttemptsExhausted, afterRestart.RefusalReason);
    }

    [TestMethod]
    public async Task CheckAsync_WhenATemporaryCredentialIsRefused_RaisesTheCountAgainstTheAccount()
    {
        // Arrange
        var account = TemporaryAccount();
        var store = new StubAccountStore(account);

        // Act
        await Check(store).CheckAsync(SignInName, "0000", CancellationToken.None);

        // Assert: the count moved on the account itself, which is what makes it survive a restart.
        Assert.AreEqual(1L, Convert.ToInt64(account["temporary_credential_failed_attempts"]));
        Assert.AreEqual(1, store.CallCount(AttemptRecordProcedure));
    }

    [TestMethod]
    public async Task CheckAsync_WhenATemporaryCredentialIsAccepted_ClearsTheCount()
    {
        // Arrange: four failures behind it, so the fifth attempt is still compared.
        var account = TemporaryAccount(failedAttempts: 4);

        // Act
        var result = await Check(new StubAccountStore(account)).CheckAsync(SignInName, Pin, CancellationToken.None);

        // Assert
        Assert.IsTrue(result.IsAccepted, "the fifth attempt is still within the limit and the credential is right");
        Assert.IsTrue(result.RequiresNewPassword, "the account is still on a temporary credential, so a new password is needed (FR-013)");
        Assert.AreEqual(0L, Convert.ToInt64(account["temporary_credential_failed_attempts"]), "an accepted attempt clears the count");
    }

    [TestMethod]
    public async Task CheckAsync_OnAnOrdinaryCredential_DoesNotCountAFailedAttempt()
    {
        // Arrange: the same account, no longer on a temporary credential.
        var account = TemporaryAccount();
        account["require_password_change"] = 0L;
        var store = new StubAccountStore(account);

        // Act
        var result = await Check(store).CheckAsync(SignInName, "0000", CancellationToken.None);

        // Assert: the limit is about short shared credentials, not about a mistyped password (FR-012).
        Assert.IsFalse(result.IsAccepted);
        Assert.AreEqual(CredentialCheckRefusals.CredentialRefused, result.RefusalReason);
        Assert.AreEqual(0, store.CallCount(AttemptRecordProcedure), "an ordinary credential is never counted");
        Assert.AreEqual(0L, Convert.ToInt64(account["temporary_credential_failed_attempts"]));
    }

    [TestMethod]
    public async Task CheckAsync_OnAnOrdinaryCredential_AcceptsAndAsksForNoNewPassword()
    {
        // Arrange
        var account = TemporaryAccount();
        account["require_password_change"] = 0L;

        // Act
        var result = await Check(new StubAccountStore(account)).CheckAsync(SignInName, Pin, CancellationToken.None);

        // Assert
        Assert.IsTrue(result.IsAccepted);
        Assert.IsFalse(result.RequiresNewPassword, "an ordinary credential is not a temporary one (FR-013)");
        Assert.AreEqual(7L, result.UserId);
        Assert.AreEqual("J. Koll", result.DisplayName);
    }

    [TestMethod]
    public async Task CheckAsync_WithAMixedCaseSignInName_SendsItUpperCasedToTheStore()
    {
        // Arrange: sign-in names are stored and compared upper case, so the check normalises before it asks.
        var store = new StubAccountStore(TemporaryAccount());

        // Act
        await Check(store).CheckAsync("  jkoll  ", Pin, CancellationToken.None);

        // Assert
        Assert.AreEqual("JKOLL", store.LastParameters(CredentialReadProcedure)["p_username"]);
    }

    private static CredentialCheckService Check(StubAccountStore store) => new(store);

    /// <summary>One account row in the column names the credential read returns.</summary>
    private static Dictionary<string, object?> TemporaryAccount(long failedAttempts = 0) => new()
    {
        ["id"] = 7L,
        ["role_code"] = "role:production",
        ["display_name"] = "J. Koll",
        ["employee_identifier"] = "1042",
        ["password_hash"] = PasswordSecretHasher.Hash(Pin, s_salt),
        ["password_salt"] = s_salt,
        ["require_password_change"] = 1L,
        ["temporary_credential_failed_attempts"] = failedAttempts,
    };

    /// <summary>
    /// A store that answers the credential read and moves the wrong-attempt count the way the store's own
    /// procedure does, so an account behaves here as it would in the database.
    /// </summary>
    private sealed class StubAccountStore : IMySqlHelperServer
    {
        private readonly Dictionary<string, object?> _account;
        private readonly List<string> _called = [];

        public StubAccountStore(Dictionary<string, object?> account) => _account = account;

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
        {
            Record(storedProcedureName, parameters);

            return Task.FromResult<IReadOnlyList<Dictionary<string, object?>>>(
                string.Equals(storedProcedureName, CredentialReadProcedure, StringComparison.Ordinal)
                    ? [_account]
                    : []);
        }

        public Task<int> ExecuteStoredProcedureNonQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
        {
            Record(storedProcedureName, parameters);

            if (string.Equals(storedProcedureName, AttemptRecordProcedure, StringComparison.Ordinal))
            {
                var wasSuccessful = Convert.ToInt64(parameters["p_was_successful"], System.Globalization.CultureInfo.InvariantCulture) == 1;

                _account["temporary_credential_failed_attempts"] = wasSuccessful
                    ? 0L
                    : Convert.ToInt64(_account["temporary_credential_failed_attempts"], System.Globalization.CultureInfo.InvariantCulture) + 1;
            }

            return Task.FromResult(1);
        }

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteSqlQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException("No statement text is written in the sign-in path (constitution III).");

        public Task<int> ExecuteSqlNonQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException("No statement text is written in the sign-in path (constitution III).");

        public int CallCount(string storedProcedureName)
            => _called.Count(name => string.Equals(name, storedProcedureName, StringComparison.Ordinal));

        public IReadOnlyDictionary<string, object?> LastParameters(string storedProcedureName)
            => _calls.Last(call => string.Equals(call.Procedure, storedProcedureName, StringComparison.Ordinal)).Parameters;

        private readonly List<(string Procedure, IReadOnlyDictionary<string, object?> Parameters)> _calls = [];

        private void Record(string storedProcedureName, IReadOnlyDictionary<string, object?> parameters)
        {
            _called.Add(storedProcedureName);
            _calls.Add((storedProcedureName, parameters));
        }
    }
}
