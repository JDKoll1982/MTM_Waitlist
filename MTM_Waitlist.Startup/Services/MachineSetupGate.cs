using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Permissions;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Module_Startup.Services;

/// <summary>
/// The gate that unlocks machine setup: it authenticates one person itself and answers whether that person may
/// configure this computer (`contracts/machine-configuration-contract.md` section 2; FR-007).
/// </summary>
/// <remarks>
/// <para>
/// <b>It authorises configuration and nothing else.</b> The answer it returns carries no launch outcome and no
/// route, so there is no way for a caller to read the shell out of it. That is what makes FR-007's second half
/// structural rather than a promise: the type cannot hand anybody to the main screens.
/// </para>
/// <para>
/// <b>It authenticates the person itself, and that is deliberate.</b> The launch pipeline's identity handling
/// answers who is signed in for the whole application, and machine setup runs before anybody is signed in. A
/// gate that resolved through <see cref="PersonIdentityService"/> would sign the person in, which is exactly what
/// FR-007 forbids. So the gate reads the account row and the permission rows for the name it was given, and
/// leaves the launch's own identity untouched.
/// </para>
/// <para>
/// <b>The authority is the permission key, not a role spelled in code.</b> The answer comes from
/// <see cref="PermissionKeys.SettingsMachineConfiguration"/>, whose baseline is the two roles that hold it,
/// <c>role:it_department</c> and <c>role:developer</c>. What those roles may do therefore changes in the store
/// rather than in a second copy of the rule here.
/// </para>
/// <para>
/// <b>Every refusal is stated, and every failure refuses.</b> A sign-in that cannot be confirmed, an account the
/// store does not hold, a credential that does not match and a person without the permission all answer with a
/// stated reason rather than raising, because the surface has to tell the person what to do next (FR-007). A
/// store that cannot answer refuses too: a gate that cannot check authority has not found any.
/// </para>
/// </remarks>
internal interface IMachineSetupGate
{
    /// <summary>
    /// Whether the named person may configure this computer.
    /// </summary>
    /// <param name="signInName">The sign-in name the person gave, which the store holds upper-normalised.</param>
    /// <param name="credential">The credential the person gave. It is checked and never kept.</param>
    /// <param name="cancellationToken">Cancels the store reads.</param>
    /// <returns>
    /// The verdict, the person it belongs to when it authorises, and the reason it refused when it does not.
    /// </returns>
    Task<MachineSetupAuthorization> AuthorizeAsync(
        string signInName,
        string credential,
        CancellationToken cancellationToken);
}

/// <summary>
/// What the gate answered: whether configuration is authorised, who it is authorised for, and why not when it is
/// not (FR-007).
/// </summary>
/// <param name="IsAuthorized">
/// True only when the credential was confirmed and the person holds
/// <see cref="PermissionKeys.SettingsMachineConfiguration"/>. False is never the same as "carry on": the caller
/// must not offer the capture form on it.
/// </param>
/// <param name="Person">
/// The person the authorisation belongs to, or <c>null</c> when it refused. It is not the launch's identity and
/// never becomes it: the person is not signed in to the application by configuring a computer (FR-007).
/// </param>
/// <param name="RefusalReason">
/// One of <see cref="MachineSetupRefusals"/> when the sign-in was refused, otherwise <c>null</c>. It is a token
/// rather than a sentence, so the surface says the refusal in the reader's own words (FR-007).
/// </param>
internal sealed record MachineSetupAuthorization(
    bool IsAuthorized,
    IPersonIdentity? Person,
    string? RefusalReason);

/// <summary>
/// The four answers the gate can refuse with, as tokens rather than sentences, so the surface says them in the
/// reader's words and the token itself never moves when the wording does.
/// </summary>
internal static class MachineSetupRefusals
{
    /// <summary>No sign-in name or no credential was given, so there is nothing to confirm.</summary>
    public const string SignInRequired = "sign_in_required";

    /// <summary>The name and the credential were not a pair the store holds.</summary>
    public const string CredentialRefused = "credential_refused";

    /// <summary>The person signed in, and does not hold authority to configure this computer.</summary>
    public const string NotPermitted = "not_permitted";

    /// <summary>The store could not answer, so the sign-in could not be confirmed and is refused.</summary>
    public const string StoreUnreadable = "store_unreadable";
}

/// <inheritdoc />
/// <remarks>
/// <para>
/// <b>Two reads, and both are named rather than written here</b> (constitution III). The account read returns the
/// identity and the two credential columns for one upper-normalised sign-in name; the permission read returns
/// the rows stored for that person and for their role, which <see cref="StoredPermissionAnswers"/> composes into
/// an answer in the order the requirements state.
/// </para>
/// <para>
/// <b>The credential is compared with the one hasher both other paths use.</b>
/// <see cref="PasswordSecretHasher"/> is the single implementation of how a secret is turned into a hash and how
/// a presented secret is checked against one, so a temporary credential issued from the user-management screen
/// is accepted here without a second copy of the arithmetic.
/// </para>
/// <para>
/// <b>The account read must exist for setup to unlock.</b> The retired credential read
/// (<c>sp_auth_credentials_check</c>) was deleted with the rest of the startup-only procedures, and no surviving
/// read returns a credential column: <c>sp_auth_user_row_get</c> deliberately omits them so an identity read
/// never pulls a hash into memory. Until <c>sp_auth_user_credential_get</c> is added, this gate refuses every
/// sign-in with a stated reason rather than authorising anybody it could not check.
/// </para>
/// </remarks>
internal sealed class MachineSetupGate : IMachineSetupGate
{
    /// <summary>
    /// The read that returns one account's identity and the material that confirms its credential, keyed on the
    /// upper-normalised sign-in name.
    /// </summary>
    private const string CredentialReadProcedure = "sp_auth_user_credential_get";

    /// <summary>The module name a store failure is recorded under, so a refusal has its cause beside it.</summary>
    private const string LogModule = "MachineSetup";

    private readonly IMySqlHelperServer _mySqlHelperServer;

    /// <summary>Creates the gate over the store seam, which is the only thing it needs.</summary>
    /// <param name="mySqlHelperServer">The stored-procedure seam every read goes through (constitution III).</param>
    public MachineSetupGate(IMySqlHelperServer mySqlHelperServer)
    {
        _mySqlHelperServer = mySqlHelperServer ?? throw new ArgumentNullException(nameof(mySqlHelperServer));
    }

    /// <inheritdoc />
    public async Task<MachineSetupAuthorization> AuthorizeAsync(
        string signInName,
        string credential,
        CancellationToken cancellationToken)
    {
        var normalizedName = NormalizeSignInName(signInName);

        if (normalizedName.Length == 0 || string.IsNullOrEmpty(credential))
        {
            return Refuse(MachineSetupRefusals.SignInRequired);
        }

        IReadOnlyList<Dictionary<string, object?>> accountRows;

        try
        {
            accountRows = await _mySqlHelperServer
                .ExecuteStoredProcedureQueryAsync(
                    CredentialReadProcedure,
                    new Dictionary<string, object?> { ["p_username"] = normalizedName },
                    MySqlDatabaseTarget.MtmWaitlist,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Abandoning setup is not a refusal, so it is not recorded as one.
            throw;
        }
        catch (Exception exception)
        {
            // A check that could not be made has confirmed nothing, so the answer is a refusal with the cause
            // rather than an authorisation nothing stands behind. The cause is recorded and the refusal the
            // person reads names the state, because a provider's message is not something to put on a screen.
            AppLog.Error(LogModule, exception, "This computer's setup sign-in could not be confirmed by the store.");

            return Refuse(MachineSetupRefusals.StoreUnreadable);
        }

        if (accountRows.Count == 0)
        {
            // An unknown name and a wrong credential are answered alike on purpose: telling them apart would say
            // which sign-in names this store holds, and the person is asked to check both either way.
            return Refuse(MachineSetupRefusals.CredentialRefused);
        }

        var account = accountRows[0];

        if (!PasswordSecretHasher.Verify(credential, ReadString(account, "password_hash"), ReadBytes(account, "password_salt")))
        {
            return Refuse(MachineSetupRefusals.CredentialRefused);
        }

        var userId = ReadInt64(account, "id");

        if (userId <= 0)
        {
            // A credential that matched a row naming no account cannot happen in this store, and it cannot be
            // authorised if it does.
            AppLog.Info(LogModule, "The store confirmed a credential without naming an account, so setup was refused.");

            return Refuse(MachineSetupRefusals.CredentialRefused);
        }

        if (!await HoldsConfigurationPermissionAsync(userId, cancellationToken).ConfigureAwait(false))
        {
            return Refuse(MachineSetupRefusals.NotPermitted);
        }

        return new MachineSetupAuthorization(
            IsAuthorized: true,
            Person: new MachineSetupPerson(
                userId,
                normalizedName,
                ReadString(account, "display_name"),
                ReadString(account, "employee_identifier"),
                ReadString(account, "role_code")),
            RefusalReason: null);
    }

    /// <summary>
    /// Whether the account holds the authority to configure a computer, read from the permission key whose
    /// baseline is the two roles that hold it.
    /// </summary>
    /// <remarks>
    /// The read is the one the whole application uses for permissions, keyed on the account rather than on the
    /// signed-in identity, because setup runs before anybody is signed in. A store that cannot answer composes to
    /// no rows, and every key then falls back to its shipped answer, which for this key is false: an unreadable
    /// store refuses the sign-in rather than handing out authority from an outage.
    /// </remarks>
    private async Task<bool> HoldsConfigurationPermissionAsync(long userId, CancellationToken cancellationToken)
    {
        var rows = await _mySqlHelperServer
            .ExecuteStoredProcedureQueryAsync(
                StoredPermissionAnswers.StoredPermissionsProcedure,
                new Dictionary<string, object?> { ["p_user_id"] = userId },
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken)
            .ConfigureAwait(false);

        var answers = StoredPermissionAnswers.Compose(rows);

        return StoredPermissionAnswers.AnswerFor(PermissionKeys.SettingsMachineConfiguration, answers);
    }

    /// <summary>A refusal with its reason stated, and no person attached to it.</summary>
    private static MachineSetupAuthorization Refuse(string refusalReason)
        => new(IsAuthorized: false, Person: null, RefusalReason: refusalReason);

    /// <summary>
    /// The sign-in name in the form the store compares: trimmed and upper case, which is how
    /// <c>username_normalized</c> is seeded and how the retired credential read's caller normalised it.
    /// </summary>
    private static string NormalizeSignInName(string? signInName)
        => signInName?.Trim().ToUpperInvariant() ?? string.Empty;

    /// <summary>One column of a row as text, or empty when the store answered nothing for it.</summary>
    private static string ReadString(IReadOnlyDictionary<string, object?> row, string columnName)
        => row.TryGetValue(columnName, out var value) && value is not null and not DBNull
            ? Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)?.Trim() ?? string.Empty
            : string.Empty;

    /// <summary>One column of a row as a whole number, or zero when the store answered nothing for it.</summary>
    private static long ReadInt64(IReadOnlyDictionary<string, object?> row, string columnName)
    {
        if (!row.TryGetValue(columnName, out var value) || value is null or DBNull)
        {
            return 0;
        }

        return value switch
        {
            long number => number,
            int number => number,
            _ => long.TryParse(
                Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture),
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out var parsed)
                ? parsed
                : 0,
        };
    }

    /// <summary>One column of a row as the bytes a salt is stored in, or <c>null</c> when there is none.</summary>
    /// <remarks>
    /// The store holds the salt as <c>VARBINARY(32)</c>, and a provider may hand it back as a byte array or as
    /// the base64 text of one. Both forms are accepted, because a salt that could not be read is not the same
    /// answer as a credential that did not match, and only one of the two is the person's to fix.
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
/// Who the gate authorised, for reading and for showing, and never a sign-in (FR-007, FR-022).
/// </summary>
/// <remarks>
/// <para>
/// <b>It is not the launch's identity and it does not become it.</b> The launch resolves who is signed in and
/// writes that through <see cref="PersonIdentityService"/>; this type answers the same read-only contract for a
/// person who is authorised to configure a computer and is deliberately signed in to nothing.
/// </para>
/// <para>
/// <b>The held set is the role in force.</b> The account read returns the one role the store puts in force, and
/// that is the role the permission key's baseline was read for, so <see cref="Holds"/> answers for the role the
/// authorisation was actually granted against rather than for a set nobody read.
/// </para>
/// </remarks>
internal sealed class MachineSetupPerson : IPersonIdentity
{
    private readonly string _roleCode;

    internal MachineSetupPerson(
        long userId,
        string signInName,
        string displayName,
        string? employeeNumber,
        string? roleCode)
    {
        UserId = userId;
        SignInName = signInName;
        DisplayName = displayName;
        EmployeeNumber = string.IsNullOrWhiteSpace(employeeNumber) ? null : employeeNumber.Trim();
        _roleCode = NormalizeRoleCode(roleCode);
    }

    /// <inheritdoc />
    public long UserId { get; }

    /// <inheritdoc />
    public string SignInName { get; }

    /// <inheritdoc />
    public string DisplayName { get; }

    /// <inheritdoc />
    public string? EmployeeNumber { get; }

    /// <inheritdoc />
    public string CurrentRoleCode => _roleCode;

    /// <inheritdoc />
    public IReadOnlyList<string> HeldRoleCodes => _roleCode.Length == 0 ? [] : [_roleCode];

    /// <inheritdoc />
    /// <remarks>
    /// Always false: configuring a computer never signs anybody in (FR-007).
    /// </remarks>
    public bool IsSignedIn => false;

    /// <inheritdoc />
    public bool Holds(string roleCode)
    {
        var normalized = NormalizeRoleCode(roleCode);

        return normalized.Length > 0
            && string.Equals(normalized, _roleCode, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>A role code with an optional <c>role:</c> scope-key prefix removed, as the catalogue holds it.</summary>
    private static string NormalizeRoleCode(string? roleCode)
        => PersonIdentityService.NormalizeRoleCode(roleCode);
}
