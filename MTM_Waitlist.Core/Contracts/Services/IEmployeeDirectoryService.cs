using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// Resolves a person from the application's own account records by the employee identifier the signed-in session
/// carries (FR-046). The directory is the authority on whether somebody exists; a caller never decides that from
/// a name it happens to hold.
/// </summary>
public interface IEmployeeDirectoryService
{
    /// <summary>
    /// The stored record for <paramref name="employeeIdentifier"/>, or <c>null</c> when no account carries that
    /// identifier.
    /// </summary>
    /// <remarks>
    /// A store that cannot be read <b>throws</b> rather than answering "no such employee": reporting an outage
    /// as a refusal the person did not earn is exactly the failure FR-026 rules out, and the caller reports the
    /// outage in plain language instead.
    /// </remarks>
    Task<EmployeeIdentity?> FindByEmployeeIdentifierAsync(string employeeIdentifier, CancellationToken cancellationToken = default);
}
