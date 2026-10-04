using System.Security.Cryptography;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Startup.Services;

namespace MTM_Waitlist.Tests.Module_Startup.Services;

/// <summary>
/// FR-014: the remembered sign-in is held in the store and encrypted, and a failure to read what protects it
/// falls back to the ordinary sign-in form, with the fault recorded and nothing kept on the machine in its place.
/// </summary>
/// <remarks>
/// <para>
/// Both doubles are hand-written. The key source stands in for the shared file, because the real one lives on a
/// share no test host can be assumed to reach and because "the key cannot be read" is the state this feature
/// cares about most and has to be producible deliberately.
/// </para>
/// <para>
/// "Nothing is kept on the machine in its place" is asserted as an absence of writes, which is the only form of
/// it a test can see: a fall-back that wrote something would have called the store's writer.
/// </para>
/// </remarks>
[TestClass]
public sealed class RememberedSignInServiceTests
{
    private const string GetProcedure = "sp_auth_remembered_sign_ins_get";
    private const string UpsertProcedure = "sp_auth_remembered_sign_ins_upsert";
    private const string ClearProcedure = "sp_auth_remembered_sign_ins_clear";

    private const long UserId = 7L;
    private const long ComputerId = 3L;
    private const string SignInName = "JKOLL";
    private const string Secret = "8391";

    [TestMethod]
    public async Task TryReadAsync_WhenTheKeyFileCannotBeRead_FallsBackAndKeepsNothingOnThisMachine()
    {
        // Arrange: a payload is stored, and the share holding the key is unreachable.
        var store = new StubRememberedSignInStore().WithRow(Guid.NewGuid().ToString("D"), [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17], Nonce(), new string('a', 64));

        var service = new RememberedSignInService(store, new StubKeySource(key: null));

        // Act
        var remembered = await service.TryReadAsync(UserId, ComputerId, CancellationToken.None);

        // Assert: the ordinary form is what the person gets, and nothing was written in the key's place
        // (FR-014).
        Assert.AreEqual(RememberedSignInOutcome.KeyUnreadable, remembered.Outcome);
        Assert.IsNull(remembered.SignInName);
        Assert.IsNull(remembered.Secret);
        Assert.AreEqual(0, store.CallCount(UpsertProcedure), "a fall-back stores nothing");
        Assert.AreEqual(0, store.CallCount(ClearProcedure));
    }

    [TestMethod]
    public async Task TryReadAsync_WhenTheKeyHasBeenRotated_FallsBackWithoutAttemptingTheCiphertext()
    {
        // Arrange: the row carries the fingerprint of a key that is no longer the one on the share.
        var currentKey = Key(1);
        var oldKey = Key(2);

        var store = new StubRememberedSignInStore()
            .WithRow(Guid.NewGuid().ToString("D"), [9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9], Nonce(), Fingerprint(oldKey));

        var service = new RememberedSignInService(store, new StubKeySource(currentKey));

        // Act
        var remembered = await service.TryReadAsync(UserId, ComputerId, CancellationToken.None);

        // Assert
        Assert.AreEqual(RememberedSignInOutcome.KeyRotated, remembered.Outcome);
        Assert.IsNull(remembered.Secret);
    }

    [TestMethod]
    public async Task TryReadAsync_WhenNothingWasEverStored_ReportsThatRatherThanAFault()
    {
        // Arrange
        var service = new RememberedSignInService(new StubRememberedSignInStore(), new StubKeySource(Key(1)));

        // Act
        var remembered = await service.TryReadAsync(UserId, ComputerId, CancellationToken.None);

        // Assert: nothing stored is the ordinary state of a machine nobody asked to remember them.
        Assert.AreEqual(RememberedSignInOutcome.NoneStored, remembered.Outcome);
    }

    [TestMethod]
    public async Task TryReadAsync_WhenTheCiphertextCannotBeUnlocked_FallsBackRatherThanThrowing()
    {
        // Arrange: the key matches its fingerprint and the bytes are not a payload it produced.
        var key = Key(3);
        var store = new StubRememberedSignInStore()
            .WithRow(Guid.NewGuid().ToString("D"), [4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20], Nonce(), Fingerprint(key));

        var service = new RememberedSignInService(store, new StubKeySource(key));

        // Act
        var remembered = await service.TryReadAsync(UserId, ComputerId, CancellationToken.None);

        // Assert: a payload nobody can unlock is the fall-back FR-014 describes, not an exception.
        Assert.AreEqual(RememberedSignInOutcome.PayloadUnreadable, remembered.Outcome);
        Assert.IsNull(remembered.Secret);
    }

    [TestMethod]
    public async Task RememberAsync_ThenTryReadAsync_HandsTheSameSignInBack()
    {
        // Arrange
        var key = Key(4);
        var store = new StubRememberedSignInStore();
        var service = new RememberedSignInService(store, new StubKeySource(key));

        // Act
        var stored = await service.RememberAsync(UserId, ComputerId, SignInName, Secret, CancellationToken.None);
        var remembered = await service.TryReadAsync(UserId, ComputerId, CancellationToken.None);

        // Assert
        Assert.IsTrue(stored, "the key could be read, so the choice was stored");
        Assert.AreEqual(RememberedSignInOutcome.Held, remembered.Outcome);
        Assert.AreEqual(SignInName, remembered.SignInName);
        Assert.AreEqual(Secret, remembered.Secret);
    }

    [TestMethod]
    public async Task RememberAsync_WritesNoPlaintextOfTheCredential()
    {
        // Arrange
        var key = Key(5);
        var store = new StubRememberedSignInStore();

        // Act
        await new RememberedSignInService(store, new StubKeySource(key))
            .RememberAsync(UserId, ComputerId, SignInName, Secret, CancellationToken.None);

        // Assert: the credential never reaches the store in a form anybody could read (FR-014).
        var parameters = store.LastParameters(UpsertProcedure);
        var ciphertext = (byte[])parameters["p_payload_ciphertext"]!;

        Assert.IsFalse(
            System.Text.Encoding.UTF8.GetString(ciphertext).Contains(Secret, StringComparison.Ordinal),
            "the stored payload must not carry the credential in the clear");
        Assert.AreEqual(
            Fingerprint(key),
            Convert.ToString(parameters["p_key_fingerprint"], System.Globalization.CultureInfo.InvariantCulture),
            "the row records which key it was bound to, so a rotated key can be detected");
    }

    [TestMethod]
    public async Task RememberAsync_WhenTheKeyCannotBeRead_StoresNothingAndSaysSo()
    {
        // Arrange
        var store = new StubRememberedSignInStore();

        // Act
        var stored = await new RememberedSignInService(store, new StubKeySource(key: null))
            .RememberAsync(UserId, ComputerId, SignInName, Secret, CancellationToken.None);

        // Assert: a payload nobody could unlock is not written, and the value is not kept here instead.
        Assert.IsFalse(stored);
        Assert.AreEqual(0, store.CallCount(UpsertProcedure));
    }

    [TestMethod]
    public async Task ForgetAsync_ClearsWhatWasRemembered()
    {
        // Arrange
        var store = new StubRememberedSignInStore();

        // Act
        var forgotten = await new RememberedSignInService(store, new StubKeySource(Key(6)))
            .ForgetAsync(UserId, ComputerId, CancellationToken.None);

        // Assert
        Assert.IsTrue(forgotten);
        Assert.AreEqual(1, store.CallCount(ClearProcedure));
    }

    [TestMethod]
    public async Task TryReadAsync_WithoutAPersonOrAMachine_ReadsNothing()
    {
        // Arrange
        var store = new StubRememberedSignInStore();

        // Act
        var remembered = await new RememberedSignInService(store, new StubKeySource(Key(7)))
            .TryReadAsync(0, ComputerId, CancellationToken.None);

        // Assert
        Assert.AreEqual(RememberedSignInOutcome.NoneStored, remembered.Outcome);
        Assert.AreEqual(0, store.CallCount(GetProcedure));
    }

    /// <summary>A key of the length AES-256 expects, derived from one byte so two keys are never equal.</summary>
    private static byte[] Key(byte seed) => [.. Enumerable.Repeat(seed, SharedKeyFileSource.KeyLengthBytes)];

    /// <summary>A nonce of the length the authenticated cipher requires.</summary>
    private static byte[] Nonce() => [.. Enumerable.Repeat((byte)3, 12)];

    /// <summary>The one-way fingerprint of a key, as the service computes it for a stored row.</summary>
    private static string Fingerprint(byte[] key)
        => Convert.ToHexString(SHA256.HashData(key)).ToLowerInvariant();

    /// <summary>A key source that answers with what it was built with, so "the file cannot be read" is producible.</summary>
    private sealed class StubKeySource : ISharedKeySource
    {
        private readonly byte[]? _key;

        public StubKeySource(byte[]? key) => _key = key;

        public byte[]? ReadKey() => _key;
    }

    /// <summary>
    /// A store that keeps one remembered-sign-in row, so a write can be read back as the store would return it.
    /// </summary>
    private sealed class StubRememberedSignInStore : IMySqlHelperServer
    {
        private readonly List<(string Procedure, IReadOnlyDictionary<string, object?> Parameters)> _calls = [];

        private Dictionary<string, object?>? _row;

        public StubRememberedSignInStore WithRow(string publicId, byte[] ciphertext, byte[] nonce, string fingerprint)
        {
            _row = new Dictionary<string, object?>
            {
                ["public_id"] = publicId,
                ["payload_ciphertext"] = ciphertext,
                ["payload_iv"] = nonce,
                ["key_fingerprint"] = fingerprint,
                ["updated_utc"] = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc),
            };

            return this;
        }

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
        {
            Record(storedProcedureName, parameters);

            return Task.FromResult<IReadOnlyList<Dictionary<string, object?>>>(
                string.Equals(storedProcedureName, GetProcedure, StringComparison.Ordinal) && _row is not null
                    ? [_row]
                    : []);
        }

        public Task<int> ExecuteStoredProcedureNonQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
        {
            Record(storedProcedureName, parameters);

            if (string.Equals(storedProcedureName, UpsertProcedure, StringComparison.Ordinal))
            {
                WithRow(
                    Convert.ToString(parameters["p_public_id"], System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
                    (byte[])parameters["p_payload_ciphertext"]!,
                    (byte[])parameters["p_payload_iv"]!,
                    Convert.ToString(parameters["p_key_fingerprint"], System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty);
            }

            if (string.Equals(storedProcedureName, ClearProcedure, StringComparison.Ordinal))
            {
                _row = null;
            }

            return Task.FromResult(1);
        }

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteSqlQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException("No statement text is written in the remembered-sign-in path (constitution III).");

        public Task<int> ExecuteSqlNonQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException("No statement text is written in the remembered-sign-in path (constitution III).");

        public int CallCount(string storedProcedureName)
            => _calls.Count(call => string.Equals(call.Procedure, storedProcedureName, StringComparison.Ordinal));

        public IReadOnlyDictionary<string, object?> LastParameters(string storedProcedureName)
            => _calls.Last(call => string.Equals(call.Procedure, storedProcedureName, StringComparison.Ordinal)).Parameters;

        private void Record(string storedProcedureName, IReadOnlyDictionary<string, object?> parameters)
            => _calls.Add((storedProcedureName, parameters));
    }
}
