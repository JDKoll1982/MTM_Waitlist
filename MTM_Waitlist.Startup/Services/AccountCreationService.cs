using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Startup.ViewModels;

namespace MTM_Waitlist.Module_Startup.Services;

/// <summary>
/// What one self set-up captured, handed to the creation call as one argument so neither PIN value is ever a
/// property of the screen (`contracts/account-creation-contract.md` §4; FR-008).
/// </summary>
/// <param name="EmployeeNumber">The employee number the plant system confirmed.</param>
/// <param name="FirstName">The confirmed first name, as the person left it.</param>
/// <param name="LastName">The confirmed last name, as the person left it.</param>
/// <param name="Pin">The PIN the person chose.</param>
/// <param name="Confirmation">The same PIN typed a second time.</param>
public sealed record AccountSetupDraft(
    string EmployeeNumber,
    string FirstName,
    string LastName,
    string Pin,
    string Confirmation);

/// <summary>
/// What the creation call answered (`contracts/account-creation-contract.md` §§3-4; FR-009, FR-010, FR-045).
/// </summary>
public sealed record AccountSetupResult
{
    /// <summary>Whether the profile, the credential and the rank were all written.</summary>
    public required bool IsCreated { get; init; }

    /// <summary>The sign-in name the account was created with, present only on a created account.</summary>
    public string? SignInName { get; init; }

    /// <summary>The name shown for the account, present only on a created account.</summary>
    public string? DisplayName { get; init; }

    /// <summary>The employee number the account carries, present only on a created account.</summary>
    public string? EmployeeNumber { get; init; }

    /// <summary>The rank the store assigned, present only on a created account.</summary>
    public string? RoleCode { get; init; }

    /// <summary>A plain-language reason when nothing was created.</summary>
    public string? FailureReason { get; init; }

    /// <summary>The account was created, on the PIN the person chose.</summary>
    internal static AccountSetupResult Created(string signInName, string displayName, string employeeNumber, string roleCode)
        => new()
        {
            IsCreated = true,
            SignInName = signInName,
            DisplayName = displayName,
            EmployeeNumber = employeeNumber,
            RoleCode = roleCode,
        };

    /// <summary>Nothing was created, and the reason says why.</summary>
    internal static AccountSetupResult Failed(string reason) => new() { IsCreated = false, FailureReason = reason };
}

/// <summary>
/// Creates a self set-up account through the one stored procedure that writes the profile, the credential and the
/// rank together (`contracts/account-creation-contract.md`; FR-009, FR-010, FR-034, FR-045, SC-011).
/// </summary>
/// <remarks>
/// <para>
/// <b>One call, one transaction.</b> The profile row, the credential and the rank assignment are written by
/// <c>sp_core_account_setup_create</c> in one transaction, so a failure anywhere leaves nothing behind — no person
/// with a credential and no rank, and no rank with no credential (SC-011).
/// </para>
/// <para>
/// <b>The PIN is hashed here and never reaches the store.</b> The application produces the salted hash and the
/// salt, and the procedure is handed only those: it never sees a PIN, and no plaintext is written anywhere.
/// </para>
/// <para>
/// <b>No rank is named.</b> The procedure reads the lowest rung from the store's own rank list by that list's own
/// ordering, and this service passes no rank in — a caller that could name its own rank could grant itself
/// anything (FR-010, FR-039).
/// </para>
/// <para>
/// <b>The chosen PIN is the credential from creation.</b> The procedure writes <c>require_password_change = 0</c>,
/// so the account never enters the temporary-credential state, never sees the five-attempt limit and never passes
/// through the new-PIN step (FR-009, FR-045).
/// </para>
/// </remarks>
internal sealed class AccountCreationService
{
    /// <summary>The one procedure that creates a self set-up account (FR-034).</summary>
    private const string CreateProcedure = "sp_core_account_setup_create";

    private const string LogModule = "SetUp";

    private readonly IMySqlHelperServer _store;

    /// <summary>Creates the service over the stored-procedure seam.</summary>
    /// <param name="store">The stored-procedure seam every read and write goes through (constitution III).</param>
    public AccountCreationService(IMySqlHelperServer store)
        => _store = store ?? throw new ArgumentNullException(nameof(store));

    /// <summary>
    /// Creates the account on the PIN the person chose, with the lowest rank the store holds.
    /// </summary>
    /// <param name="draft">The confirmed name, the employee number and the two typed PIN values.</param>
    /// <param name="cancellationToken">Cancels the write when the launch's stated maximum passes.</param>
    /// <returns>The created account, or a plain-language failure with nothing written.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="draft"/> is <c>null</c>.</exception>
    public async Task<AccountSetupResult> CreateAsync(AccountSetupDraft draft, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var employeeNumber = draft.EmployeeNumber?.Trim() ?? string.Empty;
        var firstName = draft.FirstName?.Trim() ?? string.Empty;
        var lastName = draft.LastName?.Trim() ?? string.Empty;

        if (employeeNumber.Length == 0 || firstName.Length == 0 || lastName.Length == 0)
        {
            return AccountSetupResult.Failed("A name and an employee number are needed before an account can be created.");
        }

        // The existing PIN rules, reused exactly: at least four characters, no upper bound, both values typed the
        // same way (FR-008). The floor is the one floor the product already enforces.
        if (string.IsNullOrEmpty(draft.Pin))
        {
            return AccountSetupResult.Failed("Choose a PIN before the account can be created.");
        }

        if (draft.Pin.Length < PasswordChangeViewModel.MinimumPasswordLength)
        {
            return AccountSetupResult.Failed(
                $"A PIN must be at least {PasswordChangeViewModel.MinimumPasswordLength} characters long.");
        }

        if (!string.Equals(draft.Pin, draft.Confirmation, StringComparison.Ordinal))
        {
            return AccountSetupResult.Failed("The two PINs do not match, so nothing was created.");
        }

        var salt = PasswordSecretHasher.NewSalt();
        var hash = PasswordSecretHasher.Hash(draft.Pin, salt);

        try
        {
            var rows = await _store
                .ExecuteStoredProcedureQueryAsync(
                    CreateProcedure,
                    new Dictionary<string, object?>
                    {
                        // The account's sign-in name is the employee number the plant system confirmed, so the
                        // person signs in with the number and the PIN they chose, which is the independent test
                        // US2 asks for.
                        ["p_username"] = employeeNumber,
                        ["p_first_name"] = firstName,
                        ["p_last_name"] = lastName,
                        ["p_employee_identifier"] = employeeNumber,
                        ["p_password_hash"] = hash,
                        ["p_password_salt"] = salt,
                    },
                    MySqlDatabaseTarget.MtmWaitlist,
                    cancellationToken)
                .ConfigureAwait(false);

            if (rows.Count == 0)
            {
                // The procedure answers the created row. No row means nothing can be confirmed, so the caller is
                // told the account was not created rather than shown a half-truth.
                return AccountSetupResult.Failed("The account was not created, so no PIN has been set.");
            }

            var created = rows[0];
            var signInName = ReadString(created, "username_normalized");
            var roleCode = ReadString(created, "role_code");

            AppLog.Info(
                LogModule,
                $"A self set-up account was created for employee '{employeeNumber}' on the PIN they chose.");

            return AccountSetupResult.Created(
                signInName.Length > 0 ? signInName : employeeNumber,
                ReadString(created, "display_name"),
                ReadString(created, "employee_identifier") is { Length: > 0 } stored ? stored : employeeNumber,
                roleCode);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Abandoning the launch is not a failed creation, so it is not reported as one.
            throw;
        }
        catch (Exception exception)
        {
            // The procedure is one transaction, so a failure here has left nothing behind. The reason names the
            // remedy rather than the provider's message, which is not something to put in front of a person.
            AppLog.Error(LogModule, exception, "A self set-up account could not be created, so nothing was written.");

            return AccountSetupResult.Failed(
                "The account could not be created right now, so nothing was written. Try again, or ask IT.");
        }
    }

    private static string ReadString(IReadOnlyDictionary<string, object?> row, string key)
        => row.TryGetValue(key, out var value) && value is not null
            ? Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)?.Trim() ?? string.Empty
            : string.Empty;
}
