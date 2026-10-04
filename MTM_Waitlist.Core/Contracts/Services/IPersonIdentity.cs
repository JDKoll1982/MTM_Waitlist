namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// Who is signed in, for reading only (FR-022).
/// </summary>
/// <remarks>
/// <para>
/// <b>Read-only, with one writer.</b> There is no setter on this contract and no member that changes anything.
/// The launch pipeline is the only writer, and it writes through <c>PersonIdentityService</c>, which is the type
/// this contract is implemented by; a consumer that needs a different person asks the owning service rather than
/// assigning here. That is what makes the write path one named place, and it is the property the retired
/// launch-state object lacked: a single shared mutable object is what allowed attribution to drift (D14, D15).
/// </para>
/// <para>
/// <b>Roles come from the store only.</b> A role is never inferred from the workstation, a build configuration
/// or a file; the <c>appsettings.json</c> developer allow-list is retired (D16). The role codes a person holds
/// arrive from the store at sign-in and are answered here.
/// </para>
/// <para>
/// <b>What this contract deliberately does not carry.</b> No session state (session validity is judged against
/// the store's clock and lives in the session service, FR-010), no remember-me state (it is per person and per
/// machine and survives sign-out), no credentials, tokens or key material in any form, and no
/// temporary-credential attempt count (that is a store-side fact).
/// </para>
/// </remarks>
public interface IPersonIdentity
{
    /// <summary>
    /// The signed-in person's <c>core_users_profiles.id</c>. Zero means no account has been resolved yet, which
    /// is what <see cref="IsSignedIn"/> reports.
    /// </summary>
    long UserId { get; }

    /// <summary>
    /// The person's <c>core_users_profiles.username_normalized</c>, unique and upper-normalised (FR-002).
    /// </summary>
    string SignInName { get; }

    /// <summary>
    /// The person's <c>core_users_profiles.display_name</c>: the name the shell badge and the tooltips show,
    /// and the name an issued credential is attributed to.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// The person's <c>core_users_profiles.employee_identifier</c>, or <c>null</c> when the store holds none.
    /// The request flow matches account records on it (FR-046).
    /// </summary>
    string? EmployeeNumber { get; }

    /// <summary>
    /// The role in force for this session, from <c>auth_roles_assignments</c> joined to
    /// <c>auth_roles_catalog</c>. This is the role's identity, so every access decision and every rank
    /// comparison reads this and never a display name (FR-054).
    /// </summary>
    string CurrentRoleCode { get; }

    /// <summary>
    /// Every role code the person holds, not just the one in force. <see cref="Holds"/> is answered from this
    /// set, so a person who holds two roles is admitted by both rather than only by the highest one.
    /// </summary>
    IReadOnlyList<string> HeldRoleCodes { get; }

    /// <summary>
    /// False before sign-in completes and false again once the person signs out (FR-011).
    /// </summary>
    bool IsSignedIn { get; }

    /// <summary>
    /// Whether the signed-in person holds <paramref name="roleCode"/>.
    /// </summary>
    /// <remarks>
    /// The code is matched against <see cref="CurrentRoleCode"/> and against <see cref="HeldRoleCodes"/>, case
    /// insensitively, and a <c>role:</c> prefix is accepted and ignored so that the scope-key spelling
    /// (<c>role:developer</c>) and the catalogue spelling (<c>developer</c>) both answer the same. A blank code
    /// is held by nobody. This method is the gate for machine setup (FR-007), the plant-wide locations list
    /// (FR-024) and the session-length setting (FR-029).
    /// </remarks>
    bool Holds(string roleCode);
}
