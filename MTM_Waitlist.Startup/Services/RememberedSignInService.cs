using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Module_Startup.Services;

/// <summary>
/// The shared key file a remembered sign-in is encrypted with, as the one thing a test needs to stand in for.
/// </summary>
/// <remarks>
/// The seam exists because the real file lives on a UNC share that a test host cannot be assumed to reach, and
/// because the failure this feature cares about most is the file being unreadable: a test has to be able to
/// produce that state deliberately rather than hope for it.
/// </remarks>
internal interface ISharedKeySource
{
    /// <summary>
    /// The key the shared file holds.
    /// </summary>
    /// <returns>The key bytes, or <c>null</c> when the file cannot be read or does not hold a usable key.</returns>
    byte[]? ReadKey();
}

/// <inheritdoc />
/// <remarks>
/// <para>
/// <b>Read at the UNC path, never at a mapped drive letter.</b> The drive letter is a per-user mapping that
/// resolves differently for a different account or a service, so a path built on it would work for whoever
/// tested it and fail for everybody else. The full path is the same file for every caller.
/// </para>
/// <para>
/// <b>The file is read and never written.</b> It is shared with other applications' authentication and its
/// rotation has a blast radius well outside this feature, so this type has no write path at all. A missing key
/// is a fall-back to the ordinary sign-in form, never a reason to create one (FR-014, decision D13).
/// </para>
/// <para>
/// <b>The key is never logged, never shown and never written down.</b> It is read into memory, used for one
/// encryption or decryption, and the caller holds it for that span alone. The only value derived from it that is
/// ever stored is its fingerprint, which is a one-way hash (logging contract section 6).
/// </para>
/// </remarks>
internal sealed class SharedKeyFileSource : ISharedKeySource
{
    /// <summary>
    /// The shared key file's full path. It is the documented UNC path and not a drive-letter shortcut
    /// (decision D13).
    /// </summary>
    internal const string KeyFilePath =
        @"\\mtmanu-fs01\Expo Drive\Software Development\Live Applications\MTM_Application_Keys\MTM_AUTH_USER_SECRET_KEY.txt";

    /// <summary>The key length AES-256 expects, and the length the file's contents decode to.</summary>
    internal const int KeyLengthBytes = 32;

    /// <inheritdoc />
    public byte[]? ReadKey()
    {
        string text;

        try
        {
            if (!File.Exists(KeyFilePath))
            {
                return null;
            }

            text = File.ReadAllText(KeyFilePath).Trim();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            // The share being away, the account lacking rights and the path being unusable are one answer here:
            // the key could not be read. They are deliberately not distinguished, because a sign-in that fell
            // back to the ordinary form must not depend on why.
            return null;
        }

        if (text.Length == 0)
        {
            return null;
        }

        try
        {
            var key = Convert.FromBase64String(text);

            return key.Length == KeyLengthBytes ? key : null;
        }
        catch (FormatException)
        {
            // A file that does not hold base64 is not a key, and pretending it were one would fail later in a
            // place that could not explain itself.
            return null;
        }
    }
}

/// <summary>
/// The remembered sign-in: it stores a person's choice to be recognised on one machine as an encrypted payload,
/// reads it back where the key allows, and falls back to the ordinary sign-in form where it does not
/// (`contracts/sql-contracts.md` section 2; FR-014, decisions D12 and D13).
/// </summary>
/// <remarks>
/// <para>
/// <b>It is held against the person in the store, per machine.</b> The row is keyed by person and machine, which
/// is what makes the choice follow the person to the machine they made it on rather than to every machine
/// (FR-014). It survives a sign-out, which is its whole purpose: a session is cleared on sign-out and this is
/// not a session.
/// </para>
/// <para>
/// <b>An unreadable key is a fall-back, never a failure.</b> Every path that cannot obtain the key, and every
/// payload that cannot be decrypted, records the fault and answers "nothing is held". The person sees the
/// ordinary sign-in form, and nothing is written to the machine in the key's place (FR-014, the spec's edge
/// case).
/// </para>/// <para>
/// <b>The payload is authenticated as well as encrypted.</b> It is sealed with an authenticated cipher rather
/// than plain CBC, so a payload that has been altered is refused outright instead of being reported as a
/// padding difference. That matters because the fall-back is visible to whoever presented the row, and a
/// visible difference between "padding failed" and "padding was fine" is what turns unauthenticated CBC into a
/// decryption oracle./// <para>
/// <b>A rotated key is detected rather than discovered by a failed decrypt.</b> The fingerprint of the key that
/// encrypted a payload is stored beside it, so a key that has changed is recognised before the ciphertext is
/// touched and is answered with the sign-in form rather than with an exception.
/// </para>
/// <para>
/// <b>The payload, the key and the plaintext are never logged.</b> A fault is recorded as what could not be
/// read, and never with the bytes involved (logging contract section 6).
/// </para>
/// </remarks>
internal sealed class RememberedSignInService
{
    /// <summary>The read that hands back the payload stored for one person on one machine.</summary>
    private const string GetProcedure = "sp_auth_remembered_sign_ins_get";

    /// <summary>The write that stores or replaces a remembered sign-in.</summary>
    private const string UpsertProcedure = "sp_auth_remembered_sign_ins_upsert";

    /// <summary>The write that forgets one, destroying the payload it held.</summary>
    private const string ClearProcedure = "sp_auth_remembered_sign_ins_clear";

    /// <summary>The area a fault is recorded under, so the fall-back has a name beside it.</summary>
    private const string LogModule = "RememberedSignIn";

    /// <summary>The nonce length the authenticated cipher requires, and the length written to the vector column.</summary>
    private const int NonceLengthBytes = 12;

    /// <summary>The authentication tag length, carried behind the ciphertext in the payload column.</summary>
    private const int TagLengthBytes = 16;

    private readonly IMySqlHelperServer _store;
    private readonly ISharedKeySource _keySource;

    /// <summary>Creates the service over the store seam and the key source, which are the only two things it needs.</summary>
    /// <param name="store">The stored-procedure seam every read and write goes through (constitution III).</param>
    /// <param name="keySource">Where the shared key is read from, read only.</param>
    public RememberedSignInService(IMySqlHelperServer store, ISharedKeySource keySource)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _keySource = keySource ?? throw new ArgumentNullException(nameof(keySource));
    }

    /// <summary>
    /// Reads the remembered sign-in stored for this person on this machine and unlocks it.
    /// </summary>
    /// <param name="userId">The person's account identifier.</param>
    /// <param name="computerId">The machine's registry identifier.</param>
    /// <param name="cancellationToken">Cancels the store read.</param>
    /// <returns>
    /// The sign-in that was held, or an answer saying why none was. Every non-held answer is a fall-back to the
    /// ordinary form rather than a stop (FR-014).
    /// </returns>
    /// <remarks>
    /// The person must already be known for this read to be made: the store keys a remembered sign-in by person
    /// and machine, and the launch has no person before the sign-in form has been answered. A remembered
    /// sign-in is therefore read once the name is known, and it is what lets the form complete the sign-in
    /// without asking for the credential again.
    /// </remarks>
    internal async Task<RememberedSignIn> TryReadAsync(
        long userId,
        long computerId,
        CancellationToken cancellationToken)
    {
        if (userId <= 0 || computerId <= 0)
        {
            return RememberedSignIn.NotHeld(RememberedSignInOutcome.NoneStored);
        }

        var rows = await _store
            .ExecuteStoredProcedureQueryAsync(
                GetProcedure,
                new Dictionary<string, object?>
                {
                    ["p_user_id"] = userId,
                    ["p_computer_id"] = computerId,
                },
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken)
            .ConfigureAwait(false);

        if (rows.Count == 0)
        {
            return RememberedSignIn.NotHeld(RememberedSignInOutcome.NoneStored);
        }

        var row = rows[0];
        var ciphertext = ReadBytes(row, "payload_ciphertext");
        var nonce = ReadBytes(row, "payload_iv");
        var storedFingerprint = ReadString(row, "key_fingerprint");

        if (ciphertext is null || nonce is null || ciphertext.Length == 0 || nonce.Length == 0)
        {
            // A row that does not carry both halves cannot be decrypted by anybody, so there is nothing to try.
            AppLog.Info(LogModule, "A remembered sign-in was found without both of the values it is unlocked with, so the form will ask.");

            return RememberedSignIn.NotHeld(RememberedSignInOutcome.PayloadUnreadable);
        }

        var key = _keySource.ReadKey();

        if (key is null)
        {
            // The share is away or the account cannot see it. This is the fault FR-014 names, and it is recorded
            // here so a fall-back that happens on every machine can be told from a person who never chose it.
            AppLog.Info(LogModule, "The shared key could not be read, so the remembered sign-in was left locked and the form will ask.");

            return RememberedSignIn.NotHeld(RememberedSignInOutcome.KeyUnreadable);
        }

        if (!string.Equals(Fingerprint(key), storedFingerprint, StringComparison.OrdinalIgnoreCase))
        {
            // The key has been rotated since this payload was written, so the ciphertext is unreadable by design
            // and there is no point attempting it.
            AppLog.Info(LogModule, "The shared key has changed since the remembered sign-in was stored, so the form will ask.");

            return RememberedSignIn.NotHeld(RememberedSignInOutcome.KeyRotated);
        }

        try
        {
            var payload = Decrypt(ciphertext, nonce, key);

            return string.IsNullOrWhiteSpace(payload.SignInName) || payload.Secret is null
                ? RememberedSignIn.NotHeld(RememberedSignInOutcome.PayloadUnreadable)
                : new RememberedSignIn(RememberedSignInOutcome.Held, payload.SignInName, payload.Secret);
        }
        catch (Exception exception) when (exception is CryptographicException or JsonException or FormatException)
        {
            AppLog.Error(LogModule, exception, "A remembered sign-in could not be unlocked, so the form will ask.");

            return RememberedSignIn.NotHeld(RememberedSignInOutcome.PayloadUnreadable);
        }
    }

    /// <summary>
    /// Stores the person's choice to be recognised on this machine, replacing whatever was stored before.
    /// </summary>
    /// <param name="userId">The person's account identifier.</param>
    /// <param name="computerId">The machine's registry identifier.</param>
    /// <param name="signInName">The name to present on the next launch.</param>
    /// <param name="secret">The credential to present on the next launch.</param>
    /// <param name="cancellationToken">Cancels the store write.</param>
    /// <returns>
    /// Whether the choice was stored. False means the key could not be read, and in that case nothing at all was
    /// kept: storing a payload that cannot be unlocked, or keeping the value on the machine instead, is exactly
    /// what FR-014 forbids.
    /// </returns>
    internal async Task<bool> RememberAsync(
        long userId,
        long computerId,
        string signInName,
        string secret,
        CancellationToken cancellationToken)
    {
        if (userId <= 0 || computerId <= 0 || string.IsNullOrWhiteSpace(signInName) || string.IsNullOrEmpty(secret))
        {
            return false;
        }

        var key = _keySource.ReadKey();

        if (key is null)
        {
            AppLog.Info(LogModule, "The shared key could not be read, so nothing was remembered and nothing was kept on this machine.");

            return false;
        }

        var (ciphertext, nonce) = Encrypt(signInName, secret, key);

        var affected = await _store
            .ExecuteStoredProcedureNonQueryAsync(
                UpsertProcedure,
                new Dictionary<string, object?>
                {
                    ["p_public_id"] = Guid.NewGuid().ToString("D"),
                    ["p_user_id"] = userId,
                    ["p_computer_id"] = computerId,
                    ["p_payload_ciphertext"] = ciphertext,
                    ["p_payload_iv"] = nonce,
                    ["p_key_fingerprint"] = Fingerprint(key),
                },
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken)
            .ConfigureAwait(false);

        if (affected < 1)
        {
            AppLog.Info(LogModule, "The store accepted no remembered sign-in row, so the choice does not apply to this machine.");

            return false;
        }

        AppLog.Info(LogModule, "This machine now remembers the person's sign-in, encrypted with the shared key.");

        return true;
    }

    /// <summary>
    /// Forgets the remembered sign-in for this person on this machine, destroying the payload it held.
    /// </summary>
    /// <param name="userId">The person's account identifier.</param>
    /// <param name="computerId">The machine's registry identifier.</param>
    /// <param name="cancellationToken">Cancels the store write.</param>
    /// <returns>Always true: nothing remembered is not a failure to forget something.</returns>
    internal async Task<bool> ForgetAsync(long userId, long computerId, CancellationToken cancellationToken)
    {
        if (userId <= 0 || computerId <= 0)
        {
            return true;
        }

        await _store
            .ExecuteStoredProcedureNonQueryAsync(
                ClearProcedure,
                new Dictionary<string, object?>
                {
                    ["p_user_id"] = userId,
                    ["p_computer_id"] = computerId,
                },
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken)
            .ConfigureAwait(false);

        return true;
    }

    /// <summary>
    /// The one-way fingerprint of the key, which is what a stored row carries so a rotated key can be detected.
    /// </summary>
    /// <param name="key">The key bytes, which are never stored.</param>
    /// <returns>Lower-case hexadecimal SHA-256 over the key, which is the <c>CHAR(64)</c> the column holds.</returns>
    private static string Fingerprint(byte[] key)
        => Convert.ToHexString(SHA256.HashData(key)).ToLowerInvariant();

    /// <summary>Encrypts the sign-in for one key, with a fresh nonce for every write.</summary>
    /// <param name="signInName">The name to present on the next launch.</param>
    /// <param name="secret">The credential to present on the next launch.</param>
    /// <param name="key">The shared key the payload is to be bound to.</param>
    /// <returns>
    /// The ciphertext with its authentication tag behind it, and the nonce it was produced with. The store keeps
    /// the two side by side, in the payload and vector columns the contract names.
    /// </returns>
    /// <remarks>
    /// <b>An authenticated mode, not plain CBC.</b> Microsoft's guidance on CBC-mode decryption is explicit that
    /// unauthenticated CBC with verifiable padding is attackable: an attacker who can change the ciphertext and
    /// see whether the padding failed can decrypt it. Encrypting and then authenticating with a genuine
    /// authenticated mode removes that path, so a tampered payload is refused outright rather than reported as a
    /// padding difference. The alternative, CBC with a separate keyed MAC, would need somewhere to keep the MAC,
    /// and the table's shape has one payload column and one vector column.
    /// </remarks>
    private static (byte[] Ciphertext, byte[] Nonce) Encrypt(string signInName, string secret, byte[] key)
    {
        var plaintext = JsonSerializer.SerializeToUtf8Bytes(new RememberedSignInPayload(signInName, secret));
        var nonce = RandomNumberGenerator.GetBytes(NonceLengthBytes);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagLengthBytes];

        using var aes = new AesGcm(key, TagLengthBytes);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        // The tag travels with the payload rather than in a column of its own, so a reader that has the row has
        // everything it needs to check and unlock it.
        return ([.. ciphertext, .. tag], nonce);
    }

    /// <summary>Unlocks one stored payload with the current key, refusing anything the tag does not cover.</summary>
    /// <param name="ciphertext">The stored ciphertext with its authentication tag behind it.</param>
    /// <param name="nonce">The nonce it was produced with.</param>
    /// <param name="key">The shared key, already confirmed to be the one the payload was bound to.</param>
    /// <returns>What the payload held.</returns>
    /// <remarks>
    /// A nonce of the wrong length and a payload with no room for a tag are both refused before the cipher is
    /// touched, so a row written by anything other than this writer falls back rather than raising.
    /// </remarks>
    private static RememberedSignInPayload Decrypt(byte[] ciphertext, byte[] nonce, byte[] key)
    {
        if (nonce.Length != NonceLengthBytes || ciphertext.Length <= TagLengthBytes)
        {
            return new RememberedSignInPayload(string.Empty, null);
        }

        var payload = ciphertext.AsSpan(0, ciphertext.Length - TagLengthBytes);
        var tag = ciphertext.AsSpan(ciphertext.Length - TagLengthBytes);
        var plaintext = new byte[payload.Length];

        using var aes = new AesGcm(key, TagLengthBytes);
        aes.Decrypt(nonce, payload, tag, plaintext);

        return JsonSerializer.Deserialize<RememberedSignInPayload>(plaintext)
            ?? new RememberedSignInPayload(string.Empty, null);
    }

    /// <summary>One column of a row as text, or empty when the store answered nothing for it.</summary>
    private static string ReadString(IReadOnlyDictionary<string, object?> row, string columnName)
    {
        if (!row.TryGetValue(columnName, out var value) || value is null or DBNull)
        {
            return string.Empty;
        }

        return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// One column of a row as bytes, or <c>null</c> when the store answered nothing for it.
    /// </summary>
    /// <remarks>
    /// The two byte columns are <c>VARBINARY</c>, and a provider may hand them back as a byte array or as the
    /// base64 text of one, so both forms are accepted.
    /// </remarks>
    private static byte[]? ReadBytes(IReadOnlyDictionary<string, object?> row, string columnName)
    {
        if (!row.TryGetValue(columnName, out var value) || value is null or DBNull)
        {
            return null;
        }

        if (value is byte[] bytes)
        {
            return bytes.Length > 0 ? bytes : null;
        }

        var text = Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);

        try
        {
            return string.IsNullOrWhiteSpace(text) ? null : Convert.FromBase64String(text);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    /// <summary>What the encrypted payload holds, and nothing else.</summary>
    /// <param name="SignInName">The name to present on the next launch.</param>
    /// <param name="Secret">The credential to present on the next launch.</param>
    private sealed record RememberedSignInPayload(string SignInName, string? Secret);
}

/// <summary>
/// What the remembered sign-in read answered: either a sign-in that could be presented, or why none could be
/// (FR-014).
/// </summary>
/// <param name="Outcome">Which of the five answers the read produced.</param>
/// <param name="SignInName">The name the payload held, or <c>null</c> when nothing was held.</param>
/// <param name="Secret">The credential the payload held, or <c>null</c> when nothing was held.</param>
internal sealed record RememberedSignIn(
    RememberedSignInOutcome Outcome,
    string? SignInName,
    string? Secret)
{
    /// <summary>The answer for a read that held nothing, carrying only the reason.</summary>
    /// <param name="outcome">Why nothing was held.</param>
    internal static RememberedSignIn NotHeld(RememberedSignInOutcome outcome) => new(outcome, null, null);
}

/// <summary>
/// The five answers the remembered-sign-in read can give. Every one but <see cref="Held"/> falls back to the
/// ordinary sign-in form, and the four are kept apart so the fault can be recorded with its true cause.
/// </summary>
internal enum RememberedSignInOutcome
{
    /// <summary>A sign-in was read and unlocked, and can be presented.</summary>
    Held,

    /// <summary>Nothing was ever stored for this person on this machine, and nothing has been cleared.</summary>
    NoneStored,

    /// <summary>The shared key file could not be read, which is the fault FR-014 names.</summary>
    KeyUnreadable,

    /// <summary>The key has changed since the payload was written, so the payload is no longer readable.</summary>
    KeyRotated,

    /// <summary>The row was found and could not be unlocked, or did not carry both halves of the payload.</summary>
    PayloadUnreadable,
}
