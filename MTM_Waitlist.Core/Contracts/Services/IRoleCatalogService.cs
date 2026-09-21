namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// One role the catalogue holds, with the rung it sits at (FR-010).
/// </summary>
/// <remarks>
/// This is the shape the one catalogue read returns and the shape every ladder comparison consumes, so a rung
/// only ever reaches a comparison from a row rather than from a second copy written into code.
/// </remarks>
public sealed record RoleCatalogEntry(long RoleId, string RoleCode, string RoleName, int RoleRank);

/// <summary>
/// Serves the role catalogue: the picker, the ladder and the rank comparison all read it here, and there is no
/// second source of ranks in the application (FR-010, FR-016).
/// </summary>
/// <remarks>
/// <para>
/// The read is <c>sp_auth_roles_list</c>, which excludes the retired <c>admin</c> code. The answer is cached for
/// the session, because a catalogue changes when a role ships rather than while a person works, and the cache is
/// invalidated explicitly for the case where it does change underneath a running session.
/// </para>
/// <para>
/// A store that cannot be read returns an empty catalogue rather than throwing, so a screen offers no role rather
/// than failing to open. Nothing is invented in its place: an empty list is the honest answer when the catalogue
/// cannot be read, and the caller's own unavailable state is what tells the reader so.
/// </para>
/// </remarks>
public interface IRoleCatalogService
{
    /// <summary>
    /// Every role the catalogue holds, ordered by rung descending then code ascending.
    /// </summary>
    Task<IReadOnlyList<RoleCatalogEntry>> GetRolesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Drops the cached catalogue, so the next read goes to the store again.
    /// </summary>
    void Invalidate();
}
