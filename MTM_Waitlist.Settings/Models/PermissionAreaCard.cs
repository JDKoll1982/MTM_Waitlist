namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// One card of the permissions matrix: one area of the declaration, its permissions as the columns, and the
/// people as the rows.
/// </summary>
/// <remarks>
/// <para>
/// <b>One card per area, and the areas come from the declaration.</b> <c>PermissionRegistry.Area</c> names exactly
/// five, so the page can neither invent one nor leave one out, and a permission cannot exist in the code without
/// appearing in its card (FR-047).
/// </para>
/// <para>
/// <b>The card carries only its own permissions, never a subset chosen to make it fit.</b> A card too wide for the
/// window scrolls sideways rather than dropping a column, because a column that is not there is a fact the reader
/// will not know is missing (decision 28).
/// </para>
/// </remarks>
public sealed class PermissionAreaCard
{
    public PermissionAreaCard(string areaHeadingText, IReadOnlyList<PermissionColumn> columns, IReadOnlyList<PermissionCardRow> rows)
    {
        AreaHeadingText = areaHeadingText;
        Columns = columns;
        Rows = rows;
    }

    /// <summary>The area's name, in the reader's words, which sits at the left of the card's heading row.</summary>
    public string AreaHeadingText { get; }

    /// <summary>That area's permissions, in the declaration's order, which are the card's columns.</summary>
    public IReadOnlyList<PermissionColumn> Columns { get; }

    /// <summary>One entry per person, in the roster's order, each carrying only this card's cells.</summary>
    public IReadOnlyList<PermissionCardRow> Rows { get; }
}
