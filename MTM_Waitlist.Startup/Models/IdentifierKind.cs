namespace MTM_Waitlist.Module_Startup.Models;

/// <summary>
/// What a person's identifier is: a user name, or an employee number
/// (`contracts/identifier-signin-contract.md` §1; FR-002).
/// </summary>
/// <remarks>
/// <b>Exactly four digits is an employee number; anything else is a user name.</b> The rule is the value's whole
/// shape and nothing else is inferred from it: <c>123</c>, <c>12345</c>, <c>12 3</c> and <c>123a</c> are user
/// names. A blank identifier is refused before it reaches here.
/// </remarks>
public enum IdentifierKind
{
    /// <summary>The person typed something other than four digits, so it is a sign-in name.</summary>
    UserName,

    /// <summary>The person typed exactly four digits, so it is an employee number.</summary>
    EmployeeNumber,
}
