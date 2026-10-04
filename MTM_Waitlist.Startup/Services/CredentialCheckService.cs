using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Module_Startup.Services;

/// <summary>
/// The credential check: it confirms the credential a person presented against the store, and it holds an
/// account still on a temporary credential to five attempts, counted with the account so the limit survives a
/// restart (`contracts/identity-contracts.md`, `contracts/sql-contracts.md`; FR-012, SC-007).
/// </summary>
/// <remarks>
/// <para>
/// <b>The limit applies to temporary credentials only.</b> An ordinary account that mistypes its password is
/// refused and told so, and nothing is counted: the count exists because a temporary credential is short, shared
/// and read out loud, which is what makes repeated guessing a real risk rather than a mistyped key. Counting
/// ordinary failures too would lock an operator out of a machine over a typo (FR-012).
/// </para>
/// <para>
/// <b>The count is the store's, not this process's.</b> It is held on the account row and written through
/// <c>sp_auth_temporary_credential_attempt_record</c>, so closing and reopening the application does not start
/// the count again. That is what SC-007 asks for and what an in-memory counter could not deliver.
/// </para>
/// <para>
/// <b>The refusal is decided before the credential is compared.</b> Once the count has reached its limit the
/// attempt is refused whatever value is presented, including the correct one, because a limit that the right
/// answer resets is not a limit (FR-012).
/// </para>
/// <para>
/// <b>One read answers everything else.</b> The identity, the role in force, the two credential columns, the
/// temporary-state flag and the count all come back from <c>sp_auth_user_credential_get</c> in one round trip,
/// so a single attempt costs one read plus at most one small write.
/// </para>
/// <para>
/// <b>Neither the credential nor the hash is kept or recorded.</b> The presented value lives as a parameter for
/// the length of the comparison, and the row's credential columns are read into locals for the same span. Only
/// the account's name and the outcome are ever written to a log, and a store failure is recorded as a failure
/// rather than with its statement or its parameters (logging contract section 6).
/// </para>
/// </remarks>
internal sealed class CredentialCheckService
{
    /// <summary>
    /// How many failed attempts an account on a temporary credential is allowed before it is refused outright
    /// (FR-012, SC-007).
    /// </summary>
    internal const int MaximumTemporaryCredentialAttempts = 5;

    /// <summary>
    /// The read that returns one account's identity and the material that confirms its credential, keyed on the
    /// upper-normalised sign-in name. The same read serves the machine-setup gate, so the two cannot drift into
    /// asking different questions about the same row.
    /// </summary>
    private const string CredentialReadProcedure = "sp_auth_user_credential_get";

    /// <summary>The write that moves the account's wrong-attempt count up on a failure and back to zero on a success.</summary>
    private const string AttemptRecordProcedure = "sp_auth_temporary_credential_attempt_record";

    /// <summary>The area a store failure is recorded under, so the fault has a name beside it.</summary>
    private const string LogModule = "SignIn";

    private readonly IMySqlHelperServer _store;

    /// <summary>Creates the check over the stored-procedure seam, which is the only thing it needs.</summary>
    /// <param name="store">The stored-procedure seam every read and write goes through (constitution III).</param>
    public CredentialCheckService(IMySqlHelperServer store)
        => _store = store ?? throw new ArgumentNullException(nameof(store));

    /// <summary>
    /// Confirms the credential the person presented, and counts the attempt when the account is on a temporary
    /// credential.
    /// </summary>
    /// <param name="signInName">The sign-in name the person gave. It is normalised before the store is asked.</param>
    /// <param name="credential">The credential the person gave. It is compared and never kept.</param>
    /// <param name="cancellationToken">Cancels the store work when the launch's stated maximum passes.</param>
    /// <returns>
    /// An accepted answer carrying the person's identity and whether they must now choose a password, or a
    /// refusal carrying the reason as a token the surface says in the reader's own words.
    /// </returns>
    internal async Task<CredentialCheckResult> CheckAsync(
        string? signInName,
        string? credential,
        CancellationToken cancellationToken)
    {
        var normalizedName = NormalizeSignInName(signInName);

        if (normalizedName.Length == 0 || string.IsNullOrEmpty(credential))
        {
            // Nothing was given, so there is nothing to confirm and no read that could answer. Naming which of
            // the two is missing is the surface's job; the token only says one of them was.
            return CredentialCheckResult.Refuse(CredentialCheckRefusals.SignInRequired);
        }

        IReadOnlyList<Dictionary<string, object?>> rows;

        try
        {
            rows = await _store
                .ExecuteStoredProcedureQueryAsync(
                    CredentialReadProcedure,
                    new Dictionary<string, object?> { ["p_username"] = normalizedName },
                    MySqlDatabaseTarget.MtmWaitlist,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Abandoning the launch is not a refused credential, so it is not reported as one.
            throw;
        }
        catch (Exception exception)
        {
            // A check that could not be made has confirmed nothing. The refusal names the state rather than the
            // provider's message, because a provider's sentence is not something to put on a sign-in screen.
            AppLog.Error(LogModule, exception, "The sign-in could not be confirmed because the store did not answer.");

            return CredentialCheckResult.Refuse(CredentialCheckRefusals.StoreUnreadable);
        }

        if (rows.Count == 0)
        {
            // An unknown name and a wrong credential are answered alike on purpose: telling them apart would say
            // which sign-in names this store holds, and the person is asked to check both either way.
            return CredentialCheckResult.Refuse(CredentialCheckRefusals.CredentialRefused);
        }

        var account = rows[0];
        var userId = ReadInt64(account, "id");

        if (userId <= 0)
        {
            // A row naming no account cannot be signed in as, whatever it holds.
            AppLog.Info(LogModule, "The store answered a sign-in without naming an account, so the sign-in was refused.");

            return CredentialCheckResult.Refuse(CredentialCheckRefusals.CredentialRefused);
        }

        var onTemporaryCredential = ReadBool(account, "require_password_change");

        if (onTemporaryCredential && ReadInt64(account, "temporary_credential_failed_attempts") >= MaximumTemporaryCredentialAttempts)
        {
            // The limit is checked before the credential is compared, so the attempt is refused even when the
            // value presented is the right one. A limit the correct answer resets is not a limit (FR-012).
            AppLog.Info(LogModule, "A temporary credential has reached its attempt limit, so the sign-in was refused.");

            return CredentialCheckResult.Refuse(CredentialCheckRefusals.AttemptsExhausted);
        }

        var matched = PasswordSecretHasher.Verify(
            credential,
            ReadString(account, "password_hash"),
            ReadBytes(account, "password_salt"));

        if (onTemporaryCredential)
        {
            // Only the temporary state is counted, and the count is written before the answer is returned so a
            // process that ends on the failed attempt cannot lose it (FR-012, SC-007).
            await RecordAttemptAsync(userId, matched, cancellationToken).ConfigureAwait(false);
        }

        if (!matched)
        {
            return CredentialCheckResult.Refuse(CredentialCheckRefusals.CredentialRefused);
        }

        return CredentialCheckResult.Accept(
            userId,
            normalizedName,
            ReadString(account, "display_name"),
            NullableString(account, "employee_identifier"),
            ReadString(account, "role_code"),
            requiresNewPassword: onTemporaryCredential);
    }

    /// <summary>
    /// Finds the account a sign-in name belongs to, without comparing a credential and without counting an
    /// attempt.
    /// </summary>
    /// <param name="signInName">The sign-in name the person gave. It is normalised before the store is asked.</param>
    /// <param name="cancellationToken">Cancels the store read.</param>
    /// <returns>The account's identifier, or zero when the store holds no active account under that name.</returns>
    /// <remarks>
    /// The remembered-sign-in read is keyed by person and machine, and the sign-in form has only a name when the
    /// person asks whether this machine remembers them. This answers that one question and nothing else: it does
    /// not confirm a credential, does not count an attempt and does not sign anybody in, so it cannot be used to
    /// skip the check that follows. A store that cannot answer yields zero, which the caller reports as a
    /// refused sign-in rather than as a signed-in one.
    /// </remarks>
    internal async Task<long> FindAccountIdAsync(string? signInName, CancellationToken cancellationToken)
    {
        var normalizedName = NormalizeSignInName(signInName);

        if (normalizedName.Length == 0)
        {
            return 0;
        }

        var rows = await _store
            .ExecuteStoredProcedureQueryAsync(
                CredentialReadProcedure,
                new Dictionary<string, object?> { ["p_username"] = normalizedName },
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken)
            .ConfigureAwait(false);

        return rows.Count == 0 ? 0 : ReadInt64(rows[0], "id");
    }

    /// <summary>
    /// Records the outcome of one attempt against a temporary credential, so the count rises on a failure and
    /// returns to zero on a success.
    /// </summary>
    /// <remarks>
    /// A store that refuses this write is reported and then ignored. The credential itself was still compared
    /// correctly, so failing the sign-in over an uncounted attempt would refuse a person whose credential was
    /// right, which is worse than losing the count (FR-012 takes the store's count as the record; a write that
    /// could not land means the next attempt reads the older count).
    /// </remarks>
    private async Task RecordAttemptAsync(long userId, bool wasSuccessful, CancellationToken cancellationToken)
    {
        try
        {
            await _store
                .ExecuteStoredProcedureNonQueryAsync(
                    AttemptRecordProcedure,
                    new Dictionary<string, object?>
                    {
                        ["p_user_id"] = userId,
                        ["p_was_successful"] = wasSuccessful ? 1 : 0,
                    },
                    MySqlDatabaseTarget.MtmWaitlist,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            AppLog.Error(LogModule, exception, "A temporary credential attempt could not be counted against the account.");
        }
    }

    /// <summary>
    /// The sign-in name in the form the store compares: trimmed and upper case, which is how
    /// <c>username_normalized</c> is seeded and how every other read of that column normalises it.
    /// </summary>
    private static string NormalizeSignInName(string? signInName)
        => signInName?.Trim().ToUpperInvariant() ?? string.Empty;

    /// <summary>One column of a row as text, or empty when the store answered nothing for it.</summary>
    private static string ReadString(IReadOnlyDictionary<string, object?> row, string columnName)
        => NullableString(row, columnName) ?? string.Empty;

    /// <summary>One column of a row as text, or <c>null</c> when the store answered nothing for it.</summary>
    private static string? NullableString(IReadOnlyDictionary<string, object?> row, string columnName)
    {
        if (!row.TryGetValue(columnName, out var value) || value is null or DBNull)
        {
            return null;
        }

        var text = Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)?.Trim();

        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    /// <summary>One column of a row as a whole number, or zero when the store answered nothing for it.</summary>
    private static long ReadInt64(IReadOnlyDictionary<string, object?> row, string columnName)
    {
        if (!row.TryGetValue(columnName, out var value) || value is null or DBNull)
        {
            return 0;
        }

        // TINYINT(1) columns arrive as a bool from the provider, and converting a bool to a number throws, so the
        // type is guarded before the conversion rather than caught after it.
        if (value is bool boolean)
        {
            return boolean ? 1 : 0;
        }

        if (value is long number)
        {
            return number;
        }

        return long.TryParse(
            Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture),
            System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture,
            out var parsed)
            ? parsed
            : 0;
    }

    /// <summary>One column of a row as a flag, or <c>false</c> when the store answered nothing for it.</summary>
    private static bool ReadBool(IReadOnlyDictionary<string, object?> row, string columnName)
        => ReadInt64(row, columnName) == 1;

    /// <summary>
    /// One column of a row as the bytes a salt is stored in, or <c>null</c> when there is none.
    /// </summary>
    /// <remarks>
    /// The store holds the salt as <c>VARBINARY(32)</c>, and a provider may hand it back as a byte array or as
    /// the base64 text of one. Both are the same salt, and a salt that could not be read is not the same answer
    /// as a credential that did not match.
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
}

/// <summary>
/// What the credential check answered: either the person it confirmed, or the reason it refused (FR-012).
/// </summary>
/// <param name="IsAccepted">
/// True only when the credential matched and, for a temporary credential, the attempt limit had not been
/// reached. False is never "carry on": the launch must not resolve a role or open the shell on it.
/// </param>
/// <param name="RequiresNewPassword">
/// True when the account is still on a temporary credential, so the person must choose a new password before
/// anything else (FR-013). It is only ever true on an accepted answer.
/// </param>
/// <param name="RefusalReason">One of <see cref="CredentialCheckRefusals"/> when the check refused, otherwise <c>null</c>.</param>
/// <param name="UserId">The account's identifier, or zero when the check refused.</param>
/// <param name="SignInName">The normalised sign-in name the account was found by, or empty when it refused.</param>
/// <param name="DisplayName">The name to show the person, or empty when the check refused.</param>
/// <param name="EmployeeNumber">The employee identifier, or <c>null</c> when the store holds none.</param>
/// <param name="RoleCode">The role in force for this sign-in, or empty when the check refused.</param>
internal sealed record CredentialCheckResult(
    bool IsAccepted,
    bool RequiresNewPassword,
    string? RefusalReason,
    long UserId,
    string SignInName,
    string DisplayName,
    string? EmployeeNumber,
    string RoleCode)
{
    /// <summary>The answer for a credential that matched.</summary>
    /// <param name="userId">The account's identifier.</param>
    /// <param name="signInName">The normalised sign-in name the account was found by.</param>
    /// <param name="displayName">The name to show the person.</param>
    /// <param name="employeeNumber">The employee identifier, or <c>null</c> when the store holds none.</param>
    /// <param name="roleCode">The role in force for this sign-in.</param>
    /// <param name="requiresNewPassword">Whether the account is still on a temporary credential (FR-013).</param>
    internal static CredentialCheckResult Accept(
        long userId,
        string signInName,
        string displayName,
        string? employeeNumber,
        string roleCode,
        bool requiresNewPassword)
        => new(
            IsAccepted: true,
            RequiresNewPassword: requiresNewPassword,
            RefusalReason: null,
            UserId: userId,
            SignInName: signInName,
            DisplayName: displayName,
            EmployeeNumber: employeeNumber,
            RoleCode: roleCode);

    /// <summary>The answer for a credential that did not stand up, carrying only the reason.</summary>
    /// <param name="refusalReason">One of <see cref="CredentialCheckRefusals"/>.</param>
    internal static CredentialCheckResult Refuse(string refusalReason)
        => new(
            IsAccepted: false,
            RequiresNewPassword: false,
            RefusalReason: refusalReason,
            UserId: 0,
            SignInName: string.Empty,
            DisplayName: string.Empty,
            EmployeeNumber: null,
            RoleCode: string.Empty);
}

/// <summary>
/// The four answers the credential check can refuse with, as tokens rather than sentences, so the sign-in
/// surface says them in the reader's words and the token itself never moves when the wording does.
/// </summary>
internal static class CredentialCheckRefusals
{
    /// <summary>No sign-in name or no credential was given, so there is nothing to confirm.</summary>
    internal const string SignInRequired = "sign_in_required";

    /// <summary>The name and the credential were not a pair this store holds, or the name is unknown to it.</summary>
    internal const string CredentialRefused = "credential_refused";

    /// <summary>The account is on a temporary credential and its five attempts are spent (FR-012, SC-007).</summary>
    internal const string AttemptsExhausted = "attempts_exhausted";

    /// <summary>The store could not answer, so nothing was confirmed and nothing may be admitted.</summary>
    internal const string StoreUnreadable = "store_unreadable";
}
