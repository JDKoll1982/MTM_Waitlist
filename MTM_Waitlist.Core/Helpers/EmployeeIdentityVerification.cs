using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Core.Helpers;

/// <summary>
/// The rule that decides whether the signed-in person may raise a request, given the record the employee lookup
/// returned for their own employee identifier (FR-046).
/// </summary>
/// <remarks>
/// <para>
/// It moved here, into Core, with <see cref="EmployeeVerificationResult"/>: both the work-order wizard and the
/// generic request flow judge the same record, and neither library may reference the other
/// (024-dependency-modernization, MOD-007). It has one owner, so the two flows cannot drift apart on who may raise.
/// </para>
/// <para>
/// The rule decides from the <b>stored identifier</b> and never from a name: a row is only an answer for the number
/// it was asked about, and the name it carries is only what the request will show as "Requested by". A person no
/// active record accounts for is refused — the lookup is what makes the rule permissive, so this is not the rule
/// being loosened. There is no literal employee here either: the accounts are the whole list of people the
/// application knows, and they are read from the store rather than written into code.
/// </para>
/// </remarks>
public static class EmployeeIdentityVerification
{
    /// <summary>
    /// Whether <paramref name="employeeNumber"/> may raise a request, judged by the account record
    /// <paramref name="storedEmployee"/>.
    /// </summary>
    /// <param name="employeeNumber">The employee identifier the signed-in session carries.</param>
    /// <param name="storedEmployee">
    /// The account the lookup returned for that identifier, or <c>null</c> when no account carries it.
    /// </param>
    public static EmployeeVerificationResult Verify(string? employeeNumber, EmployeeIdentity? storedEmployee)
    {
        var normalized = (employeeNumber ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return new EmployeeVerificationResult
            {
                IsValid = false,
                IsActive = false,
                EmployeeNumber = string.Empty,
                EmployeeName = string.Empty,
                Message = "Employee number is required.",
            };
        }

        // The record has to be the one this number was asked about. A row that carries a different identifier is
        // not an answer for this person, and a name that happens to match is never the evidence (FR-046).
        if (storedEmployee is null
            || !string.Equals(storedEmployee.EmployeeNumber?.Trim(), normalized, StringComparison.OrdinalIgnoreCase))
        {
            return new EmployeeVerificationResult
            {
                IsValid = false,
                IsActive = false,
                EmployeeNumber = normalized,
                EmployeeName = string.Empty,
                Message = "No active employee was found for that number.",
            };
        }

        if (!storedEmployee.IsActive)
        {
            return new EmployeeVerificationResult
            {
                IsValid = false,
                IsActive = false,
                EmployeeNumber = normalized,
                EmployeeName = storedEmployee.DisplayName?.Trim() ?? string.Empty,
                Message = "This employee is not active and cannot create a request.",
            };
        }

        // An account with no stored display name still raises a request: the request itself requires a name, so
        // the identifier the account is known by stands in rather than leaving the request unattributable.
        var storedName = storedEmployee.DisplayName?.Trim() ?? string.Empty;

        return new EmployeeVerificationResult
        {
            IsValid = true,
            IsActive = true,
            EmployeeNumber = normalized,
            EmployeeName = string.IsNullOrWhiteSpace(storedName) ? normalized : storedName,
            Message = "Employee verified.",
        };
    }
}
