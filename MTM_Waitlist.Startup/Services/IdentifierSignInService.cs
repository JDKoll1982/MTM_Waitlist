using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Startup.Models;

namespace MTM_Waitlist.Module_Startup.Services;

/// <summary>
/// The identifier stage's work: classify what the person typed, resolve it store-first and answer with one of the
/// four outcomes (`contracts/identifier-signin-contract.md` §§1-3; FR-002 to FR-006, FR-012, FR-013).
/// </summary>
/// <remarks>
/// <para>
/// <b>Exactly four digits is an employee number; anything else is a user name.</b> The rule is the value's whole
/// shape: <c>123</c>, <c>12345</c>, <c>12 3</c> and <c>123a</c> are user names, and nothing is inferred from a
/// partly numeric value (FR-002).
/// </para>
/// <para>
/// <b>The store is asked first and the plant system second.</b> An employee number is resolved against the
/// store's profile records (<c>sp_core_employee_by_identifier_get</c>, which answers the active account first
/// when two people share a number); only a number the store does not hold is put to the plant system through the
/// sixth read shape. A user name is resolved by the store's own credential read and never goes to the plant
/// system at all (FR-003, FR-004).
/// </para>
/// <para>
/// <b>An inactive account ends resolution immediately.</b> The store answering "this number belongs to an
/// account that is not active" is a refusal in its own right, so the plant system is not consulted and no set-up
/// is offered — which is what makes a duplicate account impossible for a number the store already holds.
/// </para>
/// <para>
/// <b>This service reads; it never signs anybody in.</b> It answers what the identifier resolved to and the
/// credential stage that follows is unchanged: the attempt count, the session check, the machine gate, the
/// temporary-credential check and the new-PIN write all behave exactly as they do today (FR-005).
/// </para>
/// </remarks>
internal sealed class IdentifierSignInService
{
    /// <summary>The number of digits an employee number has, and no other length (FR-002).</summary>
    internal const int EmployeeNumberLength = 4;

    private readonly IEmployeeDirectoryService _directory;
    private readonly CredentialCheckService _credentialCheck;
    private readonly IPlantEmployeeReader _plant;

    /// <summary>Creates the identifier stage over the store directory, the credential check and the plant read.</summary>
    /// <param name="directory">The store's profile records, read by employee identifier.</param>
    /// <param name="credentialCheck">The store read that answers whether a sign-in name exists and is active.</param>
    /// <param name="plant">The plant system, read through the sixth shape's cached fallback.</param>
    public IdentifierSignInService(
        IEmployeeDirectoryService directory,
        CredentialCheckService credentialCheck,
        IPlantEmployeeReader plant)
    {
        _directory = directory ?? throw new ArgumentNullException(nameof(directory));
        _credentialCheck = credentialCheck ?? throw new ArgumentNullException(nameof(credentialCheck));
        _plant = plant ?? throw new ArgumentNullException(nameof(plant));
    }

    /// <summary>Whether what the person typed is blank, which is refused before anything is asked.</summary>
    /// <param name="identifier">What the person typed.</param>
    public static bool IsBlank(string? identifier) => string.IsNullOrWhiteSpace(identifier);

    /// <summary>
    /// Classifies an identifier: exactly four digits is an employee number, anything else is a user name
    /// (`contracts/identifier-signin-contract.md` §1; FR-002).
    /// </summary>
    /// <param name="identifier">What the person typed.</param>
    /// <returns>The kind, with a blank value classified as a user name so no null has to be carried.</returns>
    public static IdentifierKind Classify(string? identifier)
    {
        var trimmed = identifier?.Trim() ?? string.Empty;

        if (trimmed.Length != EmployeeNumberLength)
        {
            return IdentifierKind.UserName;
        }

        foreach (var character in trimmed)
        {
            if (character is < '0' or > '9')
            {
                return IdentifierKind.UserName;
            }
        }

        return IdentifierKind.EmployeeNumber;
    }

    /// <summary>
    /// Resolves what the person typed into one of the four outcomes.
    /// </summary>
    /// <param name="identifier">What the person typed, taken as typed and trimmed here.</param>
    /// <param name="cancellationToken">Cancels the store and plant reads.</param>
    /// <returns>Where the identifier resolved to, which is what the surface routes on.</returns>
    /// <exception cref="ArgumentException">The identifier is blank, which the surface refuses first (FR-015).</exception>
    public async Task<IdentifierSignInResult> ResolveAsync(string? identifier, CancellationToken cancellationToken = default)
    {
        var trimmed = identifier?.Trim() ?? string.Empty;
        var kind = Classify(trimmed);

        if (trimmed.Length == 0)
        {
            throw new ArgumentException(
                "A blank identifier is refused by the field's own hint rather than resolved (FR-015).",
                nameof(identifier));
        }

        return kind is IdentifierKind.UserName
            ? await ResolveUserNameAsync(trimmed, cancellationToken).ConfigureAwait(false)
            : await ResolveEmployeeNumberAsync(trimmed, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Resolves a user name against the store alone. A name the store does not hold is refused in plain words and
    /// the plant system is never asked, because a plant-system person is found by their employee number
    /// (FR-003, FR-012).
    /// </summary>
    private async Task<IdentifierSignInResult> ResolveUserNameAsync(string userName, CancellationToken cancellationToken)
    {
        var accountId = await _credentialCheck
            .FindAccountIdAsync(userName, cancellationToken)
            .ConfigureAwait(false);

        return accountId > 0
            ? IdentifierSignInResult.ResolvedInStore(IdentifierKind.UserName, userName, userName, employeeNumber: null)
            : IdentifierSignInResult.Unknown(
                IdentifierKind.UserName,
                userName,
                "The store holds no active account with that sign-in name, so that sign-in was not recognised. "
                + "Check the spelling, or ask IT if you are new here.");
    }

    /// <summary>
    /// Resolves an employee number store-first and consults the plant system only when the store holds nothing
    /// (FR-003, FR-004, FR-006, FR-013).
    /// </summary>
    private async Task<IdentifierSignInResult> ResolveEmployeeNumberAsync(string employeeNumber, CancellationToken cancellationToken)
    {
        var account = await _directory
            .FindByEmployeeIdentifierAsync(employeeNumber, cancellationToken)
            .ConfigureAwait(false);

        if (account is not null)
        {
            // The store holds the number. An inactive account ends resolution here: the plant system is not asked
            // and no set-up panel is offered, so no duplicate account can be created for a number it already has.
            if (!account.IsActive)
            {
                return IdentifierSignInResult.NotActive(
                    employeeNumber,
                    "That employee number belongs to an account that is not active, so it cannot sign in. "
                    + "Ask IT to reactivate it.");
            }

            var signInName = string.IsNullOrWhiteSpace(account.SignInName) ? employeeNumber : account.SignInName;

            return IdentifierSignInResult.ResolvedInStore(
                IdentifierKind.EmployeeNumber,
                employeeNumber,
                signInName,
                employeeNumber);
        }

        var lookup = await _plant
            .FindByNumberAsync(employeeNumber, cancellationToken)
            .ConfigureAwait(false);

        if (lookup.Employee is { } employee)
        {
            return IdentifierSignInResult.KnownToPlantSystemOnly(
                employeeNumber,
                employee.FirstName,
                employee.LastName);
        }

        return lookup.WasReadable
            ? IdentifierSignInResult.Unknown(
                IdentifierKind.EmployeeNumber,
                employeeNumber,
                "Neither the store nor the plant system holds that employee number. Check the number, or ask IT.")
            : IdentifierSignInResult.Unknown(
                IdentifierKind.EmployeeNumber,
                employeeNumber,
                "The plant system cannot be reached and this computer has never held that employee number, so set-up "
                + "cannot be completed right now. Try again once the plant system is answering.");
    }
}
