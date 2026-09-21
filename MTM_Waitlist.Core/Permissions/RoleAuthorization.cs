namespace MTM_Waitlist.Module_Core.Permissions;

using MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// The ladder readers: the rank comparison, the roles at or above a rung, its mirror, and the highest rung a
/// person holds when more than one live role is held (FR-016, FR-017).
/// </summary>
/// <remarks>
/// <para>
/// Every comparison here reads the ranks off the entries it was handed, and the entries come from the one
/// catalogue read. Nothing in this type holds a rank of its own, so the rank comparison and the roles-at-or-above
/// list cannot disagree: there is one set of numbers and one place they are compared.
/// </para>
/// <para>
/// A role the catalogue does not hold is not a rung anybody stands on, so neither the comparison nor the list
/// invents a value for it. A caller that has no entry for a person's role has no ranking question to ask.
/// </para>
/// </remarks>
public static class RoleAuthorization
{
    /// <summary>
    /// Whether <paramref name="held"/> stands at or above <paramref name="required"/>'s rung.
    /// </summary>
    public static bool IsAtLeast(RoleCatalogEntry held, RoleCatalogEntry required)
    {
        ArgumentNullException.ThrowIfNull(held);
        ArgumentNullException.ThrowIfNull(required);

        return held.RoleRank >= required.RoleRank;
    }

    /// <summary>
    /// Whether <paramref name="held"/> stands strictly above <paramref name="other"/>'s rung. Peers are not above
    /// each other, which is what lets two people at one rung edit each other.
    /// </summary>
    public static bool IsAbove(RoleCatalogEntry held, RoleCatalogEntry other)
    {
        ArgumentNullException.ThrowIfNull(held);
        ArgumentNullException.ThrowIfNull(other);

        return held.RoleRank > other.RoleRank;
    }

    /// <summary>
    /// The roles at or above <paramref name="rung"/>, in catalogue order.
    /// </summary>
    public static IReadOnlyList<RoleCatalogEntry> AtOrAbove(
        IEnumerable<RoleCatalogEntry> catalogue,
        RoleCatalogEntry rung)
    {
        ArgumentNullException.ThrowIfNull(catalogue);
        ArgumentNullException.ThrowIfNull(rung);

        return Order(catalogue.Where(entry => entry.RoleRank >= rung.RoleRank));
    }

    /// <summary>
    /// The roles at or below <paramref name="rung"/>, in catalogue order. This is the mirror of
    /// <see cref="AtOrAbove"/>: a picker offering the roles one person may assign asks this question, and asking
    /// it here rather than filtering in a screen keeps the comparison in one place.
    /// </summary>
    public static IReadOnlyList<RoleCatalogEntry> AtOrBelow(
        IEnumerable<RoleCatalogEntry> catalogue,
        RoleCatalogEntry rung)
    {
        ArgumentNullException.ThrowIfNull(catalogue);
        ArgumentNullException.ThrowIfNull(rung);

        return Order(catalogue.Where(entry => entry.RoleRank <= rung.RoleRank));
    }

    /// <summary>
    /// The highest rung among <paramref name="heldRoles"/>, or <c>null</c> when the person holds none.
    /// </summary>
    /// <remarks>
    /// Stored assignments still permit more than one role even though nothing in this feature writes a second one,
    /// so a reader takes the highest live role the person holds rather than the first row the store happened to
    /// return (FR-017).
    /// </remarks>
    public static RoleCatalogEntry? Highest(IEnumerable<RoleCatalogEntry> heldRoles)
    {
        ArgumentNullException.ThrowIfNull(heldRoles);

        return Order(heldRoles).FirstOrDefault();
    }

    /// <summary>
    /// Rung descending, then code ascending, which is the order the catalogue read returns and the order every
    /// caller sees, so a list and a single pick agree.
    /// </summary>
    private static IReadOnlyList<RoleCatalogEntry> Order(IEnumerable<RoleCatalogEntry> roles) =>
        roles
            .OrderByDescending(entry => entry.RoleRank)
            .ThenBy(entry => entry.RoleCode, StringComparer.Ordinal)
            .ToArray();
}
