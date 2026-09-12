namespace MTM_Waitlist.Mock.Service.Api;

/// <summary>
/// The application roles that may use the service's network API, and the header that names the caller.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why a role and not a credential (T147).</b> The service used to gate every endpoint behind a shared
/// credential it generated for itself on first start and then never displayed — so no operator could record
/// it, no client could be given it, and the service's own status surface was unobservable. The owner
/// decision (2026-09-12) replaced it: an API caller is identified by the <b>application user</b> it runs as,
/// and is authorized by that user's <b>role</b>, resolved against the application's own store
/// (<c>mtm_waitlist.core_users_profiles</c> + <c>auth_roles_assignments</c> +
/// <c>auth_roles_catalog</c>).
/// </para>
/// <para>
/// <b>What this does and does not prove.</b> The caller asserts its user name and the service verifies the
/// asserted name really holds an approved role, so an invented user name or a user with an ordinary
/// shop-floor role is refused. It does <i>not</i> cryptographically prove the caller is that user — the
/// transport is plain HTTP on the plant network. The owner accepted that trade when choosing the role model
/// over a shared secret; do not describe this as strong authentication.
/// </para>
/// <para>
/// The approved set deliberately mirrors <c>DunnageWorkflowService.AllowedQuickAddRoles</c> — the same
/// display-name form, the same case-insensitive comparison — so the service does not invent a second role
/// vocabulary for the same plant.
/// </para>
/// </remarks>
public static class ServiceOperatorRoles
{
    /// <summary>Header carrying the user name the caller runs as.</summary>
    public const string UserNameHeaderName = "X-MTM-Mock-User";

    /// <summary>
    /// Role display names (<c>auth_roles_catalog.role_name</c>) permitted to call the API.
    /// </summary>
    public static IReadOnlyList<string> Approved { get; } =
    [
        "Admin",
        "Developer",
        "Plant Manager",
        "Setup Lead",
        "Production Lead",
    ];

    /// <summary>
    /// Whether a resolved role may use the API.
    /// </summary>
    /// <param name="roleName">The role resolved from the application's store, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the role is one of <see cref="Approved"/>.</returns>
    public static bool IsApproved(string? roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return false;
        }

        return Approved.Any(role => string.Equals(role, roleName.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>The approved roles as one readable list, for the settings and status surfaces.</summary>
    public static string DisplayText => string.Join(", ", Approved);
}
