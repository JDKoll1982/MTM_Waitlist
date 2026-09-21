namespace MTM_Waitlist.Module_Core.Permissions;

/// <summary>
/// The badge shown beside the signed-in name, keyed on the role code (FR-105, FR-106, FR-107).
/// </summary>
/// <remarks>
/// <para>
/// The badge is presentation, not access. It is fixed rather than gated, it is not a permission, and it
/// deliberately does not appear in <see cref="PermissionRegistry"/> (FR-058). What changed here is the keying: the
/// old lookup matched display names, and the vocabulary it carried named roles the catalogue has never held.
/// This one holds exactly the roles the catalogue holds, one entry each, so no role reaches the grey default.
/// </para>
/// <para>
/// <b>Keyed on the code, not the name.</b> "Plant Manager" is a display name and matches nothing here;
/// "plant_manager" is the code and matches its entry. That distinction is what makes a later rename of a role's
/// displayed name a presentation change rather than a badge regression.
/// </para>
/// <para>
/// The glyphs are drawn from the set this application already renders. The glyph is a Segoe Fluent Icons
/// codepoint and a wrong one renders as a box rather than as an error, which is why every codepoint below is one
/// already in use elsewhere in this repository rather than a plausible-looking new one.
/// </para>
/// </remarks>
public static class RoleBadgeCatalog
{
    /// <summary>One role's badge: its glyph and its colour.</summary>
    public sealed record Entry(string RoleCode, string Glyph, string ColorHex);

    /// <summary>
    /// The grey default. It is reachable only by a genuinely unknown or blank role (FR-106), which
    /// <see cref="For"/> guarantees by returning it only when nothing matches.
    /// </summary>
    public static Entry Fallback { get; } = new(string.Empty, "\uE77B", "#FF5C5C5C");

    /// <summary>
    /// One entry per role the catalogue holds, so every role gets its own badge.
    /// </summary>
    public static IReadOnlyList<Entry> All { get; } = new[]
    {
        new Entry("developer", "\uE713", "#FF0078D4"),
        new Entry("it_department", "\uE7EF", "#FFC4314B"),
        new Entry("plant_manager", "\uE716", "#FFD67D00"),
        new Entry("production_lead", "\uE8FB", "#FF8764B8"),
        new Entry("setup_lead", "\uE930", "#FF0099BC"),
        new Entry("material_handler_lead", "\uE7C1", "#FFCA5010"),
        new Entry("material_handler", "\uE7B8", "#FF008272"),
        new Entry("production", "\uE8FD", "#FF498205"),
        new Entry("setup", "\uE8B7", "#FF7A7574"),
    };

    /// <summary>
    /// The badge for <paramref name="roleCode"/>, or the grey default when the role is unknown or blank.
    /// </summary>
    public static Entry For(string? roleCode) =>
        string.IsNullOrWhiteSpace(roleCode)
            ? Fallback
            : All.FirstOrDefault(entry => string.Equals(entry.RoleCode, roleCode.Trim(), StringComparison.OrdinalIgnoreCase))
                ?? Fallback;
}
