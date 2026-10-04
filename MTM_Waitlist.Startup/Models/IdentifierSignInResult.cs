namespace MTM_Waitlist.Module_Startup.Models;

/// <summary>
/// What the identifier stage resolved to, which is what decides where the sign-in goes next
/// (`contracts/identifier-signin-contract.md` §3; FR-003 to FR-006, FR-012, FR-013).
/// </summary>
public enum IdentifierSignInOutcome
{
    /// <summary>The store holds the person, so the credential stage asks for their PIN.</summary>
    ResolvedInStore,

    /// <summary>The store holds nothing but the plant system knows the number, so set-up is offered.</summary>
    KnownToPlantSystemOnly,

    /// <summary>Neither source holds the identifier, so the person is refused and the form stays open.</summary>
    Unknown,

    /// <summary>The store holds the employee number but the account is not active, so no set-up is offered.</summary>
    NotActive,
}

/// <summary>
/// What the first sign-in stage produced after the identifier was resolved
/// (`contracts/identifier-signin-contract.md` §§2-3; FR-002 to FR-013).
/// </summary>
/// <remarks>
/// <para>
/// <b>One answer, one route.</b> The sign-in surface reads <see cref="Outcome"/> and does exactly one of four
/// things: move to the credential stage, open the set-up panel, stay open with a plain refusal, or stay open with
/// "that employee is not active". Nothing here decides any of that for the surface.
/// </para>
/// <para>
/// <b>The sign-in name is the store's, not the typed value.</b> An employee number resolves to the account's own
/// user name, because the credential check is keyed on the sign-in name; handing it the four digits would ask the
/// store a question it cannot answer.
/// </para>
/// </remarks>
public sealed record IdentifierSignInResult
{
    /// <summary>Where the identifier resolved to.</summary>
    public required IdentifierSignInOutcome Outcome { get; init; }

    /// <summary>What the identifier was classified as (FR-002).</summary>
    public required IdentifierKind Kind { get; init; }

    /// <summary>The identifier exactly as it was resolved, trimmed of surrounding whitespace.</summary>
    public string Identifier { get; init; } = string.Empty;

    /// <summary>The account's sign-in name, present for <see cref="IdentifierSignInOutcome.ResolvedInStore"/>.</summary>
    public string? SignInName { get; init; }

    /// <summary>The four digits, present when the identifier was an employee number.</summary>
    public string? EmployeeNumber { get; init; }

    /// <summary>The first name the plant system answered, present for a plant-system-only person.</summary>
    public string? FirstName { get; init; }

    /// <summary>The last name the plant system answered, present for a plant-system-only person.</summary>
    public string? LastName { get; init; }

    /// <summary>A plain-language refusal for <see cref="IdentifierSignInOutcome.Unknown"/> and <see cref="IdentifierSignInOutcome.NotActive"/>.</summary>
    public string? Reason { get; init; }

    /// <summary>The store holds the person; the credential stage follows (FR-005).</summary>
    /// <param name="kind">What the identifier was.</param>
    /// <param name="identifier">The identifier as resolved.</param>
    /// <param name="signInName">The account's own sign-in name.</param>
    /// <param name="employeeNumber">The four digits, or <c>null</c> for a user-name identifier.</param>
    public static IdentifierSignInResult ResolvedInStore(
        IdentifierKind kind,
        string identifier,
        string signInName,
        string? employeeNumber)
        => new()
        {
            Outcome = IdentifierSignInOutcome.ResolvedInStore,
            Kind = kind,
            Identifier = identifier,
            SignInName = signInName,
            EmployeeNumber = employeeNumber,
        };

    /// <summary>The plant system knows the number and the store does not; set-up follows (FR-006, FR-007).</summary>
    /// <param name="identifier">The employee number as typed.</param>
    /// <param name="firstName">The first name the plant system answered.</param>
    /// <param name="lastName">The last name the plant system answered.</param>
    public static IdentifierSignInResult KnownToPlantSystemOnly(
        string identifier,
        string firstName,
        string lastName)
        => new()
        {
            Outcome = IdentifierSignInOutcome.KnownToPlantSystemOnly,
            Kind = IdentifierKind.EmployeeNumber,
            Identifier = identifier,
            EmployeeNumber = identifier,
            FirstName = firstName,
            LastName = lastName,
        };

    /// <summary>Neither source holds the identifier; the person is refused and the form stays open (FR-012).</summary>
    /// <param name="kind">What the identifier was.</param>
    /// <param name="identifier">The identifier as typed.</param>
    /// <param name="reason">The plain-language refusal.</param>
    public static IdentifierSignInResult Unknown(IdentifierKind kind, string identifier, string reason)
        => new()
        {
            Outcome = IdentifierSignInOutcome.Unknown,
            Kind = kind,
            Identifier = identifier,
            Reason = reason,
        };

    /// <summary>The store holds the number but the account is not active; no set-up is offered (clarification).</summary>
    /// <param name="identifier">The employee number as typed.</param>
    /// <param name="reason">The plain-language refusal.</param>
    public static IdentifierSignInResult NotActive(string identifier, string reason)
        => new()
        {
            Outcome = IdentifierSignInOutcome.NotActive,
            Kind = IdentifierKind.EmployeeNumber,
            Identifier = identifier,
            EmployeeNumber = identifier,
            Reason = reason,
        };
}
