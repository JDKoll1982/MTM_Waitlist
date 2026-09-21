using MTM_Waitlist.Module_Core.Permissions;

namespace MTM_Waitlist.Mock.Service.Api;

/// <summary>
/// The one permission that admits a caller to the service's network API, and the header that names the caller.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why a role and not a credential (T147).</b> The service used to gate every endpoint behind a shared
/// credential it generated for itself on first start and then never displayed — so no operator could record
/// it, no client could be given it, and the service's own status surface was unobservable. The owner
/// decision (2026-09-12) replaced it: an API caller is identified by the <b>application user</b> it runs as,
/// and is authorized against the application's own store (<c>mtm_waitlist.core_users_profiles</c> +
/// <c>auth_roles_assignments</c> + <c>auth_roles_catalog</c>).
/// </para>
/// <para>
/// <b>Why a permission and not a role list (T038).</b> This type used to carry <c>Approved</c> — five role
/// display names, a copy of who may operate kept inside the service host. A copy drifts: a person the
/// Settings screen admitted could be refused here, or the other way round, with no single place to look. The
/// operator answer is now <see cref="RequiredPermissionKey"/>, resolved for the caller from the application's
/// own store through the same declaration every other gate reads, so who may call the API is stored data and
/// never a second list (FR-054, FR-057, decision 7).
/// </para>
/// <para>
/// <b>What this does and does not prove.</b> The caller asserts its user name and the service verifies the
/// asserted name really holds the permission, so an invented user name or a user with an ordinary
/// shop-floor role is refused. It does <i>not</i> cryptographically prove the caller is that user — the
/// transport is plain HTTP on the plant network. The owner accepted that trade when choosing the role model
/// over a shared secret; do not describe this as strong authentication.
/// </para>
/// <para>
/// <b>Fails closed.</b> The permission's shipped fallback is a refusal, so a store that cannot be reached
/// refuses the caller rather than admitting one (FR-050).
/// </para>
/// </remarks>
public static class ServiceOperatorRoles
{
    /// <summary>Header carrying the user name the caller runs as.</summary>
    public const string UserNameHeaderName = "X-MTM-Mock-User";

    /// <summary>
    /// The permission that decides whether a caller may use the API. Read from the declaration rather than
    /// restated, so the key the service asks for and the key a baseline is written under cannot drift.
    /// </summary>
    public static string RequiredPermissionKey => PermissionKeys.CacheRefreshApi;

    /// <summary>
    /// Whether a caller whose stored permission rows are <paramref name="storedAnswers"/> may use the API, with
    /// the declaration's shipped fallback answering a key the store does not hold (FR-050).
    /// </summary>
    /// <param name="storedAnswers">The answers the store holds for the caller, keyed by permission key.</param>
    public static bool IsPermitted(IReadOnlyDictionary<string, bool> storedAnswers) =>
        StoredPermissionAnswers.AnswerFor(RequiredPermissionKey, storedAnswers);

    /// <summary>
    /// What the service's own surfaces show in place of the retired role list: the permission that decides
    /// access. The service reports the permission rather than a role list because the roles live in stored data
    /// this host does not own, so a list here could only be a stale copy.
    /// </summary>
    public static string DisplayText => RequiredPermissionKey;
}
